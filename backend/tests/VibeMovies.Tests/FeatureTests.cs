using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VibeMovies.Api.Infrastructure;
using VibeMovies.Application.Catalog;
using VibeMovies.Application.Services;
using VibeMovies.Domain.Entities;
using VibeMovies.Domain.Models;
using VibeMovies.Infrastructure.External;
using VibeMovies.Infrastructure.Persistence;
using VibeMovies.Infrastructure.Repositories;
using Xunit;

namespace VibeMovies.Tests;

public class SurpriseAndSimilarTests
{
    [Fact]
    public async Task SurprisePick_ShouldRespectVibeRuntimeAndExclusions_AndExplainInPtBr()
    {
        using var testDb = new SqliteTestDb();
        var service = new SurprisePickService(testDb.CreateTmdbService(), new Random(42));

        for (var i = 0; i < 15; i++)
        {
            var pick = await service.PickAsync(new SurpriseRequest
            {
                VibeIds = new() { "plot-twist" },
                MaxRuntimeMinutes = 140,
                ExcludeIds = new() { 11324 } // Ilha do Medo
            });

            Assert.NotNull(pick);
            Assert.NotEqual(11324, pick!.Movie.TmdbId);
            Assert.Contains("plot-twist", pick.Movie.MatchedVibes);
            Assert.True(pick.Movie.RuntimeMinutes <= 140);
            Assert.Contains("Plot Twist Chocante", pick.Reason);
            Assert.Contains("nota", pick.Reason);
            Assert.StartsWith($"Que tal {pick.Movie.Title}", pick.SpokenText);
            Assert.EndsWith(".", pick.SpokenText);
        }
    }

    [Fact]
    public async Task Discover_ShouldNotMutateSharedCuratedDataset()
    {
        var before = CuratedMovieDataset.Movies.Select(m => (m.TmdbId, m.VibeMatchScore)).ToList();
        using var testDb = new SqliteTestDb();
        var tmdb = testDb.CreateTmdbService();

        var result = await tmdb.DiscoverByVibeAsync(new VibeDiscoverQuery { VibeIds = new() { "plot-twist", "vinganca" } });
        foreach (var m in result.Movies) m.VibeMatchScore = 1;

        Assert.Equal(before, CuratedMovieDataset.Movies.Select(m => (m.TmdbId, m.VibeMatchScore)).ToList());
        Assert.DoesNotContain(result.Movies, m => CuratedMovieDataset.Movies.Any(c => ReferenceEquals(c, m)));
    }

    [Fact]
    public void SpokenText_ShouldReadNaturally()
    {
        var movie = CuratedMovieDataset.FindClone(11324)!;
        var text = MovieTextBuilder.BuildSpokenText(movie, new[] { VibeCatalog.GetById("plot-twist")! }, Array.Empty<string>());
        Assert.Equal("Que tal Ilha do Medo, de 2010? Suspense com plot twist, nota 8,2, tem na Netflix.", text);

        var preferPrime = MovieTextBuilder.BuildSpokenText(movie, Array.Empty<VibeDefinition>(), new[] { "Prime Video" });
        Assert.EndsWith("tem no Prime Video.", preferPrime);
    }

    [Fact]
    public async Task Similar_WithoutApiKey_ShouldRankCuratedBySharedVibesAndExcludeItself()
    {
        using var testDb = new SqliteTestDb();
        var similar = await testDb.CreateTmdbService().GetSimilarMoviesAsync(11324);

        Assert.NotEmpty(similar);
        Assert.True(similar.Count <= 8);
        Assert.DoesNotContain(similar, m => m.TmdbId == 11324);

        var source = CuratedMovieDataset.Movies.First(m => m.TmdbId == 11324);
        int Shared(MovieCardDto m) => m.MatchedVibes.Intersect(source.MatchedVibes).Count() * 2 + m.GenreIds.Intersect(source.GenreIds).Count();
        Assert.All(similar, m => Assert.True(Shared(m) > 0));
        Assert.True(Shared(similar[0]) >= Shared(similar[^1]));
    }

