using VibeMovies.Domain.Entities;
using VibeMovies.Domain.Models;

namespace VibeMovies.Application.Interfaces;

public interface ITmdbService
{
    Task<DiscoverResultDto> DiscoverByVibeAsync(VibeDiscoverQuery query, string? runtimeApiKey = null, CancellationToken ct = default);
    Task<List<MovieCardDto>> SearchMoviesAsync(string queryText, string region = "BR", string? runtimeApiKey = null, CancellationToken ct = default);
    Task<MovieCardDto?> GetMovieDetailsAsync(int tmdbId, string region = "BR", string? runtimeApiKey = null, CancellationToken ct = default);
    Task<WatchProvidersDto> GetWatchProvidersAsync(int tmdbId, string region = "BR", string? runtimeApiKey = null, CancellationToken ct = default);
    Task<List<MovieCardDto>> GetNowPlayingMoviesAsync(string region = "BR", string? runtimeApiKey = null, CancellationToken ct = default);
}

public interface ICinemaRadarService
{
    Task<CinemaRadarResponseDto> GetLocalRadarAsync(string city, string? runtimeApiKey = null, CancellationToken ct = default);
    IReadOnlyList<string> GetSupportedCities();
}

public interface IWatchlistRepository
{
    Task<List<WatchlistItem>> GetByUserIdAsync(Guid userId, WatchlistType? filterType = null, CancellationToken ct = default);
    Task<WatchlistItem?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<WatchlistItem?> GetByTmdbIdAsync(Guid userId, int tmdbMovieId, CancellationToken ct = default);
    Task<WatchlistItem> UpsertAsync(WatchlistItem item, CancellationToken ct = default);
    Task<WatchlistItem?> UpdateAsync(Guid id, Guid userId, UpdateWatchlistItemDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken ct = default);
}

public interface IUserRepository
{
    Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ApplicationUser> CreateAsync(ApplicationUser user, CancellationToken ct = default);
}

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto, CancellationToken ct = default);
    Task<AuthResponseDto?> LoginAsync(LoginRequestDto dto, CancellationToken ct = default);
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
}
