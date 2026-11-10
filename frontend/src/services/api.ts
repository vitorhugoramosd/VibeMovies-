import {
  AuthResponseDto,
  CinemaRadarResponseDto,
  DiscoverResultDto,
  GenreDto,
  MovieCardDto,
  VibeDefinition,
  WatchlistItem,
  WatchlistStatsDto,
  WatchlistType
} from '../types/api';
import {
  buildFallbackCinemaRadar,
  buildFallbackDiscover,
  computeWatchlistStats,
  FALLBACK_DEMO_AUTH,
  FALLBACK_GENRES,
  FALLBACK_VIBES,
  INITIAL_FALLBACK_WATCHLIST
} from './fallbackData';

const API_BASE = import.meta.env.VITE_API_URL || 'http://localhost:5150/api';

const STORAGE_TOKEN_KEY = 'vibemovies_jwt_token';
const STORAGE_USER_KEY = 'vibemovies_user';
const STORAGE_TMDB_KEY = 'vibemovies_tmdb_api_key';
const STORAGE_FALLBACK_WATCHLIST = 'vibemovies_fallback_watchlist';

export function getStoredToken(): string | null {
  return localStorage.getItem(STORAGE_TOKEN_KEY);
}

export function getStoredUser(): AuthResponseDto | null {
  const raw = localStorage.getItem(STORAGE_USER_KEY);
  if (!raw) return null;
  try {
    return JSON.parse(raw);
  } catch {
    return null;
  }
}

export function setStoredAuth(auth: AuthResponseDto | null) {
  if (!auth) {
    localStorage.removeItem(STORAGE_TOKEN_KEY);
    localStorage.removeItem(STORAGE_USER_KEY);
  } else {
    localStorage.setItem(STORAGE_TOKEN_KEY, auth.token);
    localStorage.setItem(STORAGE_USER_KEY, JSON.stringify(auth));
  }
}

export function getStoredTmdbKey(): string {
  return localStorage.getItem(STORAGE_TMDB_KEY) || '';
}

export function setStoredTmdbKey(key: string) {
  if (!key.trim()) {
    localStorage.removeItem(STORAGE_TMDB_KEY);
  } else {
    localStorage.setItem(STORAGE_TMDB_KEY, key.trim());
  }
}

function getLocalWatchlist(): WatchlistItem[] {
  const raw = localStorage.getItem(STORAGE_FALLBACK_WATCHLIST);
  if (!raw) {
    localStorage.setItem(STORAGE_FALLBACK_WATCHLIST, JSON.stringify(INITIAL_FALLBACK_WATCHLIST));
    return [...INITIAL_FALLBACK_WATCHLIST];
  }
  try {
    return JSON.parse(raw);
  } catch {
    return [...INITIAL_FALLBACK_WATCHLIST];
  }
}

function setLocalWatchlist(items: WatchlistItem[]) {
  localStorage.setItem(STORAGE_FALLBACK_WATCHLIST, JSON.stringify(items));
}

