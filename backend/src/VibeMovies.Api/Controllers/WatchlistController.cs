using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VibeMovies.Api.Infrastructure;
using VibeMovies.Application.Interfaces;
using VibeMovies.Application.Services;
using VibeMovies.Domain.Entities;
using VibeMovies.Domain.Models;

namespace VibeMovies.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WatchlistController : ControllerBase
{
    private readonly IWatchlistRepository _watchlistRepository;
    private readonly RecommendationService _recommendations;
    private readonly TimeProvider _timeProvider;

    public WatchlistController(IWatchlistRepository watchlistRepository, RecommendationService recommendations, TimeProvider timeProvider)
    {
        _watchlistRepository = watchlistRepository;
        _recommendations = recommendations;
        _timeProvider = timeProvider;
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    [HttpGet]
    public async Task<ActionResult<List<WatchlistItem>>> GetAll(
        [FromQuery] WatchlistType? type = null,
        CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var items = await _watchlistRepository.GetByUserIdAsync(userId, type, ct);
        return Ok(items);
    }

    [HttpGet("stats")]
    public async Task<ActionResult<WatchlistStatsDto>> GetStats(CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var all = await _watchlistRepository.GetByUserIdAsync(userId, null, ct);
        return Ok(WatchlistStatsCalculator.Compute(all, _timeProvider));
    }

    /// <summary>"Para você": recomendações a partir dos assistidos com nota ≥4 e do Quero Ver (exclui o que já está no Diário).</summary>
    [HttpGet("recommendations")]
    public async Task<ActionResult<List<MovieCardDto>>> GetRecommendations([FromQuery] string region = "BR", CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var runtimeApiKey = Request.Headers["X-TMDB-Api-Key"].FirstOrDefault();
        return Ok(await _recommendations.GetForUserAsync(userId, region, runtimeApiKey, 12, ct));
    }

    /// <summary>Exporta o Diário: CSV (";" + BOM UTF-8, Excel pt-BR) ou JSON.</summary>
    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] string format = "csv", CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var items = await _watchlistRepository.GetByUserIdAsync(userId, null, ct);
        var stamp = _timeProvider.GetLocalNow().ToString("yyyyMMdd");
        return format.Trim().ToLowerInvariant() switch
        {
            "json" => File(WatchlistExporter.ToJson(items), "application/json; charset=utf-8", $"vibemovies-diario-{stamp}.json"),
            "csv" => File(WatchlistExporter.ToCsv(items), "text/csv; charset=utf-8", $"vibemovies-diario-{stamp}.csv"),
            _ => GlobalExceptionHandler.Problem(StatusCodes.Status400BadRequest, "Formato inválido. Use format=csv ou format=json.")
        };
    }

    [HttpPost]
    public async Task<ActionResult<WatchlistItem>> Upsert(
        [FromBody] UpsertWatchlistItemDto dto,
        CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var item = new WatchlistItem
        {
            UserId = userId,
            TmdbMovieId = dto.TmdbMovieId,
            Title = dto.Title,
            OriginalTitle = dto.OriginalTitle ?? dto.Title,
            PosterPath = dto.PosterPath ?? "",
            BackdropPath = dto.BackdropPath ?? "",
            ReleaseYear = dto.ReleaseYear,
            Overview = dto.Overview ?? "",
            ListType = dto.ListType,
            PersonalRating = dto.PersonalRating,
            PersonalNotes = dto.PersonalNotes,
            WatchedAt = dto.WatchedAt,
            GenresCsv = dto.GenresCsv ?? "",
            RuntimeMinutes = dto.RuntimeMinutes,
            TmdbVoteAverage = dto.TmdbVoteAverage
        };

        var saved = await _watchlistRepository.UpsertAsync(item, ct);
        return Ok(saved);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<WatchlistItem>> Update(
        Guid id,
        [FromBody] UpdateWatchlistItemDto dto,
        CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var updated = await _watchlistRepository.UpdateAsync(id, userId, dto, ct);
        if (updated is null) return NotFound();

        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var deleted = await _watchlistRepository.DeleteAsync(id, userId, ct);
        if (!deleted) return NotFound();

        return NoContent();
    }
}
