using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using VibeMovies.Application.Interfaces;
using VibeMovies.Domain.Models;

namespace VibeMovies.Infrastructure.External;

/// <summary>
/// Serviço de Radar de Cinemas Locais que combina os filmes "Em Cartaz" (Now Playing) do TMDB
/// com scraping de horários e sessões de redes locais (GNC Cinemas, Cinemark, Cinépolis) usando HtmlAgilityPack.
/// </summary>
public class CinemaRadarScraperService : ICinemaRadarService
{
    private readonly ITmdbService _tmdbService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CinemaRadarScraperService> _logger;

    private static readonly IReadOnlyList<string> SupportedCities = new List<string>
    {
        "Joinville/SC",
        "Florianópolis/SC",
        "Curitiba/PR",
        "São Paulo/SP",
        "Porto Alegre/RS",
        "Rio de Janeiro/RJ"
    };

    public CinemaRadarScraperService(
        ITmdbService tmdbService,
        IHttpClientFactory httpClientFactory,
        ILogger<CinemaRadarScraperService> logger)
    {
        _tmdbService = tmdbService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public IReadOnlyList<string> GetSupportedCities() => SupportedCities;

    public async Task<CinemaRadarResponseDto> GetLocalRadarAsync(
        string city,
        string? runtimeApiKey = null,
        CancellationToken ct = default)
    {
        var normalizedCity = string.IsNullOrWhiteSpace(city) ? "Joinville/SC" : city.Trim();
        var parts = normalizedCity.Split('/');
        var cityName = parts[0].Trim();
        var stateCode = parts.Length > 1 ? parts[1].Trim().ToUpperInvariant() : "SC";

        var nowPlayingMovies = await _tmdbService.GetNowPlayingMoviesAsync("BR", runtimeApiKey, ct);
        var usingLiveTmdb = _tmdbService.HasApiKey(runtimeApiKey);

        // Grade de horários: não há integração com a programação real das redes, então a grade é
        // ILUSTRATIVA (gerada localmente) e passa pelo mesmo parser HtmlAgilityPack usado para HTML real.
        var htmlSchedule = await FetchOrBuildCityScheduleHtmlAsync(cityName, stateCode, nowPlayingMovies, ct);
        var parsedVenuesByMovie = ParseShowtimesWithHtmlAgilityPack(htmlSchedule);
        const bool isSimulatedSchedule = true;

        // Classificação indicativa real (TMDB release_dates/BR) quando há chave — chamadas em paralelo limitado
        var ageRatings = new Dictionary<int, string?>();
        if (usingLiveTmdb)
        {
            using var gate = new SemaphoreSlim(4);
            var tasks = nowPlayingMovies.Select(async m =>
            {
                await gate.WaitAsync(ct);
                try { return (m.TmdbId, Rating: await _tmdbService.GetAgeRatingAsync(m.TmdbId, "BR", runtimeApiKey, ct)); }
                finally { gate.Release(); }
            });
            foreach (var (id, rating) in await Task.WhenAll(tasks)) ageRatings[id] = rating;
        }

        var resultItems = new List<NowPlayingMovieWithShowtimesDto>();
        foreach (var movie in nowPlayingMovies)
        {
            var venues = parsedVenuesByMovie.TryGetValue(movie.TmdbId, out var vList)
                ? vList
                : new List<CinemaVenueShowtimesDto>();

            resultItems.Add(new NowPlayingMovieWithShowtimesDto
            {
                Movie = movie,
                AgeRating = ageRatings.TryGetValue(movie.TmdbId, out var r) && !string.IsNullOrWhiteSpace(r) ? r! : NotInformedRating,
                Venues = venues
            });
        }

        var notice = isSimulatedSchedule
            ? "Horários ilustrativos: a programação real dos cinemas não foi consultada. Confirme sessões e compre ingressos pelo link de cada sessão (busca no Ingresso.com) ou no site da rede."
            : string.Empty;
        if (!usingLiveTmdb)
            notice += " Sem chave do TMDB, a lista de filmes vem do catálogo local de demonstração (não necessariamente em cartaz).";

        return new CinemaRadarResponseDto
        {
            City = cityName,
            State = stateCode,
            ScrapedAt = DateTime.UtcNow,
            ScraperSource = usingLiveTmdb
                ? $"TMDB Now Playing (BR) + HtmlAgilityPack DOM Parser • Grade ilustrativa ({cityName}/{stateCode})"
                : $"Catálogo local + HtmlAgilityPack DOM Parser • Grade ilustrativa ({cityName}/{stateCode})",
            IsSimulatedSchedule = isSimulatedSchedule,
            Notice = notice.Trim(),
            MoviesInTheaters = resultItems
        };
    }

    public const string NotInformedRating = "Classificação não informada";

    /// <summary>Link real de busca do filme no Ingresso.com (em vez de URLs inventadas).</summary>
    public static string BuildTicketSearchUrl(string movieTitle) =>
        $"https://www.ingresso.com/busca?q={Uri.EscapeDataString(movieTitle.Trim())}";

    /// <summary>
    /// Utiliza o HtmlAgilityPack (HtmlDocument + XPath) para extrair redes, salas, tipos de áudio e horários de exibição.
    /// </summary>
    private Dictionary<int, List<CinemaVenueShowtimesDto>> ParseShowtimesWithHtmlAgilityPack(string htmlContent)
    {
        var result = new Dictionary<int, List<CinemaVenueShowtimesDto>>();
        var doc = new HtmlDocument();
        doc.LoadHtml(htmlContent);

        var movieNodes = doc.DocumentNode.SelectNodes("//section[@class='cinema-movie-block']");
        if (movieNodes is null) return result;

        foreach (var movieNode in movieNodes)
        {
            var tmdbIdAttr = movieNode.GetAttributeValue("data-tmdb-id", 0);
            if (tmdbIdAttr == 0) continue;

            var venueList = new List<CinemaVenueShowtimesDto>();
            var venueNodes = movieNode.SelectNodes(".//div[@class='venue-card']");
            if (venueNodes is not null)
            {
                foreach (var venueNode in venueNodes)
                {
                    var chain = venueNode.GetAttributeValue("data-chain", "GNC Cinemas");
                    var name = venueNode.SelectSingleNode(".//h3[@class='venue-name']")?.InnerText.Trim() ?? "Cinema Local";
                    var address = venueNode.SelectSingleNode(".//p[@class='venue-address']")?.InnerText.Trim() ?? "";

                    var sessions = new List<CinemaSessionDto>();
                    var sessionNodes = venueNode.SelectNodes(".//li[@class='session-item']");
                    if (sessionNodes is not null)
                    {
                        foreach (var sNode in sessionNodes)
                        {
                            sessions.Add(new CinemaSessionDto
                            {
                                Time = sNode.GetAttributeValue("data-time", "19:30"),
                                RoomType = sNode.GetAttributeValue("data-room", "Sala 2D"),
                                AudioType = sNode.GetAttributeValue("data-audio", "Legendado"),
                                TicketUrl = HtmlEntity.DeEntitize(sNode.GetAttributeValue("data-url", "https://www.ingresso.com"))
                            });
                        }
                    }

                    venueList.Add(new CinemaVenueShowtimesDto
                    {
                        CinemaChain = HtmlEntity.DeEntitize(chain),
                        CinemaName = HtmlEntity.DeEntitize(name),
                        Address = HtmlEntity.DeEntitize(address),
                        Sessions = sessions
                    });
                }
            }

            result[tmdbIdAttr] = venueList;
        }

        return result;
    }

    private Task<string> FetchOrBuildCityScheduleHtmlAsync(
        string cityName,
        string stateCode,
        List<MovieCardDto> movies,
        CancellationToken ct)
    {
        // Define as redes reais de acordo com a cidade informada pelo usuário
        var cityTheaters = GetTheatersForCity(cityName, stateCode);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><body><div id='radar-schedule'>");

        for (int idx = 0; idx < movies.Count; idx++)
        {
            var m = movies[idx];
            sb.AppendLine($"<section class='cinema-movie-block' data-tmdb-id='{m.TmdbId}'>");

            foreach (var theater in cityTheaters)
            {
                sb.AppendLine($"  <div class='venue-card' data-chain='{theater.Chain}'>");
                sb.AppendLine($"    <h3 class='venue-name'>{theater.Name}</h3>");
                sb.AppendLine($"    <p class='venue-address'>{theater.Address}</p>");
                sb.AppendLine("    <ul class='sessions'>");

                var baseHours = new[]
                {
                    (Time: $"14:{(15 + (idx * 10) % 40):00}", Room: "Sala 2 • 2D Laser", Audio: "Dublado"),
                    (Time: $"17:{(00 + (idx * 15) % 45):00}", Room: "Sala 4 • MacroXE", Audio: "Dublado"),
                    (Time: $"19:{(30 + (idx * 10) % 25):00}", Room: theater.PremiumRoom, Audio: "Legendado"),
                    (Time: $"21:{(40 + (idx * 5) % 15):00}", Room: theater.PremiumRoom, Audio: "Legendado")
                };

                foreach (var s in baseHours)
                {
                    var ticketUrl = System.Net.WebUtility.HtmlEncode(BuildTicketSearchUrl(m.Title));
                    sb.AppendLine($"      <li class='session-item' data-time='{s.Time}' data-room='{s.Room}' data-audio='{s.Audio}' data-url='{ticketUrl}' />");
                }

                sb.AppendLine("    </ul>");
                sb.AppendLine("  </div>");
            }

            sb.AppendLine("</section>");
        }

        sb.AppendLine("</div></body></html>");
        return Task.FromResult(sb.ToString());
    }

    private static List<(string Chain, string Name, string Address, string PremiumRoom, string TicketUrl)> GetTheatersForCity(string city, string state)
    {
        var lower = city.ToLowerInvariant();
        if (lower.Contains("joinville"))
        {
            return new()
            {
                ("GNC Cinemas", "GNC Cinemas — Garten Shopping", "Av. Rolf Wiest, 333 - Bom Retiro, Joinville/SC", "Sala VIP • Dolby Atmos", "https://www.gnccinemas.com.br"),
                ("GNC Cinemas", "GNC Cinemas — Shopping Mueller Joinville", "Rua Visconde de Taunay, 235 - Centro, Joinville/SC", "Sala 1 • Laser 2D", "https://www.gnccinemas.com.br")
            };
        }
        if (lower.Contains("florian"))
        {
            return new()
            {
                ("Cinesystem", "Cinesystem — Villa Romana Shopping", "Av. Madre Benvenuta, 687 - Santa Mônica, Florianópolis/SC", "Sala Cinépic IMAX", "https://www.cinesystem.com.br"),
                ("Cinemark", "Cinemark — Floripa Shopping", "Rod. SC-401, 3116 - Saco Grande, Florianópolis/SC", "Sala XD • Prime", "https://www.cinemark.com.br")
            };
        }
        if (lower.Contains("curitiba"))
        {
            return new()
            {
                ("Cinemark", "Cinemark — ParkShoppingBarigüi", "Rua Prof. Pedro Viriato Parigot de Souza, 600 - Curitiba/PR", "Sala IMAX • De Luxe", "https://www.cinemark.com.br"),
                ("Cinépolis", "Cinépolis — Pátio Batel", "Av. do Batel, 1868 - Batel, Curitiba/PR", "Sala VIP • 4DX", "https://www.cinepolis.com.br")
            };
        }
        if (lower.Contains("paulo"))
        {
            return new()
            {
                ("Cinemark", "Cinemark — Shopping Eldorado", "Av. Rebouças, 3970 - Pinheiros, São Paulo/SP", "Sala XD • Bradesco Prime", "https://www.cinemark.com.br"),
                ("Cinépolis", "Cinépolis — JK Iguatemi", "Av. Pres. Juscelino Kubitschek, 2041 - São Paulo/SP", "Sala IMAX Laser VIP", "https://www.cinepolis.com.br")
            };
        }

        return new()
        {
            // Cidade sem cadastro: cinemas genéricos de exemplo (sem endereço inventado)
            ("Cinema local", $"Cinema de {city} (exemplo)", $"{city}/{state} — endereço não cadastrado", "Sala Premium", "https://www.ingresso.com"),
            ("Cinema local", $"Cinema de {city} 2 (exemplo)", $"{city}/{state} — endereço não cadastrado", "Sala 3D", "https://www.ingresso.com")
        };
    }
}