    [Fact]
    public async Task UnknownMovieProviders_ShouldBeEmptyWithTmdbLink_NotInvented()
    {
        using var testDb = new SqliteTestDb();
        var providers = await testDb.CreateTmdbService().GetWatchProvidersAsync(999999, "BR");

        Assert.Empty(providers.Flatrate);
        Assert.Empty(providers.Rent);
        Assert.Empty(providers.Buy);
        Assert.Equal("https://www.themoviedb.org/movie/999999/watch?locale=BR", providers.TmdbWatchLink);
    }

    [Fact]
    public async Task Discover_ProviderFilter_ShouldMapToTmdbIdsAndAcceptMultipleProviders()
    {
        using var testDb = new SqliteTestDb();
        var result = await testDb.CreateTmdbService().DiscoverByVibeAsync(new VibeDiscoverQuery { WatchProviderFilter = "Netflix, Globoplay" });

        Assert.Equal("8|307", result.AppliedTmdbQueryParameters["with_watch_providers"]);
        Assert.Equal("flatrate", result.AppliedTmdbQueryParameters["watch_monetization_types"]);
        Assert.NotEmpty(result.Movies);
        Assert.All(result.Movies, m => Assert.Contains(m.WatchProviders.Flatrate, p => p.ProviderId is 8 or 307));
    }

    [Fact]
    public void LiveVibeScore_ShouldRewardGenreOverlapAndRating()
    {
        var plotTwist = VibeCatalog.GetById("plot-twist")!;
        var feelGood = VibeCatalog.GetById("feel-good")!;

        var thriller = TmdbHttpClientService.ComputeVibeMatch(new[] { 53, 9648 }, 8.2, 20000, new[] { plotTwist, feelGood }, usedKeywords: true);
        var comedyLow = TmdbHttpClientService.ComputeVibeMatch(new[] { 35 }, 5.5, 200, new[] { plotTwist, feelGood }, usedKeywords: true);
        var noMatch = TmdbHttpClientService.ComputeVibeMatch(new[] { 99 }, 7.0, 1000, new[] { plotTwist }, usedKeywords: false);

        Assert.Equal(new[] { "plot-twist" }, thriller.MatchedVibes);
        Assert.Equal(new[] { "feel-good" }, comedyLow.MatchedVibes);
        Assert.Empty(noMatch.MatchedVibes);
        Assert.True(thriller.Score > comedyLow.Score);
        Assert.InRange(thriller.Score, 50, 99);
    }

    [Fact]
    public void AgeRating_ShouldParseBrazilianCertification()
    {
        using var doc = System.Text.Json.JsonDocument.Parse("""
            {"results":[{"iso_3166_1":"US","release_dates":[{"certification":"R","type":3}]},
                        {"iso_3166_1":"BR","release_dates":[{"certification":"","type":1},{"certification":"16","type":3}]}]}
            """);
        Assert.Equal("16 anos", TmdbHttpClientService.ParseCertification(doc.RootElement, "BR"));
        Assert.Equal("Livre", TmdbHttpClientService.FormatCertification("L"));
        Assert.Null(TmdbHttpClientService.ParseCertification(doc.RootElement, "PT"));
    }
}

