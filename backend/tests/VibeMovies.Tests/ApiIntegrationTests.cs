using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using VibeMovies.Api;
using Xunit;

namespace VibeMovies.Tests;

/// <summary>
/// Sobe a API real (Kestrel em 127.0.0.1, porta efêmera, SQLite temporário, sem chave do TMDB).
/// Faz o papel do WebApplicationFactory (pacote Mvc.Testing indisponível no ambiente offline) e,
/// por ser Kestrel de verdade, exercita o guard de loopback com o RemoteIpAddress real.
/// </summary>
public sealed class ApiFixture : IAsyncLifetime
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "vibemovies-api-" + Guid.NewGuid().ToString("N"));
    private WebApplication? _app;
    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_dir);
        _app = await VibeMoviesApp.BuildAsync(new[] { "--urls=http://127.0.0.1:0", "--environment=Testing" }, builder =>
        {
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "Sqlite",
                ["ConnectionStrings:Sqlite"] = $"Data Source={Path.Combine(_dir, "test.db")};Pooling=False",
                ["Jwt:KeyDirectory"] = _dir,
                ["Frontend:DistPath"] = Path.Combine(_dir, "sem-frontend"),
                ["Tmdb:ApiKey"] = "",
                ["RateLimiting:AuthPermitLimit"] = "50"
            });
        });
        await _app.StartAsync();
        Client = new HttpClient { BaseAddress = new Uri(_app.Urls.First()) };
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
        SqliteConnection.ClearAllPools();
        try { Directory.Delete(_dir, true); } catch { /* ignore */ }
    }

    public async Task<string> LoginDemoAsync()
    {
        var res = await Client.PostAsJsonAsync("/api/auth/login", new { email = "demo@vibemovies.app", password = "vibe1234" });
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
    }
}

public class ApiIntegrationTests : IClassFixture<ApiFixture>
{
    private readonly ApiFixture _f;
    public ApiIntegrationTests(ApiFixture fixture) => _f = fixture;

    private async Task<JsonElement> GetJsonAsync(string url, string? token = null)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        if (token is not null) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var res = await _f.Client.SendAsync(req);
        Assert.True(res.IsSuccessStatusCode, $"{url} -> {(int)res.StatusCode}: {await res.Content.ReadAsStringAsync()}");
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task Health_ShouldReportVersionTmdbAndFrontend()
    {
        var health = await GetJsonAsync("/api/health");
        Assert.Equal("Healthy", health.GetProperty("status").GetString());
        Assert.False(health.GetProperty("usingLiveTmdb").GetBoolean());
        Assert.False(health.GetProperty("frontendServed").GetBoolean());
        Assert.False(string.IsNullOrWhiteSpace(health.GetProperty("version").GetString()));
    }

