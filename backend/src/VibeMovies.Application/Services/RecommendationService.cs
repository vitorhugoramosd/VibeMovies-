using VibeMovies.Application.Catalog;
using VibeMovies.Application.Interfaces;
using VibeMovies.Domain.Entities;
using VibeMovies.Domain.Models;

namespace VibeMovies.Application.Services;

/// <summary>"Para você": recomendações derivadas dos assistidos bem avaliados (≥4) e do Quero Ver.</summary>
public class RecommendationService
{
    private readonly ITmdbService _tmdb;
    private readonly IWatchlistRepository _watchlist;

    public RecommendationService(ITmdbService tmdb, IWatchlistRepository watchlist)
    {
        _tmdb = tmdb;
        _watchlist = watchlist;
    }

    public sealed record TasteProfile(
        Dictionary<int, double> GenreWeights,
        List<VibeDefinition> TopVibes,
        List<WatchlistItem> Seeds);

    public static TasteProfile BuildTasteProfile(IReadOnlyCollection<WatchlistItem> items)
    {
        var seeds = items
            .Where(i => (i.ListType == WatchlistType.Watched && i.PersonalRating >= 4) || i.ListType == WatchlistType.WantToWatch)
            .OrderByDescending(SeedWeight)
            .ThenByDescending(i => i.WatchedAt ?? i.AddedAt)
            .ToList();

        var genreWeights = new Dictionary<int, double>();
        foreach (var seed in seeds)
        {
            foreach (var gid in WatchlistStatsCalculator.GenreIdsFromCsv(seed.GenresCsv))
                genreWeights[gid] = genreWeights.GetValueOrDefault(gid) + SeedWeight(seed);
        }

        var topVibes = VibeCatalog.AllVibes
            .Select(v => (Vibe: v, Score: v.SuggestedGenreIds.Sum(g => genreWeights.GetValueOrDefault(g))))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(2)
            .Select(x => x.Vibe)
            .ToList();

        return new TasteProfile(genreWeights, topVibes, seeds);
    }

    private static double SeedWeight(WatchlistItem i) => i.ListType == WatchlistType.Watched
        ? (i.PersonalRating >= 5 ? 3.0 : 2.0)
        : 1.0;

    public async Task<List<MovieCardDto>> GetForUserAsync(Guid userId, string region = "BR", string? runtimeApiKey = null, int take = 12, CancellationToken ct = default)
    {
        var items = await _watchlist.GetByUserIdAsync(userId, null, ct);
        return await GetForItemsAsync(items, region, runtimeApiKey, take, ct);
    }

    public async Task<List<MovieCardDto>> GetForItemsAsync(IReadOnlyCollection<WatchlistItem> items, string region = "BR", string? runtimeApiKey = null, int take = 12, CancellationToken ct = default)
    {
        var excluded = items.Select(i => i.TmdbMovieId).ToHashSet();
        var profile = BuildTasteProfile(items);
        var candidates = new Dictionary<int, MovieCardDto>();

        void Add(IEnumerable<MovieCardDto> movies, Func<MovieCardDto, string> reason)
        {
            foreach (var m in movies)
            {
                if (excluded.Contains(m.TmdbId) || candidates.ContainsKey(m.TmdbId)) continue;
                m.RecommendationReason = reason(m);
                candidates[m.TmdbId] = m;
            }
        }

        // 1) Parecidos com os filmes-semente mais fortes
        foreach (var seed in profile.Seeds.Take(3))
        {
            var similar = await _tmdb.GetSimilarMoviesAsync(seed.TmdbMovieId, region, runtimeApiKey, ct);
            var seedText = seed.ListType == WatchlistType.Watched
                ? $"Porque você curtiu {seed.Title}"
                : $"Parecido com {seed.Title}, que está no seu Quero Ver";
            Add(similar, _ => seedText);
        }

        // 2) Vibes que mais combinam com os gêneros favoritos
        if (profile.TopVibes.Count > 0)
        {
            var byVibe = await _tmdb.DiscoverByVibeAsync(new VibeDiscoverQuery
            {
                VibeIds = profile.TopVibes.Select(v => v.Id).ToList(),
                Region = region,
                SortBy = "vote_average.desc"
            }, runtimeApiKey, ct);

            Add(byVibe.Movies, m =>
            {
                var vibe = profile.TopVibes.FirstOrDefault(v => m.MatchedVibes.Contains(v.Id)) ?? profile.TopVibes[0];
                return $"Combina com sua vibe {vibe.Name} {vibe.Emoji}";
            });
        }

        // 3) Gênero favorito
        var topGenre = profile.GenreWeights.OrderByDescending(kv => kv.Value).Select(kv => (int?)kv.Key).FirstOrDefault();
        if (topGenre.HasValue)
        {
            var byGenre = await _tmdb.DiscoverByVibeAsync(new VibeDiscoverQuery
            {
                GenreIds = new List<int> { topGenre.Value },
                Region = region
            }, runtimeApiKey, ct);
            Add(byGenre.Movies, _ => $"Você curte {VibeCatalog.GetGenreName(topGenre.Value)}");
        }

        // 4) Sem histórico: populares bem avaliados
        if (candidates.Count == 0)
        {
            var general = await _tmdb.DiscoverByVibeAsync(new VibeDiscoverQuery { Region = region }, runtimeApiKey, ct);
            Add(general.Movies, _ => "Popular e bem avaliado — avalie filmes no Diário para recomendações mais certeiras");
        }

        var maxWeight = profile.GenreWeights.Count > 0 ? profile.GenreWeights.Values.Sum() : 1.0;
        return candidates.Values
            .Select(m =>
            {
                var affinity = m.GenreIds.Sum(g => profile.GenreWeights.GetValueOrDefault(g)) / maxWeight; // 0..~1
                var score = 55 + affinity * 30 + Math.Clamp(m.VoteAverage - 6.0, 0, 3) * 4.5;
                m.VibeMatchScore = (int)Math.Clamp(Math.Round(score), 50, 99);
                if (m.VoteAverage > 0)
                    m.RecommendationReason = $"{m.RecommendationReason} • nota {MovieTextBuilder.FormatRating(m.VoteAverage)}";
                return m;
            })
            .OrderByDescending(m => m.VibeMatchScore)
            .ThenByDescending(m => m.VoteAverage)
            .Take(take)
            .ToList();
    }
}
