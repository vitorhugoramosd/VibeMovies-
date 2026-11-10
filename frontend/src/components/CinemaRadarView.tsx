import React, { useEffect, useState } from 'react';
import {
  Clock,
  ExternalLink,
  MapPin,
  Radar,
  RefreshCw,
  Sparkles,
  Star,
  Ticket
} from 'lucide-react';
import { api } from '../services/api';
import { CinemaRadarResponseDto, MovieCardDto } from '../types/api';

interface CinemaRadarViewProps {
  defaultCity?: string;
  onSelectMovie: (movie: MovieCardDto) => void;
}

export const CinemaRadarView: React.FC<CinemaRadarViewProps> = ({
  defaultCity = 'Joinville/SC',
  onSelectMovie
}) => {
  const [cities, setCities] = useState<string[]>([
    'Joinville/SC',
    'Florianópolis/SC',
    'Curitiba/PR',
    'São Paulo/SP',
    'Porto Alegre/RS',
    'Rio de Janeiro/RJ'
  ]);
  const [selectedCity, setSelectedCity] = useState(defaultCity);
  const [customCityInput, setCustomCityInput] = useState('');
  const [radarData, setRadarData] = useState<CinemaRadarResponseDto | null>(null);
  const [loading, setLoading] = useState(true);

  const loadRadar = async (cityToLoad: string) => {
    setLoading(true);
    try {
      const data = await api.getCinemaRadar(cityToLoad);
      setRadarData(data);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    api.getCinemaCities().then(setCities).catch(() => {});
    loadRadar(selectedCity);
  }, []);

  const handleCitySelect = (city: string) => {
    setSelectedCity(city);
    loadRadar(city);
  };

  const handleCustomCitySubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!customCityInput.trim()) return;
    setSelectedCity(customCityInput.trim());
    loadRadar(customCityInput.trim());
    setCustomCityInput('');
  };

  return (
    <div className="space-y-8">
      {/* Header Banner */}
      <div className="p-6 sm:p-8 rounded-3xl bg-gradient-to-br from-violet-950/50 via-[#12141F] to-[#12141F] border border-violet-500/30 shadow-xl">
        <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-6">
          <div className="space-y-2 max-w-2xl">
            <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full text-xs font-bold bg-violet-500/20 text-violet-300 border border-violet-500/30">
              <Radar className="w-3.5 h-3.5 animate-pulse" />
              Radar Em Cartaz • TMDB Now Playing + HtmlAgilityPack Scraper
            </div>
            <h1 className="font-display text-2xl sm:text-4xl font-extrabold text-white">
              Radar de Cinemas Locais
            </h1>
            <p className="text-sm text-slate-300">
              Cruza os filmes marcados como <span className="text-violet-300 font-semibold">Now Playing (Em Cartaz)</span> no TMDB com a grade real de salas e horários extraída via <code className="text-xs bg-slate-900 px-1.5 py-0.5 rounded text-emerald-400">HtmlAgilityPack</code> para redes locais como GNC Cinemas, Cinemark e Cinépolis.
            </p>
          </div>

          {/* City Selector */}
          <div className="flex flex-col gap-3 min-w-[280px]">
            <label className="text-xs font-bold uppercase tracking-wider text-slate-400 flex items-center gap-1.5">
              <MapPin className="w-3.5 h-3.5 text-violet-400" />
              Cidade Selecionada: <span className="text-white">{selectedCity}</span>
            </label>
            <div className="flex flex-wrap gap-1.5">
              {cities.map((c) => (
                <button
                  key={c}
                  type="button"
                  onClick={() => handleCitySelect(c)}
                  className={`px-3 py-1.5 rounded-xl text-xs font-semibold transition-all ${
                    selectedCity.toLowerCase() === c.toLowerCase()
                      ? 'bg-violet-600 text-white shadow-lg shadow-violet-900/40'
                      : 'bg-slate-800/90 text-slate-300 hover:bg-slate-700 border border-slate-700'
                  }`}
                >
                  {c}
                </button>
              ))}
            </div>
            <form onSubmit={handleCustomCitySubmit} className="flex gap-2">
              <input
                type="text"
                value={customCityInput}
                onChange={(e) => setCustomCityInput(e.target.value)}
                placeholder="Outra cidade (ex: Blumenau/SC)..."
                className="flex-1 rounded-xl bg-slate-900/90 border border-slate-700 px-3 py-1.5 text-xs text-white placeholder-slate-500 focus:outline-none focus:border-violet-500"
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
            <span className="flex items-center gap-1.5">
              <Sparkles className="w-3.5 h-3.5 text-emerald-400" />
              Fonte do Scraper: <strong className="text-slate-200">{radarData.scraperSource}</strong>
            </span>
            <button
              type="button"
              onClick={() => loadRadar(selectedCity)}
              className="inline-flex items-center gap-1.5 text-violet-400 hover:text-violet-300 font-medium"
            >
              <RefreshCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin' : ''}`} />
              Atualizar Horários
            </button>
          </div>
        )}
      </div>

      {/* Movies & Showtimes List */}
      {loading ? (
        <div className="grid grid-cols-1 gap-6">
          {[1, 2, 3].map((n) => (
            <div
              key={n}
              className="h-56 rounded-2xl bg-slate-900/60 border border-slate-800 animate-pulse"
            />
          ))}
        </div>
      ) : (
        <div className="space-y-6">
          {radarData?.moviesInTheaters.map((entry) => (
            <div
              key={entry.movie.tmdbId}
              className="p-5 sm:p-6 rounded-2xl bg-[#12141F] border border-slate-800/90 hover:border-violet-500/40 transition-all grid grid-cols-1 lg:grid-cols-12 gap-6"
            >
              {/* Movie Info Column */}
              <div className="lg:col-span-4 flex gap-4">
                <img
                  src={entry.movie.posterUrl}
                  alt={entry.movie.title}
                  onClick={() => onSelectMovie(entry.movie)}
                  className="w-28 sm:w-32 h-44 object-cover rounded-xl cursor-pointer hover:scale-105 transition-transform shrink-0 shadow-lg"
                />
                <div className="space-y-2">
                  <div className="flex flex-wrap items-center gap-1.5">
                    <span className="px-2 py-0.5 rounded text-[11px] font-bold bg-amber-500/20 text-amber-300 border border-amber-500/30">
                      {entry.ageRating}
                    </span>
                    <span className="inline-flex items-center gap-1 text-xs font-bold text-amber-400">
                      <Star className="w-3.5 h-3.5 fill-amber-400" />
                      {entry.movie.voteAverage.toFixed(1)}
                    </span>
                  </div>

                  <h3
                    onClick={() => onSelectMovie(entry.movie)}
                    className="font-display font-bold text-lg text-white hover:text-violet-300 cursor-pointer"
                  >
                    {entry.movie.title}
                  </h3>

                  <p className="text-xs text-slate-400 line-clamp-3">{entry.movie.overview}</p>

                  {entry.movie.runtimeMinutes && (
                    <span className="inline-flex items-center gap-1 text-xs text-slate-400">
                      <Clock className="w-3 h-3 text-violet-400" />
                      {entry.movie.runtimeMinutes} min
                    </span>
                  )}
                </div>
              </div>

              {/* Scraped Venues & Showtimes Column */}
              <div className="lg:col-span-8 grid grid-cols-1 md:grid-cols-2 gap-4">
                {entry.venues.map((venue, vIdx) => (
                  <div
                    key={vIdx}
                    className="p-4 rounded-xl bg-slate-900/90 border border-slate-800 flex flex-col justify-between gap-3"
                  >
                    <div>
                      <div className="flex items-center justify-between gap-2 mb-1">
                        <span className="text-[11px] font-bold uppercase tracking-wider text-violet-400">
                          {venue.cinemaChain}
                        </span>
                        <Ticket className="w-4 h-4 text-emerald-400" />
                      </div>
                      <h4 className="font-bold text-sm text-white">{venue.cinemaName}</h4>
                      <p className="text-[11px] text-slate-400 mt-0.5">{venue.address}</p>
                    </div>

                    <div className="space-y-2 pt-2 border-t border-slate-800/80">
                      <span className="text-[11px] font-semibold text-slate-300 block">
                        Horários Hoje ({radarData.city}):
                      </span>
                      <div className="flex flex-wrap gap-2">
                        {venue.sessions.map((s, sIdx) => (
                          <a
                            key={sIdx}
                            href={s.ticketUrl}
                            target="_blank"
                            rel="noreferrer"
                            className="group/btn flex flex-col px-2.5 py-1.5 rounded-lg bg-[#161929] hover:bg-violet-600/20 border border-slate-700 hover:border-violet-500 transition-all"
                          >
                            <div className="flex items-center gap-1.5">
                              <span className="text-xs font-extrabold text-white group-hover/btn:text-violet-300">
                                {s.time}
                              </span>
                              <span
                                className={`text-[10px] px-1.5 py-0.2 rounded font-semibold ${
                                  s.audioType === 'Legendado'
                                    ? 'bg-violet-500/20 text-violet-300'
                                    : 'bg-sky-500/20 text-sky-300'
                                }`}
                              >
                                {s.audioType === 'Legendado' ? 'LEG' : 'DUB'}
                              </span>
                              <ExternalLink className="w-2.5 h-2.5 text-slate-500 group-hover/btn:text-violet-300" />
                            </div>
                            <span className="text-[10px] text-slate-400">{s.roomType}</span>
                          </a>
                        ))}
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};
