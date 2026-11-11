import React, { useRef } from 'react';
import { ChevronLeft, ChevronRight, Star } from 'lucide-react';
import { MovieCardDto } from '../types/api';
import { PosterImage } from './PosterImage';

interface MovieCarouselProps {
  movies: MovieCardDto[];
  onSelect: (movie: MovieCardDto) => void;
  /** Accessible label of the list. */
  label: string;
  /** Optional per-movie subtitle (e.g. recommendation reason). */
  subtitleFor?: (movie: MovieCardDto) => string | null | undefined;
  loading?: boolean;
  size?: 'sm' | 'md';
}

/** Horizontal, scroll-snapping poster carousel. */
export const MovieCarousel: React.FC<MovieCarouselProps> = ({
  movies,
  onSelect,
  label,
  subtitleFor,
  loading = false,
  size = 'md'
}) => {
  const scrollerRef = useRef<HTMLUListElement>(null);
  const itemWidth = size === 'sm' ? 'w-28 sm:w-32' : 'w-36 sm:w-44';

  const scrollBy = (dir: 1 | -1) => {
    const el = scrollerRef.current;
    if (!el) return;
    el.scrollBy({ left: dir * el.clientWidth * 0.8, behavior: 'smooth' });
  };

  return (
    <div className="relative group/carousel">
      <ul
        ref={scrollerRef}
        aria-label={label}
        className="flex gap-3 overflow-x-auto snap-x snap-mandatory scrollbar-none pb-1 -mx-1 px-1"
      >
        {loading
          ? Array.from({ length: 6 }, (_, i) => (
              <li key={i} className={`${itemWidth} shrink-0`} aria-hidden="true">
                <div className="aspect-[2/3] rounded-xl skeleton" />
                <div className="h-3 mt-2 w-3/4 rounded skeleton" />
              </li>
            ))
          : movies.map((m) => {
              const subtitle = subtitleFor?.(m);
              return (
                <li key={m.tmdbId} className={`${itemWidth} shrink-0 snap-start`}>
                  <button
                    type="button"
                    onClick={() => onSelect(m)}
                    className="group w-full text-left rounded-xl"
                    aria-label={`Ver detalhes de ${m.title}${subtitle ? ` — ${subtitle}` : ''}`}
                  >
                    <div className="relative aspect-[2/3] rounded-xl overflow-hidden border border-slate-800 group-hover:border-violet-500/60 transition-colors">
                      <PosterImage
                        src={m.posterUrl}
                        alt=""
                        fallbackTitle={m.title}
                        className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500"
                      />
                      <span className="absolute top-1.5 right-1.5 inline-flex items-center gap-0.5 px-1.5 py-0.5 rounded-full bg-black/75 text-[10px] font-bold text-amber-400">
                        <Star className="w-2.5 h-2.5 fill-amber-400" aria-hidden="true" />
                        {m.voteAverage.toFixed(1)}
                      </span>
                    </div>
                    <span className="block mt-1.5 text-xs font-semibold text-white line-clamp-1 group-hover:text-violet-300">
                      {m.title}
                    </span>
                    {subtitle ? (
                      <span className="block text-[11px] text-fuchsia-300/90 line-clamp-2 leading-snug">
                        {subtitle}
                      </span>
                    ) : (
                      m.releaseYear && <span className="block text-[11px] text-slate-500">{m.releaseYear}</span>
                    )}
                  </button>
                </li>
              );
            })}
      </ul>
      {!loading && movies.length > 3 && (
        <>
          <button
            type="button"
            onClick={() => scrollBy(-1)}
            aria-label="Rolar para a esquerda"
            className="hidden sm:flex absolute left-0 top-[35%] -translate-x-1/2 w-9 h-9 items-center justify-center rounded-full bg-black/80 border border-slate-700 text-white opacity-0 group-hover/carousel:opacity-100 focus-visible:opacity-100 transition-opacity"
          >
            <ChevronLeft className="w-5 h-5" aria-hidden="true" />
          </button>
          <button
            type="button"
            onClick={() => scrollBy(1)}
            aria-label="Rolar para a direita"
            className="hidden sm:flex absolute right-0 top-[35%] translate-x-1/2 w-9 h-9 items-center justify-center rounded-full bg-black/80 border border-slate-700 text-white opacity-0 group-hover/carousel:opacity-100 focus-visible:opacity-100 transition-opacity"
          >
            <ChevronRight className="w-5 h-5" aria-hidden="true" />
          </button>
        </>
      )}
    </div>
  );
};
