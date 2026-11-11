import {
  AuthResponseDto,
  CinemaRadarResponseDto,
  DiscoverResultDto,
  GenreDto,
  MovieCardDto,
  SurpriseResponseDto,
  UserProfileDto,
  VibeDefinition,
  WatchlistItem,
  WatchlistStatsDto,
  WatchlistType
} from '../types/api';
import { providerMatches } from '../lib/constants';

export const FALLBACK_VIBES: VibeDefinition[] = [
  {
    id: 'explodir-cabeca',
    name: 'Explodir a Cabeça',
    subtitle: 'Plot Twists & Sci-Fi Cerebral',
    description: 'Realidades paralelas, paradoxos temporais e finais que obrigam você a repensar tudo o que acabou de assistir.',
    emoji: '🤯',
    accentColor: '#8B5CF6',
    tmdbKeywordIds: [310, 4379, 10085, 9882],
    suggestedGenreIds: [878, 53, 9648],
    minVoteAverage: 7.2,
    sampleTags: ['Plot Twist', 'Viagem no Tempo', 'Mind-Bending', 'Paradoxo']
  },
  {
    id: 'conforto-domingo',
    name: 'Conforto de Domingo',
    subtitle: 'Sessão da Tarde & Nostalgia Anos 90/2000',
    description: 'Filmes acolhedores, clássicos nostálgicos de 1990 a 2010 e histórias leves para assistir tomando café.',
    emoji: '☕',
    accentColor: '#F59E0B',
    tmdbKeywordIds: [9715, 6054, 18035],
    suggestedGenreIds: [35, 10751, 12, 10749],
    releaseDateGte: '1990-01-01',
    releaseDateLte: '2012-12-31',
    minVoteAverage: 6.8,
    sampleTags: ['Feel-Good', 'Anos 90/2000', 'Acolhedor', 'Nostalgia']
  },
  {
    id: 'tensao-maxima',
    name: 'Tensão do Início ao Fim',
    subtitle: 'Thrillers Implacáveis & Ritmo Frenético',
    description: 'Narrativas claustrofóbicas, corridas contra o relógio e suspense psicológico sem nenhum minuto de respiro.',
    emoji: '⚡',
    accentColor: '#EF4444',
    tmdbKeywordIds: [10349, 12565, 9937],
    suggestedGenreIds: [53, 80, 27],
    minVoteAverage: 7.0,
    sampleTags: ['Suspense', 'Adrenalina', 'Psicológico', 'Sem Respiro']
  },
  {
    id: 'chorar-desidratar',
    name: 'Para Chorar no Escuro',
    subtitle: 'Dramas Humanos & Catarse Emocional',
    description: 'Retratos profundos sobre memória, amor, despedidas e conexões humanas que ficam na alma por semanas.',
    emoji: '🌧️',
    accentColor: '#3B82F6',
    tmdbKeywordIds: [9673, 10683, 5600],
    suggestedGenreIds: [18, 10749],
    minVoteAverage: 7.5,
    sampleTags: ['Emocionante', 'Catarse', 'Memória', 'Oscar Worthy']
  },
  {
    id: 'cyber-neon',
    name: 'Estética Neon & Noite Urbana',
    subtitle: 'Neo-Noir, Synthwave & Metrópoles',
    description: 'Luzes de neon refletidas no asfalto molhado, trilhas eletrônicas hipnóticas, anti-heróis solitários e cyberpunk.',
    emoji: '🌃',
    accentColor: '#EC4899',
    tmdbKeywordIds: [12190, 2106, 4565],
    suggestedGenreIds: [878, 80, 53],
    minVoteAverage: 7.0,
    sampleTags: ['Cyberpunk', 'Neo-Noir', 'Synthwave', 'Visual Hipnótico']
  },
  {
    id: 'curto-direto',
    name: 'Curto & Direto ao Ponto',
    subtitle: 'Até 100 Minutos • Zero Enrolação',
    description: 'Quando você tem pouco tempo antes de dormir e quer um filmaço enxuto, intenso e abaixo de 1h40.',
    emoji: '⏱️',
    accentColor: '#10B981',
    tmdbKeywordIds: [10085, 9937],
    suggestedGenreIds: [53, 35, 27, 16],
    maxRuntimeMinutes: 100,
    minVoteAverage: 7.0,
    sampleTags: ['< 100 min', 'Ritmo Ágil', 'Direto ao Ponto']
  }
];

export const FALLBACK_GENRES: GenreDto[] = [
  { id: 878, name: 'Ficção Científica', icon: '🚀' },
  { id: 53, name: 'Thriller / Suspense', icon: '🔪' },
  { id: 18, name: 'Drama', icon: '🎭' },
  { id: 35, name: 'Comédia', icon: '🍿' },
  { id: 80, name: 'Crime / Policial', icon: '🕵️' },
  { id: 27, name: 'Terror Psicológico', icon: '👻' },
  { id: 9648, name: 'Mistério', icon: '🧩' },
  { id: 10749, name: 'Romance', icon: '💜' },
  { id: 16, name: 'Animação', icon: '✨' },
  { id: 12, name: 'Aventura', icon: '🧭' }
];

const PROVIDERS = {
  netflix: {
    providerId: 8,
    providerName: 'Netflix',
    logoUrl: 'https://image.tmdb.org/t/p/original/pbpMk2JmcoNnQwx5JGpXngfoWtp.jpg',
    displayPriority: 1
  },
  prime: {
    providerId: 119,
    providerName: 'Amazon Prime Video',
    logoUrl: 'https://image.tmdb.org/t/p/original/dQeAar5H991VYporEjUspolDarG.jpg',
    displayPriority: 2
  },
  max: {
    providerId: 1899,
    providerName: 'Max',
    logoUrl: 'https://image.tmdb.org/t/p/original/fksCUZ9QDWZMUwL2LgMtLckROUN.jpg',
    displayPriority: 3
  },
  disney: {
    providerId: 337,
    providerName: 'Disney+',
    logoUrl: 'https://image.tmdb.org/t/p/original/97yvRBw1GzX7fXprcF80er19ot.jpg',
    displayPriority: 4
  },
  mubi: {
    providerId: 11,
    providerName: 'MUBI',
    logoUrl: 'https://image.tmdb.org/t/p/original/bVR4Z1LCHY7gidXAJF5pMa4QrDS.jpg',
    displayPriority: 5
  },
  globoplay: {
    providerId: 307,
    providerName: 'Globoplay',
    logoUrl: 'https://image.tmdb.org/t/p/original/7Cg8esVVXOijXAm1f1vrS7jVjcN.jpg',
    displayPriority: 6
  },
  apple: {
    providerId: 350,
    providerName: 'Apple TV',
    logoUrl: 'https://image.tmdb.org/t/p/original/2E03IAZsX4ZaUqM7tXlctEPMGWS.jpg',
    displayPriority: 7
  }
};

