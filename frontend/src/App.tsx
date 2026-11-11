import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { api, getStoredTmdbKey, isBackendOffline, subscribeConnectivity } from './services/api';
import { DiscoverFilters, GenreDto, MovieCardDto, VibeDefinition, WatchlistType } from './types/api';
import { AuthProvider, useAuth } from './context/AuthContext';
import { ToastProvider, useToast } from './context/ToastContext';
import { useDebounce } from './hooks/useDebounce';
import { useDiscover } from './hooks/useDiscover';
import { useWatchlist } from './hooks/useWatchlist';
import { useKeyboardShortcuts } from './hooks/useKeyboardShortcuts';
import { buildQueryString, emptyFilters, readFiltersFromUrl, readMovieIdFromUrl, replaceUrl } from './lib/urlState';
import { STORAGE_KEYS, storageGet, storageSet } from './lib/storage';
import { Header, MainView } from './components/Header';
import { VibeFilterBar } from './components/VibeFilterBar';
import { FilterPanel } from './components/FilterPanel';
import { MovieGrid } from './components/MovieGrid';
import { ForYouRow } from './components/ForYouRow';
import { MovieDetailsModal } from './components/MovieDetailsModal';
import { SurpriseModal } from './components/SurpriseModal';
import { CinemaRadarView } from './components/CinemaRadarView';
import { WatchlistDiaryView } from './components/WatchlistDiaryView';
import { AuthModal } from './components/AuthModal';
import { TmdbKeyModal } from './components/TmdbKeyModal';
import { ProfilePanel } from './components/ProfilePanel';

const VIEWS: MainView[] = ['discover', 'radar', 'diary'];

function readLastView(): MainView {
  const stored = storageGet(STORAGE_KEYS.lastView);
  return VIEWS.includes(stored as MainView) ? (stored as MainView) : 'discover';
}