async function apiFetch<T>(path: string, options: RequestInit = {}): Promise<T> {
  const headers = new Headers(options.headers || {});
  headers.set('Content-Type', 'application/json');

  const token = getStoredToken();
  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  const tmdbKey = getStoredTmdbKey();
  if (tmdbKey) {
    headers.set('X-TMDB-Api-Key', tmdbKey);
  }

  const response = await fetch(`${API_BASE}${path}`, {
    ...options,
    headers
  });

  if (!response.ok) {
    const errorBody = await response.json().catch(() => ({}));
    throw new Error(errorBody.message || `Erro HTTP ${response.status}`);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json();
}

export const api = {
  getVibes: async (): Promise<VibeDefinition[]> => {
    try {
      return await apiFetch<VibeDefinition[]>('/movies/vibes');
    } catch {
      return FALLBACK_VIBES;
    }
  },

  getGenres: async (): Promise<GenreDto[]> => {
    try {
      return await apiFetch<GenreDto[]>('/movies/genres');
    } catch {
      return FALLBACK_GENRES;
    }
  },

  discoverMovies: async (params: {
    vibes?: string[];
    genres?: number[];
    maxRuntime?: number | null;
    decade?: number | null;
    minRating?: number | null;
    search?: string;
    provider?: string | null;
  }): Promise<DiscoverResultDto> => {
    const query = new URLSearchParams();
    if (params.vibes && params.vibes.length > 0) query.set('vibes', params.vibes.join(','));
    if (params.genres && params.genres.length > 0) query.set('genres', params.genres.join(','));
    if (params.maxRuntime) query.set('maxRuntime', String(params.maxRuntime));
    if (params.decade) query.set('decade', String(params.decade));
    if (params.minRating) query.set('minRating', String(params.minRating));
    if (params.search?.trim()) query.set('search', params.search.trim());
    if (params.provider) query.set('provider', params.provider);

    try {
      return await apiFetch<DiscoverResultDto>(`/movies/discover?${query.toString()}`);
    } catch {
      return buildFallbackDiscover(params);
    }
  },

  getCinemaCities: async (): Promise<string[]> => {
    try {
      return await apiFetch<string[]>('/cinemas/cities');
    } catch {
      return [
        'Joinville/SC',
        'Florianópolis/SC',
        'Curitiba/PR',
        'São Paulo/SP',
        'Porto Alegre/RS',
        'Rio de Janeiro/RJ'
      ];
    }
  },

  getCinemaRadar: async (city: string): Promise<CinemaRadarResponseDto> => {
    try {
      return await apiFetch<CinemaRadarResponseDto>(
        `/cinemas/now-playing?city=${encodeURIComponent(city)}`
      );
    } catch {
      return buildFallbackCinemaRadar(city);
    }
  },

  login: async (email: string, password: string): Promise<AuthResponseDto> => {
    try {
      return await apiFetch<AuthResponseDto>('/auth/login', {
        method: 'POST',
        body: JSON.stringify({ email, password })
      });
    } catch {
      return {
        ...FALLBACK_DEMO_AUTH,
        email: email || FALLBACK_DEMO_AUTH.email
      };
    }
  },

  register: async (
    name: string,
    email: string,
    password: string,
    defaultCity: string
  ): Promise<AuthResponseDto> => {
    try {
      return await apiFetch<AuthResponseDto>('/auth/register', {
        method: 'POST',
        body: JSON.stringify({ name, email, password, preferredRegion: 'BR', defaultCity })
      });
    } catch {
      return {
        ...FALLBACK_DEMO_AUTH,
        name: name || 'Cinéfilo',
        email,
        defaultCity: defaultCity || 'Joinville/SC'
      };
    }
  },

  getWatchlist: async (type?: WatchlistType): Promise<WatchlistItem[]> => {
    try {
      return await apiFetch<WatchlistItem[]>(`/watchlist${type ? `?type=${type}` : ''}`);
    } catch {
      const items = getLocalWatchlist();
      return type ? items.filter((i) => i.listType === type) : items;
    }
  },

  getWatchlistStats: async (): Promise<WatchlistStatsDto> => {
    try {
      return await apiFetch<WatchlistStatsDto>('/watchlist/stats');
    } catch {
      return computeWatchlistStats(getLocalWatchlist());
    }
  },

  upsertWatchlist: async (
    movie: MovieCardDto,
    listType: WatchlistType,
    personalRating?: number,
    personalNotes?: string
  ): Promise<WatchlistItem> => {
    try {
      return await apiFetch<WatchlistItem>('/watchlist', {
        method: 'POST',
        body: JSON.stringify({
          tmdbMovieId: movie.tmdbId,
          title: movie.title,
          originalTitle: movie.originalTitle,
          posterPath: movie.posterUrl,
          backdropPath: movie.backdropUrl,
          releaseYear: movie.releaseYear,
          overview: movie.overview,
          listType,
          personalRating,
          personalNotes,
          watchedAt: listType === WatchlistType.Watched ? new Date().toISOString() : null,
          genresCsv: movie.genres.join(', '),
          runtimeMinutes: movie.runtimeMinutes,
          tmdbVoteAverage: movie.voteAverage
        })
      });
    } catch {
      const list = getLocalWatchlist();
      const existingIdx = list.findIndex((x) => x.tmdbMovieId === movie.tmdbId);
      const item: WatchlistItem = {
        id: existingIdx >= 0 ? list[existingIdx].id : `local-${Date.now()}`,
        userId: 'demo-user-01',
        tmdbMovieId: movie.tmdbId,
        title: movie.title,
        originalTitle: movie.originalTitle,
        posterPath: movie.posterUrl,
        backdropPath: movie.backdropUrl,
        releaseYear: movie.releaseYear,
        overview: movie.overview,
        listType,
        personalRating: listType === WatchlistType.Watched ? personalRating ?? 5 : undefined,
        personalNotes,
        watchedAt: listType === WatchlistType.Watched ? new Date().toISOString() : undefined,
        addedAt: existingIdx >= 0 ? list[existingIdx].addedAt : new Date().toISOString(),
        genresCsv: movie.genres.join(', '),
        runtimeMinutes: movie.runtimeMinutes,
        tmdbVoteAverage: movie.voteAverage
      };
      if (existingIdx >= 0) {
        list[existingIdx] = item;
      } else {
        list.unshift(item);
      }
      setLocalWatchlist(list);
      return item;
    }
  },

  updateWatchlistItem: async (
    id: string,
    listType: WatchlistType,
    personalRating?: number,
    personalNotes?: string
  ): Promise<WatchlistItem> => {
    try {
      return await apiFetch<WatchlistItem>(`/watchlist/${id}`, {
        method: 'PUT',
        body: JSON.stringify({
          listType,
          personalRating,
          personalNotes,
          watchedAt: listType === WatchlistType.Watched ? new Date().toISOString() : null
        })
      });
    } catch {
      const list = getLocalWatchlist();
      const idx = list.findIndex((x) => x.id === id);
      if (idx >= 0) {
        list[idx] = {
          ...list[idx],
          listType,
          personalRating,
          personalNotes,
          watchedAt: listType === WatchlistType.Watched ? new Date().toISOString() : undefined
        };
        setLocalWatchlist(list);
        return list[idx];
      }
      throw new Error('Item não encontrado');
    }
  },

  deleteWatchlistItem: async (id: string): Promise<void> => {
    try {
      await apiFetch<void>(`/watchlist/${id}`, { method: 'DELETE' });
    } catch {
      const list = getLocalWatchlist().filter((x) => x.id !== id);
      setLocalWatchlist(list);
    }
  }
};
