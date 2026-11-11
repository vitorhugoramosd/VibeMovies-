import React, { useCallback, useEffect, useRef, useState } from 'react';
import { Bookmark, Dices, Info, Loader2, Star, Volume2 } from 'lucide-react';
import { Modal } from './Modal';
import { PosterImage } from './PosterImage';
import { api, DiscoverParams, isAbortError } from '../services/api';
import { FALLBACK_MOVIES } from '../services/fallbackData';
import { MovieCardDto, SurpriseResponseDto } from '../types/api';

interface SurpriseModalProps {
  open: boolean;
  onClose: () => void;
  filters: DiscoverParams;
  /** Posters used for the shuffle animation (current results). */
  posterPool: MovieCardDto[];
  onOpenDetails: (movie: MovieCardDto) => void;
  onWantToWatch: (movie: MovieCardDto) => Promise<void>;
}

const MAX_EXCLUDED = 15;
const SPIN_MS = 1700;

function prefersReducedMotion(): boolean {
  try {
    return window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  } catch {
    return false;
  }
}

export const SurpriseModal: React.FC<SurpriseModalProps> = ({
  open,
  onClose,
  filters,
  posterPool,
  onOpenDetails,
  onWantToWatch
}) => {
  const [phase, setPhase] = useState<'spinning' | 'revealed' | 'error'>('spinning');
  const [pick, setPick] = useState<SurpriseResponseDto | null>(null);
  const [shuffleIndex, setShuffleIndex] = useState(0);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const recentIdsRef = useRef<number[]>([]);
  const controllerRef = useRef<AbortController | null>(null);
  const timersRef = useRef<number[]>([]);
  const filtersRef = useRef(filters);
  filtersRef.current = filters;

  const pool = posterPool.length >= 3 ? posterPool : FALLBACK_MOVIES;

  const clearTimers = () => {
    timersRef.current.forEach((t) => window.clearTimeout(t));
    timersRef.current = [];
  };

  const spin = useCallback(async () => {
    controllerRef.current?.abort();
    clearTimers();
    const controller = new AbortController();
    controllerRef.current = controller;
    setPhase('spinning');
    setError(null);

    // Decelerating shuffle: each tick waits a bit longer than the previous one.
    const reduced = prefersReducedMotion();
    const duration = reduced ? 300 : SPIN_MS;
    if (!reduced) {
      let elapsed = 0;
      let delay = 60;
      while (elapsed + delay < duration) {
        elapsed += delay;
        timersRef.current.push(window.setTimeout(() => setShuffleIndex((i) => i + 1), elapsed));
        delay = Math.round(delay * 1.13);
      }
    }

    try {
      const [result] = await Promise.all([
        api.getSurprise(filtersRef.current, recentIdsRef.current, controller.signal),
        new Promise((resolve) => timersRef.current.push(window.setTimeout(resolve, duration)))
      ]);
      if (controller.signal.aborted) return;
      recentIdsRef.current = [result.movie.tmdbId, ...recentIdsRef.current.filter((id) => id !== result.movie.tmdbId)].slice(
        0,
        MAX_EXCLUDED
      );
      setPick(result);
      setPhase('revealed');
    } catch (err) {
      if (isAbortError(err)) return;
      setError(err instanceof Error ? err.message : 'Não foi possível sortear um filme.');
      setPhase('error');
    }
  }, []);

  useEffect(() => {
    if (open) spin();
    return () => {
      controllerRef.current?.abort();
      clearTimers();
      try {
        window.speechSynthesis?.cancel();
      } catch {
        // ignore
      }
    };
  }, [open, spin]);

  const speak = () => {
    if (!pick) return;
    try {
      const synth = window.speechSynthesis;
      if (!synth) return;
      synth.cancel();
      const utter = new SpeechSynthesisUtterance(pick.spokenText || pick.reason);
      utter.lang = 'pt-BR';
      synth.speak(utter);
    } catch {
      // ignore
    }
  };

  const canSpeak = typeof window !== 'undefined' && 'speechSynthesis' in window;
  const shuffleMovie = pool[shuffleIndex % pool.length];

  return (
    <Modal open={open} onClose={onClose} labelledBy="surprise-title" panelClassName="w-full max-w-lg p-5 sm:p-7">
      <div className="space-y-5">
        <div className="flex items-center gap-2 pr-10">
          <Dices className="w-5 h-5 text-fuchsia-400" aria-hidden="true" />
          <h2 id="surprise-title" className="font-display font-bold text-xl text-white">
            Me surpreenda
          </h2>
        </div>

        <div aria-live="polite" className="sr-only">
          {phase === 'spinning' ? 'Sorteando um filme...' : pick ? `Sorteado: ${pick.movie.title}. ${pick.reason}` : ''}
        </div>

        {phase === 'error' ? (
          <div role="alert" className="p-4 rounded-2xl bg-red-950/30 border border-red-500/30 text-sm text-red-200">
            {error}
          </div>
        ) : (
          <div className="flex flex-col sm:flex-row gap-5 items-center sm:items-start">
            <div className="relative w-40 sm:w-44 shrink-0 aspect-[2/3] rounded-2xl overflow-hidden border border-slate-700 shadow-2xl shadow-fuchsia-950/40">
              {phase === 'spinning' ? (
                <>
                  <PosterImage
                    key={shuffleMovie.tmdbId}
                    src={shuffleMovie.posterUrl}
                    alt=""
                    eager
                    className="w-full h-full object-cover blur-[1px] scale-105"
                  />
                  <div className="absolute inset-0 bg-gradient-to-t from-black/70 to-transparent flex items-end justify-center pb-3">
                    <Loader2 className="w-6 h-6 text-white animate-spin" aria-hidden="true" />
                  </div>
                </>
              ) : (
                pick && (
                  <div className="w-full h-full animate-reveal">
                    <PosterImage
                      src={pick.movie.posterUrl}
                      alt={`Pôster de ${pick.movie.title}`}
                      fallbackTitle={pick.movie.title}
                      eager
                      className="w-full h-full object-cover"
                    />
                  </div>
                )
              )}
            </div>

            <div className="flex-1 min-w-0 space-y-3 text-center sm:text-left">
              {phase === 'spinning' || !pick ? (
                <>
                  <p className="text-sm font-semibold text-slate-300">Embaralhando o catálogo…</p>
                  <div className="h-5 w-3/4 rounded skeleton mx-auto sm:mx-0" />
                  <div className="h-3 w-full rounded skeleton" />
                  <div className="h-3 w-5/6 rounded skeleton" />
                </>
              ) : (
                <div className="space-y-3 animate-slide-up">
                  <div>
                    <h3 className="font-display text-2xl font-extrabold text-white leading-tight">{pick.movie.title}</h3>
                    <p className="text-xs text-slate-400 mt-1 flex flex-wrap items-center justify-center sm:justify-start gap-x-2">
                      {pick.movie.releaseYear && <span>{pick.movie.releaseYear}</span>}
                      {pick.movie.runtimeMinutes && <span>• {pick.movie.runtimeMinutes} min</span>}
                      <span className="inline-flex items-center gap-0.5 text-amber-400 font-bold">
                        • <Star className="w-3 h-3 fill-amber-400" aria-hidden="true" />
                        {pick.movie.voteAverage.toFixed(1)}
                      </span>
                    </p>
                  </div>
                  {pick.movie.tagline && <p className="text-sm italic text-violet-300">&ldquo;{pick.movie.tagline}&rdquo;</p>}
                  <p className="text-sm text-slate-200 leading-relaxed p-3 rounded-xl bg-fuchsia-950/30 border border-fuchsia-500/20">
                    {pick.reason}
                  </p>
                  {canSpeak && pick.spokenText && (
                    <button
                      type="button"
                      onClick={speak}
                      className="inline-flex items-center gap-1.5 text-xs font-semibold text-slate-400 hover:text-white"
                    >
                      <Volume2 className="w-3.5 h-3.5" aria-hidden="true" />
                      Ouvir sugestão
                    </button>
                  )}
                </div>
              )}
            </div>
          </div>
        )}

        <div className="grid grid-cols-1 sm:grid-cols-3 gap-2 pt-4 border-t border-slate-800">
          <button
            type="button"
            onClick={spin}
            disabled={phase === 'spinning'}
            data-autofocus
            className="inline-flex items-center justify-center gap-1.5 px-4 py-2.5 rounded-xl bg-slate-800 hover:bg-slate-700 disabled:opacity-60 text-sm font-semibold text-white"
          >
            <Dices className="w-4 h-4" aria-hidden="true" />
            Outro
          </button>
          <button
            type="button"
            disabled={!pick || phase !== 'revealed'}
            onClick={() => pick && onOpenDetails(pick.movie)}
            className="inline-flex items-center justify-center gap-1.5 px-4 py-2.5 rounded-xl bg-slate-800 hover:bg-slate-700 disabled:opacity-60 text-sm font-semibold text-white"
          >
            <Info className="w-4 h-4" aria-hidden="true" />
            Ver detalhes
          </button>
          <button
            type="button"
            disabled={!pick || phase !== 'revealed' || saving}
            onClick={async () => {
              if (!pick) return;
              setSaving(true);
              try {
                await onWantToWatch(pick.movie);
              } finally {
                setSaving(false);
              }
            }}
            className="inline-flex items-center justify-center gap-1.5 px-4 py-2.5 rounded-xl bg-violet-600 hover:bg-violet-500 disabled:opacity-60 text-sm font-bold text-white"
          >
            {saving ? <Loader2 className="w-4 h-4 animate-spin" aria-hidden="true" /> : <Bookmark className="w-4 h-4" aria-hidden="true" />}
            Quero ver
          </button>
        </div>
      </div>
    </Modal>
  );
};
