using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using VibeMovies.Application.Interfaces;
using VibeMovies.Application.Services;
using VibeMovies.Domain.Entities;
using VibeMovies.Infrastructure.External;
using VibeMovies.Infrastructure.Persistence;
using VibeMovies.Infrastructure.Repositories;

namespace VibeMovies.Desktop;

public partial class App : System.Windows.Application
{
    public VibeMoviesDbContext DbContext { get; private set; } = null!;
    public IUserRepository UserRepository { get; private set; } = null!;
    public IWatchlistRepository WatchlistRepository { get; private set; } = null!;
    public ITmdbService TmdbService { get; private set; } = null!;
    public ICinemaRadarService CinemaRadarService { get; private set; } = null!;
    public IAuthService AuthService { get; private set; } = null!;
    public string ActiveDatabaseProvider { get; private set; } = "SQLite (vibemovies_desktop.db)";

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        bool usePostgres = IsPostgresPortOpen("localhost", 5432);
        var optionsBuilder = new DbContextOptionsBuilder<VibeMoviesDbContext>();

        if (usePostgres)
        {
            const string postgresConn = "Host=localhost;Port=5432;Database=vibemovies_db;Username=vibemovies;Password=vibemovies_secret";
            optionsBuilder.UseNpgsql(postgresConn);
            ActiveDatabaseProvider = "PostgreSQL (localhost:5432)";
        }
        else
        {
            string dbPath = Path.Combine(AppContext.BaseDirectory, "vibemovies_desktop.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
            ActiveDatabaseProvider = "SQLite Local (vibemovies_desktop.db)";
        }

        DbContext = new VibeMoviesDbContext(optionsBuilder.Options);

        try
        {
            await DbContext.Database.EnsureCreatedAsync();
        }
        catch
        {
            // Fallback para SQLite local caso o servidor na porta 5432 recuse autenticação
            string dbPath = Path.Combine(AppContext.BaseDirectory, "vibemovies_desktop.db");
            var fallbackBuilder = new DbContextOptionsBuilder<VibeMoviesDbContext>();
            fallbackBuilder.UseSqlite($"Data Source={dbPath}");
            DbContext = new VibeMoviesDbContext(fallbackBuilder.Options);
            ActiveDatabaseProvider = "SQLite Local (vibemovies_desktop.db)";
            await DbContext.Database.EnsureCreatedAsync();
        }

        var userRepo = new UserRepository(DbContext);
        var watchlistRepo = new WatchlistRepository(DbContext);
        var httpClientFactory = new DesktopHttpClientFactory();
        var configuration = new DesktopInMemoryConfiguration();

        var tmdbService = new TmdbHttpClientService(
            httpClientFactory,
            configuration,
            DbContext,
            NullLogger<TmdbHttpClientService>.Instance);

        var cinemaRadarService = new CinemaRadarScraperService(
            tmdbService,
            httpClientFactory,
            NullLogger<CinemaRadarScraperService>.Instance);

        var authService = new AuthService(
            userRepo,
            user => $"desktop-session-{user.Id:N}-{DateTime.UtcNow.Ticks}");

        UserRepository = userRepo;
        WatchlistRepository = watchlistRepo;
        TmdbService = tmdbService;
        CinemaRadarService = cinemaRadarService;
        AuthService = authService;

        await SeedDemoDataAsync(DbContext, AuthService);

        var mainWindow = new MainWindow(
            UserRepository,
            WatchlistRepository,
            TmdbService,
            CinemaRadarService,
            AuthService,
            ActiveDatabaseProvider);

        MainWindow = mainWindow;
        mainWindow.Closed += (_, _) => Shutdown();
        mainWindow.Show();
        mainWindow.Activate();
    }

    private static bool IsPostgresPortOpen(string host, int port)
    {
        try
        {
            using var tcp = new TcpClient();
            var connectTask = tcp.ConnectAsync(host, port);
            return connectTask.Wait(TimeSpan.FromMilliseconds(300)) && tcp.Connected;
        }
        catch
        {
            return false;
        }
    }

