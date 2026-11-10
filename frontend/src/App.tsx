import React, { useEffect, useState } from 'react';
import {
  Bookmark,
  Clock,
  Code2,
  Compass,
  Film,
  KeyRound,
  LogOut,
  Radar,
  RotateCcw,
  Search,
  Sparkles,
  Tv,
  X
} from 'lucide-react';
import {
  api,
  getStoredTmdbKey,
  getStoredUser,
  setStoredAuth,
  setStoredTmdbKey
} from './services/api';
import {
  AuthResponseDto,
  DiscoverResultDto,
  GenreDto,
  MovieCardDto,
  VibeDefinition,
  WatchlistItem,
  WatchlistStatsDto,
  WatchlistType
} from './types/api';
import { MovieCard } from './components/MovieCard';
import { MovieDetailsModal } from './components/MovieDetailsModal';
import { CinemaRadarView } from './components/CinemaRadarView';
import { WatchlistDiaryView } from './components/WatchlistDiaryView';

type MainView = 'discover' | 'radar' | 'diary';

const STREAMING_PROVIDERS = [
  'Netflix',
  'Amazon Prime Video',
  'Max',
  'Disney+',
  'Globoplay',
  'MUBI'
];

const RUNTIME_OPTIONS: { label: string; value: number | null }[] = [
  { label: 'Qualquer duração', value: null },
  { label: '<= 90 min', value: 90 },
  { label: '<= 120 min', value: 120 },
  { label: '<= 150 min', value: 150 }
];

