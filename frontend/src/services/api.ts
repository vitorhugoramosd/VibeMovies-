import {
  AuthResponseDto,
  CinemaRadarResponseDto,
  DiscoverFilters,
  DiscoverResultDto,
  GenreDto,
  MovieCardDto,
  SurpriseResponseDto,
  UpdateProfileRequest,
  UserProfileDto,
  VibeDefinition,
  WatchlistItem,
  WatchlistStatsDto,
  WatchlistType
} from '../types/api';
import {
  buildFallbackCinemaRadar,
  buildFallbackDiscover,
  buildFallbackProfile,
  buildFallbackRecommendations,
  buildFallbackSimilar,
  buildFallbackSurprise,
  buildWatchlistCsv,
  computeWatchlistStats,
  FALLBACK_DEMO_AUTH,
  FALLBACK_GENRES,
  FALLBACK_VIBES,
  findFallbackMovie,
  INITIAL_FALLBACK_WATCHLIST
} from './fallbackData';
import {
  STORAGE_KEYS,
  storageGet,
  storageGetJson,
  storageRemove,
  storageSet,
  storageSetJson
} from '../lib/storage';
import { DEFAULT_CITIES } from '../lib/constants';

const API_BASE = import.meta.env.VITE_API_URL || '/api';

/** How long we skip network calls after detecting that the backend is down. */
const OFFLINE_RETRY_MS = 20_000;

// ---------------------------------------------------------------------------
// Errors & connectivity
// ---------------------------------------------------------------------------

export class ApiError extends Error {
  readonly status: number;
  /** True when the backend could not be reached at all (network error / proxy error). */
  readonly offline: boolean;

  constructor(message: string, status: number, offline = false) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.offline = offline;
  }
}

export function isAbortError(err: unknown): boolean {
  return err instanceof DOMException && err.name === 'AbortError';
}

type ConnectivityListener = (offline: boolean) => void;
let offlineUntil = 0;
let lastReportedOffline = false;
const connectivityListeners = new Set<ConnectivityListener>();

function reportConnectivity(offline: boolean) {
  if (offline) offlineUntil = Date.now() + OFFLINE_RETRY_MS;
  if (offline !== lastReportedOffline) {
    lastReportedOffline = offline;
    connectivityListeners.forEach((l) => l(offline));
  }
}

export function isBackendOffline(): boolean {
  return lastReportedOffline;
}

export function subscribeConnectivity(listener: ConnectivityListener): () => void {
  connectivityListeners.add(listener);
  return () => {
    connectivityListeners.delete(listener);
  };
}

// ---------------------------------------------------------------------------
// Local storage helpers (auth, TMDB key, offline watchlist & profile)
// ---------------------------------------------------------------------------

export function getStoredToken(): string | null {
  return storageGet(STORAGE_KEYS.token);
}

export function getStoredUser(): AuthResponseDto | null {
  return storageGetJson<AuthResponseDto>(STORAGE_KEYS.user);
}

export function setStoredAuth(auth: AuthResponseDto | null) {
  if (!auth) {
    storageRemove(STORAGE_KEYS.token);
    storageRemove(STORAGE_KEYS.user);
  } else {
    storageSet(STORAGE_KEYS.token, auth.token);
    storageSetJson(STORAGE_KEYS.user, auth);
  }
}

export function getStoredTmdbKey(): string {
  return storageGet(STORAGE_KEYS.tmdbKey) || '';
}

export function setStoredTmdbKey(key: string) {
  if (!key.trim()) storageRemove(STORAGE_KEYS.tmdbKey);
  else storageSet(STORAGE_KEYS.tmdbKey, key.trim());
}

function getLocalWatchlist(): WatchlistItem[] {
  const stored = storageGetJson<WatchlistItem[]>(STORAGE_KEYS.fallbackWatchlist);
  if (!stored || !Array.isArray(stored)) {
    storageSetJson(STORAGE_KEYS.fallbackWatchlist, INITIAL_FALLBACK_WATCHLIST);
    return [...INITIAL_FALLBACK_WATCHLIST];
  }
  return stored;
}

function setLocalWatchlist(items: WatchlistItem[]) {
  storageSetJson(STORAGE_KEYS.fallbackWatchlist, items);
}

function getLocalProfile(): UserProfileDto {
  return buildFallbackProfile(
    getStoredUser(),
    storageGetJson<Partial<UserProfileDto>>(STORAGE_KEYS.fallbackProfile)
  );
}

/** Providers used by the offline "Só nos meus streamings" filter. */
function getKnownMyProviders(): string[] {
  return getStoredUser()?.myProviders ?? getLocalProfile().myProviders;
}

