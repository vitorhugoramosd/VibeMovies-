using System.IdentityModel.Tokens.Jwt;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using VibeMovies.Api.Infrastructure;
using VibeMovies.Application.Interfaces;
using VibeMovies.Application.Services;
using VibeMovies.Domain.Entities;
using VibeMovies.Infrastructure.External;
using VibeMovies.Infrastructure.Persistence;
using VibeMovies.Infrastructure.Repositories;

namespace VibeMovies.Api;

/// <summary>Monta a aplicação web (usado pelo Program.cs e pelos testes de integração).</summary>
public static class VibeMoviesApp
{
    public const string DefaultUrl = "http://localhost:5150";
    public const string AuthRateLimitPolicy = "auth";

    /// <summary>Informações calculadas na inicialização (expostas em /api/health).</summary>
    public sealed record RuntimeInfo(string DatabaseProvider, string? FrontendDistPath, string Version);

    public static async Task<WebApplication> BuildAsync(string[] args, Action<WebApplicationBuilder>? configure = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ContentRootPath = ResolveContentRoot()
        });

        configure?.Invoke(builder);

        // Garante http://localhost:5150 também em "dotnet VibeMovies.Api.dll" (sem launchSettings),
        // a menos que ASPNETCORE_URLS / --urls / Kestrel:Endpoints tenham sido informados.
        var cfg = builder.Configuration;
        if (string.IsNullOrWhiteSpace(cfg["urls"]) &&
            string.IsNullOrWhiteSpace(cfg["http_ports"]) &&
            string.IsNullOrWhiteSpace(cfg["https_ports"]) &&
            !cfg.GetSection("Kestrel:Endpoints").Exists())
        {
            builder.WebHost.UseUrls(DefaultUrl);
        }

        ConfigureServices(builder, out var usePostgres, out var keyDirectory);

        var frontendDist = ResolveFrontendDist(builder.Configuration, builder.Environment.ContentRootPath);
        var version = typeof(VibeMoviesApp).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion?.Split('+')[0]
                      ?? typeof(VibeMoviesApp).Assembly.GetName().Version?.ToString()
                      ?? "1.0.0";
        var runtimeInfo = new RuntimeInfo(usePostgres ? "PostgreSQL (Npgsql)" : "SQLite (Fallback Local)", frontendDist, version);
        builder.Services.AddSingleton(runtimeInfo);

        var app = builder.Build();

        // Inicialização do banco: EnsureCreated + upgrader idempotente de colunas + seed
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VibeMoviesDbContext>();
            var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("VibeMovies.Startup");
            await db.Database.EnsureCreatedAsync();
            await DatabaseSchemaUpgrader.UpgradeAsync(db, logger);
            await DemoDataSeeder.SeedAsync(db, authService);
        }

        ConfigurePipeline(app, runtimeInfo);
        return app;
    }

    private static string ResolveContentRoot()
    {
        var cwd = Directory.GetCurrentDirectory();
        if (File.Exists(Path.Combine(cwd, "appsettings.json"))) return cwd;
        var baseDir = AppContext.BaseDirectory;
        return File.Exists(Path.Combine(baseDir, "appsettings.json")) ? baseDir : cwd;
    }

    private static void ConfigureServices(WebApplicationBuilder builder, out bool usePostgres, out string keyDirectory)
    {
        var services = builder.Services;
        var config = builder.Configuration;

        services.AddControllers().AddApplicationPart(typeof(VibeMoviesApp).Assembly);
        services.Configure<ApiBehaviorOptions>(options =>
        {
            // Erros de validação: ValidationProblemDetails + "message" (pt-BR) para compatibilidade com o frontend
            options.InvalidModelStateResponseFactory = context =>
            {
                var problem = new ValidationProblemDetails(context.ModelState)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Dados inválidos"
                };
                var first = context.ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                    .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m));
                problem.Extensions["message"] = first ?? "Dados inválidos. Verifique os campos enviados.";
                return new BadRequestObjectResult(problem) { ContentTypes = { "application/problem+json" } };
            };
        });

        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "VibeMovies API",
                Version = "v1",
                Description = "Buscador de Filmes por Vibe, Disponibilidade em Streamings (JustWatch/TMDB), Radar de Cinemas Locais, Diário de Bordo e integração Jarvis."
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
                    new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
                    Array.Empty<string>()
                }
            });
        });

        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });

        // CORS: só origens locais (localhost / 127.0.0.1 / [::1], qualquer porta) + Cors:AllowedOrigins
        var allowedOrigins = (config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
            .Concat((config["Cors:AllowedOrigins"] ?? string.Empty).Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Select(o => o.TrimEnd('/'))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        services.AddCors(options =>
        {
            options.AddPolicy("VibeMoviesCors", policy =>
            {
                policy.SetIsOriginAllowed(origin => IsAllowedOrigin(origin, allowedOrigins))
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .WithExposedHeaders("Content-Disposition");
            });
        });

        // Rate limiting: login/registro limitados por IP (janela fixa)
        var authPermitLimit = config.GetValue("RateLimiting:AuthPermitLimit", 10);
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(AuthRateLimitPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = Math.Max(1, authPermitLimit),
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
            options.OnRejected = async (context, ct) =>
            {
                var problem = new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Muitas tentativas",
                    Detail = "Muitas tentativas em pouco tempo. Aguarde um minuto e tente novamente."
                };
                problem.Extensions["message"] = problem.Detail;
                context.HttpContext.Response.Headers.RetryAfter = "60";
                await context.HttpContext.Response.WriteAsJsonAsync(problem, (System.Text.Json.JsonSerializerOptions?)null, "application/problem+json", ct);
            };
        });

        // Banco: PostgreSQL (Npgsql) com fallback automático para SQLite
        var dbProvider = config["Database:Provider"] ?? "Auto";
        var postgresConn = config.GetConnectionString("Postgres")
                           ?? "Host=localhost;Port=5432;Database=vibemovies_db;Username=vibemovies;Password=vibemovies_secret";
        var sqliteConn = ResolveSqliteConnectionString(config.GetConnectionString("Sqlite") ?? "Data Source=vibemovies.db", builder.Environment.ContentRootPath);

        usePostgres = string.Equals(dbProvider, "Postgres", StringComparison.OrdinalIgnoreCase)
                      || (string.Equals(dbProvider, "Auto", StringComparison.OrdinalIgnoreCase) && IsPostgresPortOpen("localhost", 5432));
        var postgres = usePostgres;

        services.AddDbContext<VibeMoviesDbContext>(options =>
        {
            if (postgres) options.UseNpgsql(postgresConn);
            else options.UseSqlite(sqliteConn);
        });

        // JWT: chave da config/env (Jwt__SecretKey) ou chave aleatória persistida em jwt.key ao lado do banco
        keyDirectory = config["Jwt:KeyDirectory"] is { Length: > 0 } kd
            ? Path.GetFullPath(kd, builder.Environment.ContentRootPath)
            : (usePostgres ? builder.Environment.ContentRootPath : GetSqliteDirectory(sqliteConn, builder.Environment.ContentRootPath));

        using var startupLoggerFactory = LoggerFactory.Create(l => l.AddConsole());
        var signingKey = new SymmetricSecurityKey(JwtKeyProvider.ResolveSigningKey(config, keyDirectory, startupLoggerFactory.CreateLogger("VibeMovies.Jwt")));
        var jwtIssuer = config["Jwt:Issuer"] ?? "VibeMovies.Api";
        var jwtAudience = config["Jwt:Audience"] ?? "VibeMovies.Web";
        var expirationDays = Math.Clamp(config.GetValue("Jwt:ExpirationDays", 14), 1, 365);

        services
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

        services.AddAuthorization();

        // HttpClient do TMDB
        services.AddTransient<TmdbApiKeyQueryHandler>();
        services.AddHttpClient("TmdbClient", client =>
        {
            client.BaseAddress = new Uri("https://api.themoviedb.org/3/");
            client.Timeout = TimeSpan.FromSeconds(10);
        }).AddHttpMessageHandler<TmdbApiKeyQueryHandler>();

        // Camadas Application / Infrastructure
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IWatchlistRepository, WatchlistRepository>();
        services.AddScoped<ITmdbService, TmdbHttpClientService>();
        services.AddScoped<ICinemaRadarService, CinemaRadarScraperService>();
        services.AddScoped(sp => new SurprisePickService(sp.GetRequiredService<ITmdbService>()));
        services.AddScoped<RecommendationService>();
        services.AddScoped(sp => new JarvisService(
            sp.GetRequiredService<ITmdbService>(),
            sp.GetRequiredService<ICinemaRadarService>(),
            sp.GetRequiredService<IWatchlistRepository>(),
            sp.GetRequiredService<IUserRepository>(),
            sp.GetRequiredService<SurprisePickService>(),
            sp.GetRequiredService<TimeProvider>()));

        services.AddScoped<IAuthService>(sp =>
        {
            var userRepo = sp.GetRequiredService<IUserRepository>();
            return new AuthService(userRepo, user => GenerateJwtToken(user, signingKey, jwtIssuer, jwtAudience, expirationDays));
        });
    }

    private static void ConfigurePipeline(WebApplication app, RuntimeInfo info)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseResponseCompression();

        PhysicalFileProvider? spaFiles = null;
        if (info.FrontendDistPath is not null)
        {
            spaFiles = new PhysicalFileProvider(info.FrontendDistPath);
            app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = spaFiles });
            app.UseStaticFiles(new StaticFileOptions { FileProvider = spaFiles });
        }

        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseRouting();
        app.UseCors("VibeMoviesCors");
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        app.MapGet("/api/health", (RuntimeInfo runtime, IConfiguration configuration) => Results.Ok(new
        {
            status = "Healthy",
            version = runtime.Version,
            databaseProvider = runtime.DatabaseProvider,
            usingLiveTmdb = HasConfiguredTmdbKey(configuration),
            frontendServed = runtime.FrontendDistPath is not null,
            timestamp = DateTime.UtcNow
        }));

        if (spaFiles is not null)
        {
            var index = spaFiles.GetFileInfo("index.html");
            // SPA: qualquer rota que não seja /api ou /swagger (e não seja arquivo) devolve o index.html
            app.MapFallback(async context =>
            {
                if (context.Request.Path.StartsWithSegments("/api") || context.Request.Path.StartsWithSegments("/swagger"))
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    await context.Response.WriteAsJsonAsync(new { message = "Rota não encontrada." });
                    return;
                }

                context.Response.ContentType = "text/html; charset=utf-8";
                context.Response.Headers.CacheControl = "no-cache";
                await context.Response.SendFileAsync(index);
            });
        }
    }

    public static bool HasConfiguredTmdbKey(IConfiguration configuration) =>
        new[]
        {
            configuration["Tmdb:ApiKey"],
            configuration["Tmdb:ReadAccessToken"],
            Environment.GetEnvironmentVariable("TMDB__ApiKey"),
            Environment.GetEnvironmentVariable("TMDB_API_KEY")
        }.Any(k => !string.IsNullOrWhiteSpace(k));

    public static bool IsAllowedOrigin(string origin, IReadOnlySet<string> configured)
    {
        if (configured.Contains(origin.TrimEnd('/'))) return true;
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)) return false;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return false;
        return uri.Host is "localhost" or "127.0.0.1" or "[::1]" or "::1";
    }

    /// <summary>
    /// Pasta do build do frontend: <c>Frontend:DistPath</c>, ou <c>../../../frontend/dist</c> a partir do ContentRoot,
    /// ou subindo a partir do AppContext.BaseDirectory até achar <c>frontend/dist/index.html</c>.
    /// </summary>
    public static string? ResolveFrontendDist(IConfiguration configuration, string contentRoot)
    {
        static bool Valid(string? dir) => !string.IsNullOrWhiteSpace(dir) && File.Exists(Path.Combine(dir!, "index.html"));

        var configured = configuration["Frontend:DistPath"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            var full = Path.GetFullPath(configured, contentRoot);
            return Valid(full) ? full : null; // configurado explicitamente: não procura em outro lugar
        }

        var candidates = new List<string> { Path.GetFullPath(Path.Combine(contentRoot, "..", "..", "..", "frontend", "dist")) };
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 10 && dir is not null; i++, dir = dir.Parent)
            candidates.Add(Path.Combine(dir.FullName, "frontend", "dist"));

        return candidates.FirstOrDefault(Valid);
    }

    private static string ResolveSqliteConnectionString(string connectionString, string contentRoot)
    {
        var csb = new SqliteConnectionStringBuilder(connectionString);
        if (!string.IsNullOrWhiteSpace(csb.DataSource) &&
            csb.DataSource != ":memory:" &&
            !csb.DataSource.StartsWith("file:", StringComparison.OrdinalIgnoreCase) &&
            !Path.IsPathRooted(csb.DataSource))
        {
            csb.DataSource = Path.GetFullPath(csb.DataSource, contentRoot);
        }

        return csb.ToString();
    }

    private static string GetSqliteDirectory(string connectionString, string contentRoot)
    {
        var dataSource = new SqliteConnectionStringBuilder(connectionString).DataSource;
        if (string.IsNullOrWhiteSpace(dataSource) || dataSource == ":memory:" || dataSource.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
            return contentRoot;
        return Path.GetDirectoryName(Path.GetFullPath(dataSource, contentRoot)) ?? contentRoot;
    }

    public static string GenerateJwtToken(ApplicationUser user, SymmetricSecurityKey key, string issuer, string audience, int expirationDays)
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
            expires: DateTime.UtcNow.AddDays(expirationDays),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static bool IsPostgresPortOpen(string host, int port)
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
}