export const App: React.FC = () => {
  const [activeView, setActiveView] = useState<MainView>('discover');

  // Catalog metadata
  const [vibes, setVibes] = useState<VibeDefinition[]>([]);
  const [genres, setGenres] = useState<GenreDto[]>([]);

  // Active filters
  const [selectedVibes, setSelectedVibes] = useState<string[]>(['explodir-cabeca']);
  const [selectedGenres, setSelectedGenres] = useState<number[]>([]);
  const [maxRuntime, setMaxRuntime] = useState<number | null>(null);
  const [selectedProvider, setSelectedProvider] = useState<string | null>(null);
  const [searchQuery, setSearchQuery] = useState<string>('');

  // Discover Result
  const [discoverResult, setDiscoverResult] = useState<DiscoverResultDto | null>(null);
  const [loadingMovies, setLoadingMovies] = useState<boolean>(true);
  const [selectedMovie, setSelectedMovie] = useState<MovieCardDto | null>(null);

  // Auth & Watchlist State
  const [user, setUser] = useState<AuthResponseDto | null>(() => getStoredUser());
  const [watchlist, setWatchlist] = useState<WatchlistItem[]>([]);
  const [watchlistStats, setWatchlistStats] = useState<WatchlistStatsDto | null>(null);

  // TMDB API Key Modal
  const [showKeyModal, setShowKeyModal] = useState<boolean>(false);
  const [tmdbKeyInput, setTmdbKeyInput] = useState<string>(() => getStoredTmdbKey());

  // Toast notification
  const [toastMessage, setToastMessage] = useState<string | null>(null);

  const showToast = (msg: string) => {
    setToastMessage(msg);
    setTimeout(() => {
      setToastMessage((prev) => (prev === msg ? null : prev));
    }, 3200);
  };

  // Load initial vibes, genres, and auto-authenticate demo account if needed
  useEffect(() => {
    api.getVibes().then(setVibes);
    api.getGenres().then(setGenres);

    const initAuthAndWatchlist = async () => {
      let currentUser = getStoredUser();
      if (!currentUser) {
        try {
          currentUser = await api.login('demo@vibemovies.app', 'vibe1234');
          setStoredAuth(currentUser);
          setUser(currentUser);
        } catch {
          // Ignore if offline
        }
      }
      await refreshWatchlist();
    };

    initAuthAndWatchlist();
  }, []);

  const refreshWatchlist = async () => {
    try {
      const [items, stats] = await Promise.all([
        api.getWatchlist(),
        api.getWatchlistStats()
      ]);
      setWatchlist(items);
      setWatchlistStats(stats);
    } catch {
      // Handled by api fallback
    }
  };

  // Fetch movies whenever filters change
  useEffect(() => {
    let cancelled = false;
    setLoadingMovies(true);

    const timer = setTimeout(async () => {
      const res = await api.discoverMovies({
        vibes: selectedVibes,
        genres: selectedGenres,
        maxRuntime,
        provider: selectedProvider,
        search: searchQuery
      });
      if (!cancelled) {
        setDiscoverResult(res);
        setLoadingMovies(false);
      }
    }, 180);

    return () => {
      cancelled = true;
      clearTimeout(timer);
    };
  }, [selectedVibes, selectedGenres, maxRuntime, selectedProvider, searchQuery]);

  const toggleVibe = (id: string) => {
    setSelectedVibes((prev) =>
      prev.includes(id) ? prev.filter((v) => v !== id) : [...prev, id]
    );
  };

  const toggleGenre = (id: number) => {
    setSelectedGenres((prev) =>
      prev.includes(id) ? prev.filter((g) => g !== id) : [...prev, id]
    );
  };

  const clearAllFilters = () => {
    setSelectedVibes([]);
    setSelectedGenres([]);
    setMaxRuntime(null);
    setSelectedProvider(null);
    setSearchQuery('');
  };

  const handleSaveWatchlist = async (
    movie: MovieCardDto,
    type: WatchlistType,
    rating?: number,
    notes?: string
  ) => {
    await api.upsertWatchlist(movie, type, rating, notes);
    await refreshWatchlist();
    showToast(
      type === WatchlistType.Watched
        ? `"${movie.title}" registrado em Já Vi!`
        : `"${movie.title}" salvo na lista Quero Ver!`
    );
  };

  const handleLogin = async (email: string, password: string) => {
    const auth = await api.login(email, password);
    setStoredAuth(auth);
    setUser(auth);
    await refreshWatchlist();
    showToast(`Bem-vindo(a), ${auth.name}!`);
  };

  const handleRegister = async (
    name: string,
    email: string,
    password: string,
    city: string
  ) => {
    const auth = await api.register(name, email, password, city);
    setStoredAuth(auth);
    setUser(auth);
    await refreshWatchlist();
    showToast(`Conta criada para ${auth.name}!`);
  };

  const handleLogout = () => {
    setStoredAuth(null);
    setUser(null);
    showToast('Sessão encerrada.');
  };

  const handleSaveTmdbKey = (e: React.FormEvent) => {
    e.preventDefault();
    setStoredTmdbKey(tmdbKeyInput);
    setShowKeyModal(false);
    // Trigger re-fetch
    setSelectedVibes((prev) => [...prev]);
    showToast(
      tmdbKeyInput.trim()
        ? 'Chave da API TMDB configurada!'
        : 'Usando catálogo inteligente integrado.'
    );
  };

  const getWatchlistEntryForMovie = (tmdbId: number) =>
    watchlist.find((w) => w.tmdbMovieId === tmdbId);

  return (
    <div className="min-h-screen bg-[#090A0F] text-slate-100 flex flex-col">
      {/* Toast Notification */}
      {toastMessage && (
        <div className="fixed bottom-5 right-5 z-50 flex items-center gap-2.5 px-4 py-3 rounded-2xl bg-violet-600 text-white text-xs sm:text-sm font-semibold shadow-2xl shadow-violet-950/70 border border-violet-400/40 animate-bounce">
          <Sparkles className="w-4 h-4 shrink-0" />
          {toastMessage}
        </div>
      )}

      {/* Top Navigation Header */}
      <header className="sticky top-0 z-40 bg-[#090A0F]/85 backdrop-blur-xl border-b border-slate-800/80">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 h-20 flex items-center justify-between gap-4">
          {/* Brand Logo */}
          <div
            onClick={() => setActiveView('discover')}
            className="flex items-center gap-3 cursor-pointer group"
          >
            <div className="w-11 h-11 rounded-2xl bg-gradient-to-br from-violet-600 to-fuchsia-600 flex items-center justify-center shadow-lg shadow-violet-900/40 group-hover:scale-105 transition-transform">
              <Film className="w-6 h-6 text-white" />
            </div>
            <div>
              <div className="flex items-center gap-2">
                <span className="font-display font-extrabold text-xl tracking-tight text-white">
                  Vibe<span className="text-violet-400">Movies</span>
                </span>
                <span className="hidden sm:inline-block text-[10px] font-bold uppercase tracking-wider px-2 py-0.5 rounded-full bg-violet-500/15 text-violet-300 border border-violet-500/30">
                  C# .NET 8 + TMDB
                </span>
              </div>
              <p className="text-[11px] text-slate-400 hidden sm:block">
                Curadoria por Clima, Streaming BR & Radar de Cinemas
              </p>
            </div>
          </div>

          {/* Center Navigation Tabs */}
          <nav className="flex items-center gap-1.5 bg-[#12141F] p-1.5 rounded-2xl border border-slate-800/90">
            <button
              type="button"
              onClick={() => setActiveView('discover')}
              className={`flex items-center gap-2 px-3.5 py-2 rounded-xl text-xs sm:text-sm font-bold transition-all ${
                activeView === 'discover'
                  ? 'bg-violet-600 text-white shadow-lg shadow-violet-950/50'
                  : 'text-slate-400 hover:text-white'
              }`}
            >
              <Compass className="w-4 h-4" />
              <span>Explorar por Vibe</span>
            </button>

            <button
              type="button"
              onClick={() => setActiveView('radar')}
              className={`flex items-center gap-2 px-3.5 py-2 rounded-xl text-xs sm:text-sm font-bold transition-all ${
                activeView === 'radar'
                  ? 'bg-violet-600 text-white shadow-lg shadow-violet-950/50'
                  : 'text-slate-400 hover:text-white'
              }`}
            >
              <Radar className="w-4 h-4" />
              <span>Radar de Cinemas</span>
            </button>

            <button
              type="button"
              onClick={() => setActiveView('diary')}
              className={`flex items-center gap-2 px-3.5 py-2 rounded-xl text-xs sm:text-sm font-bold transition-all ${
                activeView === 'diary'
                  ? 'bg-violet-600 text-white shadow-lg shadow-violet-950/50'
                  : 'text-slate-400 hover:text-white'
              }`}
            >
              <Bookmark className="w-4 h-4" />
              <span>Diário de Bordo</span>
              {watchlist.length > 0 && (
                <span className="px-1.5 py-0.2 rounded-full text-[10px] bg-black/40 text-violet-200">
                  {watchlist.length}
                </span>
              )}
            </button>
          </nav>

          {/* Right Utilities: TMDB Key + Auth Status */}
          <div className="flex items-center gap-2.5">
            <button
              type="button"
              onClick={() => setShowKeyModal(true)}
              className="flex items-center gap-1.5 px-3 py-2 rounded-xl bg-[#12141F] hover:bg-slate-800 border border-slate-800 text-xs font-semibold text-slate-300 transition-colors"
              title="Configurar Chave da API TMDB"
            >
              <KeyRound className="w-3.5 h-3.5 text-amber-400" />
              <span className="hidden md:inline">
                {getStoredTmdbKey() ? 'TMDB Live Key Ativa' : 'Chave TMDB'}
              </span>
            </button>

            {user && (
              <button
                type="button"
                onClick={handleLogout}
                title={`Sair (${user.email})`}
                className="p-2 rounded-xl bg-[#12141F] hover:bg-red-500/10 border border-slate-800 hover:border-red-500/30 text-slate-400 hover:text-red-300 transition-colors"
              >
                <LogOut className="w-4 h-4" />
              </button>
            )}
          </div>
        </div>
      </header>

      {/* Main Content Container */}
      <main className="flex-1 max-w-7xl w-full mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {activeView === 'discover' && (
          <div className="space-y-8">
            {/* Hero Section */}
            <div className="relative overflow-hidden rounded-3xl bg-gradient-to-br from-[#171332] via-[#12141F] to-[#0D101A] border border-violet-500/25 p-6 sm:p-8 shadow-2xl">
              <div className="max-w-3xl space-y-3">
                <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full text-xs font-bold bg-violet-500/20 text-violet-300 border border-violet-500/30">
                  <Sparkles className="w-3.5 h-3.5" />
                  Descoberta Inteligente por Atmosfera & Tempo Disponível
                </div>
                <h1 className="font-display text-2xl sm:text-4xl font-extrabold text-white leading-tight">
                  Qual é a <span className="text-violet-400">vibe do filme</span> que você quer sentir hoje?
                </h1>
                <p className="text-xs sm:text-sm text-slate-300 leading-relaxed">
                  Combine uma ou mais <strong>Vibes</strong> (convertidas em fórmulas de{' '}
                  <code className="text-violet-300 bg-black/40 px-1.5 py-0.5 rounded">
                    with_keywords
                  </code>
                  ,{' '}
                  <code className="text-violet-300 bg-black/40 px-1.5 py-0.5 rounded">
                    primary_release_date
                  </code>{' '}
                  e{' '}
                  <code className="text-violet-300 bg-black/40 px-1.5 py-0.5 rounded">
                    with_runtime.lte
                  </code>{' '}
                  no backend C#), filtre pelos seus serviços de streaming no Brasil e salve no seu Diário.
                </p>
              </div>

              {/* Vibe Cards Selector Grid */}
              <div className="mt-6 grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3.5">
                {vibes.map((vibe) => {
                  const isSelected = selectedVibes.includes(vibe.id);
                  return (
                    <button
                      key={vibe.id}
                      type="button"
                      onClick={() => toggleVibe(vibe.id)}
                      className={`text-left p-4 rounded-2xl border transition-all duration-200 flex flex-col justify-between gap-2.5 ${
                        isSelected
                          ? 'bg-violet-600/20 border-violet-500 shadow-lg shadow-violet-950/50 ring-1 ring-violet-400/50'
                          : 'bg-[#0E101A]/90 border-slate-800/90 hover:border-slate-700 hover:bg-[#141726]'
                      }`}
                    >
                      <div>
                        <div className="flex items-center justify-between gap-2 mb-1">
                          <span className="text-2xl">{vibe.emoji}</span>
                          <span
                            className={`text-[10px] font-bold uppercase tracking-wider px-2 py-0.5 rounded-full ${
                              isSelected
                                ? 'bg-violet-500 text-white'
                                : 'bg-slate-800 text-slate-400'
                            }`}
                          >
                            {isSelected ? 'Vibe Ativa' : 'Selecionar'}
                          </span>
                        </div>
                        <h3 className="font-display font-bold text-base text-white">
                          {vibe.name}
                        </h3>
                        <p className="text-[11px] font-semibold text-violet-300/90">
                          {vibe.subtitle}
                        </p>
                        <p className="text-xs text-slate-400 mt-1 line-clamp-2">
                          {vibe.description}
                        </p>
                      </div>

                      <div className="flex flex-wrap gap-1 pt-2 border-t border-slate-800/80">
                        {vibe.sampleTags.map((tag) => (
                          <span
                            key={tag}
                            className="text-[10px] px-2 py-0.5 rounded bg-black/40 text-slate-300"
                          >
                            #{tag}
                          </span>
                        ))}
                      </div>
                    </button>
                  );
                })}
              </div>
            </div>

            {/* Secondary Filters Bar: Search, Runtime, Genres, Streaming Providers */}
            <div className="p-5 rounded-3xl bg-[#12141F] border border-slate-800/90 space-y-5">
              <div className="grid grid-cols-1 md:grid-cols-12 gap-4 items-center">
                {/* Search Input */}
                <div className="md:col-span-5 relative">
                  <Search className="w-4 h-4 text-slate-400 absolute left-3.5 top-1/2 -translate-y-1/2" />
                  <input
                    type="text"
                    value={searchQuery}
                    onChange={(e) => setSearchQuery(e.target.value)}
                    placeholder="Buscar por título, diretor ou palavra-chave..."
                    className="w-full pl-10 pr-4 py-2.5 rounded-xl bg-slate-900 border border-slate-700/80 text-xs sm:text-sm text-white placeholder-slate-500 focus:outline-none focus:border-violet-500"
                  />
                </div>

                {/* Runtime Filter */}
                <div className="md:col-span-5 flex flex-wrap items-center gap-1.5">
                  <span className="text-xs font-semibold text-slate-400 flex items-center gap-1 mr-1">
                    <Clock className="w-3.5 h-3.5 text-violet-400" />
                    Tempo:
                  </span>
                  {RUNTIME_OPTIONS.map((rt) => (
                    <button
                      key={rt.label}
                      type="button"
                      onClick={() => setMaxRuntime(rt.value)}
                      className={`px-2.5 py-1.5 rounded-xl text-xs font-semibold transition-all ${
                        maxRuntime === rt.value
                          ? 'bg-violet-600 text-white'
                          : 'bg-slate-900 text-slate-300 hover:bg-slate-800 border border-slate-800'
                      }`}
                    >
                      {rt.label}
                    </button>
                  ))}
                </div>

                {/* Reset Filters */}
                <div className="md:col-span-2 flex justify-end">
                  <button
                    type="button"
                    onClick={clearAllFilters}
                    className="inline-flex items-center gap-1.5 px-3 py-2 rounded-xl bg-slate-900 hover:bg-slate-800 text-xs font-semibold text-slate-300 border border-slate-800 transition-colors"
                  >
                    <RotateCcw className="w-3.5 h-3.5" />
                    Limpar Filtros
                  </button>
                </div>
              </div>

              {/* Genres Pills */}
              <div className="flex flex-wrap items-center gap-1.5 pt-3 border-t border-slate-800/80">
                <span className="text-xs font-semibold text-slate-400 mr-1">Gêneros:</span>
                {genres.map((g) => {
                  const active = selectedGenres.includes(g.id);
                  return (
                    <button
                      key={g.id}
                      type="button"
                      onClick={() => toggleGenre(g.id)}
                      className={`px-3 py-1 rounded-full text-xs font-medium transition-all ${
                        active
                          ? 'bg-fuchsia-600 text-white shadow-md shadow-fuchsia-950/50'
                          : 'bg-slate-900 text-slate-300 hover:bg-slate-800 border border-slate-800'
                      }`}
                    >
                      {g.icon} {g.name}
                    </button>
                  );
                })}
              </div>

              {/* Streaming Provider Filter */}
              <div className="flex flex-wrap items-center gap-1.5 pt-3 border-t border-slate-800/80">
                <span className="text-xs font-semibold text-emerald-400 flex items-center gap-1 mr-1">
                  <Tv className="w-3.5 h-3.5" />
                  Onde Assistir (BR):
                </span>
                <button
                  type="button"
                  onClick={() => setSelectedProvider(null)}
                  className={`px-3 py-1 rounded-xl text-xs font-semibold transition-all ${
                    selectedProvider === null
                      ? 'bg-emerald-600 text-white'
                      : 'bg-slate-900 text-slate-400 hover:text-white border border-slate-800'
                  }`}
                >
                  Todos os Streamings
                </button>
                {STREAMING_PROVIDERS.map((prov) => (
                  <button
                    key={prov}
                    type="button"
                    onClick={() =>
                      setSelectedProvider(selectedProvider === prov ? null : prov)
                    }
                    className={`px-3 py-1 rounded-xl text-xs font-semibold transition-all ${
                      selectedProvider === prov
                        ? 'bg-emerald-600 text-white shadow-md shadow-emerald-950/50'
                        : 'bg-slate-900 text-slate-300 hover:bg-slate-800 border border-slate-800'
                    }`}
                  >
                    {prov}
                  </button>
                ))}
              </div>

              {/* Applied Backend TMDB Query Formula Inspector */}
              {discoverResult && (
                <div className="p-3.5 rounded-2xl bg-[#0B0D14] border border-slate-800/90 flex flex-col sm:flex-row sm:items-center justify-between gap-3">
                  <div className="flex items-start sm:items-center gap-2.5">
                    <Code2 className="w-4 h-4 text-violet-400 shrink-0 mt-0.5 sm:mt-0" />
                    <div className="text-xs">
                      <span className="text-slate-400 font-medium">Fórmula Aplicada: </span>
                      <span className="text-violet-300 font-semibold">
                        {discoverResult.appliedFormulaSummary}
                      </span>
                    </div>
                  </div>
                  <div className="flex flex-wrap gap-1.5">
                    {Object.entries(discoverResult.appliedTmdbQueryParameters).map(
                      ([k, v]) => (
                        <span
                          key={k}
                          className="text-[10px] font-mono px-2 py-0.5 rounded bg-slate-900 text-emerald-300 border border-slate-800"
                        >
                          {k}={v}
                        </span>
                      )
                    )}
                  </div>
                </div>
              )}
            </div>

            {/* Movie Results Grid */}
            {loadingMovies ? (
              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
                {[1, 2, 3, 4, 5, 6, 7, 8].map((n) => (
                  <div
                    key={n}
                    className="aspect-[2/3] rounded-2xl bg-slate-900/60 border border-slate-800 animate-pulse"
                  />
                ))}
              </div>
            ) : discoverResult && discoverResult.movies.length > 0 ? (
              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
                {discoverResult.movies.map((movie) => (
                  <MovieCard
                    key={movie.tmdbId}
                    movie={movie}
                    watchlistEntry={getWatchlistEntryForMovie(movie.tmdbId)}
                    onSelectMovie={setSelectedMovie}
                    onQuickSave={(m, type) => handleSaveWatchlist(m, type)}
                  />
                ))}
              </div>
            ) : (
              <div className="p-12 text-center rounded-3xl bg-[#12141F] border border-slate-800 space-y-3">
                <Film className="w-10 h-10 text-slate-600 mx-auto" />
                <h3 className="font-display text-lg font-bold text-white">
                  Nenhum filme encontrado com essa combinação exata
                </h3>
                <p className="text-xs text-slate-400 max-w-md mx-auto">
                  Experimente relaxar o filtro de tempo de duração ou limpar um dos gêneros selecionados.
                </p>
                <button
                  type="button"
                  onClick={clearAllFilters}
                  className="px-4 py-2 rounded-xl bg-violet-600 text-white text-xs font-bold"
                >
                  Restaurar Catálogo Completo
                </button>
              </div>
            )}
          </div>
        )}

        {activeView === 'radar' && (
          <CinemaRadarView
            defaultCity={user?.defaultCity || 'Joinville/SC'}
            onSelectMovie={setSelectedMovie}
          />
        )}

        {activeView === 'diary' && (
          <WatchlistDiaryView
            user={user}
            items={watchlist}
            stats={watchlistStats}
            onLogin={handleLogin}
            onRegister={handleRegister}
            onUpdateItem={async (id, type, rating, notes) => {
              await api.updateWatchlistItem(id, type, rating, notes);
              await refreshWatchlist();
              showToast('Avaliação atualizada no Diário!');
            }}
            onDeleteItem={async (id) => {
              await api.deleteWatchlistItem(id);
              await refreshWatchlist();
              showToast('Filme removido do Diário.');
            }}
          />
        )}
      </main>

      {/* Movie Details Modal */}
      <MovieDetailsModal
        movie={selectedMovie}
        watchlistEntry={
          selectedMovie ? getWatchlistEntryForMovie(selectedMovie.tmdbId) : undefined
        }
        onClose={() => setSelectedMovie(null)}
        onSaveWatchlist={async (m, type, rating, notes) => {
          await handleSaveWatchlist(m, type, rating, notes);
          setSelectedMovie(null);
        }}
      />

      {/* TMDB API Key Configuration Modal */}
      {showKeyModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-sm">
          <div className="w-full max-w-md p-6 rounded-3xl bg-[#12141F] border border-slate-800 shadow-2xl space-y-4">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <KeyRound className="w-5 h-5 text-amber-400" />
                <h3 className="font-display font-bold text-lg text-white">
                  Chave da API TMDB (Opcional)
                </h3>
              </div>
              <button
                onClick={() => setShowKeyModal(false)}
                className="p-1.5 rounded-full text-slate-400 hover:text-white"
              >
                <X className="w-4 h-4" />
              </button>
            </div>

            <p className="text-xs text-slate-400 leading-relaxed">
              O backend <strong>VibeMovies.Api (.NET 8)</strong> já inclui um catálogo curado completo com dados reais de streaming no Brasil (JustWatch/TMDB). Se desejar consultar a API do TMDB ao vivo em tempo real, insira sua chave v3 abaixo:
            </p>

            <form onSubmit={handleSaveTmdbKey} className="space-y-4">
              <input
                type="text"
                value={tmdbKeyInput}
                onChange={(e) => setTmdbKeyInput(e.target.value)}
                placeholder="Cole sua TMDB v3 API Key (ou deixe em branco)..."
                className="w-full rounded-xl bg-slate-900 border border-slate-700 px-3.5 py-2.5 text-xs text-white focus:outline-none focus:border-violet-500"
              />
              <div className="flex justify-end gap-2">
                <button
                  type="button"
                  onClick={() => setShowKeyModal(false)}
                  className="px-4 py-2 rounded-xl bg-slate-800 text-xs font-semibold text-slate-300"
                >
                  Cancelar
                </button>
                <button
                  type="submit"
                  className="px-4 py-2 rounded-xl bg-violet-600 hover:bg-violet-500 text-xs font-bold text-white"
                >
                  Salvar Configuração
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};
export default App;
