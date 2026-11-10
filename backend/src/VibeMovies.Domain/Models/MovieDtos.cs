namespace VibeMovies.Domain.Models;

public class MovieCardDto
{
    public int TmdbId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string OriginalTitle { get; set; } = string.Empty;
    public string Overview { get; set; } = string.Empty;
    public string PosterUrl { get; set; } = string.Empty;
    public string BackdropUrl { get; set; } = string.Empty;
    public string ReleaseDate { get; set; } = string.Empty;
    public int? ReleaseYear { get; set; }
    public int? RuntimeMinutes { get; set; }
    public double VoteAverage { get; set; }
    public int VoteCount { get; set; }
    public List<string> Genres { get; set; } = new();
    public List<int> GenreIds { get; set; } = new();
    public List<string> MatchedVibes { get; set; } = new();
    public List<string> Keywords { get; set; } = new();
    public string? Director { get; set; }
    public List<string> Cast { get; set; } = new();
    public WatchProvidersDto WatchProviders { get; set; } = new();
    public int VibeMatchScore { get; set; } = 92;
}

public class WatchProvidersDto
{
    public string Region { get; set; } = "BR";
    public string? TmdbWatchLink { get; set; }
    public List<StreamingProviderBadgeDto> Flatrate { get; set; } = new(); // Subscription (Netflix, Prime, Max, etc.)
    public List<StreamingProviderBadgeDto> Rent { get; set; } = new();     // Aluguel (Apple TV, Google Play, Prime Store)
    public List<StreamingProviderBadgeDto> Buy { get; set; } = new();      // Compra
}

public class StreamingProviderBadgeDto
{
    public int ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    public int DisplayPriority { get; set; }
}

public class DiscoverResultDto
{
    public int Page { get; set; } = 1;
    public int TotalResults { get; set; }
    public int TotalPages { get; set; }
    public bool UsingLiveTmdbApi { get; set; }
    public string AppliedFormulaSummary { get; set; } = string.Empty;
    public Dictionary<string, string> AppliedTmdbQueryParameters { get; set; } = new();
    public List<MovieCardDto> Movies { get; set; } = new();
}
