using Microsoft.AspNetCore.Mvc;
using VibeMovies.Application.Catalog;
using VibeMovies.Application.Interfaces;
using VibeMovies.Domain.Models;

namespace VibeMovies.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MoviesController : ControllerBase
{
    private readonly ITmdbService _tmdbService;

    public MoviesController(ITmdbService tmdbService)
    {
        _tmdbService = tmdbService;
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
        CancellationToken ct = default)
    {
        var runtimeApiKey = Request.Headers["X-TMDB-Api-Key"].FirstOrDefault();

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
        if (movie is null) return NotFound();
        return Ok(movie);
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