export const FALLBACK_MOVIES: MovieCardDto[] = [
  {
    tmdbId: 27205,
    title: 'A Origem',
    originalTitle: 'Inception',
    overview: 'Dom Cobb é um ladrão com a rara habilidade de roubar segredos do inconsciente durante o estado de sono, mas recebe a missão inversa: plantar uma ideia na mente de um herdeiro.',
    posterUrl: 'https://image.tmdb.org/t/p/w500/9gk7adHYeDvHkCSEqAvQNLV5Uge.jpg',
    backdropUrl: 'https://image.tmdb.org/t/p/w1280/8ZTVqvKDQ8emSGUEMjsS4yHAwrp.jpg',
    releaseDate: '2010-07-15',
    releaseYear: 2010,
    runtimeMinutes: 148,
    voteAverage: 8.4,
    voteCount: 36100,
    genres: ['Ficção Científica', 'Thriller / Suspense', 'Aventura'],
    genreIds: [878, 53, 12],
    matchedVibes: ['explodir-cabeca', 'tensao-maxima'],
    keywords: ['sonhos lúcidos', 'subconsciente', 'paradoxo', 'arquitetura mental'],
    director: 'Christopher Nolan',
    cast: ['Leonardo DiCaprio', 'Joseph Gordon-Levitt', 'Elliot Page', 'Cillian Murphy'],
    vibeMatchScore: 98,
    watchProviders: {
      region: 'BR',
      tmdbWatchLink: 'https://www.themoviedb.org/movie/27205/watch?locale=BR',
      flatrate: [PROVIDERS.max, PROVIDERS.prime],
      rent: [PROVIDERS.apple],
      buy: [PROVIDERS.apple]
    }
  },
  {
    tmdbId: 1124,
    title: 'O Grande Truque',
    originalTitle: 'The Prestige',
    overview: 'Na Londres do século XIX, dois mágicos rivais entram em uma obsessiva disputa para criar a ilusão definitiva de teletransporte, sacrificando tudo pelo segredo.',
    posterUrl: 'https://image.tmdb.org/t/p/w500/bdN3gXuIZYaJP7ftKK2sU0nPtEA.jpg',
    backdropUrl: 'https://image.tmdb.org/t/p/w1280/mfJepkInUbiZ0mFXFhDNz8ko6Zr.jpg',
    releaseDate: '2006-10-19',
    releaseYear: 2006,
    runtimeMinutes: 130,
    voteAverage: 8.2,
    voteCount: 15800,
    genres: ['Mistério', 'Thriller / Suspense', 'Ficção Científica'],
    genreIds: [9648, 53, 878],
    matchedVibes: ['explodir-cabeca', 'tensao-maxima'],
    keywords: ['plot twist', 'obsessão', 'ilusionismo', 'duplo'],
    director: 'Christopher Nolan',
    cast: ['Christian Bale', 'Hugh Jackman', 'Scarlett Johansson', 'David Bowie'],
    vibeMatchScore: 97,
    watchProviders: {
      region: 'BR',
      tmdbWatchLink: 'https://www.themoviedb.org/movie/1124/watch?locale=BR',
      flatrate: [PROVIDERS.max],
      rent: [PROVIDERS.prime, PROVIDERS.apple],
      buy: [PROVIDERS.apple]
    }
  },
  {
    tmdbId: 329865,
    title: 'A Chegada',
    originalTitle: 'Arrival',
    overview: 'Quando doze naves misteriosas pousam ao redor da Terra, a linguista Louise Banks é recrutada para decifrar a linguagem heptápode — descobrindo uma nova percepção do próprio tempo.',
    posterUrl: 'https://image.tmdb.org/t/p/w500/x2FJsf1ElAgr63Y3PNPtJrcmpoe.jpg',
    backdropUrl: 'https://image.tmdb.org/t/p/w1280/yIZ1xendyqKvY3FGeeUYUd5X9Mm.jpg',
    releaseDate: '2016-11-10',
    releaseYear: 2016,
    runtimeMinutes: 116,
    voteAverage: 7.6,
    voteCount: 17400,
    genres: ['Ficção Científica', 'Drama', 'Mistério'],
    genreIds: [878, 18, 9648],
    matchedVibes: ['explodir-cabeca', 'chorar-desidratar'],
    keywords: ['tempo não-linear', 'linguística', 'primeiro contato', 'memória'],
    director: 'Denis Villeneuve',
    cast: ['Amy Adams', 'Jeremy Renner', 'Forest Whitaker'],
    vibeMatchScore: 95,
    watchProviders: {
      region: 'BR',
      tmdbWatchLink: 'https://www.themoviedb.org/movie/329865/watch?locale=BR',
      flatrate: [PROVIDERS.netflix, PROVIDERS.prime],
      rent: [PROVIDERS.apple],
      buy: [PROVIDERS.apple]
    }
  },
  {
    tmdbId: 37165,
    title: 'O Show de Truman',
    originalTitle: 'The Truman Show',
    overview: 'Truman Burbank vive em uma ilha pitoresca sem saber que cada segundo de sua existência é transmitido ao vivo para bilhões de espectadores em um estúdio gigantesco.',
    posterUrl: 'https://image.tmdb.org/t/p/w500/vuza0WqY239yBXOadKlGwJsZJFE.jpg',
    backdropUrl: 'https://image.tmdb.org/t/p/w1280/aCHn2TXYJfzPXQKA6r9mKPbMlUB.jpg',
    releaseDate: '1998-06-04',
    releaseYear: 1998,
    runtimeMinutes: 103,
    voteAverage: 8.1,
    voteCount: 18200,
    genres: ['Comédia', 'Drama', 'Ficção Científica'],
    genreIds: [35, 18, 878],
    matchedVibes: ['conforto-domingo', 'explodir-cabeca'],
    keywords: ['anos 90', 'realidade simulada', 'liberdade', 'clássico'],
    director: 'Peter Weir',
    cast: ['Jim Carrey', 'Ed Harris', 'Laura Linney'],
    vibeMatchScore: 94,
    watchProviders: {
      region: 'BR',
      tmdbWatchLink: 'https://www.themoviedb.org/movie/37165/watch?locale=BR',
      flatrate: [PROVIDERS.prime, PROVIDERS.globoplay],
      rent: [PROVIDERS.apple],
      buy: [PROVIDERS.apple]
    }
  },
  {
    tmdbId: 194,
    title: 'O Fabuloso Destino de Amélie Poulain',
    originalTitle: "Le Fabuleux Destin d'Amélie Poulain",
    overview: 'Em Montmartre, a jovem garçonete Amélie decide transformar secretamente a vida das pessoas ao seu redor com pequenos gestos poéticos enquanto busca seu próprio amor.',
    posterUrl: 'https://image.tmdb.org/t/p/w500/nSxDa3M9aMvGVLoItzWTepQ5h5d.jpg',
    backdropUrl: 'https://image.tmdb.org/t/p/w1280/s2bT29y0ngXxxu2IA8AOzzXTRhd.jpg',
    releaseDate: '2001-04-25',
    releaseYear: 2001,
    runtimeMinutes: 122,
    voteAverage: 7.9,
    voteCount: 11400,
    genres: ['Comédia', 'Romance'],
    genreIds: [35, 10749],
    matchedVibes: ['conforto-domingo'],
    keywords: ['paris', 'feel-good', 'poético', 'anos 2000'],
    director: 'Jean-Pierre Jeunet',
    cast: ['Audrey Tautou', 'Mathieu Kassovitz'],
    vibeMatchScore: 96,
    watchProviders: {
      region: 'BR',
      tmdbWatchLink: 'https://www.themoviedb.org/movie/194/watch?locale=BR',
      flatrate: [PROVIDERS.mubi],
      rent: [PROVIDERS.apple],
      buy: []
    }
  },
  {
    tmdbId: 244786,
    title: 'Whiplash: Em Busca da Perfeição',
    originalTitle: 'Whiplash',
    overview: 'Um jovem baterista de jazz ambicioso entra em rota de colisão psicológica com o implacável maestro Terence Fletcher no mais prestigiado conservatório dos EUA.',
    posterUrl: 'https://image.tmdb.org/t/p/w500/7fn624j5lj3xTme2SgiLCeuedmO.jpg',
    backdropUrl: 'https://image.tmdb.org/t/p/w1280/vRGqZ0b3X6tC1wG7R9Y5xK2s6wM.jpg',
    releaseDate: '2014-10-10',
    releaseYear: 2014,
    runtimeMinutes: 106,
    voteAverage: 8.4,
    voteCount: 14900,
    genres: ['Drama', 'Thriller / Suspense'],
    genreIds: [18, 53],
    matchedVibes: ['tensao-maxima'],
    keywords: ['jazz', 'obsessão', 'adrenalina', 'ritmo frenético'],
    director: 'Damien Chazelle',
    cast: ['Miles Teller', 'J.K. Simmons'],
    vibeMatchScore: 97,
    watchProviders: {
      region: 'BR',
      tmdbWatchLink: 'https://www.themoviedb.org/movie/244786/watch?locale=BR',
      flatrate: [PROVIDERS.netflix, PROVIDERS.mubi],
      rent: [PROVIDERS.prime, PROVIDERS.apple],
      buy: [PROVIDERS.apple]
    }
  },
  {
    tmdbId: 335984,
    title: 'Blade Runner 2049',
    originalTitle: 'Blade Runner 2049',
    overview: 'Trinta anos após os eventos do primeiro filme, um novo blade runner, o oficial K da polícia de Los Angeles, desenterra um segredo capaz de mergulhar a sociedade no caos.',
    posterUrl: 'https://image.tmdb.org/t/p/w500/gajva2L0rPYkEWjzgFlBXCAVBE5.jpg',
    backdropUrl: 'https://image.tmdb.org/t/p/w1280/ilRyazdMJwN05exqhwK4tMKBYZs.jpg',
    releaseDate: '2017-10-04',
    releaseYear: 2017,
    runtimeMinutes: 164,
    voteAverage: 7.6,
    voteCount: 13600,
    genres: ['Ficção Científica', 'Drama', 'Mistério'],
    genreIds: [878, 18, 9648],
    matchedVibes: ['cyber-neon', 'explodir-cabeca'],
    keywords: ['cyberpunk', 'neon', 'distopia', 'inteligência artificial'],
    director: 'Denis Villeneuve',
    cast: ['Ryan Gosling', 'Harrison Ford', 'Ana de Armas'],
    vibeMatchScore: 98,
    watchProviders: {
      region: 'BR',
      tmdbWatchLink: 'https://www.themoviedb.org/movie/335984/watch?locale=BR',
      flatrate: [PROVIDERS.netflix, PROVIDERS.max],
      rent: [PROVIDERS.prime, PROVIDERS.apple],
      buy: [PROVIDERS.apple]
    }
  },
  {
    tmdbId: 64690,
    title: 'Drive',
    originalTitle: 'Drive',
    overview: 'Um dublê de cinema de Hollywood que trabalha como motorista de fuga nas noites de Los Angeles acaba virando alvo dos criminosos mais perigosos da cidade ao ajudar sua vizinha.',
    posterUrl: 'https://image.tmdb.org/t/p/w500/602vevIURmpDfzbnv5Ubi6wIkQm.jpg',
    backdropUrl: 'https://image.tmdb.org/t/p/w1280/921v4B5I4lB0k6vA4kP8mY5mR7s.jpg',
    releaseDate: '2011-09-16',
    releaseYear: 2011,
    runtimeMinutes: 100,
    voteAverage: 7.6,
    voteCount: 12400,
    genres: ['Crime / Policial', 'Thriller / Suspense', 'Drama'],
    genreIds: [80, 53, 18],
    matchedVibes: ['cyber-neon', 'tensao-maxima', 'curto-direto'],
    keywords: ['synthwave', 'neo-noir', 'noite urbana', 'anti-herói'],
    director: 'Nicolas Winding Refn',
    cast: ['Ryan Gosling', 'Carey Mulligan', 'Oscar Isaac'],
    vibeMatchScore: 95,
    watchProviders: {
      region: 'BR',
      tmdbWatchLink: 'https://www.themoviedb.org/movie/64690/watch?locale=BR',
      flatrate: [PROVIDERS.prime, PROVIDERS.mubi],
      rent: [PROVIDERS.apple],
      buy: [PROVIDERS.apple]
    }
  },
  {
    tmdbId: 965150,
    title: 'Aftersun',
    originalTitle: 'Aftersun',
    overview: 'Sophie reflete sobre a alegria compartilhada e a melancolia silenciosa de umas férias que fez com seu jovem pai vinte anos antes em um resort na Turquia.',
    posterUrl: 'https://image.tmdb.org/t/p/w500/evKz859fbVIdHlN7kX9t5D9hM8O.jpg',
    backdropUrl: 'https://image.tmdb.org/t/p/w1280/2vFuG6bWGyQUzYS9d69E5l85nIz.jpg',
    releaseDate: '2022-10-21',
    releaseYear: 2022,
    runtimeMinutes: 102,
    voteAverage: 7.7,
    voteCount: 3800,
    genres: ['Drama'],
    genreIds: [18],
    matchedVibes: ['chorar-desidratar'],
    keywords: ['memória', 'paternidade', 'melancolia', 'anos 90'],
    director: 'Charlotte Wells',
    cast: ['Paul Mescal', 'Frankie Corio'],
    vibeMatchScore: 97,
    watchProviders: {
      region: 'BR',
      tmdbWatchLink: 'https://www.themoviedb.org/movie/965150/watch?locale=BR',
      flatrate: [PROVIDERS.mubi, PROVIDERS.netflix],
      rent: [PROVIDERS.prime],
      buy: []
    }
  },
  {
    tmdbId: 38,
    title: 'Brilho Eterno de uma Mente sem Lembranças',
    originalTitle: 'Eternal Sunshine of the Spotless Mind',
    overview: 'Quando seu relacionamento com Clementine se deteriora, Joel descobre que ela contratou uma clínica experimental para apagar todas as memórias dele — e decide fazer o mesmo.',
    posterUrl: 'https://image.tmdb.org/t/p/w500/5MwkWH9tYHv3mV9OdYTMR5qreIz.jpg',
    backdropUrl: 'https://image.tmdb.org/t/p/w1280/7y3eYvTsGjxPYDdS3k7Bo9fJ3bT.jpg',
    releaseDate: '2004-03-19',
    releaseYear: 2004,
    runtimeMinutes: 108,
    voteAverage: 8.1,
    voteCount: 14800,
    genres: ['Ficção Científica', 'Drama', 'Romance'],
    genreIds: [878, 18, 10749],
    matchedVibes: ['chorar-desidratar', 'explodir-cabeca', 'conforto-domingo'],
    keywords: ['memória', 'amor', 'subconsciente', 'melancolia'],
    director: 'Michel Gondry',
    cast: ['Jim Carrey', 'Kate Winslet', 'Kirsten Dunst', 'Mark Ruffalo'],
    vibeMatchScore: 98,
    watchProviders: {
      region: 'BR',
      tmdbWatchLink: 'https://www.themoviedb.org/movie/38/watch?locale=BR',
      flatrate: [PROVIDERS.prime, PROVIDERS.globoplay],
      rent: [PROVIDERS.apple],
      buy: [PROVIDERS.apple]
    }
  },
  {
    tmdbId: 9495,
    title: 'Corra, Lola, Corra',
    originalTitle: 'Lola rennt',
    overview: 'Em Berlim, Lola recebe um telefonema desesperado do namorado e tem exatamente 20 minutos para conseguir 100 mil marcos alemães em três linhas temporais alternativas.',
    posterUrl: 'https://image.tmdb.org/t/p/w500/v0Gi232c1mE3vBFfG1eX6cO9k4N.jpg',
    backdropUrl: 'https://image.tmdb.org/t/p/w1280/w3yR5A7s1T4C2z8mJ0p9xN6vK1L.jpg',
    releaseDate: '1998-08-20',
    releaseYear: 1998,
    runtimeMinutes: 81,
    voteAverage: 7.3,
    voteCount: 2300,
    genres: ['Thriller / Suspense', 'Crime / Policial'],
    genreIds: [53, 80],
    matchedVibes: ['curto-direto', 'tensao-maxima', 'explodir-cabeca'],
    keywords: ['efeito borboleta', '81 minutos', 'adrenalina', 'corrida contra o tempo'],
    director: 'Tom Tykwer',
    cast: ['Franka Potente', 'Moritz Bleibtreu'],
    vibeMatchScore: 95,
    watchProviders: {
      region: 'BR',
      tmdbWatchLink: 'https://www.themoviedb.org/movie/9495/watch?locale=BR',
      flatrate: [PROVIDERS.mubi],
      rent: [PROVIDERS.apple],
      buy: []
    }
  },
  {
    tmdbId: 447332,
    title: 'Um Lugar Silencioso',
    originalTitle: 'A Quiet Place',
    overview: 'Em um mundo pós-apocalíptico dominado por criaturas cegas com audição ultrassensível, uma família precisa viver em absoluto silêncio para sobreviver.',
    posterUrl: 'https://image.tmdb.org/t/p/w500/nAU74GmpUk7t5iklEp3bufwDq4n.jpg',
    backdropUrl: 'https://image.tmdb.org/t/p/w1280/roYyPiQDQKmIKUEhO912693tSja.jpg',
    releaseDate: '2018-04-03',
    releaseYear: 2018,
    runtimeMinutes: 90,
    voteAverage: 7.4,
    voteCount: 14100,
    genres: ['Terror Psicológico', 'Ficção Científica', 'Thriller / Suspense'],
    genreIds: [27, 878, 53],
    matchedVibes: ['curto-direto', 'tensao-maxima'],
    keywords: ['silêncio', 'sobrevivência', '90 minutos', 'tensão constante'],
    director: 'John Krasinski',
    cast: ['Emily Blunt', 'John Krasinski', 'Millicent Simmonds'],
    vibeMatchScore: 96,
    watchProviders: {
      region: 'BR',
      tmdbWatchLink: 'https://www.themoviedb.org/movie/447332/watch?locale=BR',
      flatrate: [PROVIDERS.netflix, PROVIDERS.prime, PROVIDERS.disney],
      rent: [PROVIDERS.apple],
      buy: [PROVIDERS.apple]
    }
  }
];

