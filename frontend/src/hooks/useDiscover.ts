import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { api, isAbortError } from '../services/api';
import { DiscoverFilters, DiscoverResultDto, MovieCardDto } from '../types/api';

interface DiscoverState {
  /** Metadata of the last page loaded (formula, totals...). */
  result: DiscoverResultDto | null;
  movies: MovieCardDto[];
  page: number;
  totalPages: number;
  loading: boolean;
  loadingMore: boolean;
  error: string | null;
}

const INITIAL: DiscoverState = {
  result: null,
  movies: [],
  page: 0,
  totalPages: 0,
  loading: true,
  loadingMore: false,
  error: null
};

/**
 * Loads /movies/discover for the given (already debounced) filters.
 * Each new filter set aborts the in-flight request, and a request id guard
 * guarantees that out-of-order responses never overwrite newer results.
 */
export function useDiscover(filters: DiscoverFilters, reloadToken = 0) {
  const [state, setState] = useState<DiscoverState>(INITIAL);
  const controllerRef = useRef<AbortController | null>(null);
  const requestIdRef = useRef(0);
  const [retryToken, setRetryToken] = useState(0);

  // Stable key so equal filter objects don't refetch.
  const filterKey = useMemo(() => JSON.stringify(filters), [filters]);
  const filtersRef = useRef(filters);
  filtersRef.current = filters;

  const fetchPage = useCallback(async (page: number) => {
    controllerRef.current?.abort();
    const controller = new AbortController();
    controllerRef.current = controller;
    const requestId = ++requestIdRef.current;

    setState((prev) =>
      page === 1
        ? { ...prev, loading: true, loadingMore: false, error: null }
        : { ...prev, loadingMore: true, error: null }
    );

    try {
      const res = await api.discoverMovies({ ...filtersRef.current, page }, controller.signal);
      if (requestId !== requestIdRef.current) return;
      setState((prev) => {
        const existing = page === 1 ? [] : prev.movies;
        const seen = new Set(existing.map((m) => m.tmdbId));
        const merged = [...existing, ...res.movies.filter((m) => !seen.has(m.tmdbId))];
        return {
          result: res,
          movies: merged,
          page: res.page || page,
          totalPages: res.totalPages || 1,
          loading: false,
          loadingMore: false,
          error: null
        };
      });
    } catch (err) {
      if (isAbortError(err) || requestId !== requestIdRef.current) return;
      setState((prev) => ({
        ...prev,
        loading: false,
        loadingMore: false,
        error: err instanceof Error ? err.message : 'Falha ao carregar filmes.'
      }));
    }
  }, []);

  useEffect(() => {
    fetchPage(1);
  }, [filterKey, reloadToken, retryToken, fetchPage]);

  useEffect(() => () => controllerRef.current?.abort(), []);

  const hasMore = state.page > 0 && state.page < state.totalPages;

  const loadMore = useCallback(() => {
    if (state.loading || state.loadingMore || !hasMore) return;
    fetchPage(state.page + 1);
  }, [state.loading, state.loadingMore, state.page, hasMore, fetchPage]);

  const retry = useCallback(() => {
    if (state.page > 0 && state.movies.length > 0) fetchPage(state.page + 1);
    else setRetryToken((t) => t + 1);
  }, [state.page, state.movies.length, fetchPage]);

  return { ...state, hasMore, loadMore, retry };
}
