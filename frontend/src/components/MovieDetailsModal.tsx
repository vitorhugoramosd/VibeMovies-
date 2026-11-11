import React, { useEffect, useRef, useState } from 'react';
import {
  Bookmark,
  CheckCircle2,
  Clock,
  ExternalLink,
  Film,
  Play,
  Share2,
  Sparkles,
  Star,
  Tv,
  Users
} from 'lucide-react';
import { MovieCardDto, StreamingProviderBadgeDto, WatchlistItem, WatchlistType } from '../types/api';
import { Modal } from './Modal';
import { MovieCarousel } from './MovieCarousel';
import { PosterImage } from './PosterImage';
import { api, isAbortError } from '../services/api';
import { buildMovieShareUrl } from '../lib/urlState';
import { useToast } from '../context/ToastContext';

interface MovieDetailsModalProps {
  movie: MovieCardDto | null;
  watchlistEntry?: WatchlistItem;
  onClose: () => void;
  onSelectMovie: (movie: MovieCardDto) => void;
  onSaveWatchlist: (movie: MovieCardDto, type: WatchlistType, rating?: number, notes?: string) => Promise<void>;
}

export const MovieDetailsModal: React.FC<MovieDetailsModalProps> = ({ movie, ...rest }) => (
  <Modal
    open={Boolean(movie)}
    onClose={rest.onClose}
    labelledBy="movie-details-title"
    panelClassName="w-full max-w-4xl"
  >
    {/* key: reset rating/notes/trailer state whenever another movie is opened */}
    {movie && <MovieDetailsContent key={movie.tmdbId} movie={movie} {...rest} />}
  </Modal>
);

const ProviderChip: React.FC<{ p: StreamingProviderBadgeDto; large?: boolean }> = ({ p, large }) => (
  <div
    className={
      large
        ? 'flex items-center gap-2 px-3 py-1.5 rounded-xl bg-emerald-950/30 border border-emerald-500/30 text-sm text-white font-medium'
        : 'flex items-center gap-1.5 px-2.5 py-1 rounded-lg bg-slate-800/80 text-xs text-slate-200'
    }
  >
    <img
      src={p.logoUrl}
      alt=""
      loading="lazy"
      className={large ? 'w-6 h-6 rounded-lg object-cover bg-slate-800' : 'w-4 h-4 rounded bg-slate-700'}
    />
    {p.providerName}
  </div>
);

