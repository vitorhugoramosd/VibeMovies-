using System.IdentityModel.Tokens.Jwt;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using VibeMovies.Application.Interfaces;
using VibeMovies.Application.Services;
using VibeMovies.Domain.Entities;
using VibeMovies.Infrastructure.External;
using VibeMovies.Infrastructure.Persistence;
using VibeMovies.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "VibeMovies API",
        Version = "v1",
        Description = "Buscador de Filmes por Vibe, Disponibilidade em Streamings (JustWatch/TMDB), Radar de Cinemas Locais e Diário de Bordo."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Informe o token JWT no formato: Bearer {seu_token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// Configuração de CORS para permitir o frontend React
builder.Services.AddCors(options =>
{
    options.AddPolicy("VibeMoviesCors", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Detecção de PostgreSQL (Npgsql) com fallback automático para SQLite caso o container Postgres não esteja ativo
var dbProvider = builder.Configuration["Database:Provider"] ?? "Auto";
var postgresConn = builder.Configuration.GetConnectionString("Postgres")
                   ?? "Host=localhost;Port=5432;Database=vibemovies_db;Username=vibemovies;Password=vibemovies_secret";
var sqliteConn = builder.Configuration.GetConnectionString("Sqlite") ?? "Data Source=vibemovies.db";

bool usePostgres = string.Equals(dbProvider, "Postgres", StringComparison.OrdinalIgnoreCase)
                   || (string.Equals(dbProvider, "Auto", StringComparison.OrdinalIgnoreCase) && IsPostgresPortOpen("localhost", 5432));

builder.Services.AddDbContext<VibeMoviesDbContext>(options =>
{
    if (usePostgres)
    {
        options.UseNpgsql(postgresConn);
    }
    else
    {
        options.UseSqlite(sqliteConn);
    }
});

// Configuração de Autenticação JWT
var jwtSecret = builder.Configuration["Jwt:SecretKey"] ?? "VibeMovies_SuperSecret_Jwt_Signing_Key_2026_CinemaLovers!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "VibeMovies.Api";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "VibeMovies.Web";
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = signingKey
        };
    });

builder.Services.AddAuthorization();

// Registro do IHttpClientFactory para o TMDB e Scraper
builder.Services.AddTransient<TmdbApiKeyQueryHandler>();
builder.Services.AddHttpClient("TmdbClient", client =>
{
    client.BaseAddress = new Uri("https://api.themoviedb.org/3/");
    client.Timeout = TimeSpan.FromSeconds(10);
}).AddHttpMessageHandler<TmdbApiKeyQueryHandler>();

// Injeção de Dependências das Camadas Application e Infrastructure
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IWatchlistRepository, WatchlistRepository>();
builder.Services.AddScoped<ITmdbService, TmdbHttpClientService>();
builder.Services.AddScoped<ICinemaRadarService, CinemaRadarScraperService>();

builder.Services.AddScoped<IAuthService>(sp =>
{
    var userRepo = sp.GetRequiredService<IUserRepository>();
    return new AuthService(userRepo, user => GenerateJwtToken(user, signingKey, jwtIssuer, jwtAudience));
});

var app = builder.Build();

// Inicialização do Banco de Dados e Seed Inicial
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<VibeMoviesDbContext>();
    var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
    await db.Database.EnsureCreatedAsync();
    await SeedDemoDataAsync(db, authService);
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("VibeMoviesCors");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "Healthy",
    databaseProvider = usePostgres ? "PostgreSQL (Npgsql)" : "SQLite (Fallback Local)",
    timestamp = DateTime.UtcNow
}));

app.Run();

static string GenerateJwtToken(ApplicationUser user, SymmetricSecurityKey key, string issuer, string audience)
{
    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new(ClaimTypes.Name, user.Name),
        new(ClaimTypes.Email, user.Email),
        new("region", user.PreferredRegion),
        new("city", user.DefaultCity)
    };

    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var token = new JwtSecurityToken(
        issuer: issuer,
        audience: audience,
        claims: claims,
        expires: DateTime.UtcNow.AddDays(14),
        signingCredentials: creds);

    return new JwtSecurityTokenHandler().WriteToken(token);
}

static bool IsPostgresPortOpen(string host, int port)
{
    try
    {
        using var tcp = new TcpClient();
        var connectTask = tcp.ConnectAsync(host, port);
        return connectTask.Wait(TimeSpan.FromMilliseconds(350)) && tcp.Connected;
    }
    catch
    {
        return false;
    }
}

static async Task SeedDemoDataAsync(VibeMoviesDbContext db, IAuthService authService)
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
