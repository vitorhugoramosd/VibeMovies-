/**
 * Safe wrappers around localStorage. Private windows, blocked site data or
 * quota errors must never break the app, so every access is guarded.
 */
export function storageGet(key: string): string | null {
  try {
    return window.localStorage.getItem(key);
  } catch {
    return null;
  }
}

export function storageSet(key: string, value: string): void {
  try {
    window.localStorage.setItem(key, value);
  } catch {
    // ignore (private mode / quota)
  }
}

export function storageRemove(key: string): void {
  try {
    window.localStorage.removeItem(key);
  } catch {
    // ignore
  }
}

export function storageGetJson<T>(key: string): T | null {
  const raw = storageGet(key);
  if (!raw) return null;
  try {
    return JSON.parse(raw) as T;
  } catch {
    return null;
  }
}

export function storageSetJson(key: string, value: unknown): void {
  storageSet(key, JSON.stringify(value));
}

export const STORAGE_KEYS = {
  token: 'vibemovies_jwt_token',
  user: 'vibemovies_user',
  tmdbKey: 'vibemovies_tmdb_api_key',
  fallbackWatchlist: 'vibemovies_fallback_watchlist',
  fallbackProfile: 'vibemovies_fallback_profile',
  lastView: 'vibemovies_last_view'
} as const;