    [Fact]
    public async Task Jarvis_FromLoopback_ShouldReturnSummaryContract()
    {
        var s = await GetJsonAsync("/api/jarvis/summary?city=Joinville/SC");

        Assert.False(s.GetProperty("usingLiveTmdb").GetBoolean());
        Assert.Equal("Cinéfilo Vibe", s.GetProperty("user").GetProperty("name").GetString());
        Assert.Equal("Joinville/SC", s.GetProperty("user").GetProperty("defaultCity").GetString());
        Assert.Equal(JsonValueKind.Object, s.GetProperty("tonightPick").ValueKind);
        Assert.False(string.IsNullOrWhiteSpace(s.GetProperty("tonightReason").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(s.GetProperty("tonightSpokenText").GetString()));
        var want = s.GetProperty("wantToWatch");
        Assert.InRange(want.GetArrayLength(), 1, 6);
        Assert.Equal(JsonValueKind.Array, want[0].GetProperty("providers").ValueKind);
        Assert.InRange(s.GetProperty("nowPlaying").GetArrayLength(), 1, 6);
        Assert.True(s.GetProperty("nowPlaying")[0].GetProperty("nextSessions").GetArrayLength() <= 3);
        Assert.True(s.GetProperty("isSimulatedSchedule").GetBoolean());
        Assert.Equal(5, s.GetProperty("stats").GetProperty("ratingDistribution").EnumerateObject().Count());
        Assert.True(s.TryGetProperty("generatedAt", out _));
    }

    [Fact]
    public async Task Jarvis_Surprise_Vibes_AndWatchlist()
    {
        var pick = await GetJsonAsync("/api/jarvis/surprise?vibe=terror&maxRuntime=130");
        Assert.Contains(27, pick.GetProperty("movie").GetProperty("genreIds").EnumerateArray().Select(e => e.GetInt32()));
        Assert.StartsWith("Que tal", pick.GetProperty("spokenText").GetString());

        var vibes = await GetJsonAsync("/api/jarvis/vibes");
        Assert.True(vibes.GetArrayLength() >= 10);
        Assert.Equal("plot-twist", vibes[0].GetProperty("id").GetString());
        Assert.True(vibes[0].TryGetProperty("emoji", out _));

        var add = await _f.Client.PostAsJsonAsync("/api/jarvis/watchlist", new { tmdbId = 27205, list = "watched", rating = 5 });
        Assert.Equal(HttpStatusCode.OK, add.StatusCode);
        var item = await add.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(27205, item.GetProperty("tmdbMovieId").GetInt32());
        Assert.Equal(5, item.GetProperty("personalRating").GetInt32());

        var bad = await _f.Client.PostAsJsonAsync("/api/jarvis/watchlist", new { tmdbId = 27205, list = "talvez" });
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace((await bad.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString()));
    }

    [Fact]
    public async Task Movies_SurpriseAndSimilar_WorkOffline()
    {
        var pick = await GetJsonAsync("/api/movies/surprise?vibes=plot-twist&excludeIds=11324,670");
        var id = pick.GetProperty("movie").GetProperty("tmdbId").GetInt32();
        Assert.NotEqual(11324, id);
        Assert.NotEqual(670, id);
        Assert.False(string.IsNullOrWhiteSpace(pick.GetProperty("reason").GetString()));

        var similar = await GetJsonAsync("/api/movies/11324/similar?region=BR");
        Assert.InRange(similar.GetArrayLength(), 1, 8);
        Assert.DoesNotContain(similar.EnumerateArray(), m => m.GetProperty("tmdbId").GetInt32() == 11324);
    }

    [Fact]
    public async Task Watchlist_Stats_Recommendations_Export_Profile()
    {
        var token = await _f.LoginDemoAsync();

        var stats = await GetJsonAsync("/api/watchlist/stats", token);
        Assert.Equal(12, stats.GetProperty("watchedPerMonth").GetArrayLength());
        Assert.True(stats.TryGetProperty("genreBreakdown", out _));
        Assert.True(stats.TryGetProperty("totalHoursWatched", out _));

        var recs = await GetJsonAsync("/api/watchlist/recommendations", token);
        var watchlist = await GetJsonAsync("/api/watchlist", token);
        var owned = watchlist.EnumerateArray().Select(w => w.GetProperty("tmdbMovieId").GetInt32()).ToHashSet();
        Assert.NotEqual(0, recs.GetArrayLength());
        foreach (var r in recs.EnumerateArray())
        {
            Assert.DoesNotContain(r.GetProperty("tmdbId").GetInt32(), owned);
            Assert.False(string.IsNullOrWhiteSpace(r.GetProperty("recommendationReason").GetString()));
        }

        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/watchlist/export?format=csv");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var csv = await _f.Client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, csv.StatusCode);
        Assert.Equal("text/csv", csv.Content.Headers.ContentType?.MediaType);
        Assert.Equal(0xEF, (await csv.Content.ReadAsByteArrayAsync())[0]);

        using var put = new HttpRequestMessage(HttpMethod.Put, "/api/auth/me")
        {
            Content = JsonContent.Create(new { myProviders = new[] { "netflix", "prime video" } })
        };
        put.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var putRes = await _f.Client.SendAsync(put);
        Assert.Equal(HttpStatusCode.OK, putRes.StatusCode);
        var me = await GetJsonAsync("/api/auth/me", token);
        Assert.Equal(new[] { "Netflix", "Amazon Prime Video" }, me.GetProperty("myProviders").EnumerateArray().Select(e => e.GetString()));

        var onlyMine = await GetJsonAsync("/api/movies/discover?onlyMyProviders=true", token);
        foreach (var m in onlyMine.GetProperty("movies").EnumerateArray())
            Assert.Contains(m.GetProperty("watchProviders").GetProperty("flatrate").EnumerateArray(), p => p.GetProperty("providerId").GetInt32() is 8 or 119);

        using var anon = await _f.Client.GetAsync("/api/movies/discover?onlyMyProviders=true");
        Assert.Equal(HttpStatusCode.Unauthorized, anon.StatusCode);
    }

    [Fact]
    public async Task Register_ShouldValidateInputWithPtBrMessage()
    {
        var res = await _f.Client.PostAsJsonAsync("/api/auth/register", new { name = "", email = "invalido", password = "123" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("message").GetString()));
        Assert.True(body.TryGetProperty("errors", out _));
    }

    [Fact]
    public async Task Cors_ShouldAllowLocalhostOnly()
    {
        async Task<string?> Preflight(string origin)
        {
            using var req = new HttpRequestMessage(HttpMethod.Options, "/api/movies/vibes");
            req.Headers.Add("Origin", origin);
            req.Headers.Add("Access-Control-Request-Method", "GET");
            using var res = await _f.Client.SendAsync(req);
            return res.Headers.TryGetValues("Access-Control-Allow-Origin", out var v) ? v.First() : null;
        }

        Assert.Equal("http://localhost:5173", await Preflight("http://localhost:5173"));
        Assert.Equal("http://127.0.0.1:4000", await Preflight("http://127.0.0.1:4000"));
        Assert.Null(await Preflight("https://evil.example.com"));
    }
}
