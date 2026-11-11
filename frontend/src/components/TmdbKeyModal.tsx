import React, { useEffect, useState } from 'react';
import { KeyRound } from 'lucide-react';
import { Modal } from './Modal';
import { getStoredTmdbKey, setStoredTmdbKey } from '../services/api';

interface TmdbKeyModalProps {
  open: boolean;
  onClose: () => void;
  onSaved: (hasKey: boolean) => void;
}

export const TmdbKeyModal: React.FC<TmdbKeyModalProps> = ({ open, onClose, onSaved }) => {
  const [value, setValue] = useState('');

  useEffect(() => {
    if (open) setValue(getStoredTmdbKey());
  }, [open]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setStoredTmdbKey(value);
    onSaved(Boolean(value.trim()));
    onClose();
  };

  return (
    <Modal open={open} onClose={onClose} labelledBy="tmdb-key-title" panelClassName="w-full max-w-md p-6">
      <div className="space-y-4">
        <div className="flex items-center gap-2 pr-10">
          <KeyRound className="w-5 h-5 text-amber-400 shrink-0" aria-hidden="true" />
          <h2 id="tmdb-key-title" className="font-display font-bold text-lg text-white">
            Chave da API TMDB (Opcional)
          </h2>
        </div>

        <p className="text-xs text-slate-400 leading-relaxed">
          O backend <strong>VibeMovies.Api (.NET 8)</strong> já inclui um catálogo curado completo com dados reais de
          streaming no Brasil (JustWatch/TMDB). Se desejar consultar a API do TMDB ao vivo em tempo real, insira sua
          chave v3 abaixo:
        </p>

        <form onSubmit={handleSubmit} className="space-y-4">
          <label htmlFor="tmdb-key-input" className="sr-only">
            Chave v3 da API TMDB
          </label>
          <input
            id="tmdb-key-input"
            type="text"
            value={value}
            onChange={(e) => setValue(e.target.value)}
            placeholder="Cole sua TMDB v3 API Key (ou deixe em branco)..."
            autoComplete="off"
            spellCheck={false}
            data-autofocus
            className="w-full rounded-xl bg-slate-900 border border-slate-700 px-3.5 py-2.5 text-xs text-white focus:outline-none focus:border-violet-500"
          />
          <div className="flex justify-end gap-2">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-xs font-semibold text-slate-300"
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
    </Modal>
  );
};
