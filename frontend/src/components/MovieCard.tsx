import React from 'react';
import { Bookmark, CheckCircle2, Clock, Eye, Sparkles, Star } from 'lucide-react';
import { MovieCardDto, WatchlistItem, WatchlistType } from '../types/api';

interface MovieCardProps {
  movie: MovieCardDto;
  watchlistEntry?: WatchlistItem;
  onSelectMovie: (movie: MovieCardDto) => void;
  onQuickSave: (movie: MovieCardDto, type: WatchlistType) => void;
}

export const MovieCard: React.FC<MovieCardProps> = ({
  movie,
  watchlistEntry,
  onSelectMovie,
  onQuickSave
}) => {
  const flatrate = movie.watchProviders?.flatrate || [];
  const rent = movie.watchProviders?.rent || [];

  const isWantToWatch = watchlistEntry?.listType === WatchlistType.WantToWatch;
  const isWatched = watchlistEntry?.listType === WatchlistType.Watched;

  return (
    <div
      className="group relative flex flex-col bg-[#12141F]/90 border border-slate-800/80 rounded-2xl overflow-hidden hover:border-violet-500/60 transition-all duration-300 hover:-translate-y-1 hover:shadow-2xl hover:shadow-violet-950/30"
    >
      {/* Poster Container */}
      <div
        onClick={() => onSelectMovie(movie)}
        className="relative aspect-[2/3] w-full overflow-hidden bg-slate-900 cursor-pointer"
      >
        <img
          src={movie.posterUrl}
          alt={movie.title}
          loading="lazy"
          className="h-full w-full object-cover object-center group-hover:scale-105 transition-transform duration-500"
        />
        <div className="absolute inset-0 bg-gradient-to-t from-[#12141F] via-[#12141F]/20 to-transparent opacity-90" />

        {/* Top Badges */}
        <div className="absolute top-3 left-3 right-3 flex items-center justify-between gap-2">
          <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-semibold bg-violet-600/90 text-white backdrop-blur-md shadow-lg">
            <Sparkles className="w-3 h-3" />
            {movie.vibeMatchScore}% Vibe
          </span>

          <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-bold bg-black/75 text-amber-400 border border-amber-500/30 backdrop-blur-md">
            <Star className="w-3.5 h-3.5 fill-amber-400 text-amber-400" />
            {movie.voteAverage.toFixed(1)}
          </span>
        </div>

        {/* Bottom Metadata Overlay on Poster */}
        <div className="absolute bottom-3 left-3 right-3">
          <div className="flex flex-wrap items-center gap-2 text-xs text-slate-300 mb-1">
            {movie.releaseYear && (
              <span className="font-medium bg-white/10 px-2 py-0.5 rounded-md backdrop-blur-sm">
                {movie.releaseYear}
              </span>
            )}
            {movie.runtimeMinutes && (
              <span className="inline-flex items-center gap-1 bg-white/10 px-2 py-0.5 rounded-md backdrop-blur-sm">
                <Clock className="w-3 h-3 text-violet-400" />
                {movie.runtimeMinutes} min
              </span>
            )}
          </div>
          <h3 className="font-display font-bold text-base sm:text-lg text-white leading-tight line-clamp-1 group-hover:text-violet-300 transition-colors">
            {movie.title}
          </h3>
        </div>
      </div>

      {/* Card Body */}
      <div className="p-4 flex-1 flex flex-col justify-between gap-3">
        <div>
          {/* Genres */}
          <div className="flex flex-wrap gap-1.5 mb-2.5">
            {movie.genres.slice(0, 3).map((g) => (
              <span
                key={g}
                className="text-[11px] font-medium px-2 py-0.5 rounded-md bg-slate-800/90 text-slate-300 border border-slate-700/60"
              >
                {g}
              </span>
            ))}
          </div>

          <p
            onClick={() => onSelectMovie(movie)}
            className="text-xs text-slate-400 line-clamp-2 cursor-pointer hover:text-slate-300"
          >
            {movie.overview}
          </p>
        </div>

        {/* Streaming Availability Section */}
        <div className="pt-2.5 border-t border-slate-800/80 flex flex-col gap-2.5">
          <div className="flex items-center justify-between gap-2 min-h-[28px]">
            {flatrate.length > 0 ? (
              <div className="flex items-center gap-1.5">
                <span className="text-[10px] uppercase tracking-wider font-semibold text-emerald-400 bg-emerald-500/10 border border-emerald-500/20 px-1.5 py-0.5 rounded">
                  Streaming
                </span>
                <div className="flex -space-x-1.5 overflow-hidden">
                  {flatrate.slice(0, 4).map((p) => (
                    <img
                      key={p.providerId}
                      src={p.logoUrl}
                      alt={p.providerName}
                      title={`Incluído em: ${p.providerName}`}
                      className="w-6 h-6 rounded-full ring-2 ring-[#12141F] object-cover"
                    />
                  ))}
                </div>
                <span className="text-xs text-slate-300 font-medium truncate max-w-[110px]">
                  {flatrate[0].providerName}
                </span>
              </div>
            ) : rent.length > 0 ? (
              <div className="flex items-center gap-1.5">
                <span className="text-[10px] uppercase tracking-wider font-semibold text-sky-400 bg-sky-500/10 border border-sky-500/20 px-1.5 py-0.5 rounded">
                  Aluguel
                </span>
                <span className="text-xs text-slate-400 truncate">
                  {rent.map((r) => r.providerName).slice(0, 2).join(', ')}
                </span>
              </div>
            ) : (
              <span className="text-[11px] text-slate-500">Consultar disponibilidade</span>
            )}
          </div>

          {/* Quick Watchlist Action Buttons */}
          <div className="grid grid-cols-2 gap-2">
            <button
              type="button"
              onClick={() => onQuickSave(movie, WatchlistType.WantToWatch)}
              className={`flex items-center justify-center gap-1.5 py-1.5 px-2.5 rounded-lg text-xs font-medium transition-all ${
                isWantToWatch
                  ? 'bg-violet-600 text-white shadow-md shadow-violet-900/40'
                  : 'bg-slate-800/80 hover:bg-slate-700 text-slate-200 border border-slate-700/70'
              }`}
            >
              <Bookmark className={`w-3.5 h-3.5 ${isWantToWatch ? 'fill-white' : ''}`} />
              {isWantToWatch ? 'Na Lista' : 'Quero Ver'}
            </button>

            <button
              type="button"
              onClick={() =>
                isWatched ? onSelectMovie(movie) : onQuickSave(movie, WatchlistType.Watched)
              }
              className={`flex items-center justify-center gap-1.5 py-1.5 px-2.5 rounded-lg text-xs font-medium transition-all ${
                isWatched
                  ? 'bg-emerald-600 text-white shadow-md shadow-emerald-900/40'
                  : 'bg-slate-800/80 hover:bg-slate-700 text-slate-200 border border-slate-700/70'
              }`}
            >
              {isWatched ? (
                <>
                  <CheckCircle2 className="w-3.5 h-3.5" />
                  Já Vi {watchlistEntry?.personalRating ? `(${watchlistEntry.personalRating}★)` : ''}
                </>
              ) : (
                <>
                  <Eye className="w-3.5 h-3.5" />
                  Já Vi
                </>
              )}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};
