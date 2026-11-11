import { forwardRef } from 'react';
import { Clock, Code2, RotateCcw, Search, Tv, UserCheck, X } from 'lucide-react';
import { DiscoverFilters, DiscoverResultDto, GenreDto, SortOption } from '../types/api';
import {
  DECADE_OPTIONS,
  FILTER_PROVIDERS,
  MIN_RATING_OPTIONS,
  RUNTIME_OPTIONS,
  SORT_OPTIONS
} from '../lib/constants';

interface FilterPanelProps {
  filters: DiscoverFilters;
  genres: GenreDto[];
  result: DiscoverResultDto | null;
  isLoggedIn: boolean;
  myProviders: string[];
  onChange: (patch: Partial<DiscoverFilters>) => void;
  onClear: () => void;
  onOpenProfile: () => void;
}

const selectClass =
  'rounded-xl bg-slate-900 border border-slate-700/80 px-3 py-2 text-xs font-semibold text-slate-200 focus:outline-none focus:border-violet-500';

function toggle<T>(list: T[], value: T): T[] {
  return list.includes(value) ? list.filter((v) => v !== value) : [...list, value];
}

export const FilterPanel = forwardRef<HTMLInputElement, FilterPanelProps>(function FilterPanel(
  { filters, genres, result, isLoggedIn, myProviders, onChange, onClear, onOpenProfile },
  searchRef
) {
  return (
    <section aria-label="Filtros" className="p-4 sm:p-5 rounded-3xl bg-[#12141F] border border-slate-800/90 space-y-5">
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-4 items-center">
        {/* Search */}
        <div className="lg:col-span-5 relative">
          <label htmlFor="discover-search" className="sr-only">
            Buscar filmes
          </label>
          <Search className="w-4 h-4 text-slate-400 absolute left-3.5 top-1/2 -translate-y-1/2" aria-hidden="true" />
          <input
            id="discover-search"
            ref={searchRef}
            type="search"
            value={filters.search}
            onChange={(e) => onChange({ search: e.target.value })}
            placeholder="Buscar por título, diretor ou palavra-chave..."
            aria-keyshortcuts="/"
            className="w-full pl-10 pr-16 py-2.5 rounded-xl bg-slate-900 border border-slate-700/80 text-sm text-white placeholder-slate-500 focus:outline-none focus:border-violet-500 [&::-webkit-search-cancel-button]:hidden"
          />
          {filters.search ? (
            <button
              type="button"
              onClick={() => onChange({ search: '' })}
              aria-label="Limpar busca"
              className="absolute right-2 top-1/2 -translate-y-1/2 p-1.5 rounded-lg text-slate-400 hover:text-white hover:bg-slate-800"
            >
              <X className="w-3.5 h-3.5" aria-hidden="true" />
            </button>
          ) : (
            <kbd className="hidden sm:block absolute right-3 top-1/2 -translate-y-1/2 text-[10px] font-mono px-1.5 py-0.5 rounded bg-slate-800 border border-slate-700 text-slate-400">
              /
            </kbd>
          )}
        </div>

        {/* Runtime */}
        <div className="lg:col-span-5 flex flex-wrap items-center gap-1.5" role="group" aria-label="Duração máxima">
          <span className="text-xs font-semibold text-slate-400 flex items-center gap-1 mr-1">
            <Clock className="w-3.5 h-3.5 text-violet-400" aria-hidden="true" />
            Tempo:
          </span>
          {RUNTIME_OPTIONS.map((rt) => (
            <button
              key={rt.label}
              type="button"
              onClick={() => onChange({ maxRuntime: rt.value })}
              aria-pressed={filters.maxRuntime === rt.value}
              className={`px-2.5 py-1.5 rounded-xl text-xs font-semibold transition-all ${
                filters.maxRuntime === rt.value
                  ? 'bg-violet-600 text-white'
                  : 'bg-slate-900 text-slate-300 hover:bg-slate-800 border border-slate-800'
              }`}
            >
              {rt.label}
            </button>
          ))}
        </div>

        <div className="lg:col-span-2 flex lg:justify-end">
          <button
            type="button"
            onClick={onClear}
            className="inline-flex items-center gap-1.5 px-3 py-2 rounded-xl bg-slate-900 hover:bg-slate-800 text-xs font-semibold text-slate-300 border border-slate-800 transition-colors"
          >
            <RotateCcw className="w-3.5 h-3.5" aria-hidden="true" />
            Limpar Filtros
          </button>
        </div>
      </div>

      {/* Decade / rating / sort */}
      <div className="flex flex-wrap items-center gap-2 pt-3 border-t border-slate-800/80">
        <label className="sr-only" htmlFor="filter-decade">
          Década
        </label>
        <select
          id="filter-decade"
          className={selectClass}
          value={filters.decade ?? ''}
          onChange={(e) => onChange({ decade: e.target.value ? Number(e.target.value) : null })}
        >
          {DECADE_OPTIONS.map((o) => (
            <option key={o.label} value={o.value ?? ''}>
              {o.label}
            </option>
          ))}
        </select>
        <label className="sr-only" htmlFor="filter-rating">
          Nota mínima
        </label>
        <select
          id="filter-rating"
          className={selectClass}
          value={filters.minRating ?? ''}
          onChange={(e) => onChange({ minRating: e.target.value ? Number(e.target.value) : null })}
        >
          {MIN_RATING_OPTIONS.map((o) => (
            <option key={o.label} value={o.value ?? ''}>
              {o.label}
            </option>
          ))}
        </select>
        <label className="sr-only" htmlFor="filter-sort">
          Ordenar por
        </label>
        <select
          id="filter-sort"
          className={selectClass}
          value={filters.sortBy}
          onChange={(e) => onChange({ sortBy: e.target.value as SortOption })}
        >
          {SORT_OPTIONS.map((o) => (
            <option key={o.value} value={o.value}>
              {o.label}
            </option>
          ))}
        </select>
      </div>

      {/* Genres */}
      <div className="flex flex-wrap items-center gap-1.5 pt-3 border-t border-slate-800/80" role="group" aria-label="Gêneros">
        <span className="text-xs font-semibold text-slate-400 mr-1">Gêneros:</span>
        {genres.map((g) => {
          const active = filters.genres.includes(g.id);
          return (
            <button
              key={g.id}
              type="button"
              onClick={() => onChange({ genres: toggle(filters.genres, g.id) })}
              aria-pressed={active}
              className={`px-3 py-1 rounded-full text-xs font-medium transition-all ${
                active
                  ? 'bg-fuchsia-600 text-white shadow-md shadow-fuchsia-950/50'
                  : 'bg-slate-900 text-slate-300 hover:bg-slate-800 border border-slate-800'
              }`}
            >
              <span aria-hidden="true">{g.icon}</span> {g.name}
            </button>
          );
        })}
      </div>

      {/* Providers */}
      <div className="space-y-3 pt-3 border-t border-slate-800/80">
        <div className="flex flex-wrap items-center gap-1.5" role="group" aria-label="Streamings">
          <span className="text-xs font-semibold text-emerald-400 flex items-center gap-1 mr-1">
            <Tv className="w-3.5 h-3.5" aria-hidden="true" />
            Onde Assistir (BR):
          </span>
          <button
            type="button"
            onClick={() => onChange({ providers: [] })}
            aria-pressed={filters.providers.length === 0}
            className={`px-3 py-1 rounded-xl text-xs font-semibold transition-all ${
              filters.providers.length === 0
                ? 'bg-emerald-600 text-white'
                : 'bg-slate-900 text-slate-400 hover:text-white border border-slate-800'
            }`}
          >
            Todos
          </button>
          {FILTER_PROVIDERS.map((prov) => {
            const active = filters.providers.includes(prov);
            return (
              <button
                key={prov}
                type="button"
                onClick={() => onChange({ providers: toggle(filters.providers, prov) })}
                aria-pressed={active}
                className={`px-3 py-1 rounded-xl text-xs font-semibold transition-all ${
                  active
                    ? 'bg-emerald-600 text-white shadow-md shadow-emerald-950/50'
                    : 'bg-slate-900 text-slate-300 hover:bg-slate-800 border border-slate-800'
                }`}
              >
                {prov}
              </button>
            );
          })}
        </div>

        {isLoggedIn && (
          <div className="flex flex-wrap items-center gap-3">
            <label
              className={`inline-flex items-center gap-2.5 text-xs font-semibold ${
                myProviders.length === 0 ? 'text-slate-500' : 'text-slate-200 cursor-pointer'
              }`}
            >
              <input
                type="checkbox"
                role="switch"
                className="peer sr-only"
                checked={filters.onlyMyProviders}
                disabled={myProviders.length === 0}
                onChange={(e) => onChange({ onlyMyProviders: e.target.checked })}
              />
              <span
                aria-hidden="true"
                className="relative w-9 h-5 rounded-full bg-slate-700 transition-colors peer-checked:bg-emerald-600 peer-focus-visible:outline peer-focus-visible:outline-2 peer-focus-visible:outline-violet-400 after:absolute after:top-0.5 after:left-0.5 after:w-4 after:h-4 after:rounded-full after:bg-white after:transition-transform peer-checked:after:translate-x-4"
              />
              <UserCheck className="w-3.5 h-3.5 text-emerald-400" aria-hidden="true" />
              Só nos meus streamings
            </label>
            {myProviders.length > 0 ? (
              <span className="text-[11px] text-slate-500">{myProviders.join(' • ')}</span>
            ) : (
              <button
                type="button"
                onClick={onOpenProfile}
                className="text-[11px] font-semibold text-violet-400 hover:text-violet-300 underline-offset-2 hover:underline"
              >
                Configure seus streamings no perfil
              </button>
            )}
          </div>
        )}
      </div>

      {/* Applied formula inspector */}
      {result && (
        <div className="p-3.5 rounded-2xl bg-[#0B0D14] border border-slate-800/90 flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          <div className="flex items-start sm:items-center gap-2.5 min-w-0">
            <Code2 className="w-4 h-4 text-violet-400 shrink-0 mt-0.5 sm:mt-0" aria-hidden="true" />
            <div className="text-xs min-w-0 break-words">
              <span className="text-slate-400 font-medium">Fórmula Aplicada: </span>
              <span className="text-violet-300 font-semibold">{result.appliedFormulaSummary}</span>
            </div>
          </div>
          <div className="flex flex-wrap gap-1.5 min-w-0">
            {Object.entries(result.appliedTmdbQueryParameters).map(([k, v]) => (
              <span
                key={k}
                className="text-[10px] font-mono px-2 py-0.5 rounded bg-slate-900 text-emerald-300 border border-slate-800 break-all"
              >
                {k}={v}
              </span>
            ))}
          </div>
        </div>
      )}
    </section>
  );
});
