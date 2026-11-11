import { DiscoverFilters, SortOption } from '../types/api';
import { DEFAULT_VIBES, SORT_OPTIONS } from './constants';

export const DEFAULT_SORT: SortOption = 'popularity.desc';

export function emptyFilters(): DiscoverFilters {
  return {
    vibes: [],
    genres: [],
    maxRuntime: null,
    decade: null,
    minRating: null,
    search: '',
    providers: [],
    onlyMyProviders: false,
    sortBy: DEFAULT_SORT
  };
}

const FILTER_KEYS = ['vibes', 'genres', 'maxRuntime', 'decade', 'minRating', 'q', 'provider', 'mine', 'sort'];

function parseNumber(value: string | null): number | null {
  if (!value) return null;
  const n = Number(value);
  return Number.isFinite(n) && n > 0 ? n : null;
}

function parseList(value: string | null): string[] {
  return (value || '')
    .split(',')
    .map((v) => v.trim())
    .filter(Boolean);
}

/** Reads filters from the querystring. Uses the default vibe when no filter key is present. */
export function readFiltersFromUrl(search: string = window.location.search): DiscoverFilters {
  const params = new URLSearchParams(search);
  const hasAny = FILTER_KEYS.some((k) => params.has(k));
  const base = emptyFilters();
  if (!hasAny) return { ...base, vibes: [...DEFAULT_VIBES] };

  const sort = params.get('sort');
  return {
    vibes: parseList(params.get('vibes')),
    genres: parseList(params.get('genres'))
      .map(Number)
      .filter((n) => Number.isFinite(n) && n > 0),
    maxRuntime: parseNumber(params.get('maxRuntime')),
    decade: parseNumber(params.get('decade')),
    minRating: parseNumber(params.get('minRating')),
    search: params.get('q') || '',
    providers: parseList(params.get('provider')),
    onlyMyProviders: params.get('mine') === '1',
    sortBy: SORT_OPTIONS.some((o) => o.value === sort) ? (sort as SortOption) : DEFAULT_SORT
  };
}

export function readMovieIdFromUrl(search: string = window.location.search): number | null {
  return parseNumber(new URLSearchParams(search).get('movie'));
}

/** Builds the querystring that represents the given filters (+ optional open movie). */
export function buildQueryString(filters: DiscoverFilters, movieId: number | null): string {
  const params = new URLSearchParams();
  if (filters.vibes.length) params.set('vibes', filters.vibes.join(','));
  if (filters.genres.length) params.set('genres', filters.genres.join(','));
  if (filters.maxRuntime) params.set('maxRuntime', String(filters.maxRuntime));
  if (filters.decade) params.set('decade', String(filters.decade));
  if (filters.minRating) params.set('minRating', String(filters.minRating));
  if (filters.search.trim()) params.set('q', filters.search.trim());
  if (filters.providers.length) params.set('provider', filters.providers.join(','));
  if (filters.onlyMyProviders) params.set('mine', '1');
  if (filters.sortBy !== DEFAULT_SORT) params.set('sort', filters.sortBy);
  // An explicitly empty filter set still needs a marker, otherwise a reload would re-apply the default vibe.
  if ([...params.keys()].length === 0) params.set('vibes', '');
  if (movieId) params.set('movie', String(movieId));
  // Keep commas readable in shared links.
  return `?${params.toString().replace(/%2C/gi, ',')}`;
}

export function replaceUrl(queryString: string): void {
  try {
    const current = `${window.location.pathname}${window.location.search}${window.location.hash}`;
    const next = `${window.location.pathname}${queryString}${window.location.hash}`;
    if (next !== current) {
      window.history.replaceState(window.history.state, '', next);
    }
  } catch {
    // ignore (sandboxed iframes etc.)
  }
}

export function buildMovieShareUrl(movieId: number): string {
  return `${window.location.origin}${window.location.pathname}?movie=${movieId}`;
}