public class WatchlistFeatureTests
{
    private static async Task<(SqliteTestDb Db, Guid UserId)> SeedAsync()
    {
        var testDb = new SqliteTestDb();
        var user = new ApplicationUser { Name = "Teste", Email = "t@t.com", PasswordHash = "x" };
        testDb.Db.Users.Add(user);
        testDb.Db.WatchlistItems.AddRange(
            new WatchlistItem { UserId = user.Id, TmdbMovieId = 11324, Title = "Ilha do Medo", ListType = WatchlistType.Watched, PersonalRating = 5, GenresCsv = "Suspense / Thriller, Mistério, Drama", RuntimeMinutes = 138, WatchedAt = new DateTime(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc) },
            new WatchlistItem { UserId = user.Id, TmdbMovieId = 496243, Title = "Parasita", ListType = WatchlistType.Watched, PersonalRating = 4, GenresCsv = "Comédia, Suspense / Thriller, Drama", RuntimeMinutes = null, WatchedAt = new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc) },
            new WatchlistItem { UserId = user.Id, TmdbMovieId = 550, Title = "Clube da Luta", ListType = WatchlistType.Watched, PersonalRating = 2, GenresCsv = "Crime", RuntimeMinutes = 139, WatchedAt = new DateTime(2025, 1, 5, 12, 0, 0, DateTimeKind.Utc) },
            new WatchlistItem { UserId = user.Id, TmdbMovieId = 670, Title = "Oldboy", ListType = WatchlistType.WantToWatch, GenresCsv = "Suspense / Thriller, Ação, Mistério", RuntimeMinutes = 120 });
        await testDb.Db.SaveChangesAsync();
        return (testDb, user.Id);
    }

    [Fact]
    public async Task Recommendations_ShouldExcludeEverythingAlreadyInWatchlist_AndExplainWhy()
    {
        var (testDb, userId) = await SeedAsync();
        using var _ = testDb;
        var service = new RecommendationService(testDb.CreateTmdbService(), new WatchlistRepository(testDb.Db));

        var recs = await service.GetForUserAsync(userId);
        var inWatchlist = new[] { 11324, 496243, 550, 670 };

        Assert.NotEmpty(recs);
        Assert.True(recs.Count <= 12);
        Assert.DoesNotContain(recs, r => inWatchlist.Contains(r.TmdbId));
        Assert.Equal(recs.Count, recs.Select(r => r.TmdbId).Distinct().Count());
        Assert.All(recs, r => Assert.False(string.IsNullOrWhiteSpace(r.RecommendationReason)));
        Assert.Contains(recs, r => r.RecommendationReason!.StartsWith("Porque você curtiu Ilha do Medo"));
    }

    [Fact]
    public async Task Stats_ShouldBuildBreakdownDistributionMonthsAndIgnoreUnknownRuntime()
    {
        var (testDb, userId) = await SeedAsync();
        using var _ = testDb;
        var items = await new WatchlistRepository(testDb.Db).GetByUserIdAsync(userId);
        var stats = WatchlistStatsCalculator.Compute(items, new FixedTimeProvider(new DateTimeOffset(2026, 11, 11, 15, 0, 0, TimeSpan.Zero)));

        Assert.Equal(3, stats.TotalWatched);
        Assert.Equal(1, stats.TotalWantToWatch);
        Assert.Equal(138 + 139, stats.TotalMinutesWatched); // Parasita sem duração não vira "120 min"
        Assert.Equal(5, stats.TotalHoursWatched);
        Assert.Equal(1, stats.WatchedWithUnknownRuntime);
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, stats.RatingDistribution.Keys.OrderBy(k => k));
        Assert.Equal(1, stats.RatingDistribution[5]);
        Assert.Equal(1, stats.RatingDistribution[4]);
        Assert.Equal(1, stats.RatingDistribution[2]);
        Assert.Equal(0, stats.RatingDistribution[1]);

        Assert.Equal(12, stats.WatchedPerMonth.Count);
        Assert.Equal("dez/25", stats.WatchedPerMonth[0].Name);
        Assert.Equal("nov/26", stats.WatchedPerMonth[^1].Name);
        Assert.Equal(1, stats.WatchedPerMonth[^1].Count);
        Assert.Equal(1, stats.WatchedPerMonth.Single(m => m.Name == "set/26").Count);
        Assert.Equal(2, stats.WatchedPerMonth.Sum(m => m.Count)); // jan/25 fora da janela

        Assert.Equal("Suspense / Thriller", stats.FavoriteGenre);
        Assert.Equal("Suspense / Thriller", stats.GenreBreakdown[0].Name);
        Assert.Equal(3, stats.GenreBreakdown[0].Count);
        Assert.True(stats.GenreBreakdown.Count <= 8);
    }

    [Fact]
    public void CsvExport_ShouldUseSemicolonBomAndEscape()
    {
        var bytes = WatchlistExporter.ToCsv(new[]
        {
            new WatchlistItem { Title = "Filme; com \"aspas\"", ListType = WatchlistType.Watched, PersonalRating = 5, PersonalNotes = "=CMD()", TmdbMovieId = 1 }
        });

        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes.Take(3).ToArray());
        var text = Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        Assert.StartsWith("Lista;Título;", text);
        Assert.Contains("Já Vi;\"Filme; com \"\"aspas\"\"\"", text);
        Assert.Contains("'=CMD()", text);
    }
}