// Extra offline metadata (trailers / taglines) for the curated catalog.
const MOVIE_EXTRAS: Record<number, Pick<MovieCardDto, 'trailerYoutubeKey' | 'tagline'>> = {
  27205: { trailerYoutubeKey: 'YoHD9XEInc0', tagline: 'Sua mente é a cena do crime.' },
  1124: { trailerYoutubeKey: null, tagline: 'Você está olhando com atenção?' },
  329865: { trailerYoutubeKey: 'tFMo3UJ4B4g', tagline: 'Por que eles estão aqui?' },
  37165: { trailerYoutubeKey: null, tagline: 'Ao vivo e sem saber de nada.' },
  194: { trailerYoutubeKey: null, tagline: 'Ela vai mudar a sua vida.' },
  244786: { trailerYoutubeKey: '7d_jQycdQGo', tagline: 'O caminho para a grandeza não é para os fracos.' },
  335984: { trailerYoutubeKey: 'gCcx85zbxz4', tagline: 'A chave para o futuro está finalmente desenterrada.' },
  64690: { trailerYoutubeKey: 'KBiOF3y1W0Y', tagline: 'Há heróis que nunca são esquecidos.' },
  965150: { trailerYoutubeKey: null, tagline: 'Algumas memórias só fazem sentido depois.' },
  38: { trailerYoutubeKey: null, tagline: 'Você pode apagar alguém da sua mente. Tirá-lo do coração é outra história.' },
  9495: { trailerYoutubeKey: null, tagline: 'Vinte minutos para mudar tudo.' },
  447332: { trailerYoutubeKey: 'WR7cc5t7tv8', tagline: 'Se eles ouvirem você, eles caçam você.' }
};

