using System.Globalization;
using VibeMovies.Application.Catalog;
using VibeMovies.Application.Interfaces;
using VibeMovies.Domain.Entities;
using VibeMovies.Domain.Models;

namespace VibeMovies.Application.Services;

/// <summary>Compõe as respostas da integração com o assistente Jarvis (/api/jarvis).</summary>
public class JarvisService
{
    private readonly ITmdbService _tmdb;
    private readonly ICinemaRadarService _radar;
    private readonly IWatchlistRepository _watchlist;
    private readonly IUserRepository _users;
    private readonly SurprisePickService _surprise;
    private readonly TimeProvider _time;

    public JarvisService(
        ITmdbService tmdb,
        ICinemaRadarService radar,
        IWatchlistRepository watchlist,
        IUserRepository users,
        SurprisePickService surprise,
        TimeProvider? time = null)
    {
        _tmdb = tmdb;
        _radar = radar;
        _watchlist = watchlist;
        _users = users;
        _surprise = surprise;
        _time = time ?? TimeProvider.System;
    }

    public async Task<ApplicationUser?> ResolveUserAsync(string? email, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(email))
        {
            var byEmail = await _users.GetByEmailAsync(email.Trim().ToLowerInvariant(), ct);
            if (byEmail is not null) return byEmail;
        }

