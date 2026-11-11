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

    /// <summary>true quando há chave do TMDB (header em tempo de execução ou configuração).</summary>
    bool HasApiKey(string? runtimeApiKey = null);

    /// <summary>Recomendações do TMDB para o filme; sem chave, ranqueia o catálogo curado por vibes/gêneros em comum (top 8).</summary>
    Task<List<MovieCardDto>> GetSimilarMoviesAsync(int tmdbId, string region = "BR", string? runtimeApiKey = null, CancellationToken ct = default);

    /// <summary>Classificação indicativa (certification do TMDB release_dates) para a região, ou null se não houver.</summary>
    Task<string?> GetAgeRatingAsync(int tmdbId, string region = "BR", string? runtimeApiKey = null, CancellationToken ct = default);

    /// <summary>Provedores de vários filmes de uma vez (cache lido em uma consulta, chamadas ao TMDB em paralelo limitado).</summary>
    Task<Dictionary<int, WatchProvidersDto>> GetWatchProvidersBatchAsync(IEnumerable<int> tmdbIds, string region = "BR", string? runtimeApiKey = null, CancellationToken ct = default);
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

    /// <summary>Primeiro usuário cadastrado (ordem de criação) — usado como fallback da integração Jarvis.</summary>
    Task<ApplicationUser?> GetFirstAsync(CancellationToken ct = default);

    Task<ApplicationUser?> UpdateAsync(ApplicationUser user, CancellationToken ct = default);
}

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto, CancellationToken ct = default);
    Task<AuthResponseDto?> LoginAsync(LoginRequestDto dto, CancellationToken ct = default);
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
}