for (const movie of FALLBACK_MOVIES) {
  Object.assign(movie, MOVIE_EXTRAS[movie.tmdbId] ?? {});
}

export const FALLBACK_PAGE_SIZE = 8;

export interface FallbackDiscoverParams {
  vibes?: string[];
  genres?: number[];
  maxRuntime?: number | null;
  decade?: number | null;
  minRating?: number | null;
  search?: string;
  providers?: string[];
  onlyMyProviders?: boolean;
  myProviders?: string[];
  sortBy?: string;
  page?: number;
  excludeIds?: number[];
}

function matchesAnyProvider(movie: MovieCardDto, wanted: string[]): boolean {
  return movie.watchProviders.flatrate.some((fp) =>
    wanted.some((w) => providerMatches(fp.providerName, w))
  );
}

/** Filters the curated catalog (no pagination). Used by discover, surprise and recommendations. */
export function filterFallbackMovies(params: FallbackDiscoverParams): {
  movies: MovieCardDto[];
  effectiveMaxRt: number | null;
  matchedDefs: VibeDefinition[];
} {
  const selectedVibes = (params.vibes || []).filter(Boolean);
  const selectedGenres = params.genres || [];
  const matchedDefs = FALLBACK_VIBES.filter((v) => selectedVibes.includes(v.id));

  let filtered = [...FALLBACK_MOVIES];
  if (selectedVibes.length > 0) {
    filtered = filtered.filter((m) => m.matchedVibes.some((mv) => selectedVibes.includes(mv)));
  }
  if (selectedGenres.length > 0) {
    filtered = filtered.filter((m) => m.genreIds.some((gid) => selectedGenres.includes(gid)));
  }
  const vibeRuntimes = matchedDefs
    .map((v) => v.maxRuntimeMinutes)
    .filter((x): x is number => typeof x === 'number');
  const effectiveMaxRt = params.maxRuntime || (vibeRuntimes.length ? Math.min(...vibeRuntimes) : null);

  if (effectiveMaxRt) {
    filtered = filtered.filter((m) => (m.runtimeMinutes || 999) <= effectiveMaxRt);
  }
  if (params.minRating) {
    const min = params.minRating;
    filtered = filtered.filter((m) => m.voteAverage >= min);
  }
  if (params.decade) {
    const start = params.decade;
    filtered = filtered.filter(
      (m) => typeof m.releaseYear === 'number' && m.releaseYear >= start && m.releaseYear < start + 10
    );
  }
  if (params.search?.trim()) {
    const q = params.search.trim().toLowerCase();
    filtered = filtered.filter(
      (m) =>
        m.title.toLowerCase().includes(q) ||
        m.originalTitle.toLowerCase().includes(q) ||
        m.overview.toLowerCase().includes(q) ||
        m.keywords.some((k) => k.toLowerCase().includes(q)) ||
        (m.director && m.director.toLowerCase().includes(q))
    );
  }
  if (params.providers && params.providers.length > 0) {
    const wanted = params.providers;
    filtered = filtered.filter((m) => matchesAnyProvider(m, wanted));
  }
  if (params.onlyMyProviders && params.myProviders && params.myProviders.length > 0) {
    const mine = params.myProviders;
    filtered = filtered.filter((m) => matchesAnyProvider(m, mine));
  }
  if (params.excludeIds && params.excludeIds.length > 0) {
    const ex = new Set(params.excludeIds);
    filtered = filtered.filter((m) => !ex.has(m.tmdbId));
  }

  switch (params.sortBy) {
    case 'vote_average.desc':
      filtered.sort((a, b) => b.voteAverage - a.voteAverage);
      break;
    case 'primary_release_date.desc':
      filtered.sort((a, b) => (b.releaseYear || 0) - (a.releaseYear || 0));
      break;
    case 'primary_release_date.asc':
      filtered.sort((a, b) => (a.releaseYear || 0) - (b.releaseYear || 0));
      break;
    default:
      break;
  }

  return { movies: filtered, effectiveMaxRt, matchedDefs };
}

