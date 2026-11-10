using VibeMovies.Domain.Models;

namespace VibeMovies.Infrastructure.External;

/// <summary>
/// Catálogo curado de filmes com metadados reais do TMDB e provedores do Brasil (JustWatch/BR).
/// Utilizado como cache inicial e fallback automático caso uma chave da API do TMDB ainda não esteja configurada.
/// </summary>
public static class CuratedMovieDataset
{
    public static readonly StreamingProviderBadgeDto Netflix = new()
    {
        ProviderId = 8,
        ProviderName = "Netflix",
        LogoUrl = "https://image.tmdb.org/t/p/original/pbpMk2JmcoNnQwx5JGpXngfoWtp.jpg",
        DisplayPriority = 1
    };

    public static readonly StreamingProviderBadgeDto PrimeVideo = new()
    {
        ProviderId = 119,
        ProviderName = "Amazon Prime Video",
        LogoUrl = "https://image.tmdb.org/t/p/original/dQeAar5H991VYporEjUspolDarG.jpg",
        DisplayPriority = 2
    };

    public static readonly StreamingProviderBadgeDto Max = new()
    {
        ProviderId = 1899,
        ProviderName = "Max",
        LogoUrl = "https://image.tmdb.org/t/p/original/fksCUZ9QDWZMUwL2LgMtLckROUN.jpg",
        DisplayPriority = 3
    };

    public static readonly StreamingProviderBadgeDto DisneyPlus = new()
    {
        ProviderId = 337,
        ProviderName = "Disney+",
        LogoUrl = "https://image.tmdb.org/t/p/original/97yvRBw1GzX7fXprcF80er19ot.jpg",
        DisplayPriority = 4
    };

    public static readonly StreamingProviderBadgeDto Globoplay = new()
    {
        ProviderId = 307,
        ProviderName = "Globoplay",
        LogoUrl = "https://image.tmdb.org/t/p/original/7Cg8esVVXOijXAm1f1vrS7jVjcN.jpg",
        DisplayPriority = 5
    };

    public static readonly StreamingProviderBadgeDto Mubi = new()
    {
        ProviderId = 11,
        ProviderName = "MUBI",
        LogoUrl = "https://image.tmdb.org/t/p/original/bVR4Z1LCHY7gidXAJF5pMa4QrDS.jpg",
        DisplayPriority = 6
    };

    public static readonly StreamingProviderBadgeDto AppleTv = new()
    {
        ProviderId = 2,
        ProviderName = "Apple TV",
        LogoUrl = "https://image.tmdb.org/t/p/original/peURlLlr8jggOwK53fJ5wdQl05y.jpg",
        DisplayPriority = 7
    };

    public static readonly StreamingProviderBadgeDto GooglePlay = new()
    {
        ProviderId = 3,
        ProviderName = "Google Play Filmes",
        LogoUrl = "https://image.tmdb.org/t/p/original/8z7rC8uIDaTM91X0ZfkRf04ydj2.jpg",
        DisplayPriority = 8
    };

