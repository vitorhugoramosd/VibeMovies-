import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  AlertTriangle,
  CalendarDays,
  Clock,
  ExternalLink,
  MapPin,
  Radar,
  RefreshCw,
  Sparkles,
  Star,
  Ticket
} from 'lucide-react';
import { api, isAbortError } from '../services/api';
import { CinemaRadarResponseDto, CinemaSessionDto, MovieCardDto } from '../types/api';
import { DEFAULT_CITIES } from '../lib/constants';
import { PosterImage } from './PosterImage';
import { StateMessage } from './StateMessage';

interface CinemaRadarViewProps {
  defaultCity?: string;
  onSelectMovie: (movie: MovieCardDto) => void;
}

type AudioFilter = 'all' | 'Dublado' | 'Legendado';

/** Groups free-form room descriptions ("IMAX 2D", "Sala 4 • 2D", "VIP / 2D") into filterable types. */
function roomCategory(roomType: string): string {
  const r = roomType.toUpperCase();
  if (r.includes('IMAX')) return 'IMAX';
  if (r.includes('XD') || r.includes('MACRO') || r.includes('PRIME')) return 'XD / Premium';
  if (r.includes('VIP')) return 'VIP';
  if (r.includes('4D') || r.includes('D-BOX')) return '4D';
  if (r.includes('3D')) return '3D';
  return '2D Padrão';
}

function audioCategory(audio: string): 'Dublado' | 'Legendado' {
  return /leg/i.test(audio) ? 'Legendado' : 'Dublado';
}

function minutesOfDay(time: string): number | null {
  const m = /^(\d{1,2}):(\d{2})/.exec(time.trim());
  return m ? Number(m[1]) * 60 + Number(m[2]) : null;
}

function nowMinutes(): number {
  const d = new Date();
  return d.getHours() * 60 + d.getMinutes();
}