const MovieDetailsContent: React.FC<Omit<MovieDetailsModalProps, 'movie'> & { movie: MovieCardDto }> = ({
  movie,
  watchlistEntry,
  onSelectMovie,
  onSaveWatchlist
}) => {
  const toast = useToast();
  const [rating, setRating] = useState<number>(watchlistEntry?.personalRating || 5);
  const [notes, setNotes] = useState<string>(watchlistEntry?.personalNotes || '');
  const [saving, setSaving] = useState(false);
  const [showTrailer, setShowTrailer] = useState(false);
  const [similar, setSimilar] = useState<MovieCardDto[]>([]);
  const [similarLoading, setSimilarLoading] = useState(true);
  const heroRef = useRef<HTMLDivElement>(null);

  // When switching to another movie (similar carousel), start again from the top of the dialog.
  useEffect(() => {
    heroRef.current?.closest('[role="dialog"]')?.scrollTo({ top: 0 });
  }, []);

  const flatrate = movie.watchProviders?.flatrate || [];
  const rent = movie.watchProviders?.rent || [];
  const buy = movie.watchProviders?.buy || [];
  const isWantToWatch = watchlistEntry?.listType === WatchlistType.WantToWatch;

  useEffect(() => {
    const controller = new AbortController();
    setSimilarLoading(true);
    api
      .getSimilar(movie.tmdbId, controller.signal)
      .then((list) => setSimilar(list.filter((m) => m.tmdbId !== movie.tmdbId)))
      .catch((err) => {
        if (!isAbortError(err)) setSimilar([]);
      })
      .finally(() => {
        if (!controller.signal.aborted) setSimilarLoading(false);
      });
    return () => controller.abort();
  }, [movie.tmdbId]);

  const handleSaveWatched = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    try {
      await onSaveWatchlist(movie, WatchlistType.Watched, rating, notes);
    } finally {
      setSaving(false);
    }
  };

  const handleShare = async () => {
    const url = buildMovieShareUrl(movie.tmdbId);
    const shareData = { title: `${movie.title} — VibeMovies`, text: movie.tagline || movie.overview.slice(0, 120), url };
    try {
      if (typeof navigator.share === 'function' && (!navigator.canShare || navigator.canShare(shareData))) {
        await navigator.share(shareData);
        return;
      }
    } catch (err) {
      // User cancelled the native sheet: nothing to do.
      if (err instanceof DOMException && err.name === 'AbortError') return;
    }
    try {
      await navigator.clipboard.writeText(url);
      toast('Link copiado para a área de transferência!');
    } catch {
      window.prompt('Copie o link do filme:', url);
    }
  };

  const trailerSearchUrl = `https://www.youtube.com/results?search_query=${encodeURIComponent(
    `${movie.title} ${movie.releaseYear ?? ''} trailer`
  )}`;

  return (
    <>
      {/* Hero: backdrop or trailer */}
      <div ref={heroRef} className="relative w-full overflow-hidden rounded-t-3xl bg-black">
        {showTrailer && movie.trailerYoutubeKey ? (
          <div className="relative aspect-video w-full">
            <iframe
              className="absolute inset-0 w-full h-full"
              src={`https://www.youtube-nocookie.com/embed/${encodeURIComponent(movie.trailerYoutubeKey)}?autoplay=1&rel=0&modestbranding=1`}
              title={`Trailer de ${movie.title}`}
              allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
              allowFullScreen
              referrerPolicy="strict-origin-when-cross-origin"
            />
          </div>
        ) : (
          <div className="relative h-56 sm:h-80">
            <PosterImage
              src={movie.backdropUrl || movie.posterUrl}
              alt=""
              eager
              fallbackTitle={movie.title}
              className="w-full h-full object-cover"
            />
            <div className="absolute inset-0 bg-gradient-to-t from-[#11131E] via-[#11131E]/60 to-transparent" />
            <div className="absolute bottom-4 left-4 right-4 sm:left-6 sm:right-6">
              <div className="flex flex-wrap items-center gap-2 mb-2">
                <span className="inline-flex items-center gap-1 px-3 py-1 rounded-full text-xs font-bold bg-violet-600 text-white">
                  <Sparkles className="w-3.5 h-3.5" aria-hidden="true" />
                  {movie.vibeMatchScore}% Compatível
                </span>
                <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-bold bg-amber-500/20 text-amber-300 border border-amber-500/30">
                  <Star className="w-3.5 h-3.5 fill-amber-400 text-amber-400" aria-hidden="true" />
                  TMDB {movie.voteAverage.toFixed(1)}
                </span>
                {movie.runtimeMinutes && (
                  <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs bg-slate-800/90 text-slate-200">
                    <Clock className="w-3.5 h-3.5 text-violet-400" aria-hidden="true" />
                    {movie.runtimeMinutes} min
                  </span>
                )}
              </div>
              <h2 id="movie-details-title" className="font-display text-2xl sm:text-4xl font-extrabold text-white pr-8">
                {movie.title}
              </h2>
              {movie.originalTitle && movie.originalTitle !== movie.title && (
                <p className="text-xs sm:text-sm text-slate-400">
                  Título original: {movie.originalTitle} {movie.releaseYear ? `(${movie.releaseYear})` : ''}
                </p>
              )}
            </div>
          </div>
        )}
      </div>

      {showTrailer && (
        <h2 id="movie-details-title" className="px-6 pt-4 font-display text-xl sm:text-2xl font-extrabold text-white">
          {movie.title}
        </h2>
      )}

      {/* Actions row */}
      <div className="px-4 sm:px-8 pt-4 flex flex-wrap items-center gap-2">
        {movie.trailerYoutubeKey ? (
          <button
            type="button"
            onClick={() => setShowTrailer((v) => !v)}
            aria-pressed={showTrailer}
            className="inline-flex items-center gap-1.5 px-4 py-2 rounded-xl bg-red-600 hover:bg-red-500 text-sm font-bold text-white"
          >
            <Play className="w-4 h-4 fill-white" aria-hidden="true" />
            {showTrailer ? 'Fechar trailer' : 'Ver trailer'}
          </button>
        ) : (
          <a
            href={trailerSearchUrl}
            target="_blank"
            rel="noreferrer"
            className="inline-flex items-center gap-1.5 px-4 py-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-sm font-semibold text-white"
          >
            <Play className="w-4 h-4" aria-hidden="true" />
            Buscar trailer no YouTube
            <ExternalLink className="w-3.5 h-3.5 text-slate-400" aria-hidden="true" />
          </a>
        )}
        <button
          type="button"
          onClick={handleShare}
          className="inline-flex items-center gap-1.5 px-4 py-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-sm font-semibold text-white"
        >
          <Share2 className="w-4 h-4" aria-hidden="true" />
          Compartilhar
        </button>
      </div>

      <div className="p-4 sm:p-8 grid grid-cols-1 lg:grid-cols-3 gap-6 sm:gap-8">
        <div className="lg:col-span-2 space-y-6 min-w-0">
          <div>
            {movie.tagline && (
              <p className="font-display text-lg italic text-violet-300 mb-3">&ldquo;{movie.tagline}&rdquo;</p>
            )}
            <h3 className="text-xs font-bold uppercase tracking-wider text-violet-400 mb-2">Sinopse & Atmosfera</h3>
            <p className="text-slate-200 text-sm sm:text-base leading-relaxed">{movie.overview}</p>

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

          {(movie.director || movie.cast?.length > 0) && (
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 p-4 rounded-2xl bg-slate-900/70 border border-slate-800/80">
              {movie.director && (
                <div className="flex items-start gap-3">
                  <Film className="w-4 h-4 text-violet-400 mt-0.5 shrink-0" aria-hidden="true" />
                  <div>
                    <span className="text-xs text-slate-400 block">Direção</span>
                    <span className="text-sm font-semibold text-white">{movie.director}</span>
                  </div>
                </div>
              )}
              {movie.cast?.length > 0 && (
                <div className="flex items-start gap-3">
                  <Users className="w-4 h-4 text-violet-400 mt-0.5 shrink-0" aria-hidden="true" />
                  <div>
                    <span className="text-xs text-slate-400 block">Elenco Principal</span>
                    <span className="text-sm font-semibold text-white">{movie.cast.join(', ')}</span>
                  </div>
                </div>
              )}
            </div>
          )}

          {/* Where to watch */}
          <div className="p-4 sm:p-5 rounded-2xl bg-[#161929] border border-slate-800 space-y-4">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <div className="flex items-center gap-2">
                <Tv className="w-5 h-5 text-emerald-400" aria-hidden="true" />
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
                  JustWatch / TMDB <ExternalLink className="w-3.5 h-3.5" aria-hidden="true" />
                </a>
              )}
            </div>

            <div>
              <span className="text-xs font-semibold uppercase tracking-wider text-emerald-400 block mb-2">
                Incluído na Assinatura (Stream)
              </span>
              {flatrate.length > 0 ? (
                <div className="flex flex-wrap gap-2.5">
                  {flatrate.map((p) => (
                    <ProviderChip key={p.providerId} p={p} large />
                  ))}
                </div>
              ) : (
                <p className="text-xs text-slate-400">Não disponível em assinatura fixa no momento.</p>
              )}
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 pt-2 border-t border-slate-800/80">
              <div>
                <span className="text-xs font-semibold uppercase tracking-wider text-sky-400 block mb-2">
                  Para Alugar (Rent)
                </span>
                {rent.length > 0 ? (
                  <div className="flex flex-wrap gap-2">
                    {rent.map((p) => (
                      <ProviderChip key={p.providerId} p={p} />
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
                      <ProviderChip key={p.providerId} p={p} />
                    ))}
                  </div>
                ) : (
                  <span className="text-xs text-slate-500">Indisponível para compra</span>
                )}
              </div>
            </div>
          </div>
        </div>

        {/* Diary controls */}
        <div className="p-4 sm:p-5 rounded-2xl bg-[#161929] border border-slate-800 flex flex-col space-y-5 h-fit">
          <div>
            <h3 className="font-display font-bold text-white text-base mb-1">Diário de Bordo</h3>
            <p className="text-xs text-slate-400 mb-4">
              Salve na sua lista &ldquo;Quero Ver&rdquo; ou registre sua avaliação pessoal em &ldquo;Já Vi&rdquo;.
            </p>

            <button
              type="button"
              onClick={() => onSaveWatchlist(movie, WatchlistType.WantToWatch)}
              aria-pressed={isWantToWatch}
              className={`w-full flex items-center justify-center gap-2 py-2.5 px-4 rounded-xl text-sm font-semibold transition-all mb-5 ${
                isWantToWatch
                  ? 'bg-violet-600 text-white shadow-lg shadow-violet-900/40'
                  : 'bg-slate-800 hover:bg-slate-700 text-slate-200 border border-slate-700'
              }`}
            >
              <Bookmark className="w-4 h-4" aria-hidden="true" />
              {isWantToWatch ? 'Salvo em Quero Ver' : 'Adicionar a Quero Ver'}
            </button>

            <form onSubmit={handleSaveWatched} className="space-y-4 pt-4 border-t border-slate-800">
              <fieldset>
                <legend className="text-xs font-semibold text-slate-300 block mb-2">Sua Nota Pessoal (1 a 5 estrelas)</legend>
                <div className="flex items-center gap-1">
                  {[1, 2, 3, 4, 5].map((star) => (
                    <button
                      key={star}
                      type="button"
                      onClick={() => setRating(star)}
                      aria-label={`${star} estrela${star > 1 ? 's' : ''}`}
                      aria-pressed={star === rating}
                      className="p-1.5 rounded-lg hover:bg-slate-800 transition-colors"
                    >
                      <Star
                        className={`w-6 h-6 ${star <= rating ? 'fill-amber-400 text-amber-400' : 'text-slate-600'}`}
                        aria-hidden="true"
                      />
                    </button>
                  ))}
                  <span className="ml-1 text-sm font-bold text-amber-400">{rating}/5</span>
                </div>
              </fieldset>

              <div>
                <label htmlFor="details-notes" className="text-xs font-semibold text-slate-300 block mb-1.5">
                  Anotações / Crítica Pessoal
                </label>
                <textarea
                  id="details-notes"
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
                className="w-full flex items-center justify-center gap-2 py-2.5 px-4 rounded-xl text-sm font-bold bg-emerald-600 hover:bg-emerald-500 disabled:opacity-60 text-white shadow-lg shadow-emerald-950/50 transition-all"
              >
                <CheckCircle2 className="w-4 h-4" aria-hidden="true" />
                {saving ? 'Salvando...' : 'Registrar como Já Vi'}
              </button>
            </form>
          </div>
        </div>
      </div>

      {/* Similar movies */}
      {(similarLoading || similar.length > 0) && (
        <section className="px-4 sm:px-8 pb-6 sm:pb-8" aria-labelledby="similar-title">
          <h3 id="similar-title" className="font-display font-bold text-white text-base mb-3">
            Filmes parecidos
          </h3>
          <MovieCarousel
            label={`Filmes parecidos com ${movie.title}`}
            movies={similar}
            loading={similarLoading}
            onSelect={onSelectMovie}
            size="sm"
          />
        </section>
      )}
    </>
  );
};
