using Microsoft.EntityFrameworkCore;
using VibeMovies.Application.Interfaces;
using VibeMovies.Domain.Entities;
using VibeMovies.Domain.Models;
using VibeMovies.Infrastructure.Persistence;

namespace VibeMovies.Infrastructure.Repositories;

public class WatchlistRepository : IWatchlistRepository
{
    private readonly VibeMoviesDbContext _db;

    public WatchlistRepository(VibeMoviesDbContext db)
    {
        _db = db;
    }

    public async Task<List<WatchlistItem>> GetByUserIdAsync(Guid userId, WatchlistType? filterType = null, CancellationToken ct = default)
    {
        var query = _db.WatchlistItems
            .AsNoTracking()
            .Where(w => w.UserId == userId);

        if (filterType.HasValue)
        {
            query = query.Where(w => w.ListType == filterType.Value);
        }

        return await query
            .OrderByDescending(w => w.WatchedAt ?? w.AddedAt)
            .ToListAsync(ct);
    }

    public async Task<WatchlistItem?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        return await _db.WatchlistItems
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId, ct);
    }

    public async Task<WatchlistItem?> GetByTmdbIdAsync(Guid userId, int tmdbMovieId, CancellationToken ct = default)
    {
        return await _db.WatchlistItems
            .FirstOrDefaultAsync(w => w.UserId == userId && w.TmdbMovieId == tmdbMovieId, ct);
    }

    public async Task<WatchlistItem> UpsertAsync(WatchlistItem item, CancellationToken ct = default)
    {
        var existing = await _db.WatchlistItems
            .FirstOrDefaultAsync(w => w.UserId == item.UserId && w.TmdbMovieId == item.TmdbMovieId, ct);

        if (existing is not null)
        {
            existing.ListType = item.ListType;
            existing.PersonalRating = item.PersonalRating;
            existing.PersonalNotes = item.PersonalNotes;
            existing.WatchedAt = item.ListType == WatchlistType.Watched
                ? (item.WatchedAt ?? DateTime.UtcNow)
                : null;
            existing.Title = string.IsNullOrWhiteSpace(item.Title) ? existing.Title : item.Title;
            existing.PosterPath = string.IsNullOrWhiteSpace(item.PosterPath) ? existing.PosterPath : item.PosterPath;
            existing.RuntimeMinutes = item.RuntimeMinutes ?? existing.RuntimeMinutes;
            if (string.IsNullOrWhiteSpace(existing.GenresCsv)) existing.GenresCsv = item.GenresCsv;
            if (existing.TmdbVoteAverage <= 0) existing.TmdbVoteAverage = item.TmdbVoteAverage;
            if (existing.ReleaseYear is null) existing.ReleaseYear = item.ReleaseYear;
            await _db.SaveChangesAsync(ct);
            return existing;
        }

        if (item.ListType == WatchlistType.Watched && item.WatchedAt is null)
        {
            item.WatchedAt = DateTime.UtcNow;
        }

        _db.WatchlistItems.Add(item);
        await _db.SaveChangesAsync(ct);
        return item;
    }

    public async Task<WatchlistItem?> UpdateAsync(Guid id, Guid userId, UpdateWatchlistItemDto dto, CancellationToken ct = default)
    {
        var existing = await GetByIdAsync(id, userId, ct);
        if (existing is null) return null;

        existing.ListType = dto.ListType;
        existing.PersonalRating = dto.PersonalRating;
        existing.PersonalNotes = dto.PersonalNotes;
        existing.WatchedAt = dto.ListType == WatchlistType.Watched
            ? (dto.WatchedAt ?? existing.WatchedAt ?? DateTime.UtcNow)
            : null;

        await _db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var existing = await GetByIdAsync(id, userId, ct);
        if (existing is null) return false;

        _db.WatchlistItems.Remove(existing);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