export function buildFallbackDiscover(params: FallbackDiscoverParams): DiscoverResultDto {
  const selectedGenres = params.genres || [];
  const { movies, effectiveMaxRt, matchedDefs } = filterFallbackMovies(params);

  const tmdbParams: Record<string, string> = {
    language: 'pt-BR',
    watch_region: 'BR',
    include_adult: 'false',
    sort_by: params.sortBy || 'popularity.desc'
  };
  if (matchedDefs.length > 0) {
    const kwIds = Array.from(new Set(matchedDefs.flatMap((v) => v.tmdbKeywordIds)));
    if (kwIds.length > 0) tmdbParams['with_keywords'] = kwIds.join('|');
  }
  const allGenres = Array.from(
    new Set([...selectedGenres, ...matchedDefs.flatMap((v) => v.suggestedGenreIds)])
  );
  if (allGenres.length > 0) tmdbParams['with_genres'] = allGenres.join(',');
  if (effectiveMaxRt) tmdbParams['with_runtime.lte'] = String(effectiveMaxRt);
  if (params.minRating) tmdbParams['vote_average.gte'] = params.minRating.toFixed(1);
  if (params.decade) {
    tmdbParams['primary_release_date.gte'] = `${params.decade}-01-01`;
    tmdbParams['primary_release_date.lte'] = `${params.decade + 9}-12-31`;
  }
  const providerNames = [
    ...(params.providers || []),
    ...(params.onlyMyProviders ? params.myProviders || [] : [])
  ];
  if (providerNames.length > 0) {
    tmdbParams['with_watch_providers'] = Array.from(new Set(providerNames)).join('|');
  }

  const formulaParts: string[] = [];
  if (matchedDefs.length > 0) formulaParts.push(`Vibes [${matchedDefs.map((v) => v.name).join(' + ')}]`);
  if (selectedGenres.length > 0) formulaParts.push(`Gêneros TMDB (${selectedGenres.join(', ')})`);
  if (effectiveMaxRt) formulaParts.push(`Duração <= ${effectiveMaxRt} min`);
  if (params.decade) formulaParts.push(`Década de ${params.decade}`);
  if (params.minRating) formulaParts.push(`Nota >= ${params.minRating}`);
  if (params.providers && params.providers.length > 0) {
    formulaParts.push(`Streaming BR: ${params.providers.join(', ')}`);
  }
  if (params.onlyMyProviders) formulaParts.push('Só nos meus streamings');

  const page = Math.max(1, params.page || 1);
  const totalPages = Math.max(1, Math.ceil(movies.length / FALLBACK_PAGE_SIZE));
  const pageItems = movies.slice((page - 1) * FALLBACK_PAGE_SIZE, page * FALLBACK_PAGE_SIZE);

  return {
    page,
    totalResults: movies.length,
    totalPages,
    usingLiveTmdbApi: false,
    appliedFormulaSummary:
      formulaParts.length > 0
        ? formulaParts.join(' • ')
        : 'Catálogo Curado Geral (Região BR + JustWatch Providers)',
    appliedTmdbQueryParameters: tmdbParams,
    movies: pageItems
  };
}

