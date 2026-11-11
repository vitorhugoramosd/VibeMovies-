using System.Globalization;
using System.Text;
using VibeMovies.Domain.Models;

namespace VibeMovies.Application.Catalog;

public static class TextNormalizer
{
    /// <summary>Minúsculas, sem acentos e com espaços simples — para comparar palavras em pt-BR.</summary>
    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var decomposed = text.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            sb.Append(char.IsWhiteSpace(c) || c == '_' ? ' ' : c);
        }

        var collapsed = sb.ToString().Normalize(NormalizationForm.FormC);
        while (collapsed.Contains("  ")) collapsed = collapsed.Replace("  ", " ");
        return collapsed.Trim();
    }
}

/// <summary>
/// Mapeamento dos streamings conhecidos no Brasil para os IDs de provedor do TMDB
/// (usados em <c>with_watch_providers</c> do /discover/movie).
/// </summary>
public static class ProviderCatalog
{
    public sealed record KnownProvider(string Name, IReadOnlyList<int> TmdbIds, IReadOnlyList<string> Aliases);

    public static readonly IReadOnlyList<KnownProvider> Known = new List<KnownProvider>
    {
        new("Netflix", new[] { 8 }, new[] { "netflix" }),
        new("Amazon Prime Video", new[] { 119 }, new[] { "prime video", "prime", "amazon", "amazon prime" }),
        new("Max", new[] { 1899, 384 }, new[] { "max", "hbo max", "hbo" }),
        new("Disney+", new[] { 337 }, new[] { "disney+", "disney plus", "disney" }),
        new("Star+", new[] { 619, 337 }, new[] { "star+", "star plus" }),
        new("Globoplay", new[] { 307 }, new[] { "globoplay", "globo play", "globo" }),
        new("Apple TV+", new[] { 350 }, new[] { "apple tv+", "apple tv plus", "appletv+" }),
        new("Paramount+", new[] { 531 }, new[] { "paramount+", "paramount plus", "paramount" }),
        new("MUBI", new[] { 11 }, new[] { "mubi" }),
        new("Claro tv+", new[] { 167 }, new[] { "claro tv+", "claro video", "claro" }),
        new("Crunchyroll", new[] { 283 }, new[] { "crunchyroll" })
    };

    /// <summary>Divide uma lista "Netflix,Max|Disney+" em nomes limpos (sem duplicatas).</summary>
    public static List<string> ParseList(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return new List<string>();
        return csv
            .Split(new[] { ',', '|', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => s.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static KnownProvider? Resolve(string? name)
    {
        var n = TextNormalizer.Normalize(name);
        if (n.Length == 0) return null;
        return Known.FirstOrDefault(k =>
            TextNormalizer.Normalize(k.Name) == n || k.Aliases.Any(a => TextNormalizer.Normalize(a) == n));
    }

    /// <summary>Nome canônico (ex.: "prime video" → "Amazon Prime Video"); desconhecidos são mantidos como vieram.</summary>
    public static string Canonicalize(string name) => Resolve(name)?.Name ?? name.Trim();

    public static List<int> GetTmdbIds(IEnumerable<string> names) =>
        names.Select(Resolve)
            .Where(k => k is not null)
            .SelectMany(k => k!.TmdbIds)
            .Distinct()
            .ToList();

    public static bool Matches(StreamingProviderBadgeDto badge, IEnumerable<string> filters)
    {
        var badgeName = TextNormalizer.Normalize(badge.ProviderName);
        foreach (var filter in filters)
        {
            var known = Resolve(filter);
            if (known is not null && badge.ProviderId != 0 && known.TmdbIds.Contains(badge.ProviderId))
                return true;

            var f = TextNormalizer.Normalize(known?.Name ?? filter);
            if (f.Length > 0 && badgeName.Contains(f)) return true;

            var raw = TextNormalizer.Normalize(filter);
            if (raw.Length > 0 && badgeName.Contains(raw)) return true;
        }

        return false;
    }

    /// <summary>true se algum provedor de assinatura (flatrate) do filme bate com a lista.</summary>
    public static bool IsAvailableOn(WatchProvidersDto providers, IReadOnlyCollection<string> filters) =>
        filters.Count > 0 && providers.Flatrate.Any(p => Matches(p, filters));
}
