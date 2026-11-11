using Microsoft.EntityFrameworkCore;
using VibeMovies.Application.Interfaces;
using VibeMovies.Domain.Entities;
using VibeMovies.Infrastructure.Persistence;

namespace VibeMovies.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly VibeMoviesDbContext _db;

    public UserRepository(VibeMoviesDbContext db)
    {
        _db = db;
    }

    public async Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        return await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), ct);
    }

    public async Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<ApplicationUser?> GetFirstAsync(CancellationToken ct = default)
    {
        return await _db.Users
            .AsNoTracking()
            .OrderBy(u => u.CreatedAt)
            .ThenBy(u => u.Email)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<ApplicationUser?> UpdateAsync(ApplicationUser user, CancellationToken ct = default)
    {
        var existing = await _db.Users.FirstOrDefaultAsync(u => u.Id == user.Id, ct);
        if (existing is null) return null;

        existing.Name = user.Name;
        existing.DefaultCity = user.DefaultCity;
        existing.PreferredRegion = user.PreferredRegion;
        existing.MyProvidersCsv = user.MyProvidersCsv ?? string.Empty;
        await _db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<ApplicationUser> CreateAsync(ApplicationUser user, CancellationToken ct = default)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        return user;
    }
}
