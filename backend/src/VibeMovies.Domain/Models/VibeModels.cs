namespace VibeMovies.Domain.Models;

public class VibeDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Emoji { get; set; } = "🎬";
    public string AccentColor { get; set; } = "#8B5CF6";
    public List<int> TmdbKeywordIds { get; set; } = new();
    public List<int> SuggestedGenreIds { get; set; } = new();
    public string? ReleaseDateGte { get; set; }
    public string? ReleaseDateLte { get; set; }
    public double? MinVoteAverage { get; set; }
    public int? MaxRuntimeMinutes { get; set; }
    public List<string> SampleTags { get; set; } = new();
}

public class GenreDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = "🎞️";
}

public class VibeDiscoverQuery
{
    public List<string> VibeIds { get; set; } = new();
    public List<int> GenreIds { get; set; } = new();
    public int? MaxRuntimeMinutes { get; set; }
    public int? ReleaseDecade { get; set; } // e.g., 1990, 2000, 2010, 2020
    public double? MinRating { get; set; }
    public string? SearchText { get; set; }
    public string? WatchProviderFilter { get; set; } // e.g. "Netflix", "Amazon Prime Video", "Max"
    public string Region { get; set; } = "BR";
    public string SortBy { get; set; } = "popularity.desc";
    public int Page { get; set; } = 1;
}
