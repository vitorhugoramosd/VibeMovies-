namespace VibeMovies.Application.Catalog;

/// <summary>Resultado do mapeamento de uma palavra livre (ex.: "terror") para vibe e/ou gênero do TMDB.</summary>
public sealed record VibeWordMatch(string? VibeId, int? GenreId, string Label)
{
    public bool IsEmpty => VibeId is null && GenreId is null;
}

/// <summary>
/// Converte o que o usuário fala para o assistente ("quero um terror", "algo de comédia", "plot-twist")
/// em um id de vibe do <see cref="VibeCatalog"/> ou em um gênero do TMDB.
/// </summary>
public static class VibeWordMapper
{
    // Termos normalizados (sem acento, minúsculos) → (vibe, gênero). A frase mais longa vence.
    private static readonly (string Term, string? VibeId, int? GenreId)[] Terms =
    {
        // Gêneros
        ("terror", null, 27), ("horror", null, 27), ("medo", null, 27), ("assustador", null, 27),
        ("comedia", null, 35), ("comedias", null, 35), ("engracado", null, 35), ("rir", null, 35), ("humor", null, 35),
        ("acao", null, 28), ("porradaria", null, 28), ("tiro", null, 28),
        ("suspense", null, 53), ("thriller", null, 53),
        ("romance", null, 10749), ("romantico", null, 10749), ("amor", null, 10749),
        ("ficcao", null, 878), ("ficcao cientifica", null, 878), ("sci-fi", null, 878), ("sci fi", null, 878), ("scifi", null, 878),
        ("drama", null, 18),
        ("animacao", null, 16), ("desenho", null, 16), ("anime", null, 16),
        ("aventura", null, 12),
        ("crime", null, 80), ("policial", null, 80), ("mafia", null, 80),
        ("misterio", null, 9648),
        ("fantasia", null, 14),

        // Vibes
        ("plot twist", "plot-twist", null), ("reviravolta", "plot-twist", null), ("final surpreendente", "plot-twist", null),
        ("cyberpunk", "cyberpunk", null), ("neon", "cyberpunk", null), ("distopia", "cyberpunk", null),
        ("pos apocaliptico", "pos-apocaliptico", null), ("pos-apocaliptico", "pos-apocaliptico", null), ("apocalipse", "pos-apocaliptico", null), ("zumbi", "pos-apocaliptico", null), ("fim do mundo", "pos-apocaliptico", null),
        ("vinganca", "vinganca", null),
        ("anos 90", "anos-90", null), ("anos noventa", "anos-90", null), ("noventa", "anos-90", null), ("nostalgia", "anos-90", null),
        ("emocionante", "emocionante", null), ("chorar", "emocionante", null), ("triste", "emocionante", null), ("emocao", "emocionante", null),
        ("mind bending", "mind-bending", null), ("mind-bending", "mind-bending", null), ("viagem no tempo", "mind-bending", null), ("paradoxo", "mind-bending", null),
        ("terror psicologico", "terror-psicologico", null), ("psicologico", "terror-psicologico", null), ("tensao", "terror-psicologico", null),
        ("noir", "noite-chuvosa", null), ("neo-noir", "noite-chuvosa", null), ("detetive", "noite-chuvosa", null), ("investigacao", "noite-chuvosa", null), ("noite chuvosa", "noite-chuvosa", null), ("chuva", "noite-chuvosa", null),
        ("leve", "feel-good", null), ("feel good", "feel-good", null), ("feel-good", "feel-good", null), ("alegre", "feel-good", null), ("feliz", "feel-good", null), ("relaxar", "feel-good", null), ("alto astral", "feel-good", null)
    };

    public static VibeWordMatch Map(string? word)
    {
        var normalized = TextNormalizer.Normalize(word);
        if (normalized.Length == 0) return new VibeWordMatch(null, null, string.Empty);

        // 1) id exato de vibe ("plot-twist", "anos-90"...)
        var vibe = VibeCatalog.AllVibes.FirstOrDefault(v =>
            string.Equals(v.Id, normalized, StringComparison.OrdinalIgnoreCase) ||
            TextNormalizer.Normalize(v.Name) == normalized);
        if (vibe is not null) return new VibeWordMatch(vibe.Id, null, vibe.Name);

        // 2) nome exato de gênero ("Ficção Científica", "Suspense / Thriller")
        var genre = VibeCatalog.AllGenres.FirstOrDefault(g => TextNormalizer.Normalize(g.Name) == normalized);
        if (genre is not null) return new VibeWordMatch(null, genre.Id, genre.Name);

        // 3) termos conhecidos, procurando como palavra/frase inteira dentro do texto (a mais longa vence)
        var padded = $" {normalized.Replace('/', ' ').Replace(',', ' ').Replace('.', ' ').Replace('!', ' ').Replace('?', ' ')} ";
        var best = Terms
            .Where(t => padded.Contains($" {t.Term} ", StringComparison.Ordinal))
            .OrderByDescending(t => t.Term.Length)
            .Select(t => ((string? VibeId, int? GenreId)?)(t.VibeId, t.GenreId))
            .FirstOrDefault();

        if (best is { } hit)
        {
            if (hit.VibeId is not null)
            {
                var v = VibeCatalog.GetById(hit.VibeId);
                return new VibeWordMatch(hit.VibeId, null, v?.Name ?? hit.VibeId);
            }

            return new VibeWordMatch(null, hit.GenreId, VibeCatalog.GetGenreName(hit.GenreId!.Value));
        }

        return new VibeWordMatch(null, null, string.Empty);
    }
}
