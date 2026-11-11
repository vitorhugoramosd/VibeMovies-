using VibeMovies.Application.Catalog;
using VibeMovies.Application.Interfaces;
using VibeMovies.Domain.Models;

namespace VibeMovies.Application.Services;

/// <summary>"Me surpreenda": sorteio ponderado (vibe match + nota) sobre o resultado do discover.</summary>
public class SurprisePickService
{
    private readonly ITmdbService _tmdb;
    private readonly Random _random;

    public SurprisePickService(ITmdbService tmdb, Random? random = null)
    {
        _tmdb = tmdb;
        _random = random ?? Random.Shared;
    }

    public async Task<SurprisePickDto?> PickAsync(SurpriseRequest request, string? runtimeApiKey = null, CancellationToken ct = default)
    {
        var vibes = request.VibeIds.Select(VibeCatalog.GetById).Where(v => v is not null).Cast<VibeDefinition>().ToList();
        var live = _tmdb.HasApiKey(runtimeApiKey);

        // Tentativas: filtros completos → sem streaming → sem duração/streaming → só a vibe/gênero → qualquer coisa
        var attempts = new List<(string? Provider, int? MaxRuntime, List<string> Vibes, List<int> Genres, string? Note)>
        {
            (request.ProviderFilter, request.MaxRuntimeMinutes, request.VibeIds, request.GenreIds, null)
        };
        if (!string.IsNullOrWhiteSpace(request.ProviderFilter))
            attempts.Add((null, request.MaxRuntimeMinutes, request.VibeIds, request.GenreIds, "Nada disponível no streaming pedido, então ignorei esse filtro"));
        if (request.MaxRuntimeMinutes.HasValue)
            attempts.Add((null, null, request.VibeIds, request.GenreIds, "Não achei nada nessa duração, então relaxei os filtros"));
        if (request.VibeIds.Count > 0 && request.GenreIds.Count > 0)
            attempts.Add((null, null, request.VibeIds, new List<int>(), "Relaxei o gênero para manter a vibe"));
        if (request.VibeIds.Count > 0 || request.GenreIds.Count > 0)
            attempts.Add((null, null, new List<string>(), new List<int>(), "Não achei nada com esses filtros, então escolhi entre os mais populares"));

        foreach (var attempt in attempts)
        {
            var pages = live ? new[] { _random.Next(1, 4), 1 }.Distinct() : new[] { 1 };
            foreach (var page in pages)
            {
                var result = await _tmdb.DiscoverByVibeAsync(new VibeDiscoverQuery
                {
                    VibeIds = attempt.Vibes,
                    GenreIds = attempt.Genres,
                    MaxRuntimeMinutes = attempt.MaxRuntime,
                    WatchProviderFilter = attempt.Provider,
                    Region = request.Region,
                    SortBy = "popularity.desc",
                    Page = page
                }, runtimeApiKey, ct);

                var candidates = result.Movies.Where(m => !request.ExcludeIds.Contains(m.TmdbId)).ToList();
                if (candidates.Count == 0) continue;

                var chosen = WeightedPick(candidates);
                var movie = await EnrichAsync(chosen, request.Region, runtimeApiKey, ct);

                return new SurprisePickDto
                {
                    Movie = movie,
                    Reason = MovieTextBuilder.BuildReason(movie, vibes, request.GenreIds, attempt.MaxRuntime, request.PreferredProviders, attempt.Note),
                    SpokenText = MovieTextBuilder.BuildSpokenText(movie, vibes, request.PreferredProviders)
                };
            }
        }

        return null;
    }

    public MovieCardDto WeightedPick(IReadOnlyList<MovieCardDto> candidates)
    {
        var weights = candidates
            .Select(m => Math.Max(1.0, m.VibeMatchScore - 60) * Math.Max(0.5, m.VoteAverage - 4.0))
            .ToList();
        var total = weights.Sum();
        var roll = _random.NextDouble() * total;
        for (var i = 0; i < candidates.Count; i++)
        {
            roll -= weights[i];
            if (roll <= 0) return candidates[i];
        }

        return candidates[^1];
    }

    /// <summary>Completa duração, trailer e tagline com os detalhes do filme, preservando vibe/score do discover.</summary>
    private async Task<MovieCardDto> EnrichAsync(MovieCardDto chosen, string region, string? runtimeApiKey, CancellationToken ct)
    {
        var details = await _tmdb.GetMovieDetailsAsync(chosen.TmdbId, region, runtimeApiKey, ct);
        if (details is null) return chosen;

        details.MatchedVibes = chosen.MatchedVibes.Count > 0 ? chosen.MatchedVibes : details.MatchedVibes;
        details.VibeMatchScore = chosen.VibeMatchScore;
        if (details.WatchProviders.Flatrate.Count == 0 && chosen.WatchProviders.Flatrate.Count > 0)
            details.WatchProviders = chosen.WatchProviders;
        return details;
    }
}