export const CinemaRadarView: React.FC<CinemaRadarViewProps> = ({ defaultCity = 'Joinville/SC', onSelectMovie }) => {
  const [cities, setCities] = useState<string[]>(DEFAULT_CITIES);
  const [selectedCity, setSelectedCity] = useState(defaultCity);
  const [customCityInput, setCustomCityInput] = useState('');
  const [radarData, setRadarData] = useState<CinemaRadarResponseDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [audio, setAudio] = useState<AudioFilter>('all');
  const [rooms, setRooms] = useState<string[]>([]);
  const [showPast, setShowPast] = useState(false);
  const [clock, setClock] = useState(nowMinutes);
  const controllerRef = useRef<AbortController | null>(null);

  const loadRadar = useCallback(async (cityToLoad: string) => {
    controllerRef.current?.abort();
    const controller = new AbortController();
    controllerRef.current = controller;
    setLoading(true);
    setError(null);
    try {
      const data = await api.getCinemaRadar(cityToLoad, controller.signal);
      if (!controller.signal.aborted) setRadarData(data);
    } catch (err) {
      if (!isAbortError(err)) setError(err instanceof Error ? err.message : 'Falha ao carregar o radar.');
    } finally {
      if (!controller.signal.aborted) setLoading(false);
    }
  }, []);

  useEffect(() => {
    api.getCinemaCities().then((list) => list.length && setCities(list));
  }, []);

  useEffect(() => {
    setSelectedCity(defaultCity);
    loadRadar(defaultCity);
  }, [defaultCity, loadRadar]);

  useEffect(() => () => controllerRef.current?.abort(), []);

  // Keep "past session" detection fresh while the tab stays open.
  useEffect(() => {
    const t = window.setInterval(() => setClock(nowMinutes()), 60_000);
    return () => window.clearInterval(t);
  }, []);

  const handleCitySelect = (city: string) => {
    setSelectedCity(city);
    loadRadar(city);
  };

  const handleCustomCitySubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const city = customCityInput.trim();
    if (!city) return;
    setSelectedCity(city);
    loadRadar(city);
    setCustomCityInput('');
  };

  const availableRooms = useMemo(() => {
    const set = new Set<string>();
    radarData?.moviesInTheaters.forEach((e) =>
      e.venues.forEach((v) => v.sessions.forEach((s) => set.add(roomCategory(s.roomType))))
    );
    return [...set].sort();
  }, [radarData]);

  const isPast = useCallback(
    (s: CinemaSessionDto) => {
      const m = minutesOfDay(s.time);
      return m !== null && m < clock;
    },
    [clock]
  );

  const matchesFilters = useCallback(
    (s: CinemaSessionDto) =>
      (audio === 'all' || audioCategory(s.audioType) === audio) &&
      (rooms.length === 0 || rooms.includes(roomCategory(s.roomType))),
    [audio, rooms]
  );

  const filteredMovies = useMemo(() => {
    if (!radarData) return [];
    return radarData.moviesInTheaters
      .map((entry) => {
        const venues = entry.venues
          .map((v) => {
            const sessions = v.sessions
              .filter(matchesFilters)
              .filter((s) => showPast || !isPast(s))
              .sort((a, b) => (minutesOfDay(a.time) ?? 0) - (minutesOfDay(b.time) ?? 0));
            return { ...v, sessions };
          })
          .filter((v) => v.sessions.length > 0);
        const upcoming = venues
          .flatMap((v) => v.sessions)
          .filter((s) => !isPast(s))
          .map((s) => minutesOfDay(s.time) ?? Infinity);
        return { ...entry, venues, nextSession: upcoming.length ? Math.min(...upcoming) : null };
      })
      .filter((e) => e.venues.length > 0);
  }, [radarData, matchesFilters, isPast, showPast]);

  const hiddenPastCount = useMemo(() => {
    if (!radarData || showPast) return 0;
    return radarData.moviesInTheaters.reduce(
      (acc, e) => acc + e.venues.reduce((a, v) => a + v.sessions.filter((s) => matchesFilters(s) && isPast(s)).length, 0),
      0
    );
  }, [radarData, matchesFilters, isPast, showPast]);

  const filtersActive = audio !== 'all' || rooms.length > 0;
  const todayLabel = new Date().toLocaleDateString('pt-BR', { weekday: 'long', day: '2-digit', month: 'long' });

  const chip = (active: boolean) =>
    `px-3 py-1.5 rounded-xl text-xs font-semibold transition-all ${
      active
        ? 'bg-violet-600 text-white shadow-lg shadow-violet-900/40'
        : 'bg-slate-800/90 text-slate-300 hover:bg-slate-700 border border-slate-700'
    }`;

  return (
    <div className="space-y-6 sm:space-y-8">
      {/* Header Banner */}
      <div className="p-5 sm:p-8 rounded-3xl bg-gradient-to-br from-violet-950/50 via-[#12141F] to-[#12141F] border border-violet-500/30 shadow-xl">
        <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-6">
          <div className="space-y-2 max-w-2xl min-w-0">
            <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full text-[11px] sm:text-xs font-bold bg-violet-500/20 text-violet-300 border border-violet-500/30">
              <Radar className="w-3.5 h-3.5 animate-pulse shrink-0" aria-hidden="true" />
              Radar Em Cartaz • TMDB Now Playing + Scraper
            </div>
            <h1 className="font-display text-2xl sm:text-4xl font-extrabold text-white">Radar de Cinemas Locais</h1>
            <p className="text-sm text-slate-300">
              Cruza os filmes marcados como <span className="text-violet-300 font-semibold">Now Playing (Em Cartaz)</span> no
              TMDB com a grade de salas e horários extraída via{' '}
              <code className="text-xs bg-slate-900 px-1.5 py-0.5 rounded text-emerald-400">HtmlAgilityPack</code> para redes
              locais como GNC Cinemas, Cinemark e Cinépolis.
            </p>
          </div>

          <div className="flex flex-col gap-3 w-full lg:w-auto lg:min-w-[280px] lg:max-w-sm">
            <p className="text-xs font-bold uppercase tracking-wider text-slate-400 flex items-center gap-1.5">
              <MapPin className="w-3.5 h-3.5 text-violet-400" aria-hidden="true" />
              Cidade: <span className="text-white normal-case">{selectedCity}</span>
            </p>
            <div className="flex flex-wrap gap-1.5" role="group" aria-label="Cidades">
              {cities.map((c) => (
                <button
                  key={c}
                  type="button"
                  onClick={() => handleCitySelect(c)}
                  aria-pressed={selectedCity.toLowerCase() === c.toLowerCase()}
                  className={chip(selectedCity.toLowerCase() === c.toLowerCase())}
                >
                  {c}
                </button>
              ))}
            </div>
            <form onSubmit={handleCustomCitySubmit} className="flex gap-2">
              <label htmlFor="radar-city" className="sr-only">
                Outra cidade
              </label>
              <input
                id="radar-city"
                type="text"
                value={customCityInput}
                onChange={(e) => setCustomCityInput(e.target.value)}
                placeholder="Outra cidade (ex: Blumenau/SC)..."
                className="flex-1 min-w-0 rounded-xl bg-slate-900/90 border border-slate-700 px-3 py-1.5 text-xs text-white placeholder-slate-500 focus:outline-none focus:border-violet-500"
              />
              <button
                type="submit"
                className="px-3 py-1.5 rounded-xl bg-slate-800 hover:bg-violet-600 text-xs font-semibold text-white transition-colors"
              >
                Buscar
              </button>
            </form>
          </div>
        </div>

        {radarData && (
          <div className="mt-5 pt-4 border-t border-slate-800/80 flex flex-wrap items-center justify-between gap-2 text-xs text-slate-400">
            <span className="flex items-center gap-1.5 min-w-0">
              <Sparkles className="w-3.5 h-3.5 text-emerald-400 shrink-0" aria-hidden="true" />
              <span className="min-w-0 break-words">
                Fonte: <strong className="text-slate-200">{radarData.scraperSource}</strong>
              </span>
            </span>
            <button
              type="button"
              onClick={() => loadRadar(selectedCity)}
              className="inline-flex items-center gap-1.5 text-violet-400 hover:text-violet-300 font-medium"
            >
              <RefreshCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin' : ''}`} aria-hidden="true" />
              Atualizar Horários
            </button>
          </div>
        )}
      </div>

      {radarData?.isSimulatedSchedule && (
        <div
          role="note"
          className="flex items-start gap-3 p-4 rounded-2xl bg-amber-500/10 border border-amber-500/30 text-amber-200"
        >
          <AlertTriangle className="w-5 h-5 shrink-0 text-amber-400 mt-0.5" aria-hidden="true" />
          <div className="text-xs sm:text-sm">
            <strong className="block font-bold text-amber-100">Horários ilustrativos — confira no site do cinema</strong>
            {radarData.notice && <span className="text-amber-200/80">{radarData.notice}</span>}
          </div>
        </div>
      )}

      {/* Session filters */}
      {radarData && (
        <div className="p-4 rounded-2xl bg-[#12141F] border border-slate-800 space-y-3">
          <div className="flex flex-wrap items-center gap-2">
            <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-lg bg-emerald-500/15 text-emerald-300 border border-emerald-500/30 text-xs font-bold">
              <CalendarDays className="w-3.5 h-3.5" aria-hidden="true" />
              Hoje
            </span>
            <span className="text-xs text-slate-400 capitalize">{todayLabel}</span>
          </div>
          <div className="flex flex-wrap items-center gap-1.5" role="group" aria-label="Áudio">
            <span className="text-xs font-semibold text-slate-400 mr-1">Áudio:</span>
            {(['all', 'Dublado', 'Legendado'] as AudioFilter[]).map((a) => (
              <button key={a} type="button" onClick={() => setAudio(a)} aria-pressed={audio === a} className={chip(audio === a)}>
                {a === 'all' ? 'Todos' : a}
              </button>
            ))}
          </div>
          {availableRooms.length > 1 && (
            <div className="flex flex-wrap items-center gap-1.5" role="group" aria-label="Tipo de sala">
              <span className="text-xs font-semibold text-slate-400 mr-1">Sala:</span>
              <button type="button" onClick={() => setRooms([])} aria-pressed={rooms.length === 0} className={chip(rooms.length === 0)}>
                Todas
              </button>
              {availableRooms.map((r) => {
                const active = rooms.includes(r);
                return (
                  <button
                    key={r}
                    type="button"
                    aria-pressed={active}
                    onClick={() => setRooms((prev) => (active ? prev.filter((x) => x !== r) : [...prev, r]))}
                    className={chip(active)}
                  >
                    {r}
                  </button>
                );
              })}
            </div>
          )}
          <label className="inline-flex items-center gap-2 text-xs text-slate-300 cursor-pointer">
            <input
              type="checkbox"
              checked={showPast}
              onChange={(e) => setShowPast(e.target.checked)}
              className="w-4 h-4 rounded accent-violet-600"
            />
            Mostrar sessões que já começaram
            {hiddenPastCount > 0 && <span className="text-slate-500">({hiddenPastCount} ocultas)</span>}
          </label>
        </div>
      )}

      {/* Movies & Showtimes List */}
      {loading && !radarData ? (
        <div className="grid grid-cols-1 gap-6" aria-busy="true" aria-label="Carregando programação">
          {[1, 2, 3].map((n) => (
            <div key={n} className="h-56 rounded-2xl skeleton" />
          ))}
        </div>
      ) : error && !radarData ? (
        <StateMessage
          tone="error"
          title="Não foi possível carregar a programação"
          description={error}
          actionLabel="Tentar novamente"
          onAction={() => loadRadar(selectedCity)}
        />
      ) : filteredMovies.length === 0 ? (
        <StateMessage
          icon={<Ticket className="w-10 h-10 text-slate-600" aria-hidden="true" />}
          title={filtersActive ? 'Nenhuma sessão com esses filtros' : 'Sem mais sessões hoje'}
          description={
            filtersActive
              ? 'Tente outro tipo de áudio ou sala.'
              : 'Todas as sessões de hoje já começaram. Marque "Mostrar sessões que já começaram" para ver a grade completa.'
          }
          actionLabel={filtersActive ? 'Limpar filtros' : 'Mostrar grade completa'}
          onAction={() => {
            if (filtersActive) {
              setAudio('all');
              setRooms([]);
            } else setShowPast(true);
          }}
        />
      ) : (
        <div className={`space-y-6 transition-opacity ${loading ? 'opacity-50' : ''}`}>
          {filteredMovies.map((entry) => (
            <article
              key={entry.movie.tmdbId}
              className="p-4 sm:p-6 rounded-2xl bg-[#12141F] border border-slate-800/90 hover:border-violet-500/40 transition-all grid grid-cols-1 lg:grid-cols-12 gap-5 sm:gap-6"
            >
              <div className="lg:col-span-4 flex gap-4 min-w-0">
                <button
                  type="button"
                  onClick={() => onSelectMovie(entry.movie)}
                  aria-label={`Ver detalhes de ${entry.movie.title}`}
                  className="shrink-0 rounded-xl overflow-hidden self-start"
                >
                  <PosterImage
                    src={entry.movie.posterUrl}
                    alt=""
                    fallbackTitle={entry.movie.title}
                    className="w-24 sm:w-32 h-36 sm:h-44 object-cover hover:scale-105 transition-transform shadow-lg"
                  />
                </button>
                <div className="space-y-2 min-w-0">
                  <div className="flex flex-wrap items-center gap-1.5">
                    <span className="px-2 py-0.5 rounded text-[11px] font-bold bg-amber-500/20 text-amber-300 border border-amber-500/30">
                      {entry.ageRating}
                    </span>
                    <span className="inline-flex items-center gap-1 text-xs font-bold text-amber-400">
                      <Star className="w-3.5 h-3.5 fill-amber-400" aria-hidden="true" />
                      {entry.movie.voteAverage.toFixed(1)}
                    </span>
                  </div>
                  <h3>
                    <button
                      type="button"
                      onClick={() => onSelectMovie(entry.movie)}
                      className="font-display font-bold text-lg text-white hover:text-violet-300 text-left"
                    >
                      {entry.movie.title}
                    </button>
                  </h3>
                  <p className="text-xs text-slate-400 line-clamp-3">{entry.movie.overview}</p>
                  <div className="flex flex-wrap gap-2 items-center">
                    {entry.movie.runtimeMinutes && (
                      <span className="inline-flex items-center gap-1 text-xs text-slate-400">
                        <Clock className="w-3 h-3 text-violet-400" aria-hidden="true" />
                        {entry.movie.runtimeMinutes} min
                      </span>
                    )}
                    {entry.nextSession !== null && (
                      <span className="text-[11px] font-semibold px-2 py-0.5 rounded-md bg-emerald-500/15 text-emerald-300 border border-emerald-500/30">
                        Próxima hoje: {String(Math.floor(entry.nextSession / 60)).padStart(2, '0')}:
                        {String(entry.nextSession % 60).padStart(2, '0')}
                      </span>
                    )}
                  </div>
                </div>
              </div>

              <div className="lg:col-span-8 grid grid-cols-1 md:grid-cols-2 gap-4 min-w-0">
                {entry.venues.map((venue) => (
                  <div
                    key={`${venue.cinemaChain}-${venue.cinemaName}`}
                    className="p-4 rounded-xl bg-slate-900/90 border border-slate-800 flex flex-col justify-between gap-3 min-w-0"
                  >
                    <div className="min-w-0">
                      <div className="flex items-center justify-between gap-2 mb-1">
                        <span className="text-[11px] font-bold uppercase tracking-wider text-violet-400">{venue.cinemaChain}</span>
                        <Ticket className="w-4 h-4 text-emerald-400" aria-hidden="true" />
                      </div>
                      <h4 className="font-bold text-sm text-white">{venue.cinemaName}</h4>
                      <p className="text-[11px] text-slate-400 mt-0.5 break-words">{venue.address}</p>
                    </div>

                    <div className="space-y-2 pt-2 border-t border-slate-800/80">
                      <span className="text-[11px] font-semibold text-slate-300 block">Horários de hoje:</span>
                      <ul className="flex flex-wrap gap-2">
                        {venue.sessions.map((s) => {
                          const past = isPast(s);
                          const isNext = !past && minutesOfDay(s.time) === entry.nextSession;
                          const leg = audioCategory(s.audioType) === 'Legendado';
                          return (
                            <li key={`${s.time}-${s.roomType}-${s.audioType}`}>
                              <a
                                href={s.ticketUrl}
                                target="_blank"
                                rel="noreferrer"
                                aria-label={`${s.time}, ${s.audioType}, ${s.roomType}${past ? ', já começou' : ''} — comprar ingresso`}
                                className={`group/btn flex flex-col px-2.5 py-1.5 rounded-lg border transition-all ${
                                  past
                                    ? 'bg-slate-900 border-slate-800 opacity-50'
                                    : isNext
                                      ? 'bg-emerald-600/15 border-emerald-500/60 hover:bg-emerald-600/25'
                                      : 'bg-[#161929] border-slate-700 hover:bg-violet-600/20 hover:border-violet-500'
                                }`}
                              >
                                <span className="flex items-center gap-1.5">
                                  <span
                                    className={`text-xs font-extrabold ${
                                      past ? 'text-slate-500 line-through' : 'text-white group-hover/btn:text-violet-300'
                                    }`}
                                  >
                                    {s.time}
                                  </span>
                                  <span
                                    className={`text-[10px] px-1.5 rounded font-semibold ${
                                      leg ? 'bg-violet-500/20 text-violet-300' : 'bg-sky-500/20 text-sky-300'
                                    }`}
                                  >
                                    {leg ? 'LEG' : 'DUB'}
                                  </span>
                                  <ExternalLink className="w-2.5 h-2.5 text-slate-500 group-hover/btn:text-violet-300" aria-hidden="true" />
                                </span>
                                <span className="text-[10px] text-slate-400">
                                  {isNext ? 'Próxima • ' : ''}
                                  {s.roomType}
                                </span>
                              </a>
                            </li>
                          );
                        })}
                      </ul>
                    </div>
                  </div>
                ))}
              </div>
            </article>
          ))}
        </div>
      )}
    </div>
  );
};