        return await _users.GetFirstAsync(ct);
    }

    public static IReadOnlyList<JarvisVibeDto> GetVibes() =>
        VibeCatalog.AllVibes.Select(v => new JarvisVibeDto { Id = v.Id, Name = v.Name, Emoji = v.Emoji }).ToList();

    public async Task<JarvisSummaryDto> GetSummaryAsync(ApplicationUser user, string? city, CancellationToken ct = default)
    {
        const string region = "BR";
        var items = await _watchlist.GetByUserIdAsync(user.Id, null, ct);
        var myProviders = ProviderCatalog.ParseList(user.MyProvidersCsv);
        var wantItems = items.Where(i => i.ListType == WatchlistType.WantToWatch).ToList();

        var providersById = wantItems.Count > 0
            ? await _tmdb.GetWatchProvidersBatchAsync(wantItems.Select(i => i.TmdbMovieId), region, null, ct)
            : new Dictionary<int, WatchProvidersDto>();

        var summary = new JarvisSummaryDto
        {
            GeneratedAt = _time.GetUtcNow().UtcDateTime,
            UsingLiveTmdb = _tmdb.HasApiKey(null),
            User = new JarvisUserDto { Name = user.Name, DefaultCity = user.DefaultCity },
            Stats = WatchlistStatsCalculator.Compute(items, _time),
            WantToWatch = wantItems
                .OrderByDescending(i => i.AddedAt)
                .Take(6)
                .Select(i => new JarvisWantToWatchDto
                {
                    TmdbId = i.TmdbMovieId,
                    Title = i.Title,
                    PosterUrl = i.PosterPath,
                    ReleaseYear = i.ReleaseYear,
                    RuntimeMinutes = i.RuntimeMinutes,
                    Providers = providersById.TryGetValue(i.TmdbMovieId, out var p)
                        ? p.Flatrate.Select(x => x.ProviderName).Distinct().ToList()
                        : new List<string>()
                })
                .ToList()
        };

        await FillTonightPickAsync(summary, items, wantItems, providersById, myProviders, region, ct);
        await FillNowPlayingAsync(summary, string.IsNullOrWhiteSpace(city) ? user.DefaultCity : city!, ct);
        return summary;
    }

    private async Task FillTonightPickAsync(
        JarvisSummaryDto summary,
        List<WatchlistItem> items,
        List<WatchlistItem> wantItems,
        Dictionary<int, WatchProvidersDto> providersById,
        List<string> myProviders,
        string region,
        CancellationToken ct)
    {
        WatchProvidersDto ProvidersOf(WatchlistItem i) =>
            providersById.TryGetValue(i.TmdbMovieId, out var p) ? p : new WatchProvidersDto { Region = region };

        var onMyProviders = wantItems.Where(i => ProviderCatalog.IsAvailableOn(ProvidersOf(i), myProviders)).ToList();
        var chosenItem = onMyProviders.OrderByDescending(i => i.TmdbVoteAverage).FirstOrDefault()
                         ?? wantItems.OrderByDescending(i => i.TmdbVoteAverage).FirstOrDefault();

        if (chosenItem is not null)
        {
            var card = await _tmdb.GetMovieDetailsAsync(chosenItem.TmdbMovieId, region, null, ct) ?? FromItem(chosenItem);
            card.WatchProviders = ProvidersOf(chosenItem);
            var availableOnMine = onMyProviders.Contains(chosenItem);

            var prefix = availableOnMine
                ? "Está no seu Quero Ver e disponível em um streaming que você assina"
                : "Está no seu Quero Ver";
            summary.TonightPick = card;
            summary.TonightReason = MovieTextBuilder.BuildReason(card, Array.Empty<VibeDefinition>(), Array.Empty<int>(), null, myProviders, prefix);
            summary.TonightSpokenText = MovieTextBuilder.BuildSpokenText(card, Array.Empty<VibeDefinition>(), myProviders, "Pra hoje à noite, que tal")
                .TrimEnd('.') + ". Ele está no seu Quero Ver.";
            return;
        }

        var profile = RecommendationService.BuildTasteProfile(items);
        var topGenre = profile.GenreWeights.OrderByDescending(kv => kv.Value).Select(kv => (int?)kv.Key).FirstOrDefault();
        var pick = await _surprise.PickAsync(new SurpriseRequest
        {
            GenreIds = topGenre.HasValue ? new List<int> { topGenre.Value } : new List<int>(),
            ExcludeIds = items.Select(i => i.TmdbMovieId).ToHashSet(),
            PreferredProviders = myProviders,
            Region = region
        }, null, ct);

        if (pick is null)
        {
            summary.TonightReason = "Não encontrei uma sugestão para hoje.";
            summary.TonightSpokenText = "Não encontrei uma sugestão de filme para hoje.";
            return;
        }

        summary.TonightPick = pick.Movie;
        summary.TonightReason = topGenre.HasValue
            ? $"Seu Quero Ver está vazio, então sorteei pelo seu gênero favorito ({VibeCatalog.GetGenreName(topGenre.Value)}) • {pick.Reason}"
            : $"Seu Quero Ver está vazio, então sorteei um filme popular • {pick.Reason}";
        summary.TonightSpokenText = pick.SpokenText;
    }

    private async Task FillNowPlayingAsync(JarvisSummaryDto summary, string city, CancellationToken ct)
    {
        var radar = await _radar.GetLocalRadarAsync(city, null, ct);
        summary.IsSimulatedSchedule = radar.IsSimulatedSchedule;
        var nowTime = TimeOnly.FromDateTime(_time.GetLocalNow().DateTime);

        summary.NowPlaying = radar.MoviesInTheaters
            .Take(6)
            .Select(entry => new JarvisNowPlayingDto
            {
                TmdbId = entry.Movie.TmdbId,
                Title = entry.Movie.Title,
                PosterUrl = entry.Movie.PosterUrl,
                VoteAverage = entry.Movie.VoteAverage,
                AgeRating = entry.AgeRating,
                NextSessions = entry.Venues
                    .SelectMany(v => v.Sessions.Select(s => (Venue: v, Session: s,
                        Time: TimeOnly.TryParseExact(s.Time, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var t) ? t : (TimeOnly?)null)))
                    .Where(x => x.Time.HasValue && x.Time.Value > nowTime)
                    .OrderBy(x => x.Time)
                    .Take(3)
                    .Select(x => new JarvisSessionDto
                    {
                        Cinema = x.Venue.CinemaName,
                        Time = x.Session.Time,
                        Room = x.Session.RoomType,
                        Audio = x.Session.AudioType
                    })
                    .ToList()
            })
            .ToList();
    }

    public async Task<SurprisePickDto?> SurpriseAsync(ApplicationUser? user, string? vibeWord, int? maxRuntime, CancellationToken ct = default)
    {
        var match = VibeWordMapper.Map(vibeWord);
        var request = new SurpriseRequest
        {
            VibeIds = match.VibeId is null ? new List<string>() : new List<string> { match.VibeId },
            GenreIds = match.GenreId is null ? new List<int>() : new List<int> { match.GenreId.Value },
            MaxRuntimeMinutes = maxRuntime is > 0 ? maxRuntime : null,
            Region = "BR"
        };

        if (user is not null)
        {
            var watched = await _watchlist.GetByUserIdAsync(user.Id, WatchlistType.Watched, ct);
            request.ExcludeIds = watched.Select(w => w.TmdbMovieId).ToHashSet();
            request.PreferredProviders = ProviderCatalog.ParseList(user.MyProvidersCsv);
        }

        var pick = await _surprise.PickAsync(request, null, ct);
        if (pick is not null && !string.IsNullOrWhiteSpace(vibeWord) && match.IsEmpty)
            pick.Reason = $"Não reconheci \"{vibeWord.Trim()}\" como vibe ou gênero, então sorteei sem esse filtro • {pick.Reason}";
        return pick;
    }

    /// <summary>Adiciona ao Diário do usuário. Retorna null se o filme não for encontrado.</summary>
    public async Task<WatchlistItem?> AddToWatchlistAsync(ApplicationUser user, JarvisAddToWatchlistRequestDto dto, CancellationToken ct = default)
    {
        var listNormalized = TextNormalizer.Normalize(dto.List);
        WatchlistType listType = listNormalized switch
        {
            "want" or "quero ver" or "wanttowatch" => WatchlistType.WantToWatch,
            "watched" or "ja vi" or "visto" => WatchlistType.Watched,
            _ => throw new ArgumentException("O campo 'list' deve ser \"want\" ou \"watched\".")
        };

        if (dto.Rating.HasValue && (dto.Rating < 1 || dto.Rating > 5))
            throw new ArgumentException("A nota deve ser de 1 a 5 estrelas.");

        var movie = await _tmdb.GetMovieDetailsAsync(dto.TmdbId, user.PreferredRegion, null, ct);
        if (movie is null) return null;

        var item = new WatchlistItem
        {
            UserId = user.Id,
            TmdbMovieId = movie.TmdbId,
            Title = movie.Title,
            OriginalTitle = string.IsNullOrWhiteSpace(movie.OriginalTitle) ? movie.Title : movie.OriginalTitle,
            PosterPath = movie.PosterUrl,
            BackdropPath = movie.BackdropUrl,
            ReleaseYear = movie.ReleaseYear,
            Overview = movie.Overview,
            ListType = listType,
            PersonalRating = listType == WatchlistType.Watched ? dto.Rating : null,
            WatchedAt = listType == WatchlistType.Watched ? _time.GetUtcNow().UtcDateTime : null,
            GenresCsv = string.Join(", ", movie.Genres),
            RuntimeMinutes = movie.RuntimeMinutes,
            TmdbVoteAverage = movie.VoteAverage
        };

        return await _watchlist.UpsertAsync(item, ct);
    }

    private static MovieCardDto FromItem(WatchlistItem i) => new()
    {
        TmdbId = i.TmdbMovieId,
        Title = i.Title,
        OriginalTitle = i.OriginalTitle,
        Overview = i.Overview,
        PosterUrl = i.PosterPath,
        BackdropUrl = i.BackdropPath,
        ReleaseYear = i.ReleaseYear,
        RuntimeMinutes = i.RuntimeMinutes,
        VoteAverage = i.TmdbVoteAverage,
        Genres = WatchlistStatsCalculator.SplitGenres(i.GenresCsv),
        GenreIds = WatchlistStatsCalculator.GenreIdsFromCsv(i.GenresCsv),
        VibeMatchScore = 0
    };
}
