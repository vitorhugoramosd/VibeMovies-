import React, { useMemo, useState } from 'react';
import {
  Bookmark,
  CheckCircle2,
  Download,
  FileJson,
  Loader2,
  Search,
  Sparkles,
  Star,
  Trash2,
  UserCheck,
  X
} from 'lucide-react';
import { AuthResponseDto, WatchlistItem, WatchlistStatsDto, WatchlistType } from '../types/api';
import { AuthForm } from './AuthForm';
import { PosterImage } from './PosterImage';
import { StateMessage } from './StateMessage';
import { StatsDashboard } from './StatsDashboard';

type DiarySort = 'date-desc' | 'date-asc' | 'rating-desc' | 'rating-asc' | 'title-asc';

const SORTS: { value: DiarySort; label: string }[] = [
  { value: 'date-desc', label: 'Mais recentes' },
  { value: 'date-asc', label: 'Mais antigos' },
  { value: 'rating-desc', label: 'Maior nota' },
  { value: 'rating-asc', label: 'Menor nota' },
  { value: 'title-asc', label: 'Título (A–Z)' }
];

interface WatchlistDiaryViewProps {
  user: AuthResponseDto | null;
  items: WatchlistItem[];
  stats: WatchlistStatsDto;
  loading: boolean;
  onUpdateItem: (id: string, type: WatchlistType, rating?: number, notes?: string) => Promise<void>;
  onDeleteItem: (id: string) => Promise<void>;
  onExport: (format: 'csv' | 'json') => Promise<void>;
}

function itemDate(i: WatchlistItem): number {
  const d = i.watchedAt || i.addedAt;
  return d ? new Date(d).getTime() : 0;
}

function normalize(text: string): string {
  return text
    .toLowerCase()
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '');
}

