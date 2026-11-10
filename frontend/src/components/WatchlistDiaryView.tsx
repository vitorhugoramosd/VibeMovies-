import React, { useState } from 'react';
import {
  Bookmark,
  CheckCircle2,
  Clock,
  Film,
  LogIn,
  Sparkles,
  Star,
  Trash2,
  UserCheck
} from 'lucide-react';
import {
  AuthResponseDto,
  WatchlistItem,
  WatchlistStatsDto,
  WatchlistType
} from '../types/api';

interface WatchlistDiaryViewProps {
  user: AuthResponseDto | null;
  items: WatchlistItem[];
  stats: WatchlistStatsDto | null;
  onLogin: (email: string, password: string) => Promise<void>;
  onRegister: (name: string, email: string, password: string, city: string) => Promise<void>;
  onUpdateItem: (
    id: string,
    type: WatchlistType,
    rating?: number,
    notes?: string
  ) => Promise<void>;
  onDeleteItem: (id: string) => Promise<void>;
}

export const WatchlistDiaryView: React.FC<WatchlistDiaryViewProps> = ({
  user,
  items,
  stats,
  onLogin,
  onRegister,
  onUpdateItem,
  onDeleteItem
}) => {
  const [activeTab, setActiveTab] = useState<WatchlistType>(WatchlistType.Watched);
  const [isRegisterMode, setIsRegisterMode] = useState(false);
  const [name, setName] = useState('');
  const [email, setEmail] = useState('demo@vibemovies.app');
  const [password, setPassword] = useState('vibe1234');
  const [city, setCity] = useState('Joinville/SC');
  const [authError, setAuthError] = useState<string | null>(null);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editRating, setEditRating] = useState<number>(5);
  const [editNotes, setEditNotes] = useState<string>('');

  const filteredItems = items.filter((i) => i.listType === activeTab);

  const handleAuthSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setAuthError(null);
    try {
      if (isRegisterMode) {
        await onRegister(name, email, password, city);
      } else {
        await onLogin(email, password);
      }
    } catch (err: any) {
      setAuthError(err.message || 'Falha na autenticação.');
    }
  };

  if (!user) {
    return (
      <div className="max-w-md mx-auto my-10 p-6 sm:p-8 rounded-3xl bg-[#12141F] border border-slate-800 shadow-2xl space-y-6">
        <div className="text-center space-y-2">
          <div className="w-12 h-12 rounded-2xl bg-violet-600/20 border border-violet-500/40 flex items-center justify-center mx-auto text-violet-400">
            <LogIn className="w-6 h-6" />
          </div>
          <h2 className="font-display text-2xl font-bold text-white">
            {isRegisterMode ? 'Criar Conta no Diário' : 'Entrar no Diário de Bordo'}
          </h2>
          <p className="text-xs text-slate-400">
            Autenticação JWT persistida no banco de dados PostgreSQL / EF Core.
          </p>
        </div>

        {authError && (
          <div className="p-3 rounded-xl bg-red-500/10 border border-red-500/30 text-xs text-red-300">
            {authError}
          </div>
        )}

        <form onSubmit={handleAuthSubmit} className="space-y-4">
          {isRegisterMode && (
            <>
              <div>
                <label className="text-xs font-semibold text-slate-300 block mb-1">Seu Nome</label>
                <input
                  type="text"
                  required
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  placeholder="Ex: Vitor Hugo"
                  className="w-full rounded-xl bg-slate-900 border border-slate-700 px-3.5 py-2 text-sm text-white"
                />
              </div>
              <div>
                <label className="text-xs font-semibold text-slate-300 block mb-1">
                  Sua Cidade (para o Radar de Cinemas)
                </label>
                <input
                  type="text"
                  value={city}
                  onChange={(e) => setCity(e.target.value)}
                  placeholder="Joinville/SC"
                  className="w-full rounded-xl bg-slate-900 border border-slate-700 px-3.5 py-2 text-sm text-white"
                />
              </div>
            </>
          )}

          <div>
            <label className="text-xs font-semibold text-slate-300 block mb-1">E-mail</label>
            <input
              type="email"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="w-full rounded-xl bg-slate-900 border border-slate-700 px-3.5 py-2 text-sm text-white"
            />
          </div>

          <div>
            <label className="text-xs font-semibold text-slate-300 block mb-1">Senha</label>
            <input
              type="password"
              required
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="w-full rounded-xl bg-slate-900 border border-slate-700 px-3.5 py-2 text-sm text-white"
            />
          </div>

          <button
            type="submit"
            className="w-full py-2.5 px-4 rounded-xl bg-violet-600 hover:bg-violet-500 text-white font-bold text-sm shadow-lg shadow-violet-900/40 transition-all"
          >
            {isRegisterMode ? 'Cadastrar e Entrar' : 'Entrar com JWT'}
          </button>
        </form>

        <div className="pt-4 border-t border-slate-800 text-center">
          <button
            type="button"
            onClick={() => setIsRegisterMode(!isRegisterMode)}
            className="text-xs text-violet-400 hover:text-violet-300 font-medium"
          >
            {isRegisterMode
              ? 'Já possui conta? Faça login'
              : 'Não tem conta? Cadastre-se gratuitamente'}
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-8">
      {/* Stats Header */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <div className="p-5 rounded-2xl bg-[#12141F] border border-slate-800 flex items-center gap-4">
          <div className="p-3 rounded-xl bg-emerald-500/10 text-emerald-400 border border-emerald-500/20">
            <CheckCircle2 className="w-6 h-6" />
          </div>
          <div>
            <span className="text-xs text-slate-400 block">Filmes Assistidos (Já Vi)</span>
            <span className="font-display text-2xl font-extrabold text-white">
              {stats?.totalWatched ?? items.filter((i) => i.listType === WatchlistType.Watched).length}
            </span>
          </div>
        </div>

        <div className="p-5 rounded-2xl bg-[#12141F] border border-slate-800 flex items-center gap-4">
          <div className="p-3 rounded-xl bg-violet-500/10 text-violet-400 border border-violet-500/20">
            <Bookmark className="w-6 h-6" />
          </div>
          <div>
            <span className="text-xs text-slate-400 block">Na Fila (Quero Ver)</span>
            <span className="font-display text-2xl font-extrabold text-white">
              {stats?.totalWantToWatch ??
                items.filter((i) => i.listType === WatchlistType.WantToWatch).length}
            </span>
          </div>
        </div>

        <div className="p-5 rounded-2xl bg-[#12141F] border border-slate-800 flex items-center gap-4">
          <div className="p-3 rounded-xl bg-amber-500/10 text-amber-400 border border-amber-500/20">
            <Star className="w-6 h-6 fill-amber-400" />
          </div>
          <div>
            <span className="text-xs text-slate-400 block">Sua Nota Média</span>
            <span className="font-display text-2xl font-extrabold text-white">
              {stats?.averagePersonalRating ? `${stats.averagePersonalRating} ★` : '4.8 ★'}
            </span>
          </div>
        </div>

        <div className="p-5 rounded-2xl bg-[#12141F] border border-slate-800 flex items-center gap-4">
          <div className="p-3 rounded-xl bg-sky-500/10 text-sky-400 border border-sky-500/20">
            <Clock className="w-6 h-6" />
          </div>
          <div>
            <span className="text-xs text-slate-400 block">Tempo de Tela</span>
            <span className="font-display text-2xl font-extrabold text-white">
              {Math.round((stats?.totalMinutesWatched || 0) / 60)}h {(stats?.totalMinutesWatched || 0) % 60}m
            </span>
          </div>
        </div>
      </div>

      {/* Sub-navigation Tabs */}
      <div className="flex flex-wrap items-center justify-between gap-4 border-b border-slate-800 pb-4">
        <div className="flex gap-2">
          <button
            type="button"
            onClick={() => setActiveTab(WatchlistType.Watched)}
            className={`flex items-center gap-2 px-4 py-2.5 rounded-xl text-sm font-bold transition-all ${
              activeTab === WatchlistType.Watched
                ? 'bg-emerald-600 text-white shadow-lg shadow-emerald-950/50'
                : 'bg-slate-900 text-slate-400 hover:text-white border border-slate-800'
            }`}
          >
            <CheckCircle2 className="w-4 h-4" />
            Já Vi ({items.filter((i) => i.listType === WatchlistType.Watched).length})
          </button>

          <button
            type="button"
            onClick={() => setActiveTab(WatchlistType.WantToWatch)}
            className={`flex items-center gap-2 px-4 py-2.5 rounded-xl text-sm font-bold transition-all ${
              activeTab === WatchlistType.WantToWatch
                ? 'bg-violet-600 text-white shadow-lg shadow-violet-950/50'
                : 'bg-slate-900 text-slate-400 hover:text-white border border-slate-800'
            }`}
          >
            <Bookmark className="w-4 h-4" />
            Quero Ver ({items.filter((i) => i.listType === WatchlistType.WantToWatch).length})
          </button>
        </div>

        <div className="flex items-center gap-2 text-xs text-slate-400">
          <UserCheck className="w-4 h-4 text-emerald-400" />
          Logado como <strong className="text-white">{user.name}</strong> ({user.email})
        </div>
      </div>

      {/* Items List */}
      {filteredItems.length === 0 ? (
        <div className="p-12 text-center rounded-3xl bg-[#12141F]/60 border border-slate-800/80 space-y-3">
          <Film className="w-10 h-10 text-slate-600 mx-auto" />
          <h3 className="font-display text-lg font-bold text-white">
            Nenhum filme nesta lista ainda
          </h3>
          <p className="text-xs text-slate-400 max-w-md mx-auto">
            Explore o buscador por Vibe e clique em &ldquo;Quero Ver&rdquo; ou &ldquo;Já Vi&rdquo; para construir seu diário cinematográfico.
          </p>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
          {filteredItems.map((item) => (
            <div
              key={item.id}
              className="p-4 sm:p-5 rounded-2xl bg-[#12141F] border border-slate-800 flex gap-4 justify-between"
            >
              <img
                src={item.posterPath}
                alt={item.title}
                className="w-24 sm:w-28 h-36 sm:h-40 object-cover rounded-xl shrink-0"
              />

              <div className="flex-1 flex flex-col justify-between">
                <div>
                  <div className="flex items-start justify-between gap-2">
                    <div>
                      <h3 className="font-display font-bold text-base sm:text-lg text-white">
                        {item.title}
                      </h3>
                      <span className="text-xs text-slate-400">
                        {item.releaseYear} • {item.genresCsv}
                      </span>
                    </div>

                    <button
                      type="button"
                      onClick={() => onDeleteItem(item.id)}
                      title="Remover do Diário"
                      className="p-1.5 rounded-lg text-slate-500 hover:text-red-400 hover:bg-red-500/10 transition-colors"
                    >
                      <Trash2 className="w-4 h-4" />
                    </button>
                  </div>

                  {item.listType === WatchlistType.Watched && (
                    <div className="mt-2 space-y-1.5">
                      <div className="flex items-center gap-1">
                        {[1, 2, 3, 4, 5].map((s) => (
                          <Star
                            key={s}
                            className={`w-4 h-4 ${
                              s <= (item.personalRating || 5)
                                ? 'fill-amber-400 text-amber-400'
                                : 'text-slate-700'
                            }`}
                          />
                        ))}
                        {item.watchedAt && (
                          <span className="text-[11px] text-slate-400 ml-2">
                            Assistido em {new Date(item.watchedAt).toLocaleDateString('pt-BR')}
                          </span>
                        )}
                      </div>
                      {item.personalNotes && (
                        <p className="text-xs text-slate-300 italic bg-slate-900/90 p-2.5 rounded-xl border border-slate-800">
                          &ldquo;{item.personalNotes}&rdquo;
                        </p>
                      )}
                    </div>
                  )}

                  {item.listType === WatchlistType.WantToWatch && (
                    <p className="text-xs text-slate-400 line-clamp-2 mt-2">{item.overview}</p>
                  )}
                </div>

                {/* Actions */}
                <div className="pt-3 mt-3 border-t border-slate-800/80 flex items-center justify-between gap-2">
                  {editingId === item.id ? (
                    <div className="w-full space-y-2">
                      <div className="flex items-center gap-1">
                        {[1, 2, 3, 4, 5].map((st) => (
                          <button
                            key={st}
                            type="button"
                            onClick={() => setEditRating(st)}
                            className="p-1"
                          >
                            <Star
                              className={`w-4 h-4 ${
                                st <= editRating
                                  ? 'fill-amber-400 text-amber-400'
                                  : 'text-slate-600'
                              }`}
                            />
                          </button>
                        ))}
                      </div>
                      <input
                        type="text"
                        value={editNotes}
                        onChange={(e) => setEditNotes(e.target.value)}
                        placeholder="Sua crítica pessoal..."
                        className="w-full rounded-lg bg-slate-900 border border-slate-700 px-2.5 py-1 text-xs text-white"
                      />
                      <div className="flex gap-2">
                        <button
                          type="button"
                          onClick={async () => {
                            await onUpdateItem(
                              item.id,
                              WatchlistType.Watched,
                              editRating,
                              editNotes
                            );
                            setEditingId(null);
                          }}
                          className="px-3 py-1 rounded-lg bg-emerald-600 text-white text-xs font-bold"
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
                    <>
                      <button
                        type="button"
                        onClick={() => {
                          setEditingId(item.id);
                          setEditRating(item.personalRating || 5);
                          setEditNotes(item.personalNotes || '');
                        }}
                        className="inline-flex items-center gap-1.5 text-xs font-semibold text-emerald-400 hover:text-emerald-300"
                      >
                        <Sparkles className="w-3.5 h-3.5" />
                        {item.listType === WatchlistType.Watched
                          ? 'Editar Nota / Crítica'
                          : 'Marcar como Já Vi e Avaliar'}
                      </button>
                    </>
                  )}
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};
