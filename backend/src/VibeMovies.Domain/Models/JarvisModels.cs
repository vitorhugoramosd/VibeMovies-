namespace VibeMovies.Domain.Models;

// Contrato da integração com o assistente Jarvis (rotas /api/jarvis). Serializado em camelCase.

public class JarvisSummaryDto
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public bool UsingLiveTmdb { get; set; }
    public JarvisUserDto User { get; set; } = new();
    public MovieCardDto? TonightPick { get; set; }
    public string TonightReason { get; set; } = string.Empty;
    public string TonightSpokenText { get; set; } = string.Empty;
    public List<JarvisWantToWatchDto> WantToWatch { get; set; } = new();
    public List<JarvisNowPlayingDto> NowPlaying { get; set; } = new();
    public bool IsSimulatedSchedule { get; set; }
    public WatchlistStatsDto Stats { get; set; } = new();
}

public class JarvisUserDto
{
    public string Name { get; set; } = string.Empty;
    public string DefaultCity { get; set; } = string.Empty;
}

public class JarvisWantToWatchDto
{
    public int TmdbId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string PosterUrl { get; set; } = string.Empty;
    public int? ReleaseYear { get; set; }
    public int? RuntimeMinutes { get; set; }
    public List<string> Providers { get; set; } = new();
}

public class JarvisNowPlayingDto
{
    public int TmdbId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string PosterUrl { get; set; } = string.Empty;
    public double VoteAverage { get; set; }
    public string AgeRating { get; set; } = string.Empty;
    public List<JarvisSessionDto> NextSessions { get; set; } = new();
}

public class JarvisSessionDto
{
    public string Cinema { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
    public string Audio { get; set; } = string.Empty;
}

public class JarvisVibeDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Emoji { get; set; } = string.Empty;
}

public class JarvisAddToWatchlistRequestDto
{
    public int TmdbId { get; set; }

    /// <summary>"want" ou "watched".</summary>
    public string List { get; set; } = "want";

    public int? Rating { get; set; }
}

/// <summary>Parâmetros do "Me surpreenda".</summary>
public class SurpriseRequest
{
    public List<string> VibeIds { get; set; } = new();
    public List<int> GenreIds { get; set; } = new();
    public int? MaxRuntimeMinutes { get; set; }
    public string? ProviderFilter { get; set; }
    public HashSet<int> ExcludeIds { get; set; } = new();
    public string Region { get; set; } = "BR";

    /// <summary>Streamings preferidos do usuário (usados só para escolher qual citar na frase).</summary>
    public List<string> PreferredProviders { get; set; } = new();
}
