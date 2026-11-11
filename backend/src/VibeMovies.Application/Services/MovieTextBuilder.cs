using System.Globalization;
using VibeMovies.Application.Catalog;
using VibeMovies.Domain.Models;

namespace VibeMovies.Application.Services;

/// <summary>Monta textos em pt-BR (explicação e frase falada) a partir de um filme.</summary>
public static class MovieTextBuilder
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    private static readonly Dictionary<string, string> SpokenVibePhrases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["plot-twist"] = "com plot twist",
        ["cyberpunk"] = "com clima cyberpunk",
        ["pos-apocaliptico"] = "pós-apocalíptico",
        ["vinganca"] = "de vingança",
        ["anos-90"] = "clássico dos anos 90",
        ["emocionante"] = "daqueles emocionantes",
        ["mind-bending"] = "de dar nó na cabeça",
        ["terror-psicologico"] = "com tensão psicológica",
        ["noite-chuvosa"] = "no clima neo-noir",
        ["feel-good"] = "leve e alto astral"
    };

    public static string FormatRating(double vote) => vote.ToString("0.0", PtBr);

    /// <summary>Escolhe qual streaming citar: primeiro um que o usuário assina, depois o de maior prioridade.</summary>
    public static StreamingProviderBadgeDto? PickFlatrate(MovieCardDto movie, IReadOnlyCollection<string> preferred)
    {
        var flat = movie.WatchProviders?.Flatrate ?? new List<StreamingProviderBadgeDto>();
        if (preferred.Count > 0)
        {
            var mine = flat.FirstOrDefault(p => ProviderCatalog.Matches(p, preferred));
            if (mine is not null) return mine;
        }

        return flat.OrderBy(p => p.DisplayPriority).FirstOrDefault();
    }

    public static string SpokenProviderName(string providerName)
    {
        var canonical = ProviderCatalog.Canonicalize(providerName);
        return canonical == "Amazon Prime Video" ? "Prime Video" : canonical;
    }

    private static string Preposition(string spokenName) =>
        string.Equals(spokenName, "Netflix", StringComparison.OrdinalIgnoreCase) ? "na" : "no";

    public static string ProviderSpokenPhrase(MovieCardDto movie, IReadOnlyCollection<string> preferred)
    {
        var flat = PickFlatrate(movie, preferred);
        if (flat is not null)
        {
            var name = SpokenProviderName(flat.ProviderName);
            return $"tem {Preposition(name)} {name}";
        }

        var rent = movie.WatchProviders?.Rent.OrderBy(p => p.DisplayPriority).FirstOrDefault();
        if (rent is not null)
        {
            var name = SpokenProviderName(rent.ProviderName);
            return $"dá pra alugar {Preposition(name)} {name}";
        }

        return "não achei em streaming por assinatura agora";
    }

    public static string ShortGenre(MovieCardDto movie)
    {
        var g = movie.Genres.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(g)) return "Filme";
        var first = g.Split('/', StringSplitOptions.TrimEntries)[0];
        return first.Length == 0 ? "Filme" : first;
    }

    public static VibeDefinition? MainVibe(MovieCardDto movie, IReadOnlyCollection<VibeDefinition> requestedVibes)
    {
        var requested = requestedVibes.FirstOrDefault(v => movie.MatchedVibes.Contains(v.Id, StringComparer.OrdinalIgnoreCase));
        if (requested is not null) return requested;
        return movie.MatchedVibes.Select(VibeCatalog.GetById).FirstOrDefault(v => v is not null);
    }

    /// <summary>Ex.: "Que tal Ilha do Medo, de 2010? Suspense com plot twist, nota 8,2, tem na Netflix."</summary>
    public static string BuildSpokenText(
        MovieCardDto movie,
        IReadOnlyCollection<VibeDefinition> requestedVibes,
        IReadOnlyCollection<string> preferredProviders,
        string opening = "Que tal")
    {
        var head = movie.ReleaseYear.HasValue
            ? $"{opening} {movie.Title}, de {movie.ReleaseYear}?"
            : $"{opening} {movie.Title}?";

        var vibe = MainVibe(movie, requestedVibes);
        var descriptor = ShortGenre(movie);
        if (vibe is not null && SpokenVibePhrases.TryGetValue(vibe.Id, out var phrase))
            descriptor = $"{descriptor} {phrase}";

        var parts = new List<string> { descriptor };
        if (movie.VoteAverage > 0) parts.Add($"nota {FormatRating(movie.VoteAverage)}");
        parts.Add(ProviderSpokenPhrase(movie, preferredProviders));

        return $"{head} {string.Join(", ", parts)}.";
    }

    /// <summary>Explicação detalhada (vibe, nota, duração, onde assistir).</summary>
    public static string BuildReason(
        MovieCardDto movie,
        IReadOnlyCollection<VibeDefinition> requestedVibes,
        IReadOnlyCollection<int> requestedGenres,
        int? maxRuntime,
        IReadOnlyCollection<string> preferredProviders,
        string? prefix = null)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(prefix)) parts.Add(prefix!);

        var matched = requestedVibes.Where(v => movie.MatchedVibes.Contains(v.Id, StringComparer.OrdinalIgnoreCase)).ToList();
        if (matched.Count > 0)
        {
            parts.Add($"Combina com a vibe {string.Join(" + ", matched.Select(v => $"{v.Name} {v.Emoji}"))}");
        }
        else if (requestedVibes.Count > 0)
        {
            parts.Add($"Escolhido para a vibe {string.Join(" + ", requestedVibes.Select(v => $"{v.Name} {v.Emoji}"))}");
        }
        else
        {
            var own = MainVibe(movie, requestedVibes);
            if (own is not null) parts.Add($"Vibe {own.Name} {own.Emoji}");
        }

        var genreHits = movie.GenreIds.Where(requestedGenres.Contains).Select(VibeCatalog.GetGenreName).ToList();
        if (genreHits.Count > 0)
            parts.Add($"gênero {string.Join(", ", genreHits)}");
        else if (movie.Genres.Count > 0 && parts.Count <= 1)
            parts.Add(string.Join(", ", movie.Genres.Take(2)));

        if (movie.VoteAverage > 0)
        {
            var votes = movie.VoteCount > 0 ? $" ({movie.VoteCount.ToString("N0", PtBr)} votos)" : "";
            parts.Add($"nota {FormatRating(movie.VoteAverage)} no TMDB{votes}");
        }

        if (movie.RuntimeMinutes is > 0)
        {
            parts.Add(maxRuntime is > 0
                ? $"{movie.RuntimeMinutes} min, dentro do limite de {maxRuntime} min"
                : $"{movie.RuntimeMinutes} min");
        }

        var flat = movie.WatchProviders?.Flatrate ?? new List<StreamingProviderBadgeDto>();
        if (flat.Count > 0)
        {
            var mine = preferredProviders.Count > 0 ? flat.Where(p => ProviderCatalog.Matches(p, preferredProviders)).ToList() : new();
            var names = (mine.Count > 0 ? mine : flat).Select(p => p.ProviderName).Distinct().Take(3).ToList();
            parts.Add(mine.Count > 0
                ? $"disponível por assinatura em {JoinPt(names)} (que você assina)"
                : $"disponível por assinatura em {JoinPt(names)}");
        }
        else if (movie.WatchProviders?.Rent.Count > 0)
        {
            parts.Add($"para alugar em {JoinPt(movie.WatchProviders.Rent.Select(p => p.ProviderName).Distinct().Take(2).ToList())}");
        }
        else
        {
            parts.Add("sem streaming por assinatura conhecido no momento");
        }

        return string.Join(" • ", parts) + ".";
    }

    public static string JoinPt(IReadOnlyList<string> items) => items.Count switch
    {
        0 => string.Empty,
        1 => items[0],
        _ => string.Join(", ", items.Take(items.Count - 1)) + " e " + items[^1]
    };
}