    private static async Task SeedDemoDataAsync(VibeMoviesDbContext db, IAuthService authService)
    {
        if (await db.Users.AnyAsync()) return;

        var demoUser = new ApplicationUser
        {
            Id = Guid.Parse("11111111-2222-3333-4444-555555555555"),
            Name = "Cinéfilo Vibe",
            Email = "demo@vibemovies.app",
            PasswordHash = authService.HashPassword("vibe1234"),
            PreferredRegion = "BR",
            DefaultCity = "Joinville/SC"
        };

        db.Users.Add(demoUser);

        db.WatchlistItems.AddRange(
            new WatchlistItem
            {
                UserId = demoUser.Id,
                TmdbMovieId = 11324,
                Title = "Ilha do Medo",
                OriginalTitle = "Shutter Island",
                PosterPath = "https://image.tmdb.org/t/p/w500/4GDy0PHYX3VRXUtwK5ysFbg3kEx.jpg",
                BackdropPath = "https://image.tmdb.org/t/p/w1280/ecvy2kMxsJ60ej52beZ0F8EOGkL.jpg",
                ReleaseYear = 2010,
                Overview = "Em 1954, o agente federal Teddy Daniels investiga o desaparecimento de uma paciente em um hospital psiquiátrico isolado em uma ilha.",
                ListType = WatchlistType.Watched,
                PersonalRating = 5,
                PersonalNotes = "Plot twist inesquecível! A fotografia e a trilha sonora constroem uma tensão impecável do início ao fim.",
                WatchedAt = DateTime.UtcNow.AddDays(-5),
                GenresCsv = "Suspense / Thriller, Mistério, Drama",
                RuntimeMinutes = 138,
                TmdbVoteAverage = 8.2
            },
            new WatchlistItem
            {
                UserId = demoUser.Id,
                TmdbMovieId = 496243,
                Title = "Parasita",
                OriginalTitle = "기생충",
                PosterPath = "https://image.tmdb.org/t/p/w500/7IiTTgloJzvGI1TAYymCfbfl3vT.jpg",
                BackdropPath = "https://image.tmdb.org/t/p/w1280/TU9NIjwzjoKPwQHoHshkFcQUCG.jpg",
                ReleaseYear = 2019,
                Overview = "Toda a família de Ki-taek está desempregada, vivendo em um porão sujo e apertado até que surge uma oportunidade inesperada.",
                ListType = WatchlistType.Watched,
                PersonalRating = 5,
                PersonalNotes = "Obra-prima de Bong Joon-ho. Mudança de tom na metade do filme é genial.",
                WatchedAt = DateTime.UtcNow.AddDays(-14),
                GenresCsv = "Comédia, Suspense / Thriller, Drama",
                RuntimeMinutes = 132,
                TmdbVoteAverage = 8.5
            },
            new WatchlistItem
            {
                UserId = demoUser.Id,
                TmdbMovieId = 335984,
                Title = "Blade Runner 2049",
                OriginalTitle = "Blade Runner 2049",
                PosterPath = "https://image.tmdb.org/t/p/w500/gajva2L0rPYkEWjzgFlBXCAVBE5.jpg",
                BackdropPath = "https://image.tmdb.org/t/p/w1280/ilRyazdMJwN05exqhwK4tMKBYZs.jpg",
                ReleaseYear = 2017,
                Overview = "Um novo blade runner, o oficial K da Polícia de Los Angeles, desenterra um segredo capaz de mergulhar o que resta da sociedade no caos.",
                ListType = WatchlistType.WantToWatch,
                GenresCsv = "Ficção Científica, Mistério, Drama",
                RuntimeMinutes = 164,
                TmdbVoteAverage = 7.6
            },
            new WatchlistItem
            {
                UserId = demoUser.Id,
                TmdbMovieId = 670,
                Title = "Oldboy",
                OriginalTitle = "올드보이",
                PosterPath = "https://image.tmdb.org/t/p/w500/pWDtjs568ZfOTMbURQBYuT4Qxka.jpg",
                BackdropPath = "https://image.tmdb.org/t/p/w1280/sd4xN5xi8tKRPrJOWwNiZEile7f.jpg",
                ReleaseYear = 2003,
                Overview = "Após ser sequestrado e aprisionado por 15 anos sem saber o motivo, Oh Dae-su é subitamente libertado e tem 5 dias para sua vingança.",
                ListType = WatchlistType.WantToWatch,
                GenresCsv = "Suspense / Thriller, Ação, Mistério",
                RuntimeMinutes = 120,
                TmdbVoteAverage = 8.3
            }
        );

        await db.SaveChangesAsync();
    }
}

public sealed class DesktopHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient(string name)
    {
        var handler = new TmdbApiKeyQueryHandler
        {
            InnerHandler = new HttpClientHandler()
        };

        var client = new HttpClient(handler, disposeHandler: true)
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        if (string.Equals(name, "TmdbClient", StringComparison.OrdinalIgnoreCase))
        {
            client.BaseAddress = new Uri("https://api.themoviedb.org/3/");
        }

        return client;
    }
}

public sealed class DesktopInMemoryConfiguration : IConfiguration
{
    private readonly Dictionary<string, string?> _values = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Tmdb:ApiKey"] = Environment.GetEnvironmentVariable("TMDB_API_KEY") ?? string.Empty
    };

    public string? this[string key]
    {
        get => _values.TryGetValue(key, out var val) ? val : null;
        set => _values[key] = value;
    }

    public IEnumerable<IConfigurationSection> GetChildren() => Enumerable.Empty<IConfigurationSection>();
    public IChangeToken GetReloadToken() => NoOpChangeToken.Instance;
    public IConfigurationSection GetSection(string key) => new DesktopConfigurationSection(this, key);
}

public sealed class DesktopConfigurationSection : IConfigurationSection
{
    private readonly IConfiguration _root;
    public DesktopConfigurationSection(IConfiguration root, string key)
    {
        _root = root;
        Key = key;
        Path = key;
    }

    public string? this[string key]
    {
        get => _root[$"{Path}:{key}"];
        set => _root[$"{Path}:{key}"] = value;
    }

    public string Key { get; }
    public string Path { get; }
    public string? Value
    {
        get => _root[Path];
        set => _root[Path] = value;
    }

    public IEnumerable<IConfigurationSection> GetChildren() => Enumerable.Empty<IConfigurationSection>();
    public IChangeToken GetReloadToken() => NoOpChangeToken.Instance;
    public IConfigurationSection GetSection(string key) => new DesktopConfigurationSection(_root, $"{Path}:{key}");
}

public sealed class NoOpChangeToken : IChangeToken
{
    public static readonly NoOpChangeToken Instance = new();
    public bool HasChanged => false;
    public bool ActiveChangeCallbacks => false;
    public IDisposable RegisterChangeCallback(Action<object?> callback, object? state) => new NoOpDisposable();

    private sealed class NoOpDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
