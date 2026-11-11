using Microsoft.EntityFrameworkCore;
using VibeMovies.Application.Interfaces;
using VibeMovies.Domain.Entities;
using VibeMovies.Infrastructure.Persistence;

namespace VibeMovies.Api.Infrastructure;

/// <summary>Usuário demo (demo@vibemovies.app / vibe1234) e alguns itens do Diário — só quando o banco está vazio.</summary>
public static class DemoDataSeeder
{
    public static async Task SeedAsync(VibeMoviesDbContext db, IAuthService authService)
    {
        if (await db.Users.AnyAsync()) return;

        var demoUser = new ApplicationUser
        {
            Id = Guid.Parse("11111111-2222-3333-4444-555555555555"),
            Name = "Cinéfilo Vibe",
            Email = "demo@vibemovies.app",
            PasswordHash = authService.HashPassword("vibe1234"),
            PreferredRegion = "BR",
            DefaultCity = "Joinville/SC",
            MyProvidersCsv = "Netflix,Max,Amazon Prime Video"
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
