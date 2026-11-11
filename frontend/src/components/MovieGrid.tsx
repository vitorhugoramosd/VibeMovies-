import React, { useEffect, useRef } from 'react';
import { Loader2 } from 'lucide-react';
import { MovieCardDto, WatchlistItem, WatchlistType } from '../types/api';
import { MovieCard } from './MovieCard';
import { StateMessage } from './StateMessage';

export const MovieCardSkeleton: React.FC = () => (
  <div
    aria-hidden="true"
    className="flex flex-col rounded-2xl overflow-hidden border border-slate-800/80 bg-[#12141F]/90"
  >
    <div className="aspect-[2/3] w-full skeleton" />
    <div className="p-4 space-y-2.5">
      <div className="flex gap-1.5">
        <div className="h-4 w-16 rounded-md skeleton" />
        <div className="h-4 w-12 rounded-md skeleton" />
      </div>
      <div className="h-3 w-full rounded skeleton" />
      <div className="h-3 w-4/5 rounded skeleton" />
      <div className="grid grid-cols-2 gap-2 pt-2">
        <div className="h-7 rounded-lg skeleton" />
        <div className="h-7 rounded-lg skeleton" />
      </div>
    </div>
  </div>
);

const GRID = 'grid grid-cols-1 min-[420px]:grid-cols-2 lg:grid-cols-4 gap-4 sm:gap-6';

interface MovieGridProps {
  movies: MovieCardDto[];
  loading: boolean;
  loadingMore: boolean;
  error: string | null;
  hasMore: boolean;
  totalResults?: number;
  entryByMovieId: Map<number, WatchlistItem>;
  onSelectMovie: (movie: MovieCardDto) => void;
  onQuickSave: (movie: MovieCardDto, type: WatchlistType) => void;
  onLoadMore: () => void;
  onRetry: () => void;
  onClearFilters: () => void;
}

export const MovieGrid: React.FC<MovieGridProps> = ({
  movies,
  loading,
  loadingMore,
  error,
  hasMore,
  totalResults,
  entryByMovieId,
  onSelectMovie,
  onQuickSave,
  onLoadMore,
  onRetry,
  onClearFilters
}) => {
  const sentinelRef = useRef<HTMLDivElement>(null);
  const loadMoreRef = useRef(onLoadMore);
  loadMoreRef.current = onLoadMore;

  // Infinite scroll: load the next page when the sentinel approaches the viewport.
  useEffect(() => {
    const el = sentinelRef.current;
    if (!el || !hasMore || error || typeof IntersectionObserver === 'undefined') return;
    const observer = new IntersectionObserver(
      (entries) => {
        if (entries.some((e) => e.isIntersecting)) loadMoreRef.current();
      },
      { rootMargin: '600px 0px' }
    );
    observer.observe(el);
    return () => observer.disconnect();
  }, [hasMore, error, movies.length]);

  if (loading && movies.length === 0) {
    return (
      <div className={GRID} aria-busy="true" aria-label="Carregando filmes">
        {Array.from({ length: 8 }, (_, i) => (
          <MovieCardSkeleton key={i} />
        ))}
      </div>
    );
  }

  if (error && movies.length === 0) {
    return (
      <StateMessage
        tone="error"
        title="Não foi possível carregar os filmes"
        description={error}
        actionLabel="Tentar novamente"
        onAction={onRetry}
      />
    );
  }

  if (!loading && movies.length === 0) {
    return (
      <StateMessage
        title="Nenhum filme encontrado com essa combinação exata"
        description="Experimente relaxar o filtro de duração, trocar de streaming ou limpar um dos gêneros selecionados."
        actionLabel="Restaurar catálogo completo"
        onAction={onClearFilters}
      />
    );
  }

  return (
    <div className="space-y-6">
      <div
        className={`${GRID} transition-opacity ${loading ? 'opacity-50' : 'opacity-100'}`}
        aria-busy={loading}
      >
        {movies.map((movie) => (
          <MovieCard
            key={movie.tmdbId}
            movie={movie}
            watchlistEntry={entryByMovieId.get(movie.tmdbId)}
            onSelectMovie={onSelectMovie}
            onQuickSave={onQuickSave}
          />
        ))}
        {loadingMore && Array.from({ length: 4 }, (_, i) => <MovieCardSkeleton key={`more-${i}`} />)}
      </div>

      <div ref={sentinelRef} className="flex flex-col items-center gap-2">
        {error ? (
          <StateMessage
            compact
            tone="error"
            title="Falha ao carregar mais filmes"
            description={error}
            actionLabel="Tentar novamente"
            onAction={onRetry}
          />
        ) : hasMore ? (
          <button
            type="button"
            onClick={onLoadMore}
            disabled={loadingMore}
            className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-slate-900 hover:bg-slate-800 border border-slate-700 text-sm font-semibold text-slate-200 disabled:opacity-60"
          >
            {loadingMore && <Loader2 className="w-4 h-4 animate-spin" aria-hidden="true" />}
            {loadingMore ? 'Carregando...' : 'Carregar mais'}
          </button>
        ) : (
          movies.length > 0 && (
            <p className="text-xs text-slate-500">
              {totalResults ? `${totalResults} filmes • ` : ''}Você chegou ao fim da lista.
            </p>
          )
        )}
      </div>
    </div>
  );
};
