using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VibeMovies.Application.Catalog;
using VibeMovies.Application.Interfaces;
using VibeMovies.Domain.Entities;
using VibeMovies.Domain.Models;
using VibeMovies.Infrastructure.Persistence;

namespace VibeMovies.Infrastructure.External;

public class TmdbHttpClientService : ITmdbService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly VibeMoviesDbContext _db;
    private readonly ILogger<TmdbHttpClientService> _logger;

    public TmdbHttpClientService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        VibeMoviesDbContext db,
        ILogger<TmdbHttpClientService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _db = db;
        _logger = logger;
    }

    public async Task<DiscoverResultDto> DiscoverByVibeAsync(
        VibeDiscoverQuery query,
        string? runtimeApiKey = null,
        CancellationToken ct = default)
    {
        var selectedVibes = query.VibeIds
            .Select(VibeCatalog.GetById)
            .Where(v => v is not null)
            .Cast<VibeDefinition>()
            .ToList();

        var keywordIds = selectedVibes
            .SelectMany(v => v.TmdbKeywordIds)
            .Distinct()
            .ToList();

        var genreIds = query.GenreIds.Distinct().ToList();

        string? releaseGte = selectedVibes.FirstOrDefault(v => !string.IsNullOrEmpty(v.ReleaseDateGte))?.ReleaseDateGte;
        string? releaseLte = selectedVibes.FirstOrDefault(v => !string.IsNullOrEmpty(v.ReleaseDateLte))?.ReleaseDateLte;

        if (query.ReleaseDecade.HasValue && query.ReleaseDecade.Value >= 1950)
        {
            releaseGte = $"{query.ReleaseDecade.Value}-01-01";
            releaseLte = $"{query.ReleaseDecade.Value + 9}-12-31";
        }

        int? maxRuntime = query.MaxRuntimeMinutes
            ?? selectedVibes.FirstOrDefault(v => v.MaxRuntimeMinutes.HasValue)?.MaxRuntimeMinutes;

        double? minVoteAverage = query.MinRating
            ?? (selectedVibes.Any(v => v.MinVoteAverage.HasValue)
                ? selectedVibes.Where(v => v.MinVoteAverage.HasValue).Min(v => v.MinVoteAverage)
                : null);

        var tmdbParams = new Dictionary<string, string>
        {
            ["language"] = "pt-BR",
            ["watch_region"] = string.IsNullOrWhiteSpace(query.Region) ? "BR" : query.Region.ToUpperInvariant(),
            ["sort_by"] = string.IsNullOrWhiteSpace(query.SortBy) ? "popularity.desc" : query.SortBy,
            ["include_adult"] = "false",
            ["vote_count.gte"] = "150",
            ["page"] = Math.Max(1, query.Page).ToString()
        };

        if (genreIds.Count > 0)
            tmdbParams["with_genres"] = string.Join(",", genreIds);

        if (keywordIds.Count > 0)
            tmdbParams["with_keywords"] = string.Join("|", keywordIds); // OR operator across Vibe keywords

        if (!string.IsNullOrEmpty(releaseGte))
            tmdbParams["primary_release_date.gte"] = releaseGte;

        if (!string.IsNullOrEmpty(releaseLte))
            tmdbParams["primary_release_date.lte"] = releaseLte;

        if (maxRuntime.HasValue && maxRuntime.Value > 0)
            tmdbParams["with_runtime.lte"] = maxRuntime.Value.ToString();

        if (minVoteAverage.HasValue && minVoteAverage.Value > 0)
            tmdbParams["vote_average.gte"] = minVoteAverage.Value.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);

        var formulaParts = new List<string>();
        if (selectedVibes.Count > 0)
            formulaParts.Add($"Vibes [{string.Join(" + ", selectedVibes.Select(v => v.Name))}] -> Keywords({string.Join("|", keywordIds)})");
        if (genreIds.Count > 0)
            formulaParts.Add($"Gêneros [{string.Join(", ", genreIds.Select(VibeCatalog.GetGenreName))}]");
        if (maxRuntime.HasValue)
            formulaParts.Add($"Duração <= {maxRuntime.Value} min");
        if (!string.IsNullOrEmpty(releaseGte))
            formulaParts.Add($"Período [{releaseGte} a {releaseLte ?? "hoje"}]");
        if (!string.IsNullOrWhiteSpace(query.WatchProviderFilter))
            formulaParts.Add($"Streaming [{query.WatchProviderFilter}]");

        var formulaSummary = formulaParts.Count > 0
            ? string.Join(" • ", formulaParts)
            : "Descoberta Geral por Popularidade e Relevância";

        var apiKey = ResolveApiKey(runtimeApiKey);
        if (!string.IsNullOrWhiteSpace(apiKey) && string.IsNullOrWhiteSpace(query.SearchText))
        {
            var liveResult = await TryDiscoverFromTmdbApiAsync(tmdbParams, selectedVibes, query, apiKey, formulaSummary, ct);
            if (liveResult is not null && liveResult.Movies.Count > 0)
            {
                return liveResult;
            }
        }
        else if (!string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(query.SearchText))
        {
            var searched = await SearchMoviesAsync(query.SearchText, query.Region, apiKey, ct);
            if (searched.Count > 0)
            {
                return new DiscoverResultDto
                {
                    Page = 1,
                    TotalPages = 1,
                    TotalResults = searched.Count,
                    UsingLiveTmdbApi = true,
                    AppliedFormulaSummary = $"Busca Textual TMDB: \"{query.SearchText}\" • {formulaSummary}",
                    AppliedTmdbQueryParameters = tmdbParams,
                    Movies = FilterMovieListLocally(searched, query, selectedVibes, maxRuntime, releaseGte, releaseLte, minVoteAverage)
                };
            }
        }

        var filteredCurated = FilterMovieListLocally(
            CuratedMovieDataset.Movies,
            query,
            selectedVibes,
            maxRuntime,
            releaseGte,
            releaseLte,
            minVoteAverage);

        return new DiscoverResultDto
        {
            Page = 1,
            TotalPages = 1,
            TotalResults = filteredCurated.Count,
            UsingLiveTmdbApi = false,
            AppliedFormulaSummary = formulaSummary,
            AppliedTmdbQueryParameters = tmdbParams,
            Movies = filteredCurated
        };
    }

    public async Task<List<MovieCardDto>> SearchMoviesAsync(
        string queryText,
        string region = "BR",
        string? runtimeApiKey = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(queryText))
            return CuratedMovieDataset.Movies;

        var apiKey = ResolveApiKey(runtimeApiKey);
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            try
            {
                var client = CreateAuthorizedClient(apiKey);
                var encoded = Uri.EscapeDataString(queryText.Trim());
                var url = $"search/movie?query={encoded}&language=pt-BR&region={region}&include_adult=false";
                using var response = await client.GetAsync(url, ct);
                if (response.IsSuccessStatusCode)
                {
                    using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
                    var movies = await ParseTmdbResultsArrayAsync(doc.RootElement, region, apiKey, new List<VibeDefinition>(), ct);
                    if (movies.Count > 0) return movies;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao consultar /search/movie na API do TMDB. Usando catálogo local.");
            }
        }

        var q = queryText.Trim().ToLowerInvariant();
        return CuratedMovieDataset.Movies
            .Where(m =>
                m.Title.ToLowerInvariant().Contains(q) ||
                m.OriginalTitle.ToLowerInvariant().Contains(q) ||
                m.Overview.ToLowerInvariant().Contains(q) ||
                (m.Director?.ToLowerInvariant().Contains(q) ?? false) ||
                m.Keywords.Any(k => k.ToLowerInvariant().Contains(q)))
            .ToList();
    }

    public async Task<MovieCardDto?> GetMovieDetailsAsync(
        int tmdbId,
        string region = "BR",
        string? runtimeApiKey = null,
        CancellationToken ct = default)
    {
        var curated = CuratedMovieDataset.Movies.FirstOrDefault(m => m.TmdbId == tmdbId);
        var apiKey = ResolveApiKey(runtimeApiKey);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return curated;
        }

        try
        {
            var client = CreateAuthorizedClient(apiKey);
            using var response = await client.GetAsync($"movie/{tmdbId}?language=pt-BR&append_to_response=credits,keywords,watch/providers", ct);
            if (!response.IsSuccessStatusCode) return curated;

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            var root = doc.RootElement;
            var card = MapSingleTmdbMovie(root, new List<VibeDefinition>());
            card.WatchProviders = await GetWatchProvidersAsync(tmdbId, region, apiKey, ct);
            return card;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao obter detalhes do filme {TmdbId} no TMDB.", tmdbId);
            return curated;
        }
    }

    public async Task<WatchProvidersDto> GetWatchProvidersAsync(
        int tmdbId,
        string region = "BR",
        string? runtimeApiKey = null,
        CancellationToken ct = default)
    {
        var normalizedRegion = string.IsNullOrWhiteSpace(region) ? "BR" : region.Trim().ToUpperInvariant();

        // Verifica cache no banco PostgreSQL / SQLite
        var cached = await _db.CachedMovies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.TmdbMovieId == tmdbId && c.Region == normalizedRegion && c.ExpiresAt > DateTime.UtcNow, ct);

        if (cached is not null && !string.IsNullOrWhiteSpace(cached.ProvidersJson))
        {
            var deserialized = JsonSerializer.Deserialize<WatchProvidersDto>(cached.ProvidersJson);
            if (deserialized is not null) return deserialized;
        }

        var apiKey = ResolveApiKey(runtimeApiKey);
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            try
            {
                var client = CreateAuthorizedClient(apiKey);
                using var response = await client.GetAsync($"movie/{tmdbId}/watch/providers", ct);
                if (response.IsSuccessStatusCode)
                {
                    using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
                    if (doc.RootElement.TryGetProperty("results", out var results) &&
                        results.TryGetProperty(normalizedRegion, out var regionNode))
                    {
                        var dto = new WatchProvidersDto
                        {
                            Region = normalizedRegion,
                            TmdbWatchLink = regionNode.TryGetProperty("link", out var linkEl) ? linkEl.GetString() : $"https://www.themoviedb.org/movie/{tmdbId}/watch?locale={normalizedRegion}",
                            Flatrate = ParseProvidersArray(regionNode, "flatrate"),
                            Rent = ParseProvidersArray(regionNode, "rent"),
                            Buy = ParseProvidersArray(regionNode, "buy")
                        };

                        await SaveProviderCacheAsync(tmdbId, normalizedRegion, dto, ct);
                        return dto;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao buscar Watch Providers para {TmdbId}.", tmdbId);
            }
        }

        var fallbackMovie = CuratedMovieDataset.Movies.FirstOrDefault(m => m.TmdbId == tmdbId);
        return fallbackMovie?.WatchProviders ?? new WatchProvidersDto
        {
            Region = normalizedRegion,
            TmdbWatchLink = $"https://www.themoviedb.org/movie/{tmdbId}/watch?locale={normalizedRegion}",
            Flatrate = new() { CuratedMovieDataset.PrimeVideo },
            Rent = new() { CuratedMovieDataset.AppleTv, CuratedMovieDataset.GooglePlay },
            Buy = new() { CuratedMovieDataset.AppleTv }
        };
    }

    public async Task<List<MovieCardDto>> GetNowPlayingMoviesAsync(
        string region = "BR",
        string? runtimeApiKey = null,
        CancellationToken ct = default)
    {
        var apiKey = ResolveApiKey(runtimeApiKey);
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            try
            {
                var client = CreateAuthorizedClient(apiKey);
                using var response = await client.GetAsync($"movie/now_playing?language=pt-BR&region={region}&page=1", ct);
                if (response.IsSuccessStatusCode)
                {
                    using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
                    var liveMovies = await ParseTmdbResultsArrayAsync(doc.RootElement, region, apiKey, new List<VibeDefinition>(), ct);
                    if (liveMovies.Count > 0) return liveMovies.Take(8).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao consultar /movie/now_playing no TMDB. Utilizando lista em cartaz local.");
            }
        }

        // Filmes em destaque para o Radar de Cinemas Locais
        return CuratedMovieDataset.Movies
            .OrderByDescending(m => m.ReleaseYear)
            .ThenByDescending(m => m.VoteAverage)
            .Take(6)
            .ToList();
    }

    private async Task<DiscoverResultDto?> TryDiscoverFromTmdbApiAsync(
        Dictionary<string, string> tmdbParams,
        List<VibeDefinition> selectedVibes,
        VibeDiscoverQuery query,
        string apiKey,
        string formulaSummary,
        CancellationToken ct)
    {
        try
        {
            var client = CreateAuthorizedClient(apiKey);
            var queryString = string.Join("&", tmdbParams.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
            using var response = await client.GetAsync($"discover/movie?{queryString}", ct);
            if (!response.IsSuccessStatusCode) return null;

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            var root = doc.RootElement;
            var movies = await ParseTmdbResultsArrayAsync(root, query.Region, apiKey, selectedVibes, ct);

            if (!string.IsNullOrWhiteSpace(query.WatchProviderFilter))
            {
                movies = movies
                    .Where(m => m.WatchProviders.Flatrate.Any(p => p.ProviderName.Contains(query.WatchProviderFilter, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            return new DiscoverResultDto
            {
                Page = root.TryGetProperty("page", out var pEl) ? pEl.GetInt32() : 1,
                TotalPages = root.TryGetProperty("total_pages", out var tpEl) ? tpEl.GetInt32() : 1,
                TotalResults = root.TryGetProperty("total_results", out var trEl) ? trEl.GetInt32() : movies.Count,
                UsingLiveTmdbApi = true,
                AppliedFormulaSummary = formulaSummary,
                AppliedTmdbQueryParameters = tmdbParams,
                Movies = movies
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao consultar /discover/movie na API do TMDB.");
            return null;
        }
    }

    private async Task<List<MovieCardDto>> ParseTmdbResultsArrayAsync(
        JsonElement root,
        string region,
        string apiKey,
        List<VibeDefinition> selectedVibes,
        CancellationToken ct)
    {
        var list = new List<MovieCardDto>();
        if (!root.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
            return list;

        foreach (var item in results.EnumerateArray().Take(12))
        {
            var card = MapSingleTmdbMovie(item, selectedVibes);
            card.WatchProviders = await GetWatchProvidersAsync(card.TmdbId, region, apiKey, ct);
            list.Add(card);
        }

        return list;
    }

    private static MovieCardDto MapSingleTmdbMovie(JsonElement item, List<VibeDefinition> selectedVibes)
    {
        var id = item.TryGetProperty("id", out var idEl) ? idEl.GetInt32() : 0;
        var title = item.TryGetProperty("title", out var tEl) ? tEl.GetString() ?? "" : "";
        var originalTitle = item.TryGetProperty("original_title", out var otEl) ? otEl.GetString() ?? title : title;
        var overview = item.TryGetProperty("overview", out var ovEl) ? ovEl.GetString() ?? "" : "";
        var posterPath = item.TryGetProperty("poster_path", out var ppEl) && ppEl.ValueKind == JsonValueKind.String
            ? $"https://image.tmdb.org/t/p/w500{ppEl.GetString()}"
            : "https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?w=500&auto=format&fit=crop&q=80";
        var backdropPath = item.TryGetProperty("backdrop_path", out var bpEl) && bpEl.ValueKind == JsonValueKind.String
            ? $"https://image.tmdb.org/t/p/w1280{bpEl.GetString()}"
            : posterPath;
        var releaseDate = item.TryGetProperty("release_date", out var rdEl) ? rdEl.GetString() ?? "" : "";
        int? releaseYear = releaseDate.Length >= 4 && int.TryParse(releaseDate[..4], out var yr) ? yr : null;

        var genreIds = new List<int>();
        if (item.TryGetProperty("genre_ids", out var gArr) && gArr.ValueKind == JsonValueKind.Array)
        {
            foreach (var g in gArr.EnumerateArray())
            {
                if (g.TryGetInt32(out var gid)) genreIds.Add(gid);
            }
        }
        else if (item.TryGetProperty("genres", out var gObjArr) && gObjArr.ValueKind == JsonValueKind.Array)
        {
            foreach (var g in gObjArr.EnumerateArray())
            {
                if (g.TryGetProperty("id", out var gidEl) && gidEl.TryGetInt32(out var gid)) genreIds.Add(gid);
            }
        }

        int? runtime = item.TryGetProperty("runtime", out var rtEl) && rtEl.ValueKind == JsonValueKind.Number
            ? rtEl.GetInt32()
            : null;

        return new MovieCardDto
        {
            TmdbId = id,
            Title = title,
            OriginalTitle = originalTitle,
            Overview = string.IsNullOrWhiteSpace(overview) ? "Sinopse disponível no catálogo TMDB." : overview,
            PosterUrl = posterPath,
            BackdropUrl = backdropPath,
            ReleaseDate = releaseDate,
            ReleaseYear = releaseYear,
            RuntimeMinutes = runtime ?? 118,
            VoteAverage = item.TryGetProperty("vote_average", out var vaEl) ? Math.Round(vaEl.GetDouble(), 1) : 7.5,
            VoteCount = item.TryGetProperty("vote_count", out var vcEl) ? vcEl.GetInt32() : 500,
            GenreIds = genreIds,
            Genres = genreIds.Select(VibeCatalog.GetGenreName).Distinct().ToList(),
            MatchedVibes = selectedVibes.Select(v => v.Id).ToList(),
            VibeMatchScore = 95
        };
    }

    private static List<StreamingProviderBadgeDto> ParseProvidersArray(JsonElement regionNode, string key)
    {
        var list = new List<StreamingProviderBadgeDto>();
        if (!regionNode.TryGetProperty(key, out var arr) || arr.ValueKind != JsonValueKind.Array)
            return list;

        foreach (var p in arr.EnumerateArray())
        {
            var logoPath = p.TryGetProperty("logo_path", out var lEl) ? lEl.GetString() : null;
            list.Add(new StreamingProviderBadgeDto
            {
                ProviderId = p.TryGetProperty("provider_id", out var idEl) ? idEl.GetInt32() : 0,
                ProviderName = p.TryGetProperty("provider_name", out var nEl) ? nEl.GetString() ?? "" : "",
                LogoUrl = !string.IsNullOrEmpty(logoPath) ? $"https://image.tmdb.org/t/p/original{logoPath}" : "",
                DisplayPriority = p.TryGetProperty("display_priority", out var dpEl) ? dpEl.GetInt32() : 99
            });
        }

        return list.OrderBy(x => x.DisplayPriority).ToList();
    }

    private async Task SaveProviderCacheAsync(int tmdbId, string region, WatchProvidersDto dto, CancellationToken ct)
    {
        try
        {
            var existing = await _db.CachedMovies
                .FirstOrDefaultAsync(c => c.TmdbMovieId == tmdbId && c.Region == region, ct);

            var json = JsonSerializer.Serialize(dto);
            if (existing is not null)
            {
                existing.ProvidersJson = json;
                existing.CachedAt = DateTime.UtcNow;
                existing.ExpiresAt = DateTime.UtcNow.AddHours(12);
            }
            else
            {
                _db.CachedMovies.Add(new CachedMovie
                {
                    TmdbMovieId = tmdbId,
                    Region = region,
                    ProvidersJson = json,
                    CachedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddHours(12)
                });
            }

            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Não foi possível gravar cache de provedores para {TmdbId}.", tmdbId);
        }
    }

    private static List<MovieCardDto> FilterMovieListLocally(
        List<MovieCardDto> source,
        VibeDiscoverQuery query,
        List<VibeDefinition> selectedVibes,
        int? maxRuntime,
        string? releaseGte,
        string? releaseLte,
        double? minVoteAverage)
    {
        IEnumerable<MovieCardDto> filtered = source;

        if (!string.IsNullOrWhiteSpace(query.SearchText))
        {
            var q = query.SearchText.Trim().ToLowerInvariant();
            filtered = filtered.Where(m =>
                m.Title.ToLowerInvariant().Contains(q) ||
                m.OriginalTitle.ToLowerInvariant().Contains(q) ||
                m.Overview.ToLowerInvariant().Contains(q) ||
                m.Keywords.Any(k => k.ToLowerInvariant().Contains(q)) ||
                (m.Director?.ToLowerInvariant().Contains(q) ?? false));
        }

        if (selectedVibes.Count > 0)
        {
            var selectedVibeIds = selectedVibes.Select(v => v.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
            filtered = filtered.Where(m => m.MatchedVibes.Any(mv => selectedVibeIds.Contains(mv)));
        }

        if (query.GenreIds.Count > 0)
        {
            var genreSet = query.GenreIds.ToHashSet();
            filtered = filtered.Where(m => m.GenreIds.Any(g => genreSet.Contains(g)));
        }

        if (maxRuntime.HasValue && maxRuntime.Value > 0)
        {
            filtered = filtered.Where(m => !m.RuntimeMinutes.HasValue || m.RuntimeMinutes.Value <= maxRuntime.Value);
        }

        if (!string.IsNullOrEmpty(releaseGte) && int.TryParse(releaseGte[..4], out var minYear))
        {
            filtered = filtered.Where(m => !m.ReleaseYear.HasValue || m.ReleaseYear.Value >= minYear);
        }

        if (!string.IsNullOrEmpty(releaseLte) && int.TryParse(releaseLte[..4], out var maxYear))
        {
            filtered = filtered.Where(m => !m.ReleaseYear.HasValue || m.ReleaseYear.Value <= maxYear);
        }

        if (minVoteAverage.HasValue && minVoteAverage.Value > 0)
        {
            filtered = filtered.Where(m => m.VoteAverage >= minVoteAverage.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.WatchProviderFilter))
        {
            filtered = filtered.Where(m =>
                m.WatchProviders.Flatrate.Any(p => p.ProviderName.Contains(query.WatchProviderFilter, StringComparison.OrdinalIgnoreCase)));
        }

        var resultList = filtered
            .Select(m =>
            {
                var matchCount = selectedVibes.Count == 0
                    ? 1
                    : m.MatchedVibes.Count(mv => selectedVibes.Any(sv => sv.Id == mv));
                var score = Math.Min(99, 86 + (matchCount * 5) + (int)Math.Round(m.VoteAverage - 7.0));
                m.VibeMatchScore = Math.Max(80, score);
                return m;
            })
            .OrderByDescending(m => m.VibeMatchScore)
            .ThenByDescending(m => m.VoteAverage)
            .ToList();

        return resultList;
    }

    private string? ResolveApiKey(string? runtimeApiKey)
    {
        if (!string.IsNullOrWhiteSpace(runtimeApiKey))
            return runtimeApiKey.Trim();

        var configKey = _configuration["Tmdb:ApiKey"]
                        ?? _configuration["Tmdb:ReadAccessToken"]
                        ?? Environment.GetEnvironmentVariable("TMDB__ApiKey")
                        ?? Environment.GetEnvironmentVariable("TMDB_API_KEY");

        return string.IsNullOrWhiteSpace(configKey) ? null : configKey.Trim();
    }

    private HttpClient CreateAuthorizedClient(string apiKey)
    {
        var client = _httpClientFactory.CreateClient("TmdbClient");
        client.BaseAddress = new Uri("https://api.themoviedb.org/3/");

        // Se for um JWT Read Access Token (v4), usa Bearer; se for chave v3 (32 hex chars), adiciona api_key ou Bearer
        if (apiKey.StartsWith("eyJ", StringComparison.Ordinal))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }
        else
        {
            // Para chaves v3 de 32 caracteres, registramos um handler ou passamos via DefaultRequestHeaders se suportado,
            // ou anexam api_key na requisição via BaseAddress/Handler.
            client.DefaultRequestHeaders.Remove("X-Api-Key-V3");
            client.DefaultRequestHeaders.Add("X-Api-Key-V3", apiKey);
        }

        return client;
    }
}