    public static readonly List<MovieCardDto> Movies = new()
    {
        new()
        {
            TmdbId = 11324,
            Title = "Ilha do Medo",
            OriginalTitle = "Shutter Island",
            Overview = "Em 1954, o agente federal Teddy Daniels investiga o desaparecimento de uma paciente em um hospital psiquiátrico isolado em uma ilha, enfrentando segredos sombrios e sua própria sanidade.",
            PosterUrl = "https://image.tmdb.org/t/p/w500/4GDy0PHYX3VRXUtwK5ysFbg3kEx.jpg",
            BackdropUrl = "https://image.tmdb.org/t/p/w1280/ecvy2kMxsJ60ej52beZ0F8EOGkL.jpg",
            ReleaseDate = "2010-02-18",
            ReleaseYear = 2010,
            RuntimeMinutes = 138,
            VoteAverage = 8.2,
            VoteCount = 23800,
            Genres = new() { "Suspense / Thriller", "Mistério", "Drama" },
            GenreIds = new() { 53, 9648, 18 },
            MatchedVibes = new() { "plot-twist", "terror-psicologico", "noite-chuvosa" },
            Keywords = new() { "plot twist", "psychological thriller", "island", "detective" },
            Director = "Martin Scorsese",
            Cast = new() { "Leonardo DiCaprio", "Mark Ruffalo", "Ben Kingsley" },
            WatchProviders = new()
            {
                Region = "BR",
                TmdbWatchLink = "https://www.themoviedb.org/movie/11324/watch?locale=BR",
                Flatrate = new() { Netflix, PrimeVideo },
                Rent = new() { AppleTv, GooglePlay },
                Buy = new() { AppleTv }
            }
        },
        new()
        {
            TmdbId = 670,
            Title = "Oldboy",
            OriginalTitle = "올드보이",
            Overview = "Após ser sequestrado e aprisionado por 15 anos em um quarto de hotel sem saber o motivo, Oh Dae-su é subitamente libertado e tem 5 dias para descobrir a verdade e consumar sua vingança.",
            PosterUrl = "https://image.tmdb.org/t/p/w500/pWDtjs568ZfOTMbURQBYuT4Qxka.jpg",
            BackdropUrl = "https://image.tmdb.org/t/p/w1280/sd4xN5xi8tKRPrJOWwNiZEile7f.jpg",
            ReleaseDate = "2003-11-21",
            ReleaseYear = 2003,
            RuntimeMinutes = 120,
            VoteAverage = 8.3,
            VoteCount = 8600,
            Genres = new() { "Suspense / Thriller", "Ação", "Mistério", "Drama" },
            GenreIds = new() { 53, 28, 9648, 18 },
            MatchedVibes = new() { "vinganca", "plot-twist", "noite-chuvosa" },
            Keywords = new() { "revenge", "plot twist", "betrayal", "imprisonment" },
            Director = "Park Chan-wook",
            Cast = new() { "Choi Min-sik", "Yoo Ji-tae", "Kang Hye-jung" },
            WatchProviders = new()
            {
                Region = "BR",
                TmdbWatchLink = "https://www.themoviedb.org/movie/670/watch?locale=BR",
                Flatrate = new() { Mubi, PrimeVideo },
                Rent = new() { AppleTv },
                Buy = new() { AppleTv }
            }
        },
        new()
        {
            TmdbId = 335984,
            Title = "Blade Runner 2049",
            OriginalTitle = "Blade Runner 2049",
            Overview = "Trinta anos após os eventos do primeiro filme, um novo blade runner, o oficial K da Polícia de Los Angeles, desenterra um segredo capaz de mergulhar o que resta da sociedade no caos.",
            PosterUrl = "https://image.tmdb.org/t/p/w500/gajva2L0rPYkEWjzgFlBXCAVBE5.jpg",
            BackdropUrl = "https://image.tmdb.org/t/p/w1280/ilRyazdMJwN05exqhwK4tMKBYZs.jpg",
            ReleaseDate = "2017-10-05",
            ReleaseYear = 2017,
            RuntimeMinutes = 164,
            VoteAverage = 7.6,
            VoteCount = 13500,
            Genres = new() { "Ficção Científica", "Mistério", "Drama" },
            GenreIds = new() { 878, 9648, 18 },
            MatchedVibes = new() { "cyberpunk", "noite-chuvosa", "pos-apocaliptico" },
            Keywords = new() { "cyberpunk", "artificial intelligence", "dystopia", "neo-noir" },
            Director = "Denis Villeneuve",
            Cast = new() { "Ryan Gosling", "Harrison Ford", "Ana de Armas" },
            WatchProviders = new()
            {
                Region = "BR",
                TmdbWatchLink = "https://www.themoviedb.org/movie/335984/watch?locale=BR",
                Flatrate = new() { Netflix, Max },
                Rent = new() { AppleTv, GooglePlay },
                Buy = new() { AppleTv, GooglePlay }
            }
        },
        new()
        {
            TmdbId = 807,
            Title = "Seven: Os Sete Crimes Capitais",
            OriginalTitle = "Se7en",
            Overview = "Dois detetives de homicídios caçam desesperadamente um serial killer meticuloso que utiliza os sete pecados capitais como motivação sob a chuva incessante da metrópole.",
            PosterUrl = "https://image.tmdb.org/t/p/w500/6yoghtyTpznpBik8EngEmJskVUO.jpg",
            BackdropUrl = "https://image.tmdb.org/t/p/w1280/i5H7zusQGsysGQ8i6P361Vnr0n2.jpg",
            ReleaseDate = "1995-09-22",
            ReleaseYear = 1995,
            RuntimeMinutes = 127,
            VoteAverage = 8.4,
            VoteCount = 20900,
            Genres = new() { "Crime", "Mistério", "Suspense / Thriller" },
            GenreIds = new() { 80, 9648, 53 },
            MatchedVibes = new() { "anos-90", "noite-chuvosa", "plot-twist", "vinganca" },
            Keywords = new() { "serial killer", "detective", "plot twist", "1990s" },
            Director = "David Fincher",
            Cast = new() { "Brad Pitt", "Morgan Freeman", "Gwyneth Paltrow" },
            WatchProviders = new()
            {
                Region = "BR",
                TmdbWatchLink = "https://www.themoviedb.org/movie/807/watch?locale=BR",
                Flatrate = new() { Max },
                Rent = new() { AppleTv, GooglePlay },
                Buy = new() { AppleTv }
            }
        },
        new()
        {
            TmdbId = 550,
            Title = "Clube da Luta",
            OriginalTitle = "Fight Club",
            Overview = "Um trabalhador de escritório com insônia crônica e um fabricante de sabonetes carismático formam um clube de luta clandestino que evolui para algo muito maior.",
            PosterUrl = "https://image.tmdb.org/t/p/w500/pB8BM7pdSp6B6Ih7QZ4DrQ3PmJK.jpg",
            BackdropUrl = "https://image.tmdb.org/t/p/w1280/hZkgoQYus5vegHoetLkCJzb17zJ.jpg",
            ReleaseDate = "1999-10-15",
            ReleaseYear = 1999,
            RuntimeMinutes = 139,
            VoteAverage = 8.4,
            VoteCount = 29400,
            Genres = new() { "Drama", "Suspense / Thriller" },
            GenreIds = new() { 18, 53 },
            MatchedVibes = new() { "anos-90", "plot-twist", "mind-bending" },
            Keywords = new() { "plot twist", "insomnia", "dual identity", "1990s" },
            Director = "David Fincher",
            Cast = new() { "Edward Norton", "Brad Pitt", "Helena Bonham Carter" },
            WatchProviders = new()
            {
                Region = "BR",
                TmdbWatchLink = "https://www.themoviedb.org/movie/550/watch?locale=BR",
                Flatrate = new() { DisneyPlus, PrimeVideo },
                Rent = new() { AppleTv },
                Buy = new() { AppleTv }
            }
        },
        new()
        {
            TmdbId = 603,
            Title = "Matrix",
            OriginalTitle = "The Matrix",
            Overview = "Um jovem programador e hacker descobre que a realidade como a conhecemos é uma simulação chamada Matrix, criada por máquinas inteligentes para subjugar a humanidade.",
            PosterUrl = "https://image.tmdb.org/t/p/w500/f89U3ADr1oiB1s9GkdPOEpXUk5H.jpg",
            BackdropUrl = "https://image.tmdb.org/t/p/w1280/fNG7i7RqMErkcqhohV2a6cV1Ehy.jpg",
            ReleaseDate = "1999-03-31",
            ReleaseYear = 1999,
            RuntimeMinutes = 136,
            VoteAverage = 8.2,
            VoteCount = 25600,
            Genres = new() { "Ação", "Ficção Científica" },
            GenreIds = new() { 28, 878 },
            MatchedVibes = new() { "cyberpunk", "anos-90", "mind-bending", "pos-apocaliptico" },
            Keywords = new() { "cyberpunk", "artificial intelligence", "virtual reality", "1990s" },
            Director = "Lana Wachowski, Lilly Wachowski",
            Cast = new() { "Keanu Reeves", "Laurence Fishburne", "Carrie-Anne Moss" },
            WatchProviders = new()
            {
                Region = "BR",
                TmdbWatchLink = "https://www.themoviedb.org/movie/603/watch?locale=BR",
                Flatrate = new() { Max, PrimeVideo },
                Rent = new() { AppleTv, GooglePlay },
                Buy = new() { AppleTv }
            }
        },
        new()
        {
            TmdbId = 76341,
            Title = "Mad Max: Estrada da Fúria",
            OriginalTitle = "Mad Max: Fury Road",
            Overview = "Assombrado por seu turbulento passado, Max se junta a Imperatriz Furiosa em uma fuga alucinante pelo deserto pós-apocalíptico contra o tirano Immortan Joe.",
            PosterUrl = "https://image.tmdb.org/t/p/w500/8tZYtuWezp8JbcsvHYO0O46tFbo.jpg",
            BackdropUrl = "https://image.tmdb.org/t/p/w1280/nlCHUWjY9XWbuEUQauCBgnY8ymF.jpg",
            ReleaseDate = "2015-05-14",
            ReleaseYear = 2015,
            RuntimeMinutes = 120,
            VoteAverage = 7.6,
            VoteCount = 22400,
            Genres = new() { "Ação", "Aventura", "Ficção Científica" },
            GenreIds = new() { 28, 12, 878 },
            MatchedVibes = new() { "pos-apocaliptico", "vinganca" },
            Keywords = new() { "post-apocalyptic future", "survival", "desert", "revenge" },
            Director = "George Miller",
            Cast = new() { "Tom Hardy", "Charlize Theron", "Nicholas Hoult" },
            WatchProviders = new()
            {
                Region = "BR",
                TmdbWatchLink = "https://www.themoviedb.org/movie/76341/watch?locale=BR",
                Flatrate = new() { Max },
                Rent = new() { AppleTv, GooglePlay },
                Buy = new() { AppleTv }
            }
        },
        new()
        {
            TmdbId = 27205,
            Title = "A Origem",
            OriginalTitle = "Inception",
            Overview = "Dom Cobb é um ladrão habilidoso na arte da extração de segredos do subconsciente durante o sonho. Sua última missão: plantar uma ideia na mente de um herdeiro corporativo.",
            PosterUrl = "https://image.tmdb.org/t/p/w500/oYuLEt3zVCKq57qu2F8dT7NIa6f.jpg",
            BackdropUrl = "https://image.tmdb.org/t/p/w1280/8ZTVqvKDQ8emSGUEMjsS4yHAwrp.jpg",
            ReleaseDate = "2010-08-06",
            ReleaseYear = 2010,
            RuntimeMinutes = 148,
            VoteAverage = 8.4,
            VoteCount = 36200,
            Genres = new() { "Ação", "Ficção Científica", "Aventura" },
            GenreIds = new() { 28, 878, 12 },
            MatchedVibes = new() { "mind-bending", "plot-twist" },
            Keywords = new() { "dream", "subconscious", "heist", "paradox" },
            Director = "Christopher Nolan",
            Cast = new() { "Leonardo DiCaprio", "Joseph Gordon-Levitt", "Elliot Page" },
            WatchProviders = new()
            {
                Region = "BR",
                TmdbWatchLink = "https://www.themoviedb.org/movie/27205/watch?locale=BR",
                Flatrate = new() { Max },
                Rent = new() { AppleTv, GooglePlay },
                Buy = new() { AppleTv }
            }
        },
        new()
        {
            TmdbId = 157336,
            Title = "Interestelar",
            OriginalTitle = "Interstellar",
            Overview = "Com a Terra à beira do colapso ambiental, uma equipe de exploradores viaja através de um buraco de minhoca no espaço na tentativa de garantir a sobrevivência da humanidade.",
            PosterUrl = "https://image.tmdb.org/t/p/w500/gEU2QniE6E77NI6lCU6MxlNBvIx.jpg",
            BackdropUrl = "https://image.tmdb.org/t/p/w1280/xJHokMbljvjADYdit5fK5VQsXEG.jpg",
            ReleaseDate = "2014-11-06",
            ReleaseYear = 2014,
            RuntimeMinutes = 169,
            VoteAverage = 8.4,
            VoteCount = 35400,
            Genres = new() { "Aventura", "Drama", "Ficção Científica" },
            GenreIds = new() { 12, 18, 878 },
            MatchedVibes = new() { "mind-bending", "emocionante", "pos-apocaliptico" },
            Keywords = new() { "space", "time dilation", "father daughter relationship", "survival" },
            Director = "Christopher Nolan",
            Cast = new() { "Matthew McConaughey", "Anne Hathaway", "Jessica Chastain" },
            WatchProviders = new()
            {
                Region = "BR",
                TmdbWatchLink = "https://www.themoviedb.org/movie/157336/watch?locale=BR",
                Flatrate = new() { Max, PrimeVideo },
                Rent = new() { AppleTv, GooglePlay },
                Buy = new() { AppleTv }
            }
        },
        new()
        {
            TmdbId = 545611,
            Title = "Tudo em Todo o Lugar ao Mesmo Tempo",
            OriginalTitle = "Everything Everywhere All at Once",
            Overview = "Uma imigrante chinesa de meia-idade é arrastada para uma aventura insana onde só ela pode salvar a existência explorando outros universos conectados às vidas que ela poderia ter levado.",
            PosterUrl = "https://image.tmdb.org/t/p/w500/w3LxiVYdWWRvEVdn5RYq6jIqkb1.jpg",
            BackdropUrl = "https://image.tmdb.org/t/p/w1280/ss0Os3uWJfQAENILHZUdX8Tt1OC.jpg",
            ReleaseDate = "2022-06-23",
            ReleaseYear = 2022,
            RuntimeMinutes = 139,
            VoteAverage = 7.8,
            VoteCount = 6400,
            Genres = new() { "Ação", "Aventura", "Ficção Científica", "Comédia" },
            GenreIds = new() { 28, 12, 878, 35 },
            MatchedVibes = new() { "mind-bending", "emocionante", "feel-good" },
            Keywords = new() { "multiverse", "family", "mother daughter", "absurdism" },
            Director = "Daniel Kwan, Daniel Scheinert",
            Cast = new() { "Michelle Yeoh", "Ke Huy Quan", "Stephanie Hsu" },
            WatchProviders = new()
            {
                Region = "BR",
                TmdbWatchLink = "https://www.themoviedb.org/movie/545611/watch?locale=BR",
                Flatrate = new() { PrimeVideo },
                Rent = new() { AppleTv, GooglePlay },
                Buy = new() { AppleTv }
            }
        },
        new()
        {
            TmdbId = 24,
            Title = "Kill Bill: Volume 1",
            OriginalTitle = "Kill Bill: Vol. 1",
            Overview = "A Noiva acorda de um coma de quatro anos após seu ex-chefe e seu esquadrão de assassinos tentarem matá-la no dia do seu casamento, dando início a uma caçada implacável.",
            PosterUrl = "https://image.tmdb.org/t/p/w500/v7TaX8kXMXs5yFFGR41guUDNcnB.jpg",
            BackdropUrl = "https://image.tmdb.org/t/p/w1280/lVy5Zqcty2NfemqKYbVJfdg44rK.jpg",
            ReleaseDate = "2003-10-10",
            ReleaseYear = 2003,
            RuntimeMinutes = 111,
            VoteAverage = 8.0,
            VoteCount = 17200,
            Genres = new() { "Ação", "Crime" },
            GenreIds = new() { 28, 80 },
            MatchedVibes = new() { "vinganca" },
            Keywords = new() { "revenge", "martial arts", "assassin", "katana" },
            Director = "Quentin Tarantino",
            Cast = new() { "Uma Thurman", "Lucy Liu", "Vivica A. Fox" },
            WatchProviders = new()
            {
                Region = "BR",
                TmdbWatchLink = "https://www.themoviedb.org/movie/24/watch?locale=BR",
                Flatrate = new() { PrimeVideo, Globoplay },
                Rent = new() { AppleTv },
                Buy = new() { AppleTv }
            }
        },
        new()
        {
            TmdbId = 496243,
            Title = "Parasita",
            OriginalTitle = "기생충",
            Overview = "Toda a família de Ki-taek está desempregada, vivendo em um porão sujo e apertado. Uma obra do acaso faz com que o filho adolescente comece a dar aulas de inglês para uma família rica.",
            PosterUrl = "https://image.tmdb.org/t/p/w500/7IiTTgloJzvGI1TAYymCfbfl3vT.jpg",
            BackdropUrl = "https://image.tmdb.org/t/p/w1280/TU9NIjwzjoKPwQHoHshkFcQUCG.jpg",
            ReleaseDate = "2019-11-07",
            ReleaseYear = 2019,
            RuntimeMinutes = 132,
            VoteAverage = 8.5,
            VoteCount = 18100,
            Genres = new() { "Comédia", "Suspense / Thriller", "Drama" },
            GenreIds = new() { 35, 53, 18 },
            MatchedVibes = new() { "plot-twist", "terror-psicologico", "noite-chuvosa" },
            Keywords = new() { "plot twist", "class differences", "dark comedy", "basement" },
            Director = "Bong Joon-ho",
            Cast = new() { "Song Kang-ho", "Lee Sun-kyun", "Cho Yeo-jeong" },
            WatchProviders = new()
            {
                Region = "BR",
                TmdbWatchLink = "https://www.themoviedb.org/movie/496243/watch?locale=BR",
                Flatrate = new() { Max, Globoplay },
                Rent = new() { AppleTv },
                Buy = new() { AppleTv }
            }
        },
        new()
        {
            TmdbId = 493922,
            Title = "Hereditário",
            OriginalTitle = "Hereditary",
            Overview = "Após a morte da reclusa avó, a família Graham começa a desvendar segredos crípticos e cada vez mais aterrorizantes sobre sua ancestralidade.",
            PosterUrl = "https://image.tmdb.org/t/p/w500/p9fmuz2Oj3HtEJEqbIwkFGUhVXD.jpg",
            BackdropUrl = "https://image.tmdb.org/t/p/w1280/4cDFJr4HnXN5AdPw4AKrmLlMWdO.jpg",
            ReleaseDate = "2018-06-21",
            ReleaseYear = 2018,
            RuntimeMinutes = 127,
            VoteAverage = 7.3,
            VoteCount = 7600,
            Genres = new() { "Terror", "Mistério", "Suspense / Thriller" },
            GenreIds = new() { 27, 9648, 53 },
            MatchedVibes = new() { "terror-psicologico", "plot-twist" },
            Keywords = new() { "psychological thriller", "grief", "occult", "family secrets" },
            Director = "Ari Aster",
            Cast = new() { "Toni Collette", "Alex Wolff", "Milly Shapiro" },
            WatchProviders = new()
            {
                Region = "BR",
                TmdbWatchLink = "https://www.themoviedb.org/movie/493922/watch?locale=BR",
                Flatrate = new() { Max, PrimeVideo },
                Rent = new() { AppleTv },
                Buy = new() { AppleTv }
            }
        },
        new()
        {
            TmdbId = 419430,
            Title = "Corra!",
            OriginalTitle = "Get Out",
            Overview = "Chris é um jovem fotógrafo negro que está prestes a conhecer os pais de Rose, sua namorada caucasiana, em uma propriedade isolada no interior — onde descobre uma verdade perturbadora.",
            PosterUrl = "https://image.tmdb.org/t/p/w500/tFXcEccSQMf3lfhfXKSU9iRBpa3.jpg",
            BackdropUrl = "https://image.tmdb.org/t/p/w1280/vZ7EVk7FaNEWYqlVWbFLHP8Z0LU.jpg",
            ReleaseDate = "2017-05-18",
            ReleaseYear = 2017,
            RuntimeMinutes = 104,
            VoteAverage = 7.6,
            VoteCount = 17200,
            Genres = new() { "Mistério", "Suspense / Thriller", "Terror" },
            GenreIds = new() { 9648, 53, 27 },
            MatchedVibes = new() { "terror-psicologico", "plot-twist" },
            Keywords = new() { "plot twist", "hypnosis", "psychological thriller" },
            Director = "Jordan Peele",
            Cast = new() { "Daniel Kaluuya", "Allison Williams", "Bradley Whitford" },
            WatchProviders = new()
            {
                Region = "BR",
                TmdbWatchLink = "https://www.themoviedb.org/movie/419430/watch?locale=BR",
                Flatrate = new() { Netflix, PrimeVideo },
                Rent = new() { AppleTv, GooglePlay },
                Buy = new() { AppleTv }
            }
        },
        new()
        {
            TmdbId = 120467,
            Title = "O Grande Hotel Budapeste",
            OriginalTitle = "The Grand Budapest Hotel",
            Overview = "As aventuras de Gustave H, um lendário concierge de um famoso hotel europeu entre as guerras, e Zero Moustafa, o mensageiro que se torna seu amigo mais confiável.",
            PosterUrl = "https://image.tmdb.org/t/p/w500/eWdyYQreja6JGCzqHWXpWHDrrPo.jpg",
            BackdropUrl = "https://image.tmdb.org/t/p/w1280/xHDynIimfsgj0ZOs0j5ma8v1vmM.jpg",
            ReleaseDate = "2014-07-03",
            ReleaseYear = 2014,
            RuntimeMinutes = 100,
            VoteAverage = 8.1,
            VoteCount = 14700,
            Genres = new() { "Comédia", "Drama", "Aventura" },
            GenreIds = new() { 35, 18, 12 },
            MatchedVibes = new() { "feel-good", "emocionante" },
            Keywords = new() { "hotel", "friendship", "feel-good", "painting theft" },
            Director = "Wes Anderson",
            Cast = new() { "Ralph Fiennes", "Tony Revolori", "Saoirse Ronan" },
            WatchProviders = new()
            {
                Region = "BR",
                TmdbWatchLink = "https://www.themoviedb.org/movie/120467/watch?locale=BR",
                Flatrate = new() { DisneyPlus },
                Rent = new() { AppleTv, GooglePlay },
                Buy = new() { AppleTv }
            }
        },
        new()
        {
            TmdbId = 77338,
            Title = "Intocáveis",
            OriginalTitle = "Intouchables",
            Overview = "Um aristocrata tetraplégico contrata um jovem da periferia de Paris para ser seu cuidador, dando origem a uma amizade improvável, hilária e transformadora.",
            PosterUrl = "https://image.tmdb.org/t/p/w500/1QU7HKgsQbGpzsJbJK4pAVQV9F5.jpg",
            BackdropUrl = "https://image.tmdb.org/t/p/w1280/bGksau9GGu0uJ8DJQ8DYc9JW5LM.jpg",
            ReleaseDate = "2012-08-31",
            ReleaseYear = 2012,
            RuntimeMinutes = 112,
            VoteAverage = 8.3,
            VoteCount = 17100,
            Genres = new() { "Drama", "Comédia" },
            GenreIds = new() { 18, 35 },
            MatchedVibes = new() { "feel-good", "emocionante" },
            Keywords = new() { "friendship", "feel-good", "true story", "caregiver" },
            Director = "Olivier Nakache, Éric Toledano",
            Cast = new() { "François Cluzet", "Omar Sy" },
            WatchProviders = new()
            {
                Region = "BR",
                TmdbWatchLink = "https://www.themoviedb.org/movie/77338/watch?locale=BR",
                Flatrate = new() { Globoplay, Mubi },
                Rent = new() { AppleTv },
                Buy = new() { AppleTv }
            }
        },
        new()
        {
            TmdbId = 9323,
            Title = "Ghost in the Shell: O Fantasma do Futuro",
            OriginalTitle = "攻殻機動隊",
            Overview = "No ano de 2029, a agente cibernética Major Motoko Kusanagi investiga um misterioso hacker conhecido como Mestre dos Fantoches que invade mentes cibernéticas.",
            PosterUrl = "https://image.tmdb.org/t/p/w500/9gC88zYUBARRSThcG93MvW14sqx.jpg",
            BackdropUrl = "https://image.tmdb.org/t/p/w1280/s2bqH035u0a2e3tzF7E9S8uB54r.jpg",
            ReleaseDate = "1995-11-18",
            ReleaseYear = 1995,
            RuntimeMinutes = 83,
            VoteAverage = 7.9,
            VoteCount = 3300,
            Genres = new() { "Animação", "Ação", "Ficção Científica" },
            GenreIds = new() { 16, 28, 878 },
            MatchedVibes = new() { "cyberpunk", "anos-90", "mind-bending" },
            Keywords = new() { "cyberpunk", "cyborg", "artificial intelligence", "1990s" },
            Director = "Mamoru Oshii",
            Cast = new() { "Atsuko Tanaka", "Akio Otsuka" },
            WatchProviders = new()
            {
                Region = "BR",
                TmdbWatchLink = "https://www.themoviedb.org/movie/9323/watch?locale=BR",
                Flatrate = new() { Netflix, PrimeVideo },
                Rent = new() { AppleTv },
                Buy = new() { AppleTv }
            }
        },
        new()
        {
            TmdbId = 680,
            Title = "Pulp Fiction: Tempo de Violência",
            OriginalTitle = "Pulp Fiction",
            Overview = "As vidas de dois assassinos da máfia, um boxeador, um gângster e sua esposa, e um par de bandidos de lanchonete se entrelaçam em quatro histórias de violência e redenção.",
            PosterUrl = "https://image.tmdb.org/t/p/w500/d5iIlFn5s0ImszYzBPb8JPIfbXD.jpg",
            BackdropUrl = "https://image.tmdb.org/t/p/w1280/suaEOtk1N1sgg2MTM7oZd2cfVp3.jpg",
            ReleaseDate = "1994-10-14",
            ReleaseYear = 1994,
            RuntimeMinutes = 154,
            VoteAverage = 8.5,
            VoteCount = 27800,
            Genres = new() { "Suspense / Thriller", "Crime" },
            GenreIds = new() { 53, 80 },
            MatchedVibes = new() { "anos-90", "vinganca" },
            Keywords = new() { "1990s", "nonlinear timeline", "hitman", "cult" },
            Director = "Quentin Tarantino",
            Cast = new() { "John Travolta", "Samuel L. Jackson", "Uma Thurman" },
            WatchProviders = new()
            {
                Region = "BR",
                TmdbWatchLink = "https://www.themoviedb.org/movie/680/watch?locale=BR",
                Flatrate = new() { PrimeVideo, Globoplay },
                Rent = new() { AppleTv, GooglePlay },
                Buy = new() { AppleTv }
            }
        },
        new()
        {
            TmdbId = 693134,
            Title = "Duna: Parte Dois",
            OriginalTitle = "Dune: Part Two",
            Overview = "Paul Atreides se une a Chani e aos Fremen enquanto busca vingança contra os conspiradores que destruíram sua família no deserto implacável de Arrakis.",
            PosterUrl = "https://image.tmdb.org/t/p/w500/1pdfLvkbY9ohJlCjQH2CZjjYVvJ.jpg",
            BackdropUrl = "https://image.tmdb.org/t/p/w1280/xOMo8BRK7PfcJv9JCnx7s5hj0PX.jpg",
            ReleaseDate = "2024-02-29",
            ReleaseYear = 2024,
            RuntimeMinutes = 166,
            VoteAverage = 8.3,
            VoteCount = 6200,
            Genres = new() { "Ficção Científica", "Aventura", "Ação" },
            GenreIds = new() { 878, 12, 28 },
            MatchedVibes = new() { "vinganca", "pos-apocaliptico" },
            Keywords = new() { "revenge", "desert", "prophecy", "empire" },
            Director = "Denis Villeneuve",
            Cast = new() { "Timothée Chalamet", "Zendaya", "Rebecca Ferguson" },
            WatchProviders = new()
            {
                Region = "BR",
                TmdbWatchLink = "https://www.themoviedb.org/movie/693134/watch?locale=BR",
                Flatrate = new() { Max },
                Rent = new() { AppleTv, GooglePlay },
                Buy = new() { AppleTv }
            }
        },
        new()
        {
            TmdbId = 1000837,
            Title = "Ainda Estou Aqui",
            OriginalTitle = "Ainda Estou Aqui",
            Overview = "No Rio de Janeiro do início dos anos 1970, Eunice Paiva é forçada a se reinventar como ativista e matriarca após o desaparecimento de seu marido Rubens Paiva.",
            PosterUrl = "https://image.tmdb.org/t/p/w500/qNLMPY3KLrYgTX2QZ5iEwwO1Rz.jpg",
            BackdropUrl = "https://image.tmdb.org/t/p/w1280/3V4kLQg0kSqPLctI5ziYWabAZYF.jpg",
            ReleaseDate = "2024-11-07",
            ReleaseYear = 2024,
            RuntimeMinutes = 135,
            VoteAverage = 8.6,
            VoteCount = 1950,
            Genres = new() { "Drama" },
            GenreIds = new() { 18 },
            MatchedVibes = new() { "emocionante" },
            Keywords = new() { "family", "true story", "brazil", "resilience" },
            Director = "Walter Salles",
            Cast = new() { "Fernanda Torres", "Selton Mello", "Fernanda Montenegro" },
            WatchProviders = new()
            {
                Region = "BR",
                TmdbWatchLink = "https://www.themoviedb.org/movie/1000837/watch?locale=BR",
                Flatrate = new() { Globoplay },
                Rent = new() { AppleTv },
                Buy = new() { AppleTv }
            }
        }
    };
}
