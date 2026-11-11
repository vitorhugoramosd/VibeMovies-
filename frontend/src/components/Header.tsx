import React from 'react';
import { Bookmark, Compass, Film, KeyRound, LogIn, LogOut, Radar, WifiOff } from 'lucide-react';
import { AuthResponseDto } from '../types/api';

export type MainView = 'discover' | 'radar' | 'diary';

interface HeaderProps {
  activeView: MainView;
  onChangeView: (view: MainView) => void;
  user: AuthResponseDto | null;
  watchlistCount: number;
  hasTmdbKey: boolean;
  offline: boolean;
  onOpenTmdbKey: () => void;
  onOpenProfile: () => void;
  onOpenLogin: () => void;
  onLogout: () => void;
}

const TABS: { id: MainView; label: string; short: string; icon: React.ElementType }[] = [
  { id: 'discover', label: 'Explorar por Vibe', short: 'Explorar', icon: Compass },
  { id: 'radar', label: 'Radar de Cinemas', short: 'Radar', icon: Radar },
  { id: 'diary', label: 'Diário de Bordo', short: 'Diário', icon: Bookmark }
];

function initials(name: string): string {
  return (
    name
      .split(/\s+/)
      .filter(Boolean)
      .slice(0, 2)
      .map((p) => p[0]?.toUpperCase())
      .join('') || '?'
  );
}

export const Header: React.FC<HeaderProps> = ({
  activeView,
  onChangeView,
  user,
  watchlistCount,
  hasTmdbKey,
  offline,
  onOpenTmdbKey,
  onOpenProfile,
  onOpenLogin,
  onLogout
}) => {
  const nav = (
    <nav
      aria-label="Seções"
      className="grid grid-cols-3 md:flex items-center gap-1 md:gap-1.5 bg-[#12141F] p-1 md:p-1.5 rounded-2xl border border-slate-800/90"
    >
      {TABS.map(({ id, label, short, icon: Icon }) => {
        const active = activeView === id;
        return (
          <button
            key={id}
            type="button"
            onClick={() => onChangeView(id)}
            aria-current={active ? 'page' : undefined}
            className={`flex items-center justify-center gap-1.5 md:gap-2 px-2 md:px-3.5 py-2 rounded-xl text-xs sm:text-sm font-bold transition-all ${
              active
                ? 'bg-violet-600 text-white shadow-lg shadow-violet-950/50'
                : 'text-slate-400 hover:text-white hover:bg-slate-800/60'
            }`}
          >
            <Icon className="w-4 h-4 shrink-0" aria-hidden="true" />
            <span className="xl:hidden">{short}</span>
            <span className="hidden xl:inline">{label}</span>
            {id === 'diary' && watchlistCount > 0 && (
              <span className="px-1.5 rounded-full text-[10px] bg-black/40 text-violet-200">
                <span className="sr-only">, </span>
                {watchlistCount}
                <span className="sr-only"> filmes</span>
              </span>
            )}
          </button>
        );
      })}
    </nav>
  );

  return (
    <header className="sticky top-0 z-40 bg-[#090A0F]/85 backdrop-blur-xl border-b border-slate-800/80">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="h-16 md:h-20 flex items-center justify-between gap-3">
          {/* Brand */}
          <button
            type="button"
            onClick={() => onChangeView('discover')}
            className="flex items-center gap-3 group rounded-2xl text-left min-w-0"
            aria-label="VibeMovies — ir para Explorar"
          >
            <div className="w-10 h-10 md:w-11 md:h-11 shrink-0 rounded-2xl bg-gradient-to-br from-violet-600 to-fuchsia-600 flex items-center justify-center shadow-lg shadow-violet-900/40 group-hover:scale-105 transition-transform">
              <Film className="w-5 h-5 md:w-6 md:h-6 text-white" aria-hidden="true" />
            </div>
            <div className="min-w-0">
              <div className="flex items-center gap-2">
                <span className="font-display font-extrabold text-lg md:text-xl tracking-tight text-white">
                  Vibe<span className="text-violet-400">Movies</span>
                </span>
                <span className="hidden xl:inline-block text-[10px] font-bold uppercase tracking-wider px-2 py-0.5 rounded-full bg-violet-500/15 text-violet-300 border border-violet-500/30">
                  C# .NET 8 + TMDB
                </span>
              </div>
              <p className="text-[11px] text-slate-400 hidden xl:block">
                Curadoria por Clima, Streaming BR & Radar de Cinemas
              </p>
            </div>
          </button>

          <div className="hidden md:block">{nav}</div>

          {/* Utilities */}
          <div className="flex items-center gap-2 shrink-0">
            {offline && (
              <span
                className="hidden sm:inline-flex items-center gap-1.5 px-2.5 py-1.5 rounded-xl text-[11px] font-semibold bg-amber-500/10 text-amber-300 border border-amber-500/30"
                title="Servidor indisponível — usando o catálogo local"
              >
                <WifiOff className="w-3.5 h-3.5" aria-hidden="true" />
                Offline
              </span>
            )}
            <button
              type="button"
              onClick={onOpenTmdbKey}
              aria-label="Configurar chave da API TMDB"
              title="Configurar Chave da API TMDB"
              className="flex items-center gap-1.5 p-2 md:px-3 rounded-xl bg-[#12141F] hover:bg-slate-800 border border-slate-800 text-xs font-semibold text-slate-300 transition-colors"
            >
              <KeyRound className="w-4 h-4 md:w-3.5 md:h-3.5 text-amber-400" aria-hidden="true" />
              <span className="hidden xl:inline">{hasTmdbKey ? 'TMDB Live Key Ativa' : 'Chave TMDB'}</span>
            </button>

            {user ? (
              <>
                <button
                  type="button"
                  onClick={onOpenProfile}
                  aria-label={`Abrir perfil de ${user.name}`}
                  title="Perfil e streamings"
                  className="flex items-center gap-2 p-1 pr-1 sm:pr-3 rounded-xl bg-[#12141F] hover:bg-slate-800 border border-slate-800 transition-colors"
                >
                  <span className="w-7 h-7 rounded-lg bg-gradient-to-br from-fuchsia-600 to-violet-600 text-[11px] font-extrabold text-white flex items-center justify-center">
                    {initials(user.name)}
                  </span>
                  <span className="hidden sm:inline text-xs font-semibold text-slate-200 max-w-[110px] truncate">
                    {user.name.split(' ')[0]}
                  </span>
                </button>
                <button
                  type="button"
                  onClick={onLogout}
                  aria-label={`Sair (${user.email})`}
                  title={`Sair (${user.email})`}
                  className="hidden sm:inline-flex p-2 rounded-xl bg-[#12141F] hover:bg-red-500/10 border border-slate-800 hover:border-red-500/30 text-slate-400 hover:text-red-300 transition-colors"
                >
                  <LogOut className="w-4 h-4" aria-hidden="true" />
                </button>
              </>
            ) : (
              <button
                type="button"
                onClick={onOpenLogin}
                className="flex items-center gap-1.5 px-3 py-2 rounded-xl bg-violet-600 hover:bg-violet-500 text-xs font-bold text-white transition-colors"
              >
                <LogIn className="w-4 h-4" aria-hidden="true" />
                Entrar
              </button>
            )}
          </div>
        </div>

        <div className="md:hidden pb-3">{nav}</div>
      </div>
    </header>
  );
};
