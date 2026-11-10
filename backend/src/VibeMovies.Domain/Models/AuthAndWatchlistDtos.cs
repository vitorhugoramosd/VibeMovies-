using VibeMovies.Domain.Entities;

namespace VibeMovies.Domain.Models;

public record RegisterRequestDto(
    string Name,
    string Email,
    string Password,
    string? PreferredRegion = "BR",
    string? DefaultCity = "Joinville/SC"
);

public record LoginRequestDto(string Email, string Password);

public record AuthResponseDto(
    string Token,
    Guid UserId,
    string Name,
    string Email,
    string PreferredRegion,
    string DefaultCity
);

public record UpsertWatchlistItemDto(
    int TmdbMovieId,
    string Title,
    string? OriginalTitle,
    string? PosterPath,
    string? BackdropPath,
    int? ReleaseYear,
    string? Overview,
    WatchlistType ListType,
    int? PersonalRating,
    string? PersonalNotes,
    DateTime? WatchedAt,
    string? GenresCsv,
    int? RuntimeMinutes,
    double TmdbVoteAverage
);

public record UpdateWatchlistItemDto(
    WatchlistType ListType,
    int? PersonalRating,
    string? PersonalNotes,
    DateTime? WatchedAt
);

public class WatchlistStatsDto
{
    public int TotalWantToWatch { get; set; }
    public int TotalWatched { get; set; }
    public int TotalMinutesWatched { get; set; }
    public double AveragePersonalRating { get; set; }
    public string FavoriteGenre { get; set; } = "Suspense";
}
