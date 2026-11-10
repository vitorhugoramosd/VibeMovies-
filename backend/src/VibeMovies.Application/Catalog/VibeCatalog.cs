using VibeMovies.Domain.Models;

namespace VibeMovies.Application.Catalog;

/// <summary>
/// Motor de Mapeamento de "Vibes" para Keywords IDs oficiais do TMDB, Gêneros e Filtros de Metadados.
/// Substitui o uso de LLMs pagas com heurísticas determinísticas e cruzamento de metadados do endpoint /discover/movie.
/// </summary>
public static class VibeCatalog
{
    public static readonly IReadOnlyList<VibeDefinition> AllVibes = new List<VibeDefinition>
    {
        new()
        {
            Id = "plot-twist",
            Name = "Plot Twist Chocante",
            Subtitle = "Nada é o que parece até o último minuto",
            Description = "Filmes com reviravoltas imprevisíveis, narradores não-confiáveis e finais de explodir a cabeça.",
            Emoji = "🌀",
            AccentColor = "#A855F7",
            // TMDB Keywords: 4344 (plot twist), 10085 (betrayal), 9826 (murder investigation), 1566 (dream/illusion)
            TmdbKeywordIds = new() { 4344, 10085, 9826 },
            SuggestedGenreIds = new() { 53, 9648 }, // Thriller, Mystery
            MinVoteAverage = 7.0,
            SampleTags = new() { "Reviravolta", "Mistério", "Final Surpreendente" }
        },
        new()
        {
            Id = "cyberpunk",
            Name = "Cyberpunk & Neon",
            Subtitle = "Alta tecnologia, baixa qualidade de vida",
            Description = "Futuros distópicos banhados em neon, inteligência artificial, megacorporações e implantes cibernéticos.",
            Emoji = "🦾",
            AccentColor = "#06B6D4",
            // TMDB Keywords: 12190 (cyberpunk), 310 (artificial intelligence), 4565 (dystopia), 10685 (cyborg)
            TmdbKeywordIds = new() { 12190, 310, 4565, 10685 },
            SuggestedGenreIds = new() { 878, 28 }, // Sci-Fi, Action
            MinVoteAverage = 6.5,
            SampleTags = new() { "Distopia", "IA", "Futuro", "Sci-Fi" }
        },
        new()
        {
            Id = "pos-apocaliptico",
            Name = "Pós-Apocalíptico",
            Subtitle = "Sobrevivência no fim da civilização",
            Description = "Mundos devastados, recursos escassos, desertos implacáveis e a luta humana para recomeçar.",
            Emoji = "☢️",
            AccentColor = "#F97316",
            // TMDB Keywords: 4458 (post-apocalyptic future), 12332 (apocalypse), 10349 (survival)
            TmdbKeywordIds = new() { 4458, 12332, 10349 },
            SuggestedGenreIds = new() { 878, 12, 28 },
            MinVoteAverage = 6.4,
            SampleTags = new() { "Sobrevivência", "Colapso", "Deserto" }
        },
        new()
        {
            Id = "vinganca",
            Name = "Sede de Vingança",
            Subtitle = "Um acerto de contas implacável",
            Description = "Protagonistas levados ao limite em jornadas intensas de justiça com as próprias mãos.",
            Emoji = "🗡️",
            AccentColor = "#EF4444",
            // TMDB Keywords: 9748 (revenge), 14604 (vigilante), 6149 (police corruption)
            TmdbKeywordIds = new() { 9748, 14604 },
            SuggestedGenreIds = new() { 28, 80, 53 }, // Action, Crime, Thriller
            MinVoteAverage = 6.8,
            SampleTags = new() { "Vingança", "Adrenalina", "Justiça" }
        },
        new()
        {
            Id = "anos-90",
            Name = "Nostalgia Anos 90",
            Subtitle = "A era de ouro do cinema cult e locadoras",
            Description = "Clássicos lançados entre 1990 e 1999 que definiram uma geração com estética analógica inesquecível.",
            Emoji = "📼",
            AccentColor = "#EAB308",
            TmdbKeywordIds = new() { 207317 },
            SuggestedGenreIds = new() { 80, 18, 53 },
            ReleaseDateGte = "1990-01-01",
            ReleaseDateLte = "1999-12-31",
            MinVoteAverage = 7.2,
            SampleTags = new() { "1990-1999", "Cult", "Clássico VHS" }
        },
        new()
        {
            Id = "emocionante",
            Name = "Emocionante & Catártico",
            Subtitle = "Prepare o lenço para histórias profundas",
            Description = "Dramas humanos poderosos sobre laços familiares, superação, memória e momentos que tocam a alma.",
            Emoji = "🥹",
            AccentColor = "#EC4899",
            // TMDB Keywords: 6054 (friendship), 10683 (coming of age), 9673 (love), 18035 (family)
            TmdbKeywordIds = new() { 6054, 10683, 18035 },
            SuggestedGenreIds = new() { 18, 10749 }, // Drama, Romance
            MinVoteAverage = 7.4,
            SampleTags = new() { "Drama", "Superação", "Laços Humanos" }
        },
        new()
        {
            Id = "mind-bending",
            Name = "Mind-Bending & Paradoxos",
            Subtitle = "Viagens no tempo, multiversos e sonhos",
            Description = "Narrativas não-lineares, física quântica, realidades paralelas e quebra-cabeças mentais.",
            Emoji = "🧠",
            AccentColor = "#6366F1",
            // TMDB Keywords: 4379 (time travel), 1566 (dream), 9882 (space), 310 (AI)
            TmdbKeywordIds = new() { 4379, 1566, 9882 },
            SuggestedGenreIds = new() { 878, 9648 },
            MinVoteAverage = 7.2,
            SampleTags = new() { "Viagem no Tempo", "Paradoxo", "Sci-Fi Mental" }
        },
        new()
        {
            Id = "terror-psicologico",
            Name = "Tensão Psicológica",
            Subtitle = "Medo atmosférico sem sustos baratos",
            Description = "Paranoia, isolamento, atmosfera densa e terror que permanece na mente dias após os créditos.",
            Emoji = "🕯️",
            AccentColor = "#10B981",
            // TMDB Keywords: 12565 (psychological thriller), 256183 (folk horror), 3358 (haunted house)
            TmdbKeywordIds = new() { 12565, 256183, 3358 },
            SuggestedGenreIds = new() { 27, 53 }, // Horror, Thriller
            MinVoteAverage = 6.6,
            SampleTags = new() { "Suspense", "Atmosférico", "Psicológico" }
        },
        new()
        {
            Id = "noite-chuvosa",
            Name = "Noite Chuvosa & Neo-Noir",
            Subtitle = "Detetives, sombras e mistérios urbanos",
            Description = "Crimes complexos, investigações noturnas sob chuva e moralidade cinzenta nas grandes metrópoles.",
            Emoji = "🌧️",
            AccentColor = "#3B82F6",
            // TMDB Keywords: 10714 (serial killer), 9826 (murder), 703 (detective), 207268 (neo-noir)
            TmdbKeywordIds = new() { 10714, 703, 207268 },
            SuggestedGenreIds = new() { 80, 9648, 53 },
            MinVoteAverage = 7.1,
            SampleTags = new() { "Neo-Noir", "Investigação", "Detetive" }
        },
        new()
        {
            Id = "feel-good",
            Name = "Feel-Good & Leve",
            Subtitle = "Para terminar o dia com um sorriso no rosto",
            Description = "Comédias inteligentes, aventuras acolhedoras e histórias alto-astral perfeitas para relaxar.",
            Emoji = "☀️",
            AccentColor = "#F59E0B",
            // TMDB Keywords: 9713 (friends), 167541 (feel-good), 6054 (friendship)
            TmdbKeywordIds = new() { 9713, 6054 },
            SuggestedGenreIds = new() { 35, 12, 10751 }, // Comedy, Adventure, Family
            MinVoteAverage = 7.0,
            MaxRuntimeMinutes = 125,
            SampleTags = new() { "Conforto", "Humor", "Alto Astral" }
        }
    };