export function buildFallbackCinemaRadar(cityInput: string): CinemaRadarResponseDto {
  const parts = (cityInput || 'Joinville/SC').split('/');
  const city = parts[0].trim();
  const state = parts[1]?.trim().toUpperCase() || 'SC';

  return {
    city,
    state,
    scrapedAt: new Date().toISOString(),
    scraperSource: `Modo offline • Programação ilustrativa (${city}/${state}) + catálogo local`,
    isSimulatedSchedule: true,
    notice: 'Horários ilustrativos — confira no site do cinema antes de sair de casa.',
    moviesInTheaters: [
      {
        movie: {
          tmdbId: 693134,
          title: 'Duna: Parte Dois',
          originalTitle: 'Dune: Part Two',
          overview: 'Paul Atreides une-se a Chani e aos Fremen em uma jornada espiritual e marcial de vingança contra os conspiradores que destruíram sua família.',
          posterUrl: 'https://image.tmdb.org/t/p/w500/8b8R8l88Qje9dn9OE8PY05Nxl1X.jpg',
          backdropUrl: 'https://image.tmdb.org/t/p/w1280/xOMo8BRK7PfcJv9JCnx7s5hj0PX.jpg',
          releaseDate: '2024-02-29',
          releaseYear: 2024,
          runtimeMinutes: 166,
          voteAverage: 8.3,
          voteCount: 6400,
          genres: ['Ficção Científica', 'Aventura'],
          genreIds: [878, 12],
          matchedVibes: ['explodir-cabeca', 'tensao-maxima'],
          keywords: ['arrakis', 'épico espacial', 'profecia', 'imax'],
          director: 'Denis Villeneuve',
          cast: ['Timothée Chalamet', 'Zendaya', 'Rebecca Ferguson', 'Javier Bardem'],
          vibeMatchScore: 98,
          watchProviders: {
            region: 'BR',
            tmdbWatchLink: 'https://www.themoviedb.org/movie/693134/watch?locale=BR',
            flatrate: [PROVIDERS.max],
            rent: [PROVIDERS.prime, PROVIDERS.apple],
            buy: [PROVIDERS.apple]
          }
        },
        ageRating: '14 anos',
        venues: [
          {
            cinemaChain: 'GNC Cinemas',
            cinemaName: `GNC ${city} Garten Shopping`,
            address: `Av. Rolf Wiest, 333 - ${city}/${state}`,
            sessions: [
              { time: '16:30', roomType: 'VIP / 2D', audioType: 'Legendado', ticketUrl: 'https://www.ingresso.com' },
              { time: '19:20', roomType: 'IMAX 2D', audioType: 'Legendado', ticketUrl: 'https://www.ingresso.com' },
              { time: '21:50', roomType: 'IMAX 2D', audioType: 'Legendado', ticketUrl: 'https://www.ingresso.com' },
              { time: '23:20', roomType: 'IMAX 2D', audioType: 'Dublado', ticketUrl: 'https://www.ingresso.com' }
            ]
          },
          {
            cinemaChain: 'Cinemark',
            cinemaName: `Cinemark Shopping Mueller ${city}`,
            address: `Rua Visconde de Taunay, 235 - Centro, ${city}/${state}`,
            sessions: [
              { time: '15:10', roomType: 'XD 2D', audioType: 'Dublado', ticketUrl: 'https://www.ingresso.com' },
              { time: '18:40', roomType: 'XD 2D', audioType: 'Legendado', ticketUrl: 'https://www.ingresso.com' },
              { time: '21:30', roomType: 'Sala 4 • 2D', audioType: 'Legendado', ticketUrl: 'https://www.ingresso.com' }
            ]
          }
        ]
      },
      {
        movie: {
          tmdbId: 929590,
          title: 'Guerra Civil',
          originalTitle: 'Civil War',
          overview: 'Em um futuro próximo onde os Estados Unidos estão fragmentados em uma guerra civil brutal, uma equipe de fotojornalistas atravessa a linha de frente rumo a Washington D.C.',
          posterUrl: 'https://image.tmdb.org/t/p/w500/sh7Rg8Er3tFcN9BpKIPOMvALgZd.jpg',
          backdropUrl: 'https://image.tmdb.org/t/p/w1280/z121dSTR7py9KxKuvwiIFSYW8cf.jpg',
          releaseDate: '2024-04-18',
          releaseYear: 2024,
          runtimeMinutes: 109,
          voteAverage: 7.4,
          voteCount: 3100,
          genres: ['Thriller / Suspense', 'Drama'],
          genreIds: [53, 18],
          matchedVibes: ['tensao-maxima'],
          keywords: ['fotojornalismo', 'distopia', 'tensão constante', 'a24'],
          director: 'Alex Garland',
          cast: ['Kirsten Dunst', 'Wagner Moura', 'Cailee Spaeny'],
          vibeMatchScore: 95,
          watchProviders: {
            region: 'BR',
            tmdbWatchLink: 'https://www.themoviedb.org/movie/929590/watch?locale=BR',
            flatrate: [PROVIDERS.prime, PROVIDERS.max],
            rent: [PROVIDERS.apple],
            buy: [PROVIDERS.apple]
          }
        },
        ageRating: '16 anos',
        venues: [
          {
            cinemaChain: 'GNC Cinemas',
            cinemaName: `GNC ${city} Garten Shopping`,
            address: `Av. Rolf Wiest, 333 - ${city}/${state}`,
            sessions: [
              { time: '13:40', roomType: 'Sala 2 • 2D', audioType: 'Dublado', ticketUrl: 'https://www.ingresso.com' },
              { time: '17:00', roomType: 'Sala 2 • 2D', audioType: 'Dublado', ticketUrl: 'https://www.ingresso.com' },
              { time: '20:15', roomType: 'VIP / 2D', audioType: 'Legendado', ticketUrl: 'https://www.ingresso.com' }
            ]
          },
          {
            cinemaChain: 'Cinemark',
            cinemaName: `Cinemark Shopping Mueller ${city}`,
            address: `Rua Visconde de Taunay, 235 - Centro, ${city}/${state}`,
            sessions: [
              { time: '19:00', roomType: 'Sala 3 • 2D', audioType: 'Legendado', ticketUrl: 'https://www.ingresso.com' },
              { time: '21:45', roomType: 'Sala 3 • 2D', audioType: 'Legendado', ticketUrl: 'https://www.ingresso.com' },
              { time: '23:05', roomType: 'Sala 3 • 3D', audioType: 'Dublado', ticketUrl: 'https://www.ingresso.com' }
            ]
          }
        ]
      }
    ]
  };
}

export const FALLBACK_DEMO_AUTH: AuthResponseDto = {
  token: 'demo-offline-jwt-token',
  userId: 'demo-user-01',
  name: 'Cinéfilo Demo',
  email: 'demo@vibemovies.app',
  preferredRegion: 'BR',
  defaultCity: 'Joinville/SC'
};

