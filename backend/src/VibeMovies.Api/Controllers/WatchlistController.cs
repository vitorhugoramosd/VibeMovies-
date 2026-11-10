using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VibeMovies.Application.Interfaces;
using VibeMovies.Domain.Entities;
using VibeMovies.Domain.Models;

namespace VibeMovies.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WatchlistController : ControllerBase
{
    private readonly IWatchlistRepository _watchlistRepository;

    public WatchlistController(IWatchlistRepository watchlistRepository)
    {
        _watchlistRepository = watchlistRepository;
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
        var wantToWatch = all.Where(x => x.ListType == WatchlistType.WantToWatch).ToList();
        var watched = all.Where(x => x.ListType == WatchlistType.Watched).ToList();

        var rated = watched.Where(x => x.PersonalRating.HasValue).ToList();
        var avgRating = rated.Count > 0
            ? Math.Round(rated.Average(x => x.PersonalRating!.Value), 1)
            : 0.0;

        var topGenre = all
            .SelectMany(x => (x.GenresCsv ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .GroupBy(g => g)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault() ?? "Suspense / Thriller";

        return Ok(new WatchlistStatsDto
        {
            TotalWantToWatch = wantToWatch.Count,
            TotalWatched = watched.Count,
            TotalMinutesWatched = watched.Sum(x => x.RuntimeMinutes ?? 120),
            AveragePersonalRating = avgRating,
            FavoriteGenre = topGenre
        });
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
            PersonalRating = dto.PersonalRating is >= 1 and <= 5 ? dto.PersonalRating : null,
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
