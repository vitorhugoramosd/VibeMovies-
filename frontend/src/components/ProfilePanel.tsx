import React, { useEffect, useState } from 'react';
import { Check, Loader2, LogOut, MapPin, Tv, UserRound } from 'lucide-react';
import { Modal } from './Modal';
import { useAuth } from '../context/AuthContext';
import { useToast } from '../context/ToastContext';
import { api } from '../services/api';
import { DEFAULT_CITIES, PROFILE_PROVIDERS } from '../lib/constants';

interface ProfilePanelProps {
  open: boolean;
  onClose: () => void;
}

export const ProfilePanel: React.FC<ProfilePanelProps> = ({ open, onClose }) => {
  const { user, updateProfile, logout } = useAuth();
  const toast = useToast();
  const [name, setName] = useState('');
  const [city, setCity] = useState('Joinville/SC');
  const [providers, setProviders] = useState<string[]>([]);
  const [cities, setCities] = useState<string[]>(DEFAULT_CITIES);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!open || !user) return;
    setName(user.name);
    setCity(user.defaultCity || 'Joinville/SC');
    setProviders(user.myProviders ?? []);
    setError(null);
    api.getCinemaCities().then((list) => setCities(list.length ? list : DEFAULT_CITIES));
  }, [open, user]);

  if (!user) return null;

  const cityOptions = cities.some((c) => c.toLowerCase() === city.toLowerCase()) ? cities : [city, ...cities];

  const toggleProvider = (p: string) =>
    setProviders((prev) => (prev.includes(p) ? prev.filter((x) => x !== p) : [...prev, p]));

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim()) {
      setError('Informe seu nome.');
      return;
    }
    setSaving(true);
    setError(null);
    try {
      await updateProfile({
        name: name.trim(),
        defaultCity: city,
        preferredRegion: user.preferredRegion || 'BR',
        myProviders: providers
      });
      toast('Perfil atualizado!');
      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível salvar o perfil.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal open={open} onClose={onClose} labelledBy="profile-title" panelClassName="w-full max-w-lg p-6 sm:p-7">
      <form onSubmit={handleSubmit} className="space-y-6">
        <div className="flex items-center gap-3 pr-10">
          <div className="w-11 h-11 rounded-2xl bg-gradient-to-br from-fuchsia-600 to-violet-600 flex items-center justify-center">
            <UserRound className="w-5 h-5 text-white" aria-hidden="true" />
          </div>
          <div className="min-w-0">
            <h2 id="profile-title" className="font-display font-bold text-xl text-white">
              Perfil & Preferências
            </h2>
            <p className="text-xs text-slate-400 truncate">{user.email}</p>
          </div>
        </div>

        {error && (
          <div role="alert" className="p-3 rounded-xl bg-red-500/10 border border-red-500/30 text-xs text-red-300">
            {error}
          </div>
        )}

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div>
            <label htmlFor="profile-name" className="text-xs font-semibold text-slate-300 block mb-1">
              Nome
            </label>
            <input
              id="profile-name"
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              autoComplete="name"
              data-autofocus
              className="w-full rounded-xl bg-slate-900 border border-slate-700 px-3.5 py-2 text-sm text-white focus:outline-none focus:border-violet-500"
            />
          </div>
          <div>
            <label htmlFor="profile-city" className="text-xs font-semibold text-slate-300 mb-1 flex items-center gap-1">
              <MapPin className="w-3.5 h-3.5 text-violet-400" aria-hidden="true" />
              Cidade padrão (Radar)
            </label>
            <select
              id="profile-city"
              value={city}
              onChange={(e) => setCity(e.target.value)}
              className="w-full rounded-xl bg-slate-900 border border-slate-700 px-3 py-2 text-sm text-white focus:outline-none focus:border-violet-500"
            >
              {cityOptions.map((c) => (
                <option key={c} value={c}>
                  {c}
                </option>
              ))}
            </select>
          </div>
        </div>

        <fieldset>
          <legend className="text-xs font-semibold text-slate-300 mb-2 flex items-center gap-1.5">
            <Tv className="w-3.5 h-3.5 text-emerald-400" aria-hidden="true" />
            Meus streamings
          </legend>
          <div className="flex flex-wrap gap-2">
            {PROFILE_PROVIDERS.map((p) => {
              const active = providers.includes(p);
              return (
                <button
                  key={p}
                  type="button"
                  onClick={() => toggleProvider(p)}
                  aria-pressed={active}
                  className={`inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl text-xs font-semibold transition-all ${
                    active
                      ? 'bg-emerald-600 text-white shadow-md shadow-emerald-950/50'
                      : 'bg-slate-900 text-slate-300 hover:bg-slate-800 border border-slate-700'
                  }`}
                >
                  {active && <Check className="w-3.5 h-3.5" aria-hidden="true" />}
                  {p}
                </button>
              );
            })}
          </div>
          <p className="text-[11px] text-slate-500 mt-2">
            Usado pelo filtro &ldquo;Só nos meus streamings&rdquo; e pelas recomendações.
          </p>
        </fieldset>

        <div className="flex flex-col-reverse sm:flex-row sm:items-center justify-between gap-3 pt-4 border-t border-slate-800">
          <button
            type="button"
            onClick={() => {
              logout();
              toast('Sessão encerrada.', 'info');
              onClose();
            }}
            className="inline-flex items-center justify-center gap-1.5 px-3 py-2 rounded-xl text-xs font-semibold text-red-300 hover:bg-red-500/10 border border-transparent hover:border-red-500/30"
          >
            <LogOut className="w-3.5 h-3.5" aria-hidden="true" />
            Sair da conta
          </button>
          <div className="flex gap-2 justify-end">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-xs font-semibold text-slate-300"
            >
              Cancelar
            </button>
            <button
              type="submit"
              disabled={saving}
              className="inline-flex items-center gap-1.5 px-4 py-2 rounded-xl bg-violet-600 hover:bg-violet-500 disabled:opacity-60 text-xs font-bold text-white"
            >
              {saving && <Loader2 className="w-3.5 h-3.5 animate-spin" aria-hidden="true" />}
              Salvar perfil
            </button>
          </div>
        </div>
      </form>
    </Modal>
  );
};