public class VibeWordMapperTests
{
    [Theory]
    [InlineData("terror", null, 27)]
    [InlineData("Terror", null, 27)]
    [InlineData("comédia", null, 35)]
    [InlineData("quero uma comedia leve", null, 35)]
    [InlineData("algo leve pra relaxar", "feel-good", null)]
    [InlineData("ação", null, 28)]
    [InlineData("suspense", null, 53)]
    [InlineData("romance", null, 10749)]
    [InlineData("ficção", null, 878)]
    [InlineData("ficção científica", null, 878)]
    [InlineData("plot-twist", "plot-twist", null)]
    [InlineData("anos-90", "anos-90", null)]
    [InlineData("um filme de vingança", "vinganca", null)]
    [InlineData("viagem no tempo", "mind-bending", null)]
    [InlineData("Suspense / Thriller", null, 53)]
    public void Map_ShouldResolveFreePtBrWords(string word, string? expectedVibe, int? expectedGenre)
    {
        var match = VibeWordMapper.Map(word);
        Assert.Equal(expectedVibe, match.VibeId);
        Assert.Equal(expectedGenre, match.GenreId);
        Assert.False(string.IsNullOrWhiteSpace(match.Label));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("xyzabc")]
    public void Map_ShouldReturnEmptyForUnknown(string? word) => Assert.True(VibeWordMapper.Map(word).IsEmpty);
}

public class SecurityAndSchemaTests
{
    [Theory]
    [InlineData("")]
    [InlineData("semponto")]
    [InlineData("isso nao e base64.### tambem nao")]
    [InlineData("YWJj.ZGVm")]
    [InlineData("a.b.c")]
    public void VerifyPassword_ShouldReturnFalseOnGarbage_NotThrow(string hash)
    {
        using var testDb = new SqliteTestDb();
        var auth = new AuthService(new UserRepository(testDb.Db), _ => "t");
        Assert.False(auth.VerifyPassword("vibe1234", hash));
        Assert.True(auth.VerifyPassword("vibe1234", auth.HashPassword("vibe1234")));
    }

    [Theory]
    [InlineData("127.0.0.1", true)]
    [InlineData("::1", true)]
    [InlineData("::ffff:127.0.0.1", true)]
    [InlineData("192.168.0.10", false)]
    [InlineData("10.0.0.5", false)]
    public void LoopbackGuard_ShouldOnlyAllowLocalRequests(string ip, bool allowed)
    {
        var http = new DefaultHttpContext();
        http.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        var ctx = new AuthorizationFilterContext(new ActionContext(http, new RouteData(), new ActionDescriptor()), new List<IFilterMetadata>());

        new LoopbackOnlyAttribute().OnAuthorization(ctx);

        if (allowed) Assert.Null(ctx.Result);
        else Assert.Equal(403, Assert.IsType<ObjectResult>(ctx.Result).StatusCode);
    }

    [Fact]
    public void LoopbackGuard_ShouldDenyUnknownRemoteAddress() => Assert.False(LoopbackOnlyAttribute.IsLoopback(null));

