using VibeMovies.Application.Catalog;
using VibeMovies.Domain.Entities;
using VibeMovies.Domain.Models;

namespace VibeMovies.Application.Services;

public static class WatchlistStatsCalculator
{
    private static readonly string[] PtBrMonths =
        { "jan", "fev", "mar", "abr", "mai", "jun", "jul", "ago", "set", "out", "nov", "dez" };

    public static List<string> SplitGenres(string? csv) =>
        (csv ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(g => g.Length > 0)
            .ToList();

    /// <summary>Converte nomes de gênero salvos no item ("Suspense / Thriller, Drama") para ids do TMDB.</summary>
    public static List<int> GenreIdsFromCsv(string? csv) =>
        SplitGenres(csv)
            .Select(name => VibeCatalog.AllGenres.FirstOrDefault(g =>
                TextNormalizer.Normalize(g.Name) == TextNormalizer.Normalize(name) ||
                TextNormalizer.Normalize(g.Name).StartsWith(TextNormalizer.Normalize(name) + " ", StringComparison.Ordinal)))
            .Where(g => g is not null)
            .Select(g => g!.Id)
            .Distinct()
            .ToList();

    public static WatchlistStatsDto Compute(IReadOnlyCollection<WatchlistItem> all, TimeProvider? timeProvider = null)
    {
        timeProvider ??= TimeProvider.System;
        var tz = timeProvider.LocalTimeZone;
        var nowLocal = timeProvider.GetLocalNow();

        var wantToWatch = all.Where(x => x.ListType == WatchlistType.WantToWatch).ToList();
        var watched = all.Where(x => x.ListType == WatchlistType.Watched).ToList();

        var rated = watched.Where(x => x.PersonalRating is >= 1 and <= 5).ToList();
        var avgRating = rated.Count > 0 ? Math.Round(rated.Average(x => x.PersonalRating!.Value), 1) : 0.0;

        var genreCounts = all
            .SelectMany(x => SplitGenres(x.GenresCsv))
            .GroupBy(g => g, StringComparer.OrdinalIgnoreCase)
            .Select(g => new NamedCountDto(g.First(), g.Count()))
            .OrderByDescending(g => g.Count)
            .ThenBy(g => g.Name, StringComparer.Ordinal)
            .ToList();

        var ratingDistribution = Enumerable.Range(1, 5).ToDictionary(i => i, _ => 0);
        foreach (var r in rated) ratingDistribution[r.PersonalRating!.Value]++;

        // Últimos 12 meses (inclui o mês atual), do mais antigo para o mais recente
        var firstMonth = new DateTime(nowLocal.Year, nowLocal.Month, 1).AddMonths(-11);
        var perMonth = new List<NamedCountDto>();
        var watchedLocalMonths = watched
            .Where(w => w.WatchedAt.HasValue)
            .Select(w =>
            {
                var utc = w.WatchedAt!.Value.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(w.WatchedAt.Value, DateTimeKind.Utc)
                    : w.WatchedAt.Value.ToUniversalTime();
                var local = TimeZoneInfo.ConvertTimeFromUtc(utc, tz);
                return (local.Year, local.Month);
            })
            .ToList();

        for (var i = 0; i < 12; i++)
        {
            var month = firstMonth.AddMonths(i);
            var label = $"{PtBrMonths[month.Month - 1]}/{month:yy}";
            perMonth.Add(new NamedCountDto(label, watchedLocalMonths.Count(m => m.Year == month.Year && m.Month == month.Month)));
        }

        var knownRuntime = watched.Where(x => x.RuntimeMinutes is > 0).ToList();
        var totalMinutes = knownRuntime.Sum(x => x.RuntimeMinutes!.Value);

        return new WatchlistStatsDto
        {
            TotalWantToWatch = wantToWatch.Count,
            TotalWatched = watched.Count,
            TotalMinutesWatched = totalMinutes,
            TotalHoursWatched = (int)Math.Round(totalMinutes / 60.0, MidpointRounding.AwayFromZero),
            WatchedWithUnknownRuntime = watched.Count - knownRuntime.Count,
            AveragePersonalRating = avgRating,
            FavoriteGenre = genreCounts.FirstOrDefault()?.Name ?? "Ainda sem dados",
            GenreBreakdown = genreCounts.Take(8).ToList(),
            RatingDistribution = ratingDistribution,
            WatchedPerMonth = perMonth
        };
    }
}
