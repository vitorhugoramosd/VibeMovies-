namespace VibeMovies.Domain.Entities;

public enum WatchlistType
{
    WantToWatch = 1,
    Watched = 2
}

public class WatchlistItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public int TmdbMovieId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string OriginalTitle { get; set; } = string.Empty;
    public string PosterPath { get; set; } = string.Empty;
    public string BackdropPath { get; set; } = string.Empty;
    public int? ReleaseYear { get; set; }
    public string Overview { get; set; } = string.Empty;
    public WatchlistType ListType { get; set; } = WatchlistType.WantToWatch;
    public int? PersonalRating { get; set; } // 1 to 5 stars
    public string? PersonalNotes { get; set; }
    public DateTime? WatchedAt { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public string GenresCsv { get; set; } = string.Empty;
    public int? RuntimeMinutes { get; set; }
    public double TmdbVoteAverage { get; set; }

    public ApplicationUser? User { get; set; }
}
