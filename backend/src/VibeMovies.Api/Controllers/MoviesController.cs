using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using VibeMovies.Api.Infrastructure;
using VibeMovies.Application.Catalog;
using VibeMovies.Application.Services;
using VibeMovies.Application.Interfaces;
using VibeMovies.Domain.Models;

namespace VibeMovies.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MoviesController : ControllerBase
{
    private readonly ITmdbService _tmdbService;
    private readonly IUserRepository _userRepository;
    private readonly SurprisePickService _surprise;

    public MoviesController(ITmdbService tmdbService, IUserRepository userRepository, SurprisePickService surprise)
    {
        _tmdbService = tmdbService;
        _userRepository = userRepository;
        _surprise = surprise;
    }

    private string? RuntimeApiKey => Request.Headers["X-TMDB-Api-Key"].FirstOrDefault();

    private static List<string> ParseCsv(string? csv) => string.IsNullOrWhiteSpace(csv)
        ? new List<string>()
        : csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    private static List<int> ParseIntCsv(string? csv) => ParseCsv(csv)
        .Select(p => int.TryParse(p, out var v) ? v : (int?)null)
        .Where(v => v.HasValue)
        .Select(v => v!.Value)
        .ToList();

    private async Task<Domain.Entities.ApplicationUser?> CurrentUserAsync(CancellationToken ct)
    {
        if (User.Identity?.IsAuthenticated != true) return null;
        return Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? await _userRepository.GetByIdAsync(id, ct)
            : null;
    }

    [HttpGet("vibes")]
    public ActionResult<IReadOnlyList<VibeDefinition>> GetVibes()
    {
        return Ok(VibeCatalog.AllVibes);
    }

    [HttpGet("genres")]
    public ActionResult<IReadOnlyList<GenreDto>> GetGenres()
    {
        return Ok(VibeCatalog.AllGenres);
    }

    [HttpGet("discover")]
    public async Task<ActionResult<DiscoverResultDto>> Discover(
        [FromQuery] string? vibes = null,
        [FromQuery] string? genres = null,
        [FromQuery] int? maxRuntime = null,
        [FromQuery] int? decade = null,
        [FromQuery] double? minRating = null,
        [FromQuery] string? search = null,
        [FromQuery] string? provider = null,
        [FromQuery] string region = "BR",
        [FromQuery] string sortBy = "popularity.desc",
        [FromQuery] int page = 1,
        [FromQuery] bool onlyMyProviders = false,
        CancellationToken ct = default)
    {
        var runtimeApiKey = RuntimeApiKey;

        if (onlyMyProviders)
        {
            var user = await CurrentUserAsync(ct);
            if (user is null)
                return GlobalExceptionHandler.Problem(StatusCodes.Status401Unauthorized, "Entre na sua conta para filtrar pelos seus streamings.");
            var mine = ProviderCatalog.ParseList(user.MyProvidersCsv);
            if (mine.Count > 0)
            {
                // combina com um eventual filtro explícito (OR)
                provider = string.Join(",", ProviderCatalog.ParseList(provider).Concat(mine).Distinct(StringComparer.OrdinalIgnoreCase));
            }
        }

        var vibeList = string.IsNullOrWhiteSpace(vibes)
            ? new List<string>()
            : vibes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        var genreList = new List<int>();
        if (!string.IsNullOrWhiteSpace(genres))
        {
            foreach (var part in genres.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (int.TryParse(part, out var gid)) genreList.Add(gid);
            }
        }

        var query = new VibeDiscoverQuery
        {
            VibeIds = vibeList,
            GenreIds = genreList,
            MaxRuntimeMinutes = maxRuntime,
            ReleaseDecade = decade,
            MinRating = minRating,
            SearchText = search,
            WatchProviderFilter = provider,
            Region = region,
            SortBy = sortBy,
            Page = page
        };

        var result = await _tmdbService.DiscoverByVibeAsync(query, runtimeApiKey, ct);
        return Ok(result);
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<MovieCardDto>>> Search(
        [FromQuery] string q,
        [FromQuery] string region = "BR",
        CancellationToken ct = default)
    {
        var runtimeApiKey = Request.Headers["X-TMDB-Api-Key"].FirstOrDefault();
        var results = await _tmdbService.SearchMoviesAsync(q, region, runtimeApiKey, ct);
        return Ok(results);
    }

    [HttpGet("{tmdbId:int}")]
    public async Task<ActionResult<MovieCardDto>> GetDetails(
        int tmdbId,
        [FromQuery] string region = "BR",
        CancellationToken ct = default)
    {
        var runtimeApiKey = Request.Headers["X-TMDB-Api-Key"].FirstOrDefault();
        var movie = await _tmdbService.GetMovieDetailsAsync(tmdbId, region, runtimeApiKey, ct);
        if (movie is null) return GlobalExceptionHandler.Problem(StatusCodes.Status404NotFound, "Filme não encontrado.");
        return Ok(movie);
    }

    /// <summary>Filmes parecidos (TMDB /recommendations; sem chave: catálogo curado por vibes/gêneros em comum). Top 8.</summary>
    [HttpGet("{tmdbId:int}/similar")]
    public async Task<ActionResult<List<MovieCardDto>>> GetSimilar(
        int tmdbId,
        [FromQuery] string region = "BR",
        CancellationToken ct = default)
    {
        var movies = await _tmdbService.GetSimilarMoviesAsync(tmdbId, region, RuntimeApiKey, ct);
        return Ok(movies.Take(8).ToList());
    }

    /// <summary>"Me surpreenda": sorteio ponderado com explicação (reason) e frase para voz (spokenText).</summary>
    [HttpGet("surprise")]
    public async Task<ActionResult<SurprisePickDto>> Surprise(
        [FromQuery] string? vibes = null,
        [FromQuery] string? genres = null,
        [FromQuery] int? maxRuntime = null,
        [FromQuery] string? provider = null,
        [FromQuery] string? excludeIds = null,
        [FromQuery] string region = "BR",
        CancellationToken ct = default)
    {
        var user = await CurrentUserAsync(ct);
        var pick = await _surprise.PickAsync(new SurpriseRequest
        {
            VibeIds = ParseCsv(vibes),
            GenreIds = ParseIntCsv(genres),
            MaxRuntimeMinutes = maxRuntime is > 0 ? maxRuntime : null,
            ProviderFilter = string.IsNullOrWhiteSpace(provider) ? null : provider,
            ExcludeIds = ParseIntCsv(excludeIds).ToHashSet(),
            Region = string.IsNullOrWhiteSpace(region) ? "BR" : region,
            PreferredProviders = user is null ? new List<string>() : ProviderCatalog.ParseList(user.MyProvidersCsv)
        }, RuntimeApiKey, ct);

        if (pick is null)
            return GlobalExceptionHandler.Problem(StatusCodes.Status404NotFound, "Não encontrei nenhum filme com esses filtros.");
        return Ok(pick);
    }

    [HttpGet("{tmdbId:int}/providers")]
    public async Task<ActionResult<WatchProvidersDto>> GetProviders(
        int tmdbId,
        [FromQuery] string region = "BR",
        CancellationToken ct = default)
    {
        var runtimeApiKey = Request.Headers["X-TMDB-Api-Key"].FirstOrDefault();
        var providers = await _tmdbService.GetWatchProvidersAsync(tmdbId, region, runtimeApiKey, ct);
        return Ok(providers);
    }
}
