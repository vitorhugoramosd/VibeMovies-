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

    /// <summary>Chave do vídeo no YouTube (trailer pt-BR preferido, depois en-US). Nulo quando desconhecido.</summary>
    public string? TrailerYoutubeKey { get; set; }

    /// <summary>Frase de efeito (tagline) do TMDB.</summary>
    public string? Tagline { get; set; }

    /// <summary>Motivo da recomendação personalizada ("Para você"). Nulo fora desse contexto.</summary>
    public string? RecommendationReason { get; set; }

    /// <summary>Cópia profunda — evita que filtros/pontuações alterem objetos compartilhados (ex.: catálogo curado estático).</summary>
    public MovieCardDto Clone() => new()
    {
        TmdbId = TmdbId,
        Title = Title,
        OriginalTitle = OriginalTitle,
        Overview = Overview,
        PosterUrl = PosterUrl,
        BackdropUrl = BackdropUrl,
        ReleaseDate = ReleaseDate,
        ReleaseYear = ReleaseYear,
        RuntimeMinutes = RuntimeMinutes,
        VoteAverage = VoteAverage,
        VoteCount = VoteCount,
        Genres = new List<string>(Genres),
        GenreIds = new List<int>(GenreIds),
        MatchedVibes = new List<string>(MatchedVibes),
        Keywords = new List<string>(Keywords),
        Director = Director,
        Cast = new List<string>(Cast),
        WatchProviders = WatchProviders.Clone(),
        VibeMatchScore = VibeMatchScore,
        TrailerYoutubeKey = TrailerYoutubeKey,
        Tagline = Tagline,
        RecommendationReason = RecommendationReason
    };
}

public class WatchProvidersDto
{
    public string Region { get; set; } = "BR";
    public string? TmdbWatchLink { get; set; }
    public List<StreamingProviderBadgeDto> Flatrate { get; set; } = new(); // Subscription (Netflix, Prime, Max, etc.)
    public List<StreamingProviderBadgeDto> Rent { get; set; } = new();     // Aluguel (Apple TV, Google Play, Prime Store)
    public List<StreamingProviderBadgeDto> Buy { get; set; } = new();      // Compra

    public WatchProvidersDto Clone() => new()
    {
        Region = Region,
        TmdbWatchLink = TmdbWatchLink,
        Flatrate = Flatrate.Select(p => p.Clone()).ToList(),
        Rent = Rent.Select(p => p.Clone()).ToList(),
        Buy = Buy.Select(p => p.Clone()).ToList()
    };
}

public class StreamingProviderBadgeDto
{
    public int ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    public int DisplayPriority { get; set; }

    public StreamingProviderBadgeDto Clone() => new()
    {
        ProviderId = ProviderId,
        ProviderName = ProviderName,
        LogoUrl = LogoUrl,
        DisplayPriority = DisplayPriority
    };
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

/// <summary>Sugestão aleatória ponderada ("Me surpreenda").</summary>
public class SurprisePickDto
{
    public MovieCardDto Movie { get; set; } = new();

    /// <summary>Explicação em pt-BR do porquê da escolha (vibe, nota, duração, onde assistir).</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>Frase curta e natural para um assistente de voz ler em voz alta.</summary>
    public string SpokenText { get; set; } = string.Empty;
}