const AppShell: React.FC = () => {
  const { user, initializing, logout } = useAuth();
  const toast = useToast();

  const [activeView, setActiveView] = useState<MainView>(readLastView);
  const [vibes, setVibes] = useState<VibeDefinition[]>([]);
  const [genres, setGenres] = useState<GenreDto[]>([]);
  const [filters, setFilters] = useState<DiscoverFilters>(() => readFiltersFromUrl());
  const [reloadToken, setReloadToken] = useState(0);
  const [selectedMovie, setSelectedMovie] = useState<MovieCardDto | null>(null);
  const [offline, setOffline] = useState(isBackendOffline);
  const [hasTmdbKey, setHasTmdbKey] = useState(() => Boolean(getStoredTmdbKey()));
  // Read once: the URL sync effect below rewrites the querystring right after mount.
  const [initialMovieId] = useState(() => readMovieIdFromUrl());

  const [surpriseOpen, setSurpriseOpen] = useState(false);
  const [profileOpen, setProfileOpen] = useState(false);
  const [authOpen, setAuthOpen] = useState(false);
  const [keyOpen, setKeyOpen] = useState(false);

  const searchRef = useRef<HTMLInputElement>(null);

  const watchlist = useWatchlist(user, initializing);
  const myProviders = user?.myProviders ?? [];

  // Search is debounced; every other filter applies immediately.
  const debouncedSearch = useDebounce(filters.search, 350);
  const appliedFilters = useMemo<DiscoverFilters>(
    () => ({
      ...filters,
      search: debouncedSearch,
      onlyMyProviders: filters.onlyMyProviders && Boolean(user) && myProviders.length > 0
    }),
    [filters, debouncedSearch, user, myProviders.length]
  );
  const discover = useDiscover(appliedFilters, reloadToken);

  // Catalog metadata + connectivity badge.
  useEffect(() => {
    api.getVibes().then(setVibes);
    api.getGenres().then(setGenres);
    return subscribeConnectivity(setOffline);
  }, []);

  // Deep link: ?movie={id} opens the details modal on load.
  useEffect(() => {
    const movieId = initialMovieId;
    if (!movieId) return;
    const controller = new AbortController();
    api
      .getMovie(movieId, controller.signal)
      .then(setSelectedMovie)
      .catch(() => {
        if (!controller.signal.aborted) toast('Filme do link não encontrado.', 'error');
      });
    return () => controller.abort();
  }, [initialMovieId, toast]);

  // Keep the querystring in sync (shareable filters + open movie).
  useEffect(() => {
    replaceUrl(buildQueryString(appliedFilters, selectedMovie?.tmdbId ?? null));
  }, [appliedFilters, selectedMovie]);

  const changeView = useCallback((view: MainView) => {
    setActiveView(view);
    storageSet(STORAGE_KEYS.lastView, view);
    window.scrollTo({ top: 0 });
  }, []);

  const patchFilters = useCallback((patch: Partial<DiscoverFilters>) => {
    setFilters((prev) => ({ ...prev, ...patch }));
  }, []);

  const toggleVibe = useCallback((id: string) => {
    setFilters((prev) => ({
      ...prev,
      vibes: prev.vibes.includes(id) ? prev.vibes.filter((v) => v !== id) : [...prev.vibes, id]
    }));
  }, []);

  const clearAllFilters = useCallback(() => setFilters(emptyFilters()), []);

  const handleSaveWatchlist = useCallback(
    async (movie: MovieCardDto, type: WatchlistType, rating?: number, notes?: string) => {
      if (!user) {
        setAuthOpen(true);
        toast('Entre na sua conta para salvar filmes no Diário.', 'info');
        return;
      }
      try {
        await watchlist.save(movie, type, rating, notes);
        toast(
          type === WatchlistType.Watched
            ? `"${movie.title}" registrado em Já Vi!`
            : `"${movie.title}" salvo na lista Quero Ver!`
        );
      } catch (err) {
        toast(err instanceof Error ? err.message : 'Não foi possível salvar.', 'error');
      }
    },
    [user, watchlist, toast]
  );

  const quickSave = useCallback(
    (m: MovieCardDto, type: WatchlistType) => {
      void handleSaveWatchlist(m, type);
    },
    [handleSaveWatchlist]
  );

  const openSurprise = useCallback(() => setSurpriseOpen(true), []);

  const focusSearch = useCallback(() => {
    const focus = () => {
      searchRef.current?.focus();
      searchRef.current?.select();
    };
    if (activeView !== 'discover') {
      changeView('discover');
      window.setTimeout(focus, 50);
    } else focus();
  }, [activeView, changeView]);

  useKeyboardShortcuts({ '/': focusSearch, s: openSurprise });

  return (
    <div className="min-h-screen bg-[#090A0F] text-slate-100 flex flex-col overflow-x-clip">
      <a
        href="#main-content"
        className="sr-only focus:not-sr-only focus:fixed focus:top-3 focus:left-3 focus:z-[70] focus:px-4 focus:py-2 focus:rounded-xl focus:bg-violet-600 focus:text-white focus:text-sm focus:font-bold"
      >
        Pular para o conteúdo
      </a>

      <Header
        activeView={activeView}
        onChangeView={changeView}
        user={user}
        watchlistCount={watchlist.items.length}
        hasTmdbKey={hasTmdbKey}
        offline={offline}
        onOpenTmdbKey={() => setKeyOpen(true)}
        onOpenProfile={() => setProfileOpen(true)}
        onOpenLogin={() => setAuthOpen(true)}
        onLogout={() => {
          logout();
          toast('Sessão encerrada.', 'info');
        }}
      />

      <main id="main-content" className="flex-1 max-w-7xl w-full mx-auto px-4 sm:px-6 lg:px-8 py-6 sm:py-8" tabIndex={-1}>
        {activeView === 'discover' && (
          <div className="space-y-6 sm:space-y-8">
            <VibeFilterBar
              vibes={vibes}
              selectedVibes={filters.vibes}
              onToggleVibe={toggleVibe}
              onSurprise={openSurprise}
            />

            <ForYouRow
              user={user}
              watchlist={watchlist.items}
              watchlistVersion={watchlist.version}
              onSelectMovie={setSelectedMovie}
            />

            <FilterPanel
              ref={searchRef}
              filters={filters}
              genres={genres}
              result={discover.result}
              isLoggedIn={Boolean(user)}
              myProviders={myProviders}
              onChange={patchFilters}
              onClear={clearAllFilters}
              onOpenProfile={() => setProfileOpen(true)}
            />

            <div className="flex items-baseline justify-between gap-3">
              <h2 className="font-display font-bold text-lg text-white">Resultados</h2>
              <span className="text-xs text-slate-400" aria-live="polite">
                {discover.loading
                  ? 'Buscando...'
                  : discover.result
                    ? `${discover.result.totalResults} filme${discover.result.totalResults === 1 ? '' : 's'}${
                        discover.result.usingLiveTmdbApi ? ' • TMDB ao vivo' : ''
                      }`
                    : ''}
              </span>
            </div>

            <MovieGrid
              movies={discover.movies}
              loading={discover.loading}
              loadingMore={discover.loadingMore}
              error={discover.error}
              hasMore={discover.hasMore}
              totalResults={discover.result?.totalResults}
              entryByMovieId={watchlist.entryByMovieId}
              onSelectMovie={setSelectedMovie}
              onQuickSave={quickSave}
              onLoadMore={discover.loadMore}
              onRetry={discover.retry}
              onClearFilters={clearAllFilters}
            />
          </div>
        )}

        {activeView === 'radar' && (
          <CinemaRadarView defaultCity={user?.defaultCity || 'Joinville/SC'} onSelectMovie={setSelectedMovie} />
        )}

        {activeView === 'diary' && (
          <WatchlistDiaryView
            user={user}
            items={watchlist.items}
            stats={watchlist.stats}
            loading={watchlist.loading}
            onUpdateItem={async (id, type, rating, notes) => {
              await watchlist.update(id, type, rating, notes);
              toast('Avaliação atualizada no Diário!');
            }}
            onDeleteItem={async (id) => {
              await watchlist.remove(id);
              toast('Filme removido do Diário.', 'info');
            }}
            onExport={async (format) => {
              try {
                await api.exportWatchlist(format, watchlist.items);
                toast(`Diário exportado em ${format.toUpperCase()}!`);
              } catch (err) {
                toast(err instanceof Error ? err.message : 'Falha ao exportar.', 'error');
              }
            }}
          />
        )}
      </main>

      <footer className="border-t border-slate-800/80 py-5 text-center text-[11px] text-slate-500 px-4">
        Atalhos: <kbd className="font-mono px-1 rounded bg-slate-800 text-slate-300">/</kbd> buscar •{' '}
        <kbd className="font-mono px-1 rounded bg-slate-800 text-slate-300">S</kbd> me surpreenda •{' '}
        <kbd className="font-mono px-1 rounded bg-slate-800 text-slate-300">Esc</kbd> fechar janelas
      </footer>

      <MovieDetailsModal
        movie={selectedMovie}
        watchlistEntry={selectedMovie ? watchlist.entryByMovieId.get(selectedMovie.tmdbId) : undefined}
        onClose={() => setSelectedMovie(null)}
        onSelectMovie={setSelectedMovie}
        onSaveWatchlist={async (m, type, rating, notes) => {
          await handleSaveWatchlist(m, type, rating, notes);
          if (user) setSelectedMovie(null);
        }}
      />

      <SurpriseModal
        open={surpriseOpen}
        onClose={() => setSurpriseOpen(false)}
        filters={appliedFilters}
        posterPool={discover.movies}
        onOpenDetails={(m) => {
          setSurpriseOpen(false);
          setSelectedMovie(m);
        }}
        onWantToWatch={async (m) => {
          await handleSaveWatchlist(m, WatchlistType.WantToWatch);
          if (user) setSurpriseOpen(false);
        }}
      />

      <TmdbKeyModal
        open={keyOpen}
        onClose={() => setKeyOpen(false)}
        onSaved={(hasKey) => {
          setHasTmdbKey(hasKey);
          setReloadToken((t) => t + 1);
          toast(hasKey ? 'Chave da API TMDB configurada!' : 'Usando catálogo inteligente integrado.');
        }}
      />

      <AuthModal open={authOpen} onClose={() => setAuthOpen(false)} />
      <ProfilePanel open={profileOpen} onClose={() => setProfileOpen(false)} />
    </div>
  );
};

export const App: React.FC = () => (
  <ToastProvider>
    <AuthProvider>
      <AppShell />
    </AuthProvider>
  </ToastProvider>
);

export default App;