    public static readonly IReadOnlyList<GenreDto> AllGenres = new List<GenreDto>
    {
        new() { Id = 28, Name = "Ação", Icon = "💥" },
        new() { Id = 12, Name = "Aventura", Icon = "🧭" },
        new() { Id = 16, Name = "Animação", Icon = "🎨" },
        new() { Id = 35, Name = "Comédia", Icon = "😄" },
        new() { Id = 80, Name = "Crime", Icon = "🕵️" },
        new() { Id = 18, Name = "Drama", Icon = "🎭" },
        new() { Id = 14, Name = "Fantasia", Icon = "✨" },
        new() { Id = 27, Name = "Terror", Icon = "👻" },
        new() { Id = 9648, Name = "Mistério", Icon = "🔍" },
        new() { Id = 10749, Name = "Romance", Icon = "❤️" },
        new() { Id = 878, Name = "Ficção Científica", Icon = "🚀" },
        new() { Id = 53, Name = "Suspense / Thriller", Icon = "⚡" }
    };

    public static VibeDefinition? GetById(string id) =>
        AllVibes.FirstOrDefault(v => string.Equals(v.Id, id, StringComparison.OrdinalIgnoreCase));

    public static string GetGenreName(int id) =>
        AllGenres.FirstOrDefault(g => g.Id == id)?.Name ?? "Cinema";
}
