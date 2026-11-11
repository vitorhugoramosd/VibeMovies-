using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using VibeMovies.Infrastructure.External;
using VibeMovies.Infrastructure.Persistence;

namespace VibeMovies.Tests;

/// <summary>HttpClient sem chave do TMDB — os serviços caem no catálogo curado (modo offline).</summary>
public sealed class FakeHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new() { BaseAddress = new Uri("https://api.themoviedb.org/3/") };
}

/// <summary>Banco SQLite em memória (relacional de verdade); a conexão fica aberta enquanto o contexto viver.</summary>
public sealed class SqliteTestDb : IDisposable
{
    private readonly SqliteConnection _connection;
    public VibeMoviesDbContext Db { get; }

    public SqliteTestDb()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        Db = new VibeMoviesDbContext(new DbContextOptionsBuilder<VibeMoviesDbContext>().UseSqlite(_connection).Options);
        Db.Database.EnsureCreated();
    }

    public TmdbHttpClientService CreateTmdbService() => new(
        new FakeHttpClientFactory(),
        new ConfigurationBuilder().AddInMemoryCollection().Build(),
        Db,
        NullLogger<TmdbHttpClientService>.Instance);

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
    }
}

public sealed class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _utcNow;
    private readonly TimeZoneInfo _zone;

    public FixedTimeProvider(DateTimeOffset utcNow, TimeZoneInfo? zone = null)
    {
        _utcNow = utcNow;
        _zone = zone ?? TimeZoneInfo.Utc;
    }

    public override DateTimeOffset GetUtcNow() => _utcNow;
    public override TimeZoneInfo LocalTimeZone => _zone;
}