export const WatchlistDiaryView: React.FC<WatchlistDiaryViewProps> = ({
  user,
  items,
  stats,
  loading,
  onUpdateItem,
  onDeleteItem,
  onExport
}) => {
  const [activeTab, setActiveTab] = useState<WatchlistType>(WatchlistType.Watched);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editRating, setEditRating] = useState<number>(5);
  const [editNotes, setEditNotes] = useState<string>('');
  const [sort, setSort] = useState<DiarySort>('date-desc');
  const [query, setQuery] = useState('');
  const [exporting, setExporting] = useState<'csv' | 'json' | null>(null);

  const watchedCount = items.filter((i) => i.listType === WatchlistType.Watched).length;
  const wantCount = items.filter((i) => i.listType === WatchlistType.WantToWatch).length;

  const visibleItems = useMemo(() => {
    const q = normalize(query.trim());
    const list = items.filter(
      (i) =>
        i.listType === activeTab &&
        (!q ||
          normalize(i.title).includes(q) ||
          normalize(i.originalTitle || '').includes(q) ||
          normalize(i.genresCsv || '').includes(q) ||
          normalize(i.personalNotes || '').includes(q))
    );
    const sorted = [...list];
    switch (sort) {
      case 'date-asc':
        sorted.sort((a, b) => itemDate(a) - itemDate(b));
        break;
      case 'rating-desc':
        sorted.sort((a, b) => (b.personalRating ?? 0) - (a.personalRating ?? 0) || itemDate(b) - itemDate(a));
        break;
      case 'rating-asc':
        sorted.sort((a, b) => (a.personalRating ?? 0) - (b.personalRating ?? 0) || itemDate(b) - itemDate(a));
        break;
      case 'title-asc':
        sorted.sort((a, b) => a.title.localeCompare(b.title, 'pt-BR'));
        break;
      default:
        sorted.sort((a, b) => itemDate(b) - itemDate(a));
    }
    return sorted;
  }, [items, activeTab, query, sort]);

  if (!user) {
    return (
      <div className="max-w-md mx-auto my-6 sm:my-10 p-6 sm:p-8 rounded-3xl bg-[#12141F] border border-slate-800 shadow-2xl">
        <AuthForm titleId="diary-auth-title" />
      </div>
    );
  }

  const handleExport = async (format: 'csv' | 'json') => {
    setExporting(format);
    try {
      await onExport(format);
    } finally {
      setExporting(null);
    }
  };

  return (
    <div className="space-y-8">
      <div className="flex flex-col sm:flex-row sm:items-end justify-between gap-3">
        <div>
          <h1 className="font-display text-2xl sm:text-3xl font-extrabold text-white">Diário de Bordo</h1>
          <p className="flex items-center gap-1.5 text-xs text-slate-400 mt-1 min-w-0">
            <UserCheck className="w-4 h-4 text-emerald-400 shrink-0" aria-hidden="true" />
            <span className="truncate">
              Logado como <strong className="text-white">{user.name}</strong> ({user.email})
            </span>
          </p>
        </div>
        <div className="flex gap-2">
          <button
            type="button"
            onClick={() => handleExport('csv')}
            disabled={exporting !== null || items.length === 0}
            className="inline-flex items-center gap-1.5 px-3 py-2 rounded-xl bg-slate-900 hover:bg-slate-800 border border-slate-700 text-xs font-semibold text-slate-200 disabled:opacity-50"
          >
            {exporting === 'csv' ? (
              <Loader2 className="w-3.5 h-3.5 animate-spin" aria-hidden="true" />
            ) : (
              <Download className="w-3.5 h-3.5" aria-hidden="true" />
            )}
            Exportar CSV
          </button>
          <button
            type="button"
            onClick={() => handleExport('json')}
            disabled={exporting !== null || items.length === 0}
            className="inline-flex items-center gap-1.5 px-3 py-2 rounded-xl bg-slate-900 hover:bg-slate-800 border border-slate-700 text-xs font-semibold text-slate-200 disabled:opacity-50"
          >
            {exporting === 'json' ? (
              <Loader2 className="w-3.5 h-3.5 animate-spin" aria-hidden="true" />
            ) : (
              <FileJson className="w-3.5 h-3.5" aria-hidden="true" />
            )}
            Exportar JSON
          </button>
        </div>
      </div>

      {loading && items.length === 0 ? (
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4" aria-hidden="true">
          {[1, 2, 3, 4].map((n) => (
            <div key={n} className="h-24 rounded-2xl skeleton" />
          ))}
        </div>
      ) : (
        <StatsDashboard stats={stats} />
      )}

      {/* Tabs + tools */}
      <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-3 border-b border-slate-800 pb-4">
        <div className="flex gap-2" role="group" aria-label="Listas do diário">
          <button
            type="button"
            aria-pressed={activeTab === WatchlistType.Watched}
            onClick={() => setActiveTab(WatchlistType.Watched)}
            className={`flex items-center gap-2 px-4 py-2.5 rounded-xl text-sm font-bold transition-all ${
              activeTab === WatchlistType.Watched
                ? 'bg-emerald-600 text-white shadow-lg shadow-emerald-950/50'
                : 'bg-slate-900 text-slate-400 hover:text-white border border-slate-800'
            }`}
          >
            <CheckCircle2 className="w-4 h-4" aria-hidden="true" />
            Já Vi ({watchedCount})
          </button>
          <button
            type="button"
            aria-pressed={activeTab === WatchlistType.WantToWatch}
            onClick={() => setActiveTab(WatchlistType.WantToWatch)}
            className={`flex items-center gap-2 px-4 py-2.5 rounded-xl text-sm font-bold transition-all ${
              activeTab === WatchlistType.WantToWatch
                ? 'bg-violet-600 text-white shadow-lg shadow-violet-950/50'
                : 'bg-slate-900 text-slate-400 hover:text-white border border-slate-800'
            }`}
          >
            <Bookmark className="w-4 h-4" aria-hidden="true" />
            Quero Ver ({wantCount})
          </button>
        </div>

        <div className="flex flex-col sm:flex-row gap-2">
          <div className="relative sm:w-64">
            <label htmlFor="diary-search" className="sr-only">
              Buscar no diário
            </label>
            <Search className="w-4 h-4 text-slate-500 absolute left-3 top-1/2 -translate-y-1/2" aria-hidden="true" />
            <input
              id="diary-search"
              type="search"
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              placeholder="Buscar título, gênero ou nota..."
              className="w-full pl-9 pr-8 py-2 rounded-xl bg-slate-900 border border-slate-700 text-xs text-white placeholder-slate-500 focus:outline-none focus:border-violet-500 [&::-webkit-search-cancel-button]:hidden"
            />
            {query && (
              <button
                type="button"
                onClick={() => setQuery('')}
                aria-label="Limpar busca do diário"
                className="absolute right-1.5 top-1/2 -translate-y-1/2 p-1 rounded-lg text-slate-400 hover:text-white"
              >
                <X className="w-3.5 h-3.5" aria-hidden="true" />
              </button>
            )}
          </div>
          <label htmlFor="diary-sort" className="sr-only">
            Ordenar diário
          </label>
          <select
            id="diary-sort"
            value={sort}
            onChange={(e) => setSort(e.target.value as DiarySort)}
            className="rounded-xl bg-slate-900 border border-slate-700 px-3 py-2 text-xs font-semibold text-slate-200 focus:outline-none focus:border-violet-500"
          >
            {SORTS.map((s) => (
              <option key={s.value} value={s.value}>
                {s.label}
              </option>
            ))}
          </select>
        </div>
      </div>

      {/* Items */}
      {visibleItems.length === 0 ? (
        query ? (
          <StateMessage
            title="Nada encontrado no diário"
            description={<>Nenhum filme desta lista combina com &ldquo;{query}&rdquo;.</>}
            actionLabel="Limpar busca"
            onAction={() => setQuery('')}
          />
        ) : (
          <StateMessage
            title="Nenhum filme nesta lista ainda"
            description={
              <>
                Explore o buscador por Vibe e clique em &ldquo;Quero Ver&rdquo; ou &ldquo;Já Vi&rdquo; para construir seu
                diário cinematográfico.
              </>
            }
          />
        )
      ) : (
        <ul className="grid grid-cols-1 md:grid-cols-2 gap-4 sm:gap-5">
          {visibleItems.map((item) => (
            <li
              key={item.id}
              className="p-3 sm:p-5 rounded-2xl bg-[#12141F] border border-slate-800 flex gap-3 sm:gap-4 min-w-0"
            >
              <PosterImage
                src={item.posterPath}
                alt={`Pôster de ${item.title}`}
                fallbackTitle={item.title}
                className="w-20 sm:w-28 h-32 sm:h-40 object-cover rounded-xl shrink-0"
              />

              <div className="flex-1 min-w-0 flex flex-col justify-between">
                <div>
                  <div className="flex items-start justify-between gap-2">
                    <div className="min-w-0">
                      <h3 className="font-display font-bold text-base sm:text-lg text-white leading-tight">{item.title}</h3>
                      <span className="text-xs text-slate-400 break-words">
                        {item.releaseYear} • {item.genresCsv}
                      </span>
                    </div>
                    <button
                      type="button"
                      onClick={() => onDeleteItem(item.id)}
                      aria-label={`Remover ${item.title} do Diário`}
                      title="Remover do Diário"
                      className="p-1.5 rounded-lg text-slate-500 hover:text-red-400 hover:bg-red-500/10 transition-colors shrink-0"
                    >
                      <Trash2 className="w-4 h-4" aria-hidden="true" />
                    </button>
                  </div>

                  {item.listType === WatchlistType.Watched && (
                    <div className="mt-2 space-y-1.5">
                      <div className="flex flex-wrap items-center gap-1">
                        <span className="sr-only">Nota {item.personalRating || 5} de 5</span>
                        {[1, 2, 3, 4, 5].map((s) => (
                          <Star
                            key={s}
                            aria-hidden="true"
                            className={`w-4 h-4 ${
                              s <= (item.personalRating || 5) ? 'fill-amber-400 text-amber-400' : 'text-slate-700'
                            }`}
                          />
                        ))}
                        {item.watchedAt && (
                          <span className="text-[11px] text-slate-400 ml-1">
                            Assistido em {new Date(item.watchedAt).toLocaleDateString('pt-BR')}
                          </span>
                        )}
                      </div>
                      {item.personalNotes && (
                        <p className="text-xs text-slate-300 italic bg-slate-900/90 p-2.5 rounded-xl border border-slate-800 break-words">
                          &ldquo;{item.personalNotes}&rdquo;
                        </p>
                      )}
                    </div>
                  )}

                  {item.listType === WatchlistType.WantToWatch && (
                    <p className="text-xs text-slate-400 line-clamp-2 mt-2">{item.overview}</p>
                  )}
                </div>

                <div className="pt-3 mt-3 border-t border-slate-800/80">
                  {editingId === item.id ? (
                    <div className="w-full space-y-2">
                      <div className="flex items-center gap-0.5" role="group" aria-label="Sua nota">
                        {[1, 2, 3, 4, 5].map((st) => (
                          <button
                            key={st}
                            type="button"
                            onClick={() => setEditRating(st)}
                            aria-label={`${st} estrela${st > 1 ? 's' : ''}`}
                            aria-pressed={st === editRating}
                            className="p-1 rounded"
                          >
                            <Star
                              aria-hidden="true"
                              className={`w-4 h-4 ${st <= editRating ? 'fill-amber-400 text-amber-400' : 'text-slate-600'}`}
                            />
                          </button>
                        ))}
                      </div>
                      <label htmlFor={`notes-${item.id}`} className="sr-only">
                        Sua crítica pessoal
                      </label>
                      <input
                        id={`notes-${item.id}`}
                        type="text"
                        value={editNotes}
                        onChange={(e) => setEditNotes(e.target.value)}
                        placeholder="Sua crítica pessoal..."
                        className="w-full rounded-lg bg-slate-900 border border-slate-700 px-2.5 py-1 text-xs text-white focus:outline-none focus:border-violet-500"
                      />
                      <div className="flex flex-wrap gap-2">
                        <button
                          type="button"
                          onClick={async () => {
                            await onUpdateItem(item.id, WatchlistType.Watched, editRating, editNotes);
                            setEditingId(null);
                          }}
                          className="px-3 py-1 rounded-lg bg-emerald-600 hover:bg-emerald-500 text-white text-xs font-bold"
                        >
                          Salvar Avaliação
                        </button>
                        <button
                          type="button"
                          onClick={() => setEditingId(null)}
                          className="px-2.5 py-1 rounded-lg bg-slate-800 text-slate-300 text-xs"
                        >
                          Cancelar
                        </button>
                      </div>
                    </div>
                  ) : (
                    <button
                      type="button"
                      onClick={() => {
                        setEditingId(item.id);
                        setEditRating(item.personalRating || 5);
                        setEditNotes(item.personalNotes || '');
                      }}
                      className="inline-flex items-center gap-1.5 text-xs font-semibold text-emerald-400 hover:text-emerald-300"
                    >
                      <Sparkles className="w-3.5 h-3.5" aria-hidden="true" />
                      {item.listType === WatchlistType.Watched ? 'Editar Nota / Crítica' : 'Marcar como Já Vi e Avaliar'}
                    </button>
                  )}
                </div>
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
};