    [Fact]
    public void JwtKeyProvider_ShouldIgnorePlaceholderAndPersistRandomKey()
    {
        var dir = Path.Combine(Path.GetTempPath(), "vibemovies-jwt-" + Guid.NewGuid().ToString("N"));
        try
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:SecretKey"] = JwtKeyProvider.LegacyPlaceholder }).Build();
            var first = JwtKeyProvider.ResolveSigningKey(config, dir);
            var second = JwtKeyProvider.ResolveSigningKey(config, dir);

            Assert.Equal(64, first.Length);
            Assert.Equal(first, second);
            Assert.True(File.Exists(Path.Combine(dir, JwtKeyProvider.KeyFileName)));

            var strong = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:SecretKey"] = new string('k', 48) }).Build();
            Assert.Equal(Encoding.UTF8.GetBytes(new string('k', 48)), JwtKeyProvider.ResolveSigningKey(strong, dir));
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public async Task SchemaUpgrader_ShouldAddMissingColumnsToOldSqliteDatabase_Idempotently()
    {
        var path = Path.Combine(Path.GetTempPath(), $"vibemovies-old-{Guid.NewGuid():N}.db");
        try
        {
            // Schema da versão anterior (sem Users.MyProvidersCsv), igual ao gerado pelo EnsureCreated antigo
            await using (var conn = new SqliteConnection($"Data Source={path};Pooling=False"))
            {
                await conn.OpenAsync();
                var cmd = conn.CreateCommand();
                cmd.CommandText = """
                    CREATE TABLE "CachedMovies" ("Id" TEXT NOT NULL CONSTRAINT "PK_CachedMovies" PRIMARY KEY, "TmdbMovieId" INTEGER NOT NULL, "Region" TEXT NOT NULL, "Title" TEXT NOT NULL, "MovieJson" TEXT NOT NULL, "ProvidersJson" TEXT NOT NULL, "CachedAt" TEXT NOT NULL, "ExpiresAt" TEXT NOT NULL);
                    CREATE TABLE "Users" ("Id" TEXT NOT NULL CONSTRAINT "PK_Users" PRIMARY KEY, "Name" TEXT NOT NULL, "Email" TEXT NOT NULL, "PasswordHash" TEXT NOT NULL, "PreferredRegion" TEXT NOT NULL DEFAULT 'BR', "DefaultCity" TEXT NOT NULL DEFAULT 'Joinville/SC', "CreatedAt" TEXT NOT NULL);
                    CREATE TABLE "WatchlistItems" ("Id" TEXT NOT NULL CONSTRAINT "PK_WatchlistItems" PRIMARY KEY, "UserId" TEXT NOT NULL, "TmdbMovieId" INTEGER NOT NULL, "Title" TEXT NOT NULL, "OriginalTitle" TEXT NOT NULL, "PosterPath" TEXT NOT NULL, "BackdropPath" TEXT NOT NULL, "ReleaseYear" INTEGER NULL, "Overview" TEXT NOT NULL, "ListType" INTEGER NOT NULL, "PersonalRating" INTEGER NULL, "PersonalNotes" TEXT NULL, "WatchedAt" TEXT NULL, "AddedAt" TEXT NOT NULL, "GenresCsv" TEXT NOT NULL, "RuntimeMinutes" INTEGER NULL, "TmdbVoteAverage" REAL NOT NULL, CONSTRAINT "FK_WatchlistItems_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE);
                    CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email");
                    INSERT INTO "Users" ("Id","Name","Email","PasswordHash","CreatedAt") VALUES ('11111111-2222-3333-4444-555555555555','Antigo','old@vibemovies.app','x','2025-01-01 00:00:00');
                    """;
                await cmd.ExecuteNonQueryAsync();
            }

            var options = new DbContextOptionsBuilder<VibeMoviesDbContext>().UseSqlite($"Data Source={path};Pooling=False").Options;
            await using (var db = new VibeMoviesDbContext(options))
            {
                await db.Database.EnsureCreatedAsync(); // não altera tabelas existentes
                await Assert.ThrowsAnyAsync<Exception>(() => db.Users.AsNoTracking().FirstAsync()); // sem upgrade o modelo novo quebra

                var added = await DatabaseSchemaUpgrader.UpgradeAsync(db);
                Assert.Equal(new[] { "Users.MyProvidersCsv" }, added);

                var user = await db.Users.AsNoTracking().FirstAsync();
                Assert.Equal("Antigo", user.Name);
                Assert.Equal(string.Empty, user.MyProvidersCsv);

                Assert.Empty(await DatabaseSchemaUpgrader.UpgradeAsync(db)); // idempotente

                var tracked = await db.Users.FirstAsync();
                tracked.MyProvidersCsv = "Netflix,Max";
                await db.SaveChangesAsync();
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            try { File.Delete(path); } catch { /* ignore */ }
        }
    }
}
