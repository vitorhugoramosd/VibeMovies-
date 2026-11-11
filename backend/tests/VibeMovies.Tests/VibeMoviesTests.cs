using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using VibeMovies.Application.Catalog;
using VibeMovies.Application.Services;
using VibeMovies.Domain.Entities;
using VibeMovies.Domain.Models;
using VibeMovies.Infrastructure.External;
using VibeMovies.Infrastructure.Persistence;
using VibeMovies.Infrastructure.Repositories;
using Xunit;

namespace VibeMovies.Tests;

public class VibeMoviesTests
{
    [Fact]
    public void VibeCatalog_ShouldContainPredefinedVibesMappedToTmdbKeywordIds()
    {
        Assert.NotEmpty(VibeCatalog.AllVibes);
        Assert.True(VibeCatalog.AllVibes.Count >= 10);

        var plotTwist = VibeCatalog.GetById("plot-twist");
        Assert.NotNull(plotTwist);
        Assert.Contains(4344, plotTwist!.TmdbKeywordIds);

        var anos90 = VibeCatalog.GetById("anos-90");
        Assert.NotNull(anos90);
        Assert.Equal("1990-01-01", anos90!.ReleaseDateGte);
        Assert.Equal("1999-12-31", anos90.ReleaseDateLte);
    }

    [Fact]
    public async Task DiscoverByVibe_ShouldCombineVibesGenresAndMaxRuntimeAndIncludeWatchProviders()
    {
        using var testDb = new SqliteTestDb();
        var tmdbService = testDb.CreateTmdbService();

        var query = new VibeDiscoverQuery
        {
            VibeIds = new List<string> { "plot-twist", "anos-90" },
            GenreIds = new List<int> { 53 }, // Suspense / Thriller
            MaxRuntimeMinutes = 140,
            Region = "BR"
        };

        var result = await tmdbService.DiscoverByVibeAsync(query);

        Assert.NotNull(result);
        Assert.NotEmpty(result.Movies);
        Assert.True(result.AppliedTmdbQueryParameters.ContainsKey("with_keywords"));
        Assert.True(result.AppliedTmdbQueryParameters.ContainsKey("with_genres"));
        Assert.Equal("140", result.AppliedTmdbQueryParameters["with_runtime.lte"]);
        Assert.Equal("1990-01-01", result.AppliedTmdbQueryParameters["primary_release_date.gte"]);

        foreach (var movie in result.Movies)
        {
            Assert.True(movie.RuntimeMinutes <= 140);
            Assert.NotNull(movie.WatchProviders);
            Assert.Equal("BR", movie.WatchProviders.Region);
        }
    }

    [Fact]
    public async Task CinemaRadarScraper_ShouldParseShowtimesWithHtmlAgilityPackForJoinville()
    {
        using var testDb = new SqliteTestDb();
        var httpFactory = new FakeHttpClientFactory();
        var tmdbService = testDb.CreateTmdbService();

        var radarService = new CinemaRadarScraperService(
            tmdbService,
            httpFactory,
            NullLogger<CinemaRadarScraperService>.Instance);

        var radar = await radarService.GetLocalRadarAsync("Joinville/SC");

        Assert.Equal("Joinville", radar.City);
        Assert.Equal("SC", radar.State);
        Assert.Contains("HtmlAgilityPack", radar.ScraperSource);
        Assert.NotEmpty(radar.MoviesInTheaters);

        var firstMovie = radar.MoviesInTheaters[0];
        Assert.NotNull(firstMovie.Movie);
        Assert.NotEmpty(firstMovie.Venues);
        Assert.Contains(firstMovie.Venues, v => v.CinemaChain.Contains("GNC Cinemas"));
        Assert.All(firstMovie.Venues, v => Assert.NotEmpty(v.Sessions));

        // Honestidade: grade ilustrativa sinalizada, classificação não inventada, ingresso via busca real
        Assert.True(radar.IsSimulatedSchedule);
        Assert.False(string.IsNullOrWhiteSpace(radar.Notice));
        Assert.All(radar.MoviesInTheaters, m => Assert.Equal("Classificação não informada", m.AgeRating));
        Assert.All(firstMovie.Venues.SelectMany(v => v.Sessions), s => Assert.StartsWith("https://www.ingresso.com/busca?q=", s.TicketUrl));
    }

    [Fact]
    public async Task AuthAndWatchlist_ShouldRegisterLoginAndPersistWantToWatchAndWatchedLists()
    {
        using var testDb = new SqliteTestDb();
        var db = testDb.Db;
        var userRepo = new UserRepository(db);
        var watchlistRepo = new WatchlistRepository(db);
        var authService = new AuthService(userRepo, u => $"test-jwt-for-{u.Email}");

        var reg = await authService.RegisterAsync(new RegisterRequestDto(
            "Maria Cinéfila",
            "maria@vibemovies.app",
            "senhaSegura123",
            "BR",
            "Joinville/SC"));

        Assert.NotNull(reg);
        Assert.Equal("maria@vibemovies.app", reg.Email);
        Assert.StartsWith("test-jwt-for-", reg.Token);

        var login = await authService.LoginAsync(new LoginRequestDto("maria@vibemovies.app", "senhaSegura123"));
        Assert.NotNull(login);
        Assert.Equal(reg.UserId, login!.UserId);

        // Quero Ver
        await watchlistRepo.UpsertAsync(new WatchlistItem
        {
            UserId = reg.UserId,
            TmdbMovieId = 550,
            Title = "Clube da Luta",
            ReleaseYear = 1999,
            ListType = WatchlistType.WantToWatch,
            RuntimeMinutes = 139
        });

        // Já Vi (1 a 5 estrelas + data)
        await watchlistRepo.UpsertAsync(new WatchlistItem
        {
            UserId = reg.UserId,
            TmdbMovieId = 807,
            Title = "Seven: Os Sete Crimes Capitais",
            ReleaseYear = 1995,
            ListType = WatchlistType.Watched,
            PersonalRating = 5,
            PersonalNotes = "Final chocante!",
            WatchedAt = DateTime.UtcNow,
            RuntimeMinutes = 127
        });

        var wantToWatch = await watchlistRepo.GetByUserIdAsync(reg.UserId, WatchlistType.WantToWatch);
        var watched = await watchlistRepo.GetByUserIdAsync(reg.UserId, WatchlistType.Watched);

        Assert.Single(wantToWatch);
        Assert.Equal("Clube da Luta", wantToWatch[0].Title);

        Assert.Single(watched);
        Assert.Equal(5, watched[0].PersonalRating);
        Assert.NotNull(watched[0].WatchedAt);
    }
}
