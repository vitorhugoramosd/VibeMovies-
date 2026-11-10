namespace VibeMovies.Domain.Entities;

public class CachedMovie
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int TmdbMovieId { get; set; }
    public string Region { get; set; } = "BR";
    public string Title { get; set; } = string.Empty;
    public string MovieJson { get; set; } = string.Empty;
    public string ProvidersJson { get; set; } = string.Empty;
    public DateTime CachedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(12);
}