// ---------------------------------------------------------------------------
// Fetch core
// ---------------------------------------------------------------------------

function buildHeaders(extra?: HeadersInit, json = true): Headers {
  const headers = new Headers(extra || {});
  if (json) headers.set('Content-Type', 'application/json');
  const token = getStoredToken();
  if (token) headers.set('Authorization', `Bearer ${token}`);
  const tmdbKey = getStoredTmdbKey();
  if (tmdbKey) headers.set('X-TMDB-Api-Key', tmdbKey);
  return headers;
}

async function rawFetch(path: string, options: RequestInit = {}, json = true): Promise<Response> {
  if (Date.now() < offlineUntil) {
    throw new ApiError('Servidor indisponível (modo offline).', 0, true);
  }

  let response: Response;
  try {
    response = await fetch(`${API_BASE}${path}`, {
      ...options,
      headers: buildHeaders(options.headers, json)
    });
  } catch (err) {
    if (isAbortError(err)) throw err;
    reportConnectivity(true);
    throw new ApiError('Não foi possível conectar ao servidor.', 0, true);
  }

  if (!response.ok) {
    const text = await response.text().catch(() => '');
    let message = '';
    let isJson = false;
    try {
      const body = JSON.parse(text) as { message?: string };
      isJson = true;
      message = body?.message || '';
    } catch {
      // not JSON
    }
    // Dev proxy answers 500/502/503/504 with a non-JSON body when the API is down.
    const offline =
      [502, 503, 504].includes(response.status) || (response.status >= 500 && !isJson);
    if (offline) reportConnectivity(true);
    else reportConnectivity(false);
    throw new ApiError(message || `Erro HTTP ${response.status}`, response.status, offline);
  }

  reportConnectivity(false);
  return response;
}

async function apiFetch<T>(path: string, options: RequestInit = {}): Promise<T> {
  const response = await rawFetch(path, options);
  if (response.status === 204) return undefined as T;
  return (await response.json()) as T;
}

type FallbackPolicy = (err: ApiError) => boolean;

/** Any failure falls back (except aborts). */
const ALWAYS: FallbackPolicy = () => true;
/** Only fall back when the backend is unreachable or the endpoint does not exist yet. */
const OFFLINE_ONLY: FallbackPolicy = (e) => e.offline || e.status === 404 || e.status >= 500;
/** Fall back unless the server rejected the payload. */
const UNLESS_VALIDATION: FallbackPolicy = (e) => e.status !== 400 && e.status !== 422;

async function withFallback<T>(
  request: () => Promise<T>,
  fallback: () => T | Promise<T>,
  policy: FallbackPolicy = ALWAYS
): Promise<T> {
  try {
    return await request();
  } catch (err) {
    if (isAbortError(err)) throw err;
    const apiErr = err instanceof ApiError ? err : new ApiError(String(err), 0, true);
    if (!policy(apiErr)) throw apiErr;
    return fallback();
  }
}

function throwIfAborted(signal?: AbortSignal) {
  if (signal?.aborted) throw new DOMException('Aborted', 'AbortError');
}

// ---------------------------------------------------------------------------
// Download helper
// ---------------------------------------------------------------------------

export function downloadBlob(blob: Blob, filename: string) {
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  a.rel = 'noopener';
  document.body.appendChild(a);
  a.click();
  a.remove();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}

function filenameFromDisposition(header: string | null): string | null {
  if (!header) return null;
  const star = /filename\*=UTF-8''([^;]+)/i.exec(header);
  if (star) return decodeURIComponent(star[1]);
  const plain = /filename="?([^";]+)"?/i.exec(header);
  return plain ? plain[1] : null;
}

// ---------------------------------------------------------------------------
// Public API
// ---------------------------------------------------------------------------

export type DiscoverParams = Partial<DiscoverFilters> & { page?: number };

function buildDiscoverQuery(params: DiscoverParams): URLSearchParams {
  const query = new URLSearchParams();
  if (params.vibes?.length) query.set('vibes', params.vibes.join(','));
  if (params.genres?.length) query.set('genres', params.genres.join(','));
  if (params.maxRuntime) query.set('maxRuntime', String(params.maxRuntime));
  if (params.decade) query.set('decade', String(params.decade));
  if (params.minRating) query.set('minRating', String(params.minRating));
  if (params.search?.trim()) query.set('search', params.search.trim());
  if (params.providers?.length) query.set('provider', params.providers.join(','));
  if (params.onlyMyProviders) query.set('onlyMyProviders', 'true');
  query.set('region', 'BR');
  if (params.sortBy) query.set('sortBy', params.sortBy);
  if (params.page && params.page > 1) query.set('page', String(params.page));
  return query;
}