export const INITIAL_FALLBACK_WATCHLIST: WatchlistItem[] = [
  {
    id: 'watch-1',
    userId: 'demo-user-01',
    tmdbMovieId: 27205,
    title: 'A Origem',
    originalTitle: 'Inception',
    posterPath: 'https://image.tmdb.org/t/p/w500/9gk7adHYeDvHkCSEqAvQNLV5Uge.jpg',
    backdropPath: 'https://image.tmdb.org/t/p/w1280/8ZTVqvKDQ8emSGUEMjsS4yHAwrp.jpg',
    releaseYear: 2010,
    overview: 'Dom Cobb é um ladrão com a rara habilidade de roubar segredos do inconsciente durante o estado de sono.',
    listType: WatchlistType.Watched,
    personalRating: 5,
    personalNotes: 'Trilha sonora do Hans Zimmer + final do pião continuam imbatíveis.',
    watchedAt: new Date(Date.now() - 14 * 86400000).toISOString(),
    addedAt: new Date(Date.now() - 20 * 86400000).toISOString(),
    genresCsv: 'Ficção Científica, Thriller',
    runtimeMinutes: 148,
    tmdbVoteAverage: 8.4
  },
  {
    id: 'watch-2',
    userId: 'demo-user-01',
    tmdbMovieId: 335984,
    title: 'Blade Runner 2049',
    originalTitle: 'Blade Runner 2049',
    posterPath: 'https://image.tmdb.org/t/p/w500/gajva2L0rPYkEWjzgFlBXCAVBE5.jpg',
    backdropPath: 'https://image.tmdb.org/t/p/w1280/ilRyazdMJwN05exqhwK4tMKBYZs.jpg',
    releaseYear: 2017,
    overview: 'Um novo blade runner desenterra um segredo capaz de mergulhar a sociedade no caos.',
    listType: WatchlistType.WantToWatch,
    addedAt: new Date(Date.now() - 3 * 86400000).toISOString(),
    genresCsv: 'Ficção Científica, Drama',
    runtimeMinutes: 164,
    tmdbVoteAverage: 7.6
  },
  ...(
    [
      [329865, 5, 40, 'A cena final reorganiza o filme inteiro na cabeça.'],
      [244786, 4, 75, 'Saí sem fôlego. J.K. Simmons assustador.'],
      [38, 5, 130, undefined],
      [37165, 4, 200, 'Clássico que envelheceu muito bem.'],
      [447332, 3, 260, undefined]
    ] as [number, number, number, string | undefined][]
  ).map(([id, rating, daysAgo, notes], idx): WatchlistItem => {
    const m = FALLBACK_MOVIES.find((x) => x.tmdbId === id)!;
    return {
      id: `watch-seed-${idx + 3}`,
      userId: 'demo-user-01',
      tmdbMovieId: m.tmdbId,
      title: m.title,
      originalTitle: m.originalTitle,
      posterPath: m.posterUrl,
      backdropPath: m.backdropUrl,
      releaseYear: m.releaseYear,
      overview: m.overview,
      listType: WatchlistType.Watched,
      personalRating: rating,
      personalNotes: notes,
      watchedAt: new Date(Date.now() - daysAgo * 86400000).toISOString(),
      addedAt: new Date(Date.now() - (daysAgo + 5) * 86400000).toISOString(),
      genresCsv: m.genres.join(', '),
      runtimeMinutes: m.runtimeMinutes,
      tmdbVoteAverage: m.voteAverage
    };
  })
];

const MONTH_LABELS = ['jan', 'fev', 'mar', 'abr', 'mai', 'jun', 'jul', 'ago', 'set', 'out', 'nov', 'dez'];

function monthLabel(date: Date): string {
  return `${MONTH_LABELS[date.getMonth()]}/${String(date.getFullYear()).slice(-2)}`;
}

function splitGenres(csv: string): string[] {
  return (csv || '')
    .split(',')
    .map((g) => g.trim())
    .filter(Boolean);
}

/** Stats computed locally (offline mode, or to fill fields an older backend does not send). */
export function computeWatchlistStats(items: WatchlistItem[]): WatchlistStatsDto {
  const watched = items.filter((i) => i.listType === WatchlistType.Watched);
  const wantToWatch = items.filter((i) => i.listType === WatchlistType.WantToWatch);
  const rated = watched.filter((i) => typeof i.personalRating === 'number' && i.personalRating > 0);
  const avg =
    rated.length > 0
      ? Number((rated.reduce((acc, cur) => acc + (cur.personalRating || 0), 0) / rated.length).toFixed(1))
      : 0;

  const genreCounts = new Map<string, number>();
  for (const item of watched) {
    for (const g of splitGenres(item.genresCsv)) {
      genreCounts.set(g, (genreCounts.get(g) || 0) + 1);
    }
  }
  const genreBreakdown = [...genreCounts.entries()]
    .map(([name, count]) => ({ name, count }))
    .sort((a, b) => b.count - a.count || a.name.localeCompare(b.name));

  const ratingDistribution: Record<string, number> = { '1': 0, '2': 0, '3': 0, '4': 0, '5': 0 };
  for (const item of rated) {
    const key = String(Math.min(5, Math.max(1, Math.round(item.personalRating || 0))));
    ratingDistribution[key] += 1;
  }

  const now = new Date();
  const watchedPerMonth: { name: string; count: number }[] = [];
  for (let offset = 11; offset >= 0; offset--) {
    const d = new Date(now.getFullYear(), now.getMonth() - offset, 1);
    const count = watched.filter((w) => {
      if (!w.watchedAt) return false;
      const wd = new Date(w.watchedAt);
      return wd.getFullYear() === d.getFullYear() && wd.getMonth() === d.getMonth();
    }).length;
    watchedPerMonth.push({ name: monthLabel(d), count });
  }

  const totalMinutesWatched = watched.reduce((acc, cur) => acc + (cur.runtimeMinutes || 110), 0);

  return {
    totalWantToWatch: wantToWatch.length,
    totalWatched: watched.length,
    totalMinutesWatched,
    averagePersonalRating: avg,
    favoriteGenre: genreBreakdown[0]?.name || '—',
    genreBreakdown,
    ratingDistribution,
    watchedPerMonth,
    totalHoursWatched: Number((totalMinutesWatched / 60).toFixed(1))
  };
}

/** Fills any stats field missing from the backend response with locally computed values. */
export function completeWatchlistStats(
  stats: WatchlistStatsDto | null,
  items: WatchlistItem[]
): WatchlistStatsDto {
  const local = computeWatchlistStats(items);
  if (!stats) return local;
  return {
    ...local,
    ...stats,
    genreBreakdown: stats.genreBreakdown ?? local.genreBreakdown,
    ratingDistribution: stats.ratingDistribution ?? local.ratingDistribution,
    watchedPerMonth: stats.watchedPerMonth ?? local.watchedPerMonth,
    totalHoursWatched:
      stats.totalHoursWatched ?? Number(((stats.totalMinutesWatched || 0) / 60).toFixed(1))
  };
}

const ALL_FALLBACK_MOVIES = (): MovieCardDto[] => {
  const cinema = buildFallbackCinemaRadar('Joinville/SC').moviesInTheaters.map((e) => e.movie);
  const seen = new Set<number>();
  return [...FALLBACK_MOVIES, ...cinema].filter((m) => {
    if (seen.has(m.tmdbId)) return false;
    seen.add(m.tmdbId);
    return true;
  });
};

