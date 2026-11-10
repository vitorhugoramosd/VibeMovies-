import React, { useState } from 'react';
import {
  Bookmark,
  CheckCircle2,
  Clock,
  ExternalLink,
  Film,
  Sparkles,
  Star,
  Tv,
  Users,
  X
} from 'lucide-react';
import { MovieCardDto, WatchlistItem, WatchlistType } from '../types/api';

interface MovieDetailsModalProps {
  movie: MovieCardDto | null;
  watchlistEntry?: WatchlistItem;
  onClose: () => void;
  onSaveWatchlist: (
    movie: MovieCardDto,
    type: WatchlistType,
    rating?: number,
    notes?: string
  ) => Promise<void>;
}

export const MovieDetailsModal: React.FC<MovieDetailsModalProps> = ({
  movie,
  watchlistEntry,
  onClose,
  onSaveWatchlist
}) => {
  if (!movie) return null;

  const [rating, setRating] = useState<number>(watchlistEntry?.personalRating || 5);
  const [notes, setNotes] = useState<string>(watchlistEntry?.personalNotes || '');
  const [saving, setSaving] = useState(false);

  const flatrate = movie.watchProviders?.flatrate || [];
  const rent = movie.watchProviders?.rent || [];
  const buy = movie.watchProviders?.buy || [];

  const handleSaveWatched = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    try {
      await onSaveWatchlist(movie, WatchlistType.Watched, rating, notes);
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-md animate-fadeIn">
      <div className="relative w-full max-w-4xl max-h-[90vh] overflow-y-auto bg-[#11131E] border border-slate-800 rounded-3xl shadow-2xl">
        {/* Close Button */}
        <button
          onClick={onClose}
          className="absolute top-4 right-4 z-20 p-2 rounded-full bg-black/60 text-slate-200 hover:bg-violet-600 hover:text-white transition-colors"
        >
          <X className="w-5 h-5" />
        </button>

        {/* Hero Backdrop */}
        <div className="relative h-64 sm:h-80 w-full overflow-hidden">
          <img
            src={movie.backdropUrl || movie.posterUrl}
            alt={movie.title}
            className="w-full h-full object-cover"
          />
          <div className="absolute inset-0 bg-gradient-to-t from-[#11131E] via-[#11131E]/60 to-transparent" />

          <div className="absolute bottom-4 left-6 right-6 flex flex-col sm:flex-row sm:items-end justify-between gap-4">
            <div>
              <div className="flex flex-wrap items-center gap-2 mb-2">
                <span className="inline-flex items-center gap-1 px-3 py-1 rounded-full text-xs font-bold bg-violet-600 text-white">
                  <Sparkles className="w-3.5 h-3.5" />
                  {movie.vibeMatchScore}% Compatível com a Vibe
                </span>
                <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-bold bg-amber-500/20 text-amber-300 border border-amber-500/30">
                  <Star className="w-3.5 h-3.5 fill-amber-400 text-amber-400" />
                  TMDB {movie.voteAverage.toFixed(1)}
                </span>
                {movie.runtimeMinutes && (
                  <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs bg-slate-800/90 text-slate-200">
                    <Clock className="w-3.5 h-3.5 text-violet-400" />
                    {movie.runtimeMinutes} min
                  </span>
                )}
              </div>
              <h2 className="font-display text-2xl sm:text-4xl font-extrabold text-white">
                {movie.title}
              </h2>
              {movie.originalTitle && movie.originalTitle !== movie.title && (
                <p className="text-xs sm:text-sm text-slate-400">
                  Título original: {movie.originalTitle} ({movie.releaseYear})
                </p>
              )}
            </div>
          </div>
        </div>

        {/* Content Grid */}
        <div className="p-6 sm:p-8 grid grid-cols-1 lg:grid-cols-3 gap-8">
          {/* Left 2 Columns: Synopsis, Cast & Watch Providers */}
          <div className="lg:col-span-2 space-y-6">
            <div>
              <h3 className="text-xs font-bold uppercase tracking-wider text-violet-400 mb-2">
                Sinopse & Atmosfera
              </h3>
              <p className="text-slate-200 text-sm sm:text-base leading-relaxed">
                {movie.overview}
              </p>

              <div className="flex flex-wrap gap-2 mt-4">
                {movie.genres.map((g) => (
                  <span
                    key={g}
                    className="px-3 py-1 rounded-lg text-xs font-semibold bg-slate-800 text-slate-200 border border-slate-700"
                  >
                    {g}
                  </span>
                ))}
                {movie.keywords.map((kw) => (
                  <span
                    key={kw}
                    className="px-2.5 py-1 rounded-lg text-xs bg-violet-950/50 text-violet-300 border border-violet-800/40"
                  >
                    #{kw}
                  </span>
                ))}
              </div>
            </div>

            {(movie.director || (movie.cast && movie.cast.length > 0)) && (
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 p-4 rounded-2xl bg-slate-900/70 border border-slate-800/80">
                {movie.director && (
                  <div className="flex items-start gap-3">
                    <Film className="w-4 h-4 text-violet-400 mt-0.5 shrink-0" />
                    <div>
                      <span className="text-xs text-slate-400 block">Direção</span>
                      <span className="text-sm font-semibold text-white">{movie.director}</span>
                    </div>
                  </div>
                )}
                {movie.cast && movie.cast.length > 0 && (
                  <div className="flex items-start gap-3">
                    <Users className="w-4 h-4 text-violet-400 mt-0.5 shrink-0" />
                    <div>
                      <span className="text-xs text-slate-400 block">Elenco Principal</span>
                      <span className="text-sm font-semibold text-white">
                        {movie.cast.join(', ')}
                      </span>
                    </div>
                  </div>
                )}
              </div>
            )}

            {/* Onde Assistir (JustWatch via TMDB) */}
            <div className="p-5 rounded-2xl bg-[#161929] border border-slate-800 space-y-4">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <Tv className="w-5 h-5 text-emerald-400" />
                  <h3 className="font-display font-bold text-white text-base">
                    Onde Assistir ({movie.watchProviders?.region || 'BR'})
                  </h3>
                </div>
                {movie.watchProviders?.tmdbWatchLink && (
                  <a
                    href={movie.watchProviders.tmdbWatchLink}
                    target="_blank"
                    rel="noreferrer"
                    className="inline-flex items-center gap-1 text-xs text-violet-400 hover:text-violet-300 font-medium"
                  >
                    JustWatch / TMDB <ExternalLink className="w-3.5 h-3.5" />
                  </a>
                )}
              </div>

              {/* Assinatura */}
              <div>
                <span className="text-xs font-semibold uppercase tracking-wider text-emerald-400 block mb-2">
                  Incluído na Assinatura (Stream)
                </span>
                {flatrate.length > 0 ? (
                  <div className="flex flex-wrap gap-2.5">
                    {flatrate.map((p) => (
                      <div
                        key={p.providerId}
                        className="flex items-center gap-2 px-3 py-1.5 rounded-xl bg-emerald-950/30 border border-emerald-500/30 text-sm text-white font-medium"
                      >
                        <img
                          src={p.logoUrl}
                          alt={p.providerName}
                          className="w-6 h-6 rounded-lg object-cover"
                        />
                        {p.providerName}
                      </div>
                    ))}
                  </div>
                ) : (
                  <p className="text-xs text-slate-400">
                    Não disponível em assinatura fixa no momento.
                  </p>
                )}
              </div>

              {/* Aluguel & Compra */}
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 pt-2 border-t border-slate-800/80">
                <div>
                  <span className="text-xs font-semibold uppercase tracking-wider text-sky-400 block mb-2">
                    Para Alugar (Rent)
                  </span>
                  {rent.length > 0 ? (
                    <div className="flex flex-wrap gap-2">
                      {rent.map((p) => (
                        <div
                          key={p.providerId}
                          className="flex items-center gap-1.5 px-2.5 py-1 rounded-lg bg-slate-800/80 text-xs text-slate-200"
                        >
                          <img src={p.logoUrl} alt={p.providerName} className="w-4 h-4 rounded" />
                          {p.providerName}
                        </div>
                      ))}
                    </div>
                  ) : (
                    <span className="text-xs text-slate-500">Indisponível para aluguel</span>
                  )}
                </div>

                <div>
                  <span className="text-xs font-semibold uppercase tracking-wider text-amber-400 block mb-2">
                    Para Comprar (Buy)
                  </span>
                  {buy.length > 0 ? (
                    <div className="flex flex-wrap gap-2">
                      {buy.map((p) => (
                        <div
                          key={p.providerId}
                          className="flex items-center gap-1.5 px-2.5 py-1 rounded-lg bg-slate-800/80 text-xs text-slate-200"
                        >
                          <img src={p.logoUrl} alt={p.providerName} className="w-4 h-4 rounded" />
                          {p.providerName}
                        </div>
                      ))}
                    </div>
                  ) : (
                    <span className="text-xs text-slate-500">Indisponível para compra</span>
                  )}
                </div>
              </div>
            </div>
          </div>

          {/* Right Column: Diário de Bordo Controls */}
          <div className="p-5 rounded-2xl bg-[#161929] border border-slate-800 flex flex-col justify-between space-y-5">
            <div>
              <h3 className="font-display font-bold text-white text-base mb-1">
                Diário de Bordo
              </h3>
              <p className="text-xs text-slate-400 mb-4">
                Salve na sua lista &ldquo;Quero Ver&rdquo; ou registre sua avaliação pessoal em &ldquo;Já Vi&rdquo;.
              </p>

              <button
                type="button"
                onClick={() => onSaveWatchlist(movie, WatchlistType.WantToWatch)}
                className={`w-full flex items-center justify-center gap-2 py-2.5 px-4 rounded-xl text-sm font-semibold transition-all mb-5 ${
                  watchlistEntry?.listType === WatchlistType.WantToWatch
                    ? 'bg-violet-600 text-white shadow-lg shadow-violet-900/40'
                    : 'bg-slate-800 hover:bg-slate-700 text-slate-200 border border-slate-700'
                }`}
              >
                <Bookmark className="w-4 h-4" />
                {watchlistEntry?.listType === WatchlistType.WantToWatch
                  ? 'Salvo em Quero Ver'
                  : 'Adicionar a Quero Ver'}
              </button>

              <form onSubmit={handleSaveWatched} className="space-y-4 pt-4 border-t border-slate-800">
                <div>
                  <label className="text-xs font-semibold text-slate-300 block mb-2">
                    Sua Nota Pessoal (1 a 5 estrelas)
                  </label>
                  <div className="flex items-center gap-1.5">
                    {[1, 2, 3, 4, 5].map((star) => (
                      <button
                        key={star}
                        type="button"
                        onClick={() => setRating(star)}
                        className="p-1.5 rounded-lg hover:bg-slate-800 transition-colors"
                      >
                        <Star
                          className={`w-6 h-6 ${
                            star <= rating
                              ? 'fill-amber-400 text-amber-400'
                              : 'text-slate-600'
                          }`}
                        />
                      </button>
                    ))}
                    <span className="ml-2 text-sm font-bold text-amber-400">{rating}/5</span>
                  </div>
                </div>

                <div>
                  <label className="text-xs font-semibold text-slate-300 block mb-1.5">
                    Anotações / Crítica Pessoal
                  </label>
                  <textarea
                    rows={3}
                    value={notes}
                    onChange={(e) => setNotes(e.target.value)}
                    placeholder="O que achou da atmosfera, direção ou plot twist?"
                    className="w-full rounded-xl bg-slate-900 border border-slate-700 px-3 py-2 text-xs text-white placeholder-slate-500 focus:outline-none focus:border-violet-500"
                  />
                </div>

                <button
                  type="submit"
                  disabled={saving}
                  className="w-full flex items-center justify-center gap-2 py-2.5 px-4 rounded-xl text-sm font-bold bg-emerald-600 hover:bg-emerald-500 text-white shadow-lg shadow-emerald-950/50 transition-all"
                >
                  <CheckCircle2 className="w-4 h-4" />
                  {saving ? 'Salvando...' : 'Registrar como Já Vi'}
                </button>
              </form>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