export const api = {
  getVibes: (): Promise<VibeDefinition[]> =>
    withFallback(() => apiFetch<VibeDefinition[]>('/movies/vibes'), () => FALLBACK_VIBES),

  getGenres: (): Promise<GenreDto[]> =>
    withFallback(() => apiFetch<GenreDto[]>('/movies/genres'), () => FALLBACK_GENRES),

  discoverMovies: (params: DiscoverParams, signal?: AbortSignal): Promise<DiscoverResultDto> =>
    withFallback(
      () => apiFetch<DiscoverResultDto>(`/movies/discover?${buildDiscoverQuery(params)}`, { signal }),
      () => {
        throwIfAborted(signal);
        return buildFallbackDiscover({ ...params, myProviders: getKnownMyProviders() });
      },
      OFFLINE_ONLY
    ),

  getMovie: (id: number, signal?: AbortSignal): Promise<MovieCardDto> =>
    withFallback(
      () => apiFetch<MovieCardDto>(`/movies/${id}?region=BR`, { signal }),
      () => {
        const movie = findFallbackMovie(id);
        if (!movie) throw new ApiError('Filme não encontrado no catálogo offline.', 404);
        return movie;
      }
    ),

  getSimilar: (id: number, signal?: AbortSignal): Promise<MovieCardDto[]> =>
    withFallback(
      () => apiFetch<MovieCardDto[]>(`/movies/${id}/similar?region=BR`, { signal }),
      () => {
        throwIfAborted(signal);
        return buildFallbackSimilar(id);
      }
    ),

  getSurprise: (
    filters: DiscoverParams,
    excludeIds: number[],
    signal?: AbortSignal
  ): Promise<SurpriseResponseDto> => {
    const query = new URLSearchParams();
    if (filters.vibes?.length) query.set('vibes', filters.vibes.join(','));
    if (filters.genres?.length) query.set('genres', filters.genres.join(','));
    if (filters.maxRuntime) query.set('maxRuntime', String(filters.maxRuntime));
    if (filters.providers?.length) query.set('provider', filters.providers.join(','));
    if (filters.onlyMyProviders) query.set('onlyMyProviders', 'true');
    if (excludeIds.length) query.set('excludeIds', excludeIds.join(','));
    query.set('region', 'BR');
    return withFallback(
      () => apiFetch<SurpriseResponseDto>(`/movies/surprise?${query}`, { signal }),
      () => {
        throwIfAborted(signal);
        return buildFallbackSurprise({ ...filters, myProviders: getKnownMyProviders() }, excludeIds);
      }
    );
  },

  getCinemaCities: (): Promise<string[]> =>
    withFallback(() => apiFetch<string[]>('/cinemas/cities'), () => [...DEFAULT_CITIES]),

  getCinemaRadar: (city: string, signal?: AbortSignal): Promise<CinemaRadarResponseDto> =>
    withFallback(
      () =>
        apiFetch<CinemaRadarResponseDto>(`/cinemas/now-playing?city=${encodeURIComponent(city)}`, {
          signal
        }),
      () => buildFallbackCinemaRadar(city)
    ),

  login: (email: string, password: string): Promise<AuthResponseDto> =>
    withFallback(
      () =>
        apiFetch<AuthResponseDto>('/auth/login', {
          method: 'POST',
          body: JSON.stringify({ email, password })
        }),
      () => ({ ...FALLBACK_DEMO_AUTH, ...pickProfile(getLocalProfile()), email: email || FALLBACK_DEMO_AUTH.email }),
      OFFLINE_ONLY
    ),

  register: (name: string, email: string, password: string, defaultCity: string): Promise<AuthResponseDto> =>
    withFallback(
      () =>
        apiFetch<AuthResponseDto>('/auth/register', {
          method: 'POST',
          body: JSON.stringify({ name, email, password, preferredRegion: 'BR', defaultCity })
        }),
      () => {
        const profile: UserProfileDto = {
          name: name || 'Cinéfilo',
          defaultCity: defaultCity || 'Joinville/SC',
          preferredRegion: 'BR',
          myProviders: []
        };
        storageSetJson(STORAGE_KEYS.fallbackProfile, profile);
        return { ...FALLBACK_DEMO_AUTH, ...pickProfile(profile), email };
      },
      OFFLINE_ONLY
    ),

  getMe: (): Promise<UserProfileDto> =>
    withFallback(
      async () => {
        const me = await apiFetch<UserProfileDto>('/auth/me');
        return { ...me, myProviders: me.myProviders ?? [] };
      },
      () => getLocalProfile()
    ),

  updateMe: (body: UpdateProfileRequest): Promise<UserProfileDto> =>
    withFallback(
      async () => {
        const me = await apiFetch<UserProfileDto>('/auth/me', {
          method: 'PUT',
          body: JSON.stringify(body)
        });
        return { ...me, myProviders: me.myProviders ?? body.myProviders };
      },
      () => {
        storageSetJson(STORAGE_KEYS.fallbackProfile, body);
        return getLocalProfile();
      },
      UNLESS_VALIDATION
    ),

  getWatchlist: (type?: WatchlistType): Promise<WatchlistItem[]> =>
    withFallback(
      () => apiFetch<WatchlistItem[]>(`/watchlist${type ? `?type=${type}` : ''}`),
      () => {
        const items = getLocalWatchlist();
        return type ? items.filter((i) => i.listType === type) : items;
      }
    ),

  getWatchlistStats: (): Promise<WatchlistStatsDto> =>
    withFallback(
      () => apiFetch<WatchlistStatsDto>('/watchlist/stats'),
      () => computeWatchlistStats(getLocalWatchlist())
    ),

  /** `currentItems` feeds the offline recommender with whatever diary the UI is showing. */
  getRecommendations: (currentItems: WatchlistItem[], signal?: AbortSignal): Promise<MovieCardDto[]> =>
    withFallback(
      () => apiFetch<MovieCardDto[]>('/watchlist/recommendations', { signal }),
      () => {
        throwIfAborted(signal);
        return buildFallbackRecommendations(currentItems);
      }
    ),

  /** Downloads the diary export. Falls back to a client-generated file. */
  exportWatchlist: async (format: 'csv' | 'json', currentItems: WatchlistItem[]): Promise<void> => {
    const stamp = new Date().toISOString().slice(0, 10);
    const defaultName = `vibemovies-diario-${stamp}.${format}`;
    const { blob, filename } = await withFallback(
      async () => {
        const res = await rawFetch(`/watchlist/export?format=${format}`, {}, false);
        return {
          blob: await res.blob(),
          filename: filenameFromDisposition(res.headers.get('Content-Disposition')) || defaultName
        };
      },
      () => ({
        blob:
          format === 'csv'
            ? new Blob([buildWatchlistCsv(currentItems)], { type: 'text/csv;charset=utf-8' })
            : new Blob([JSON.stringify(currentItems, null, 2)], { type: 'application/json' }),
        filename: defaultName
      })
    );
    downloadBlob(blob, filename);
  },

  upsertWatchlist: (
    movie: MovieCardDto,
    listType: WatchlistType,
    personalRating?: number,
    personalNotes?: string
  ): Promise<WatchlistItem> =>
    withFallback(
      () =>
        apiFetch<WatchlistItem>('/watchlist', {
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
        }),
      () => {
        const list = getLocalWatchlist();
        const existingIdx = list.findIndex((x) => x.tmdbMovieId === movie.tmdbId);
        const existing = existingIdx >= 0 ? list[existingIdx] : undefined;
        const item: WatchlistItem = {
          id: existing?.id ?? `local-${Date.now()}`,
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
          addedAt: existing?.addedAt ?? new Date().toISOString(),
          genresCsv: movie.genres.join(', '),
          runtimeMinutes: movie.runtimeMinutes,
          tmdbVoteAverage: movie.voteAverage
        };
        if (existingIdx >= 0) list[existingIdx] = item;
        else list.unshift(item);
        setLocalWatchlist(list);
        return item;
      }
    ),

  updateWatchlistItem: (
    id: string,
    listType: WatchlistType,
    personalRating?: number,
    personalNotes?: string
  ): Promise<WatchlistItem> =>
    withFallback(
      () =>
        apiFetch<WatchlistItem>(`/watchlist/${id}`, {
          method: 'PUT',
          body: JSON.stringify({
            listType,
            personalRating,
            personalNotes,
            watchedAt: listType === WatchlistType.Watched ? new Date().toISOString() : null
          })
        }),
      () => {
        const list = getLocalWatchlist();
        const idx = list.findIndex((x) => x.id === id);
        if (idx < 0) throw new ApiError('Item não encontrado', 404);
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
    ),

  deleteWatchlistItem: (id: string): Promise<void> =>
    withFallback(
      () => apiFetch<void>(`/watchlist/${id}`, { method: 'DELETE' }),
      () => {
        setLocalWatchlist(getLocalWatchlist().filter((x) => x.id !== id));
      }
    )
};

function pickProfile(p: UserProfileDto): Pick<AuthResponseDto, 'name' | 'defaultCity' | 'preferredRegion' | 'myProviders'> {
  return {
    name: p.name,
    defaultCity: p.defaultCity,
    preferredRegion: p.preferredRegion,
    myProviders: p.myProviders
  };
}
