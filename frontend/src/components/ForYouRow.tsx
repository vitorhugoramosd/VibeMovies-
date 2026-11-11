import React, { useEffect, useState } from 'react';
import { Heart } from 'lucide-react';
import { api, isAbortError } from '../services/api';
import { AuthResponseDto, MovieCardDto, WatchlistItem } from '../types/api';
import { MovieCarousel } from './MovieCarousel';

interface ForYouRowProps {
  user: AuthResponseDto | null;
  watchlist: WatchlistItem[];
  /** Changes whenever the diary changes, so recommendations refresh. */
  watchlistVersion: number;
  onSelectMovie: (movie: MovieCardDto) => void;
}

/** "Para você" row — only rendered for logged-in users with at least one recommendation. */
export const ForYouRow: React.FC<ForYouRowProps> = ({ user, watchlist, watchlistVersion, onSelectMovie }) => {
  const [movies, setMovies] = useState<MovieCardDto[]>([]);
  const [loading, setLoading] = useState(true);
  const watchlistRef = React.useRef(watchlist);
  watchlistRef.current = watchlist;

  const userKey = user ? `${user.userId}:${user.token}` : '';

  useEffect(() => {
    if (!userKey || watchlistVersion === 0) {
      setMovies([]);
      setLoading(Boolean(userKey));
      return;
    }
    const controller = new AbortController();
    api
      .getRecommendations(watchlistRef.current, controller.signal)
      .then((list) => {
        const listed = new Set(watchlistRef.current.map((w) => w.tmdbMovieId));
        setMovies(list.filter((m) => !listed.has(m.tmdbId)));
      })
      .catch((err) => {
        if (!isAbortError(err)) setMovies([]);
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false);
      });
    return () => controller.abort();
  }, [userKey, watchlistVersion]);

  if (!user || (!loading && movies.length === 0)) return null;

  return (
    <section
      aria-labelledby="for-you-title"
      className="p-4 sm:p-5 rounded-3xl bg-gradient-to-br from-fuchsia-950/30 via-[#12141F] to-[#12141F] border border-fuchsia-500/20 space-y-3"
    >
      <div className="flex items-center gap-2">
        <Heart className="w-4 h-4 text-fuchsia-400 fill-fuchsia-400" aria-hidden="true" />
        <h2 id="for-you-title" className="font-display font-bold text-lg text-white">
          Para você
        </h2>
        <span className="text-xs text-slate-400 hidden sm:inline">com base no seu Diário de Bordo</span>
      </div>
      <MovieCarousel
        label="Recomendações para você"
        movies={movies}
        loading={loading}
        onSelect={onSelectMovie}
        subtitleFor={(m) => m.recommendationReason}
      />
    </section>
  );
};
