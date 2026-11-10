namespace VibeMovies.Domain.Entities;

public class ApplicationUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PreferredRegion { get; set; } = "BR";
    public string DefaultCity { get; set; } = "Joinville/SC";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<WatchlistItem> WatchlistItems { get; set; } = new List<WatchlistItem>();
}
