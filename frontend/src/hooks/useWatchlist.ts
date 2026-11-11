import { useCallback, useEffect, useMemo, useState } from 'react';
import { api } from '../services/api';
import { completeWatchlistStats } from '../services/fallbackData';
import { AuthResponseDto, MovieCardDto, WatchlistItem, WatchlistStatsDto, WatchlistType } from '../types/api';

export function useWatchlist(user: AuthResponseDto | null, initializing: boolean) {
  const [items, setItems] = useState<WatchlistItem[]>([]);
  const [rawStats, setRawStats] = useState<WatchlistStatsDto | null>(null);
  const [loading, setLoading] = useState(true);
  /** Bumps whenever the diary changes, so dependants (e.g. recommendations) can refresh. */
  const [version, setVersion] = useState(0);

  const refresh = useCallback(async () => {
    try {
      const [list, stats] = await Promise.all([api.getWatchlist(), api.getWatchlistStats()]);
      setItems(list);
      setRawStats(stats);
      setVersion((v) => v + 1);
    } finally {
      setLoading(false);
    }
  }, []);

  const userKey = user ? `${user.userId}:${user.token}` : '';

  useEffect(() => {
    if (initializing) return;
    if (!userKey) {
      setItems([]);
      setRawStats(null);
      setLoading(false);
      return;
    }
    setLoading(true);
    refresh();
  }, [userKey, initializing, refresh]);

  const stats = useMemo(() => completeWatchlistStats(rawStats, items), [rawStats, items]);

  const entryByMovieId = useMemo(() => {
    const map = new Map<number, WatchlistItem>();
    items.forEach((i) => map.set(i.tmdbMovieId, i));
    return map;
  }, [items]);

  const save = useCallback(
    async (movie: MovieCardDto, type: WatchlistType, rating?: number, notes?: string) => {
      await api.upsertWatchlist(movie, type, rating, notes);
      await refresh();
    },
    [refresh]
  );

  const update = useCallback(
    async (id: string, type: WatchlistType, rating?: number, notes?: string) => {
      await api.updateWatchlistItem(id, type, rating, notes);
      await refresh();
    },
    [refresh]
  );

  const remove = useCallback(
    async (id: string) => {
      await api.deleteWatchlistItem(id);
      await refresh();
    },
    [refresh]
  );

  return { items, stats, loading, version, entryByMovieId, refresh, save, update, remove };
}
