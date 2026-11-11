using System.Globalization;
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
    private const int MaxParallelProviderRequests = 4;
    private const int ResultsPerPage = 12;
    private static readonly TimeSpan ProviderCacheTtl = TimeSpan.FromHours(12);

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

    public bool HasApiKey(string? runtimeApiKey = null) => !string.IsNullOrWhiteSpace(ResolveApiKey(runtimeApiKey));

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
        var providerNames = ProviderCatalog.ParseList(query.WatchProviderFilter);
        var providerIds = ProviderCatalog.GetTmdbIds(providerNames);

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

        var region = NormalizeRegion(query.Region);
        var tmdbParams = new Dictionary<string, string>
        {
            ["language"] = "pt-BR",
            ["watch_region"] = region,
            ["sort_by"] = string.IsNullOrWhiteSpace(query.SortBy) ? "popularity.desc" : query.SortBy,
            ["include_adult"] = "false",
            ["vote_count.gte"] = "150",
            ["page"] = Math.Max(1, query.Page).ToString(CultureInfo.InvariantCulture)
        };

        if (genreIds.Count > 0)
            tmdbParams["with_genres"] = string.Join(",", genreIds);

        if (keywordIds.Count > 0)
            tmdbParams["with_keywords"] = string.Join("|", keywordIds); // OR entre as keywords das vibes

        if (providerIds.Count > 0)
        {
            // Filtra NA ORIGEM (antes da paginação do TMDB) — "|" = OR entre provedores
            tmdbParams["with_watch_providers"] = string.Join("|", providerIds);
            tmdbParams["watch_monetization_types"] = "flatrate";
        }

        if (!string.IsNullOrEmpty(releaseGte))
            tmdbParams["primary_release_date.gte"] = releaseGte;

        if (!string.IsNullOrEmpty(releaseLte))
            tmdbParams["primary_release_date.lte"] = releaseLte;

        if (maxRuntime.HasValue && maxRuntime.Value > 0)
            tmdbParams["with_runtime.lte"] = maxRuntime.Value.ToString(CultureInfo.InvariantCulture);

        if (minVoteAverage.HasValue && minVoteAverage.Value > 0)
            tmdbParams["vote_average.gte"] = minVoteAverage.Value.ToString("0.0", CultureInfo.InvariantCulture);

        var formulaParts = new List<string>();
        if (selectedVibes.Count > 0)
            formulaParts.Add($"Vibes [{string.Join(" + ", selectedVibes.Select(v => v.Name))}] -> Keywords({string.Join("|", keywordIds)})");
        if (genreIds.Count > 0)
            formulaParts.Add($"Gêneros [{string.Join(", ", genreIds.Select(VibeCatalog.GetGenreName))}]");
        if (maxRuntime.HasValue)
            formulaParts.Add($"Duração <= {maxRuntime.Value} min");
        if (!string.IsNullOrEmpty(releaseGte))
            formulaParts.Add($"Período [{releaseGte} a {releaseLte ?? "hoje"}]");
        if (providerNames.Count > 0)
            formulaParts.Add($"Streaming [{string.Join(" ou ", providerNames.Select(ProviderCatalog.Canonicalize))}]");

        var formulaSummary = formulaParts.Count > 0
            ? string.Join(" • ", formulaParts)
            : "Descoberta Geral por Popularidade e Relevância";

        var apiKey = ResolveApiKey(runtimeApiKey);
        if (!string.IsNullOrWhiteSpace(apiKey) && string.IsNullOrWhiteSpace(query.SearchText))
        {
            var liveResult = await TryDiscoverFromTmdbApiAsync(tmdbParams, selectedVibes, keywordIds.Count > 0, providerNames, region, apiKey, formulaSummary, ct);
            if (liveResult is not null && liveResult.Movies.Count > 0)
            {
                return liveResult;
            }
        }
        else if (!string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(query.SearchText))
        {
            var searched = await SearchMoviesAsync(query.SearchText, region, apiKey, ct);
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
                    Movies = FilterMovieListLocally(searched, query, selectedVibes, providerNames, maxRuntime, releaseGte, releaseLte, minVoteAverage)
                };
            }
        }

        var filteredCurated = FilterMovieListLocally(
            CuratedMovieDataset.Movies,
            query,
            selectedVibes,
            providerNames,
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
            return CuratedMovieDataset.CloneAll();

        region = NormalizeRegion(region);
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
                    var movies = await ParseTmdbResultsArrayAsync(doc.RootElement, region, apiKey, new List<VibeDefinition>(), false, ct);
                    if (movies.Count > 0) return movies;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Falha ao consultar /search/movie na API do TMDB. Usando catálogo local.");
            }
        }

        var q = TextNormalizer.Normalize(queryText);
        return CuratedMovieDataset.Movies
            .Where(m =>
                TextNormalizer.Normalize(m.Title).Contains(q) ||
                TextNormalizer.Normalize(m.OriginalTitle).Contains(q) ||
                TextNormalizer.Normalize(m.Overview).Contains(q) ||
                TextNormalizer.Normalize(m.Director).Contains(q) ||
                m.Keywords.Any(k => TextNormalizer.Normalize(k).Contains(q)))
            .Select(m => m.Clone())
            .ToList();
    }

    public async Task<MovieCardDto?> GetMovieDetailsAsync(
        int tmdbId,
        string region = "BR",
        string? runtimeApiKey = null,
        CancellationToken ct = default)
    {
        region = NormalizeRegion(region);
        var curated = CuratedMovieDataset.FindClone(tmdbId);
        var apiKey = ResolveApiKey(runtimeApiKey);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return curated;
        }

        try
        {
            var client = CreateAuthorizedClient(apiKey);
            var url = $"movie/{tmdbId}?language=pt-BR&append_to_response=credits,keywords,videos,watch/providers&include_video_language=pt,en,null";
            using var response = await client.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode) return curated;

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            var root = doc.RootElement;
            var card = MapSingleTmdbMovie(root, new List<VibeDefinition>(), false);

            card.Tagline = root.TryGetProperty("tagline", out var tagEl) && tagEl.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(tagEl.GetString())
                ? tagEl.GetString()
                : null;
            card.TrailerYoutubeKey = PickTrailerKey(root);
            ParseCredits(root, card);
            ParseKeywords(root, card);

            // Vibes do catálogo curado (quando o filme é conhecido) para manter os badges
            if (curated is not null)
            {
                card.MatchedVibes = curated.MatchedVibes;
                card.VibeMatchScore = curated.VibeMatchScore;
            }

            if (root.TryGetProperty("watch/providers", out var wp) && wp.TryGetProperty("results", out var wpResults))
            {
                card.WatchProviders = ParseRegionProviders(wpResults, tmdbId, region);
                await SaveProviderCacheBatchAsync(new Dictionary<int, WatchProvidersDto> { [tmdbId] = card.WatchProviders }, region, ct);
            }
            else
            {
                card.WatchProviders = await GetWatchProvidersAsync(tmdbId, region, apiKey, ct);
            }

            return card;
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
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
        var batch = await GetWatchProvidersBatchAsync(new[] { tmdbId }, region, runtimeApiKey, ct);
        return batch.TryGetValue(tmdbId, out var dto) ? dto : EmptyProviders(tmdbId, NormalizeRegion(region));
    }

    public async Task<Dictionary<int, WatchProvidersDto>> GetWatchProvidersBatchAsync(
        IEnumerable<int> tmdbIds,
        string region = "BR",
        string? runtimeApiKey = null,
        CancellationToken ct = default)
    {
        var normalizedRegion = NormalizeRegion(region);
        var ids = tmdbIds.Where(id => id > 0).Distinct().ToList();
        var result = new Dictionary<int, WatchProvidersDto>();
        if (ids.Count == 0) return result;

        // 1) Cache: UMA consulta para todos os ids (o DbContext não é thread-safe, então nada de DB dentro do paralelo)
        try
        {
            var now = DateTime.UtcNow;
            var cachedRows = await _db.CachedMovies
                .AsNoTracking()
                .Where(c => ids.Contains(c.TmdbMovieId) && c.Region == normalizedRegion && c.ExpiresAt > now)
                .Select(c => new { c.TmdbMovieId, c.ProvidersJson })
                .ToListAsync(ct);

            foreach (var row in cachedRows)
            {
                if (string.IsNullOrWhiteSpace(row.ProvidersJson)) continue;
                try
                {
                    var dto = JsonSerializer.Deserialize<WatchProvidersDto>(row.ProvidersJson);
                    if (dto is not null) result[row.TmdbMovieId] = dto;
                }
                catch (JsonException)
                {
                    // cache corrompido: ignora e busca de novo
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            _logger.LogDebug(ex, "Falha ao ler cache de provedores.");
        }

        var missing = ids.Where(id => !result.ContainsKey(id)).ToList();
        if (missing.Count == 0) return result;

        // 2) TMDB em paralelo limitado (só HTTP)
        var apiKey = ResolveApiKey(runtimeApiKey);
        var fetched = new Dictionary<int, WatchProvidersDto>();
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            var client = CreateAuthorizedClient(apiKey);
            using var gate = new SemaphoreSlim(MaxParallelProviderRequests);
            var tasks = missing.Select(async id =>
            {
                await gate.WaitAsync(ct);
                try
                {
                    return (Id: id, Dto: await FetchProvidersFromApiAsync(client, id, normalizedRegion, ct));
                }
                finally
                {
                    gate.Release();
                }
            });

            foreach (var (id, dto) in await Task.WhenAll(tasks))
            {
                if (dto is not null) fetched[id] = dto;
            }

            // 3) Grava o cache UMA vez no final
            if (fetched.Count > 0) await SaveProviderCacheBatchAsync(fetched, normalizedRegion, ct);
        }

        foreach (var id in missing)
        {
            if (fetched.TryGetValue(id, out var dto))
            {
                result[id] = dto;
                continue;
            }

            // Fallback honesto: dados do catálogo curado se conhecido; senão, lista vazia com o link do TMDB.
            var curated = CuratedMovieDataset.FindClone(id);
            result[id] = curated?.WatchProviders is { } cp && string.Equals(cp.Region, normalizedRegion, StringComparison.OrdinalIgnoreCase)
                ? cp
                : EmptyProviders(id, normalizedRegion);
        }

        return result;
    }

    public async Task<List<MovieCardDto>> GetNowPlayingMoviesAsync(
        string region = "BR",
        string? runtimeApiKey = null,
        CancellationToken ct = default)
    {
        region = NormalizeRegion(region);
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
                    var liveMovies = await ParseTmdbResultsArrayAsync(doc.RootElement, region, apiKey, new List<VibeDefinition>(), false, ct);
                    if (liveMovies.Count > 0) return liveMovies.Take(8).ToList();
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Falha ao consultar /movie/now_playing no TMDB. Utilizando lista em cartaz local.");
            }
        }

        // Sem chave: destaques do catálogo curado (não são necessariamente lançamentos em cartaz)
        return CuratedMovieDataset.Movies
            .OrderByDescending(m => m.ReleaseYear)
            .ThenByDescending(m => m.VoteAverage)
            .Take(6)
            .Select(m => m.Clone())
            .ToList();
    }

    public async Task<List<MovieCardDto>> GetSimilarMoviesAsync(
        int tmdbId,
        string region = "BR",
        string? runtimeApiKey = null,
        CancellationToken ct = default)
    {
        region = NormalizeRegion(region);
        var apiKey = ResolveApiKey(runtimeApiKey);
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            try
            {
                var client = CreateAuthorizedClient(apiKey);
                foreach (var endpoint in new[] { "recommendations", "similar" })
                {
                    using var response = await client.GetAsync($"movie/{tmdbId}/{endpoint}?language=pt-BR&page=1", ct);
                    if (!response.IsSuccessStatusCode) continue;

                    using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
                    var movies = await ParseTmdbResultsArrayAsync(doc.RootElement, region, apiKey, new List<VibeDefinition>(), false, ct, take: 8);
                    movies = movies.Where(m => m.TmdbId != tmdbId).Take(8).ToList();
                    if (movies.Count > 0) return movies;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Falha ao consultar recomendações do filme {TmdbId} no TMDB.", tmdbId);
            }
        }

        return RankCuratedSimilar(tmdbId, 8);
    }

    /// <summary>Fallback offline: ranqueia o catálogo curado por vibes (peso 2) e gêneros (peso 1) em comum.</summary>
    public static List<MovieCardDto> RankCuratedSimilar(int tmdbId, int take)
    {
        var source = CuratedMovieDataset.Movies.FirstOrDefault(m => m.TmdbId == tmdbId);
        var others = CuratedMovieDataset.Movies.Where(m => m.TmdbId != tmdbId);

        if (source is null)
        {
            return others.OrderByDescending(m => m.VoteAverage).Take(take).Select(m => m.Clone()).ToList();
        }

        return others
            .Select(m => (Movie: m,
                Vibes: m.MatchedVibes.Intersect(source.MatchedVibes, StringComparer.OrdinalIgnoreCase).Count(),
                Genres: m.GenreIds.Intersect(source.GenreIds).Count()))
            .Where(x => x.Vibes + x.Genres > 0)
            .OrderByDescending(x => x.Vibes * 2 + x.Genres)
            .ThenByDescending(x => x.Movie.VoteAverage)
            .Take(take)
            .Select(x =>
            {
                var clone = x.Movie.Clone();
                clone.VibeMatchScore = Math.Clamp(70 + x.Vibes * 8 + x.Genres * 4, 50, 99);
                return clone;
            })
            .ToList();
    }

    public async Task<string?> GetAgeRatingAsync(
        int tmdbId,
        string region = "BR",
        string? runtimeApiKey = null,
        CancellationToken ct = default)
    {
        var apiKey = ResolveApiKey(runtimeApiKey);
        if (string.IsNullOrWhiteSpace(apiKey)) return null;
        region = NormalizeRegion(region);

        try
        {
            var client = CreateAuthorizedClient(apiKey);
            using var response = await client.GetAsync($"movie/{tmdbId}/release_dates", ct);
            if (!response.IsSuccessStatusCode) return null;

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            return ParseCertification(doc.RootElement, region);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            _logger.LogDebug(ex, "Falha ao obter classificação indicativa de {TmdbId}.", tmdbId);
            return null;
        }
    }

    /// <summary>Extrai a certificação da região (preferindo lançamento em cinema) e formata em pt-BR ("Livre", "14 anos").</summary>
    public static string? ParseCertification(JsonElement root, string region)
    {
        if (!root.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array) return null;

        foreach (var r in results.EnumerateArray())
        {
            if (!r.TryGetProperty("iso_3166_1", out var isoEl) ||
                !string.Equals(isoEl.GetString(), region, StringComparison.OrdinalIgnoreCase)) continue;
            if (!r.TryGetProperty("release_dates", out var dates) || dates.ValueKind != JsonValueKind.Array) continue;

            var cert = dates.EnumerateArray()
                .Select(d => (
                    Cert: d.TryGetProperty("certification", out var c) ? c.GetString()?.Trim() : null,
                    Type: d.TryGetProperty("type", out var t) && t.TryGetInt32(out var ti) ? ti : 99))
                .Where(x => !string.IsNullOrWhiteSpace(x.Cert))
                .OrderBy(x => x.Type == 3 ? 0 : x.Type == 2 ? 1 : 2)
                .Select(x => x.Cert)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(cert)) return FormatCertification(cert!);
        }

        return null;
    }

    public static string FormatCertification(string cert)
    {
        var c = cert.Trim().ToUpperInvariant();
        if (c is "L" or "AL" or "LIVRE") return "Livre";
        return int.TryParse(c, NumberStyles.Integer, CultureInfo.InvariantCulture, out var age) ? $"{age} anos" : cert.Trim();
    }

    private async Task<DiscoverResultDto?> TryDiscoverFromTmdbApiAsync(
        Dictionary<string, string> tmdbParams,
        List<VibeDefinition> selectedVibes,
        bool usedKeywords,
        List<string> providerNames,
        string region,
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
            var movies = await ParseTmdbResultsArrayAsync(root, region, apiKey, selectedVibes, usedKeywords, ct);

            // Filtro local de segurança (o TMDB já filtrou por with_watch_providers)
            if (providerNames.Count > 0)
            {
                movies = movies.Where(m => m.WatchProviders.Flatrate.Any(p => ProviderCatalog.Matches(p, providerNames))).ToList();
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
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
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
        bool usedKeywords,
        CancellationToken ct,
        int take = ResultsPerPage)
    {
        var list = new List<MovieCardDto>();
        if (!root.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
            return list;

        foreach (var item in results.EnumerateArray().Take(take))
        {
            list.Add(MapSingleTmdbMovie(item, selectedVibes, usedKeywords));
        }

        var providers = await GetWatchProvidersBatchAsync(list.Select(m => m.TmdbId), region, apiKey, ct);
        foreach (var card in list)
        {
            card.WatchProviders = providers.TryGetValue(card.TmdbId, out var p) ? p : EmptyProviders(card.TmdbId, region);
        }

        return list;
    }

    private static MovieCardDto MapSingleTmdbMovie(JsonElement item, List<VibeDefinition> selectedVibes, bool usedKeywords)
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

        // Sem valores inventados: duração só quando o TMDB informa (o /discover não traz runtime)
        int? runtime = item.TryGetProperty("runtime", out var rtEl) && rtEl.ValueKind == JsonValueKind.Number && rtEl.GetInt32() > 0
            ? rtEl.GetInt32()
            : null;
        var voteAverage = item.TryGetProperty("vote_average", out var vaEl) && vaEl.ValueKind == JsonValueKind.Number ? Math.Round(vaEl.GetDouble(), 1) : 0;
        var voteCount = item.TryGetProperty("vote_count", out var vcEl) && vcEl.ValueKind == JsonValueKind.Number ? vcEl.GetInt32() : 0;

        var (score, matched) = ComputeVibeMatch(genreIds, voteAverage, voteCount, selectedVibes, usedKeywords);

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
            RuntimeMinutes = runtime,
            VoteAverage = voteAverage,
            VoteCount = voteCount,
            GenreIds = genreIds,
            Genres = genreIds.Select(VibeCatalog.GetGenreName).Distinct().ToList(),
            MatchedVibes = matched,
            VibeMatchScore = score
        };
    }

    /// <summary>
    /// Pontuação real de "vibe match" (50–99): sobreposição de gêneros com os gêneros sugeridos das vibes,
    /// nota média e volume de votos. MatchedVibes = vibes cujos gêneros sugeridos se sobrepõem; se nenhuma
    /// sobrepor mas o TMDB filtrou por keyword (OR), mantém as vibes selecionadas (o filme bateu alguma keyword).
    /// </summary>
    public static (int Score, List<string> MatchedVibes) ComputeVibeMatch(
        IReadOnlyCollection<int> genreIds,
        double voteAverage,
        int voteCount,
        IReadOnlyCollection<VibeDefinition> selectedVibes,
        bool usedKeywords)
    {
        var matched = selectedVibes
            .Where(v => v.SuggestedGenreIds.Any(genreIds.Contains))
            .Select(v => v.Id)
            .ToList();

        double genreComponent;
        if (selectedVibes.Count == 0)
        {
            genreComponent = 14; // sem vibe: neutro
        }
        else
        {
            var bestOverlap = selectedVibes
                .Select(v => v.SuggestedGenreIds.Count == 0 ? 0 : (double)v.SuggestedGenreIds.Count(genreIds.Contains) / v.SuggestedGenreIds.Count)
                .DefaultIfEmpty(0)
                .Max();
            var coverage = (double)matched.Count / selectedVibes.Count;
            genreComponent = 22 * (0.6 * bestOverlap + 0.4 * coverage);
            if (usedKeywords) genreComponent += 6; // passou pelo filtro de keywords das vibes
        }

        var ratingComponent = Math.Clamp((voteAverage - 5.0) * 4.0, 0, 16);       // 5.0 → 0 ; 9.0 → 16
        var popularityComponent = voteCount > 0 ? Math.Clamp(Math.Log10(voteCount) * 1.6, 0, 6) : 0; // 10k votos ≈ 6

        var score = (int)Math.Round(55 + genreComponent + ratingComponent + popularityComponent);
        score = Math.Clamp(score, 50, 99);

        if (matched.Count == 0 && usedKeywords)
            matched = selectedVibes.Select(v => v.Id).ToList();

        return (score, matched);
    }

    private static string? PickTrailerKey(JsonElement root)
    {
        if (!root.TryGetProperty("videos", out var videos) ||
            !videos.TryGetProperty("results", out var results) ||
            results.ValueKind != JsonValueKind.Array) return null;

        return results.EnumerateArray()
            .Where(v => v.TryGetProperty("site", out var s) && string.Equals(s.GetString(), "YouTube", StringComparison.OrdinalIgnoreCase)
                        && v.TryGetProperty("key", out var k) && !string.IsNullOrWhiteSpace(k.GetString()))
            .Select(v =>
            {
                var type = v.TryGetProperty("type", out var t) ? t.GetString() : null;
                var lang = v.TryGetProperty("iso_639_1", out var l) ? l.GetString() : null;
                var official = v.TryGetProperty("official", out var o) && o.ValueKind == JsonValueKind.True;
                return (Key: v.GetProperty("key").GetString(),
                    TypeRank: type == "Trailer" ? 0 : type == "Teaser" ? 1 : 2,
                    LangRank: lang == "pt" ? 0 : lang == "en" ? 1 : 2,
                    OfficialRank: official ? 0 : 1);
            })
            .OrderBy(x => x.TypeRank)
            .ThenBy(x => x.LangRank)
            .ThenBy(x => x.OfficialRank)
            .Select(x => x.Key)
            .FirstOrDefault();
    }

    private static void ParseCredits(JsonElement root, MovieCardDto card)
    {
        if (!root.TryGetProperty("credits", out var credits)) return;

        if (credits.TryGetProperty("crew", out var crew) && crew.ValueKind == JsonValueKind.Array)
        {
            card.Director = crew.EnumerateArray()
                .Where(c => c.TryGetProperty("job", out var j) && j.GetString() == "Director")
                .Select(c => c.TryGetProperty("name", out var n) ? n.GetString() : null)
                .FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? card.Director;
        }

        if (credits.TryGetProperty("cast", out var cast) && cast.ValueKind == JsonValueKind.Array)
        {
            var names = cast.EnumerateArray()
                .Select(c => c.TryGetProperty("name", out var n) ? n.GetString() : null)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Take(6)
                .Cast<string>()
                .ToList();
            if (names.Count > 0) card.Cast = names;
        }
    }

    private static void ParseKeywords(JsonElement root, MovieCardDto card)
    {
        if (!root.TryGetProperty("keywords", out var kw) ||
            !kw.TryGetProperty("keywords", out var arr) ||
            arr.ValueKind != JsonValueKind.Array) return;

        var names = arr.EnumerateArray()
            .Select(k => k.TryGetProperty("name", out var n) ? n.GetString() : null)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Take(10)
            .Cast<string>()
            .ToList();
        if (names.Count > 0) card.Keywords = names;
    }

    private async Task<WatchProvidersDto?> FetchProvidersFromApiAsync(HttpClient client, int tmdbId, string region, CancellationToken ct)
    {
        try
        {
            using var response = await client.GetAsync($"movie/{tmdbId}/watch/providers", ct);
            if (!response.IsSuccessStatusCode) return null;

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            return doc.RootElement.TryGetProperty("results", out var results)
                ? ParseRegionProviders(results, tmdbId, region)
                : EmptyProviders(tmdbId, region);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Falha ao buscar Watch Providers para {TmdbId}.", tmdbId);
            return null;
        }
    }

    private static WatchProvidersDto ParseRegionProviders(JsonElement results, int tmdbId, string region)
    {
        if (results.ValueKind != JsonValueKind.Object || !results.TryGetProperty(region, out var regionNode))
            return EmptyProviders(tmdbId, region); // TMDB não lista nenhum provedor nesta região

        return new WatchProvidersDto
        {
            Region = region,
            TmdbWatchLink = regionNode.TryGetProperty("link", out var linkEl) && !string.IsNullOrWhiteSpace(linkEl.GetString())
                ? linkEl.GetString()
                : TmdbWatchLink(tmdbId, region),
            Flatrate = ParseProvidersArray(regionNode, "flatrate"),
            Rent = ParseProvidersArray(regionNode, "rent"),
            Buy = ParseProvidersArray(regionNode, "buy")
        };
    }

    private static string TmdbWatchLink(int tmdbId, string region) => $"https://www.themoviedb.org/movie/{tmdbId}/watch?locale={region}";

    private static WatchProvidersDto EmptyProviders(int tmdbId, string region) => new()
    {
        Region = region,
        TmdbWatchLink = TmdbWatchLink(tmdbId, region)
    };

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

    private async Task SaveProviderCacheBatchAsync(Dictionary<int, WatchProvidersDto> dtos, string region, CancellationToken ct)
    {
        if (dtos.Count == 0) return;
        try
        {
            var ids = dtos.Keys.ToList();
            var existingRows = await _db.CachedMovies
                .Where(c => ids.Contains(c.TmdbMovieId) && c.Region == region)
                .ToListAsync(ct);

            var now = DateTime.UtcNow;
            foreach (var (id, dto) in dtos)
            {
                var json = JsonSerializer.Serialize(dto);
                var existing = existingRows.FirstOrDefault(r => r.TmdbMovieId == id);
                if (existing is not null)
                {
                    existing.ProvidersJson = json;
                    existing.CachedAt = now;
                    existing.ExpiresAt = now.Add(ProviderCacheTtl);
                }
                else
                {
                    _db.CachedMovies.Add(new CachedMovie
                    {
                        TmdbMovieId = id,
                        Region = region,
                        ProvidersJson = json,
                        CachedAt = now,
                        ExpiresAt = now.Add(ProviderCacheTtl)
                    });
                }
            }

            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            _logger.LogDebug(ex, "Não foi possível gravar cache de provedores.");
            _db.ChangeTracker.Clear();
        }
    }

    /// <summary>Filtra e pontua o catálogo/lista SEM alterar os objetos de origem (devolve clones).</summary>
    private static List<MovieCardDto> FilterMovieListLocally(
        IEnumerable<MovieCardDto> source,
        VibeDiscoverQuery query,
        List<VibeDefinition> selectedVibes,
        List<string> providerNames,
        int? maxRuntime,
        string? releaseGte,
        string? releaseLte,
        double? minVoteAverage)
    {
        IEnumerable<MovieCardDto> filtered = source;

        if (!string.IsNullOrWhiteSpace(query.SearchText))
        {
            var q = TextNormalizer.Normalize(query.SearchText);
            filtered = filtered.Where(m =>
                TextNormalizer.Normalize(m.Title).Contains(q) ||
                TextNormalizer.Normalize(m.OriginalTitle).Contains(q) ||
                TextNormalizer.Normalize(m.Overview).Contains(q) ||
                m.Keywords.Any(k => TextNormalizer.Normalize(k).Contains(q)) ||
                TextNormalizer.Normalize(m.Director).Contains(q));
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

        if (providerNames.Count > 0)
        {
            filtered = filtered.Where(m => m.WatchProviders.Flatrate.Any(p => ProviderCatalog.Matches(p, providerNames)));
        }

        return filtered
            .Select(m =>
            {
                var clone = m.Clone();
                var matchCount = selectedVibes.Count == 0
                    ? 1
                    : clone.MatchedVibes.Count(mv => selectedVibes.Any(sv => sv.Id == mv));
                var score = Math.Min(99, 86 + (matchCount * 5) + (int)Math.Round(clone.VoteAverage - 7.0));
                clone.VibeMatchScore = Math.Max(80, score);
                return clone;
            })
            .OrderByDescending(m => m.VibeMatchScore)
            .ThenByDescending(m => m.VoteAverage)
            .ToList();
    }

    private static string NormalizeRegion(string? region) =>
        string.IsNullOrWhiteSpace(region) ? "BR" : region.Trim().ToUpperInvariant();

    private string? ResolveApiKey(string? runtimeApiKey)
    {
        if (!string.IsNullOrWhiteSpace(runtimeApiKey))
            return runtimeApiKey.Trim();

        var candidates = new[]
        {
            _configuration["Tmdb:ApiKey"],
            _configuration["Tmdb:ReadAccessToken"],
            Environment.GetEnvironmentVariable("TMDB__ApiKey"),
            Environment.GetEnvironmentVariable("TMDB_API_KEY")
        };

        var configKey = candidates.FirstOrDefault(k => !string.IsNullOrWhiteSpace(k));
        return string.IsNullOrWhiteSpace(configKey) ? null : configKey.Trim();
    }

    private HttpClient CreateAuthorizedClient(string apiKey)
    {
        var client = _httpClientFactory.CreateClient("TmdbClient");
        client.BaseAddress ??= new Uri("https://api.themoviedb.org/3/");

        // JWT Read Access Token (v4) → Bearer; chave v3 (32 hex) → header interno convertido em ?api_key= pelo TmdbApiKeyQueryHandler
        if (apiKey.StartsWith("eyJ", StringComparison.Ordinal))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }
        else
        {
            client.DefaultRequestHeaders.Remove("X-Api-Key-V3");
            client.DefaultRequestHeaders.Add("X-Api-Key-V3", apiKey);
        }

        return client;
    }
}
