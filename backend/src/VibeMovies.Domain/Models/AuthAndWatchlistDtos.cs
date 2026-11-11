using System.ComponentModel.DataAnnotations;
using VibeMovies.Domain.Entities;

namespace VibeMovies.Domain.Models;

public static class ValidationRules
{
    public const string EmailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
    public const int MinPasswordLength = 6;
    public const int MaxNotesLength = 2000;
}

public record RegisterRequestDto(
    [Required(AllowEmptyStrings = false, ErrorMessage = "O nome é obrigatório.")]
    [StringLength(120, ErrorMessage = "O nome deve ter no máximo 120 caracteres.")]
    string Name,

    [Required(AllowEmptyStrings = false, ErrorMessage = "O e-mail é obrigatório.")]
    [RegularExpression(ValidationRules.EmailPattern, ErrorMessage = "Informe um e-mail válido.")]
    [StringLength(180, ErrorMessage = "O e-mail deve ter no máximo 180 caracteres.")]
    string Email,

    [Required(AllowEmptyStrings = false, ErrorMessage = "A senha é obrigatória.")]
    [MinLength(ValidationRules.MinPasswordLength, ErrorMessage = "A senha deve ter pelo menos 6 caracteres.")]
    string Password,

    string? PreferredRegion = "BR",
    string? DefaultCity = "Joinville/SC"
);

public record LoginRequestDto(
    [Required(AllowEmptyStrings = false, ErrorMessage = "O e-mail é obrigatório.")]
    string Email,

    [Required(AllowEmptyStrings = false, ErrorMessage = "A senha é obrigatória.")]
    string Password
);

public record AuthResponseDto(
    string Token,
    Guid UserId,
    string Name,
    string Email,
    string PreferredRegion,
    string DefaultCity
);

/// <summary>Atualização do perfil (PUT /api/auth/me). Campos nulos são mantidos.</summary>
public record UpdateProfileRequestDto(
    [StringLength(120, MinimumLength = 1, ErrorMessage = "O nome deve ter entre 1 e 120 caracteres.")]
    string? Name,

    [StringLength(100, ErrorMessage = "A cidade deve ter no máximo 100 caracteres.")]
    string? DefaultCity,

    [StringLength(10, MinimumLength = 2, ErrorMessage = "A região deve ter entre 2 e 10 caracteres (ex.: BR).")]
    string? PreferredRegion,

    List<string>? MyProviders
);

public class UserProfileDto
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PreferredRegion { get; set; } = "BR";
    public string DefaultCity { get; set; } = string.Empty;
    public List<string> MyProviders { get; set; } = new();
}

public record UpsertWatchlistItemDto(
    int TmdbMovieId,
    [Required(AllowEmptyStrings = false, ErrorMessage = "O título é obrigatório.")]
    string Title,
    string? OriginalTitle,
    string? PosterPath,
    string? BackdropPath,
    int? ReleaseYear,
    string? Overview,
    WatchlistType ListType,
    [Range(1, 5, ErrorMessage = "A nota deve ser de 1 a 5 estrelas.")]
    int? PersonalRating,
    [StringLength(ValidationRules.MaxNotesLength, ErrorMessage = "As notas pessoais devem ter no máximo 2000 caracteres.")]
    string? PersonalNotes,
    DateTime? WatchedAt,
    string? GenresCsv,
    int? RuntimeMinutes,
    double TmdbVoteAverage
);

public record UpdateWatchlistItemDto(
    WatchlistType ListType,
    [Range(1, 5, ErrorMessage = "A nota deve ser de 1 a 5 estrelas.")]
    int? PersonalRating,
    [StringLength(ValidationRules.MaxNotesLength, ErrorMessage = "As notas pessoais devem ter no máximo 2000 caracteres.")]
    string? PersonalNotes,
    DateTime? WatchedAt
);

public class NamedCountDto
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }

    public NamedCountDto() { }
    public NamedCountDto(string name, int count) { Name = name; Count = count; }
}

public class WatchlistStatsDto
{
    public int TotalWantToWatch { get; set; }
    public int TotalWatched { get; set; }

    /// <summary>Soma das durações conhecidas dos filmes assistidos (filmes sem duração não entram).</summary>
    public int TotalMinutesWatched { get; set; }
    public double AveragePersonalRating { get; set; }
    public string FavoriteGenre { get; set; } = "Ainda sem dados";

    /// <summary>Top 8 gêneros (todas as listas).</summary>
    public List<NamedCountDto> GenreBreakdown { get; set; } = new();

    /// <summary>Distribuição das notas pessoais dos assistidos; chaves 1..5 sempre presentes.</summary>
    public Dictionary<int, int> RatingDistribution { get; set; } = new();

    /// <summary>Filmes assistidos por mês nos últimos 12 meses (mais antigo primeiro), rótulo "MMM/yy" pt-BR.</summary>
    public List<NamedCountDto> WatchedPerMonth { get; set; } = new();

    public int TotalHoursWatched { get; set; }

    /// <summary>Quantos assistidos ficaram fora da soma de minutos por não terem duração conhecida.</summary>
    public int WatchedWithUnknownRuntime { get; set; }
}