export function findFallbackMovie(id: number): MovieCardDto | null {
  return ALL_FALLBACK_MOVIES().find((m) => m.tmdbId === id) ?? null;
}

function similarityScore(a: MovieCardDto, b: MovieCardDto): number {
  const sharedGenres = a.genreIds.filter((g) => b.genreIds.includes(g)).length;
  const sharedVibes = a.matchedVibes.filter((v) => b.matchedVibes.includes(v)).length;
  const sameDirector = a.director && a.director === b.director ? 2 : 0;
  return sharedGenres * 2 + sharedVibes * 3 + sameDirector + b.voteAverage / 10;
}

export function buildFallbackSimilar(id: number): MovieCardDto[] {
  const all = ALL_FALLBACK_MOVIES();
  const base = all.find((m) => m.tmdbId === id);
  const others = all.filter((m) => m.tmdbId !== id);
  if (!base) return others.slice(0, 8);
  return others
    .map((m) => ({ m, score: similarityScore(base, m) }))
    .filter((x) => x.score > 1)
    .sort((x, y) => y.score - x.score)
    .slice(0, 10)
    .map((x) => x.m);
}

export function buildFallbackSurprise(
  params: FallbackDiscoverParams,
  excludeIds: number[]
): SurpriseResponseDto {
  let pool = filterFallbackMovies({ ...params, page: 1, excludeIds }).movies;
  let relaxed = false;
  if (pool.length === 0) {
    // Relax the filters but still honour the vibes when possible.
    pool = filterFallbackMovies({ vibes: params.vibes, excludeIds }).movies;
    relaxed = true;
  }
  if (pool.length === 0) {
    pool = FALLBACK_MOVIES.filter((m) => !excludeIds.includes(m.tmdbId));
    relaxed = true;
  }
  if (pool.length === 0) pool = [...FALLBACK_MOVIES];

  const movie = pool[Math.floor(Math.random() * pool.length)];
  const vibeNames = FALLBACK_VIBES.filter(
    (v) => (params.vibes || []).includes(v.id) && movie.matchedVibes.includes(v.id)
  ).map((v) => v.name);

  const reasonParts: string[] = [];
  if (vibeNames.length > 0) reasonParts.push(`Combina com a vibe "${vibeNames.join('" + "')}"`);
  else reasonParts.push(`Um achado de ${movie.genres.slice(0, 2).join(' e ').toLowerCase()}`);
  reasonParts.push(`nota ${movie.voteAverage.toFixed(1)} no TMDB`);
  if (movie.runtimeMinutes) reasonParts.push(`${movie.runtimeMinutes} min`);
  const flat = movie.watchProviders.flatrate[0];
  if (flat) reasonParts.push(`disponível na ${flat.providerName}`);
  const reason = reasonParts.join(' • ') + (relaxed ? ' (filtros relaxados para achar algo novo)' : '');

  return {
    movie,
    reason,
    spokenText: `Que tal assistir ${movie.title}${movie.director ? `, de ${movie.director}` : ''}? ${
      vibeNames.length ? `Tem tudo a ver com a vibe ${vibeNames[0]}.` : ''
    }`.trim()
  };
}

export function buildFallbackRecommendations(watchlist: WatchlistItem[]): MovieCardDto[] {
  const listed = new Set(watchlist.map((w) => w.tmdbMovieId));
  const liked = watchlist
    .filter((w) => w.listType === WatchlistType.Watched && (w.personalRating ?? 0) >= 4)
    .map((w) => ({ item: w, movie: findFallbackMovie(w.tmdbMovieId) }))
    .filter((x): x is { item: WatchlistItem; movie: MovieCardDto } => x.movie !== null);
  const wanted = watchlist
    .filter((w) => w.listType === WatchlistType.WantToWatch)
    .map((w) => ({ item: w, movie: findFallbackMovie(w.tmdbMovieId) }))
    .filter((x): x is { item: WatchlistItem; movie: MovieCardDto } => x.movie !== null);
  const seeds = [...liked, ...wanted];
  if (seeds.length === 0) return [];

  const results: MovieCardDto[] = [];
  for (const candidate of FALLBACK_MOVIES) {
    if (listed.has(candidate.tmdbId)) continue;
    let best: { score: number; seed: { item: WatchlistItem; movie: MovieCardDto } } | null = null;
    for (const seed of seeds) {
      const score = similarityScore(seed.movie, candidate);
      if (!best || score > best.score) best = { score, seed };
    }
    if (best && best.score > 3) {
      const isLiked = best.seed.item.listType === WatchlistType.Watched;
      results.push({
        ...candidate,
        recommendationReason: isLiked
          ? `Porque você curtiu ${best.seed.item.title}`
          : `Parecido com ${best.seed.item.title}, que está na sua lista`
      });
    }
  }
  return results.sort((a, b) => b.vibeMatchScore - a.vibeMatchScore).slice(0, 10);
}

/** Offline profile, persisted in localStorage by the api layer. */
export function buildFallbackProfile(
  auth: AuthResponseDto | null,
  stored: Partial<UserProfileDto> | null
): UserProfileDto {
  return {
    userId: auth?.userId ?? FALLBACK_DEMO_AUTH.userId,
    email: auth?.email ?? FALLBACK_DEMO_AUTH.email,
    name: stored?.name ?? auth?.name ?? FALLBACK_DEMO_AUTH.name,
    preferredRegion: stored?.preferredRegion ?? auth?.preferredRegion ?? 'BR',
    defaultCity: stored?.defaultCity ?? auth?.defaultCity ?? 'Joinville/SC',
    myProviders: stored?.myProviders ?? auth?.myProviders ?? []
  };
}

function csvCell(value: unknown): string {
  const text = value === undefined || value === null ? '' : String(value);
  return /[";\n\r,]/.test(text) ? `"${text.replace(/"/g, '""')}"` : text;
}

/** Client-side CSV export of the diary (offline fallback of /watchlist/export). */
export function buildWatchlistCsv(items: WatchlistItem[]): string {
  const header = [
    'titulo',
    'titulo_original',
    'ano',
    'lista',
    'nota_pessoal',
    'anotacoes',
    'assistido_em',
    'adicionado_em',
    'generos',
    'duracao_min',
    'nota_tmdb',
    'tmdb_id'
  ];
  const rows = items.map((i) =>
    [
      i.title,
      i.originalTitle,
      i.releaseYear ?? '',
      i.listType === WatchlistType.Watched ? 'Já Vi' : 'Quero Ver',
      i.personalRating ?? '',
      i.personalNotes ?? '',
      i.watchedAt ? i.watchedAt.slice(0, 10) : '',
      i.addedAt ? i.addedAt.slice(0, 10) : '',
      i.genresCsv,
      i.runtimeMinutes ?? '',
      i.tmdbVoteAverage,
      i.tmdbMovieId
    ]
      .map(csvCell)
      .join(',')
  );
  // BOM so Excel opens accents correctly.
  return '﻿' + [header.join(','), ...rows].join('\r\n');
}
