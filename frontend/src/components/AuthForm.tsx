import React, { useState } from 'react';
import { Loader2, LogIn } from 'lucide-react';
import { useAuth } from '../context/AuthContext';
import { useToast } from '../context/ToastContext';

interface AuthFormProps {
  onSuccess?: () => void;
  /** id for the heading, so a dialog can reference it with aria-labelledby. */
  titleId?: string;
}

const inputClass =
  'w-full rounded-xl bg-slate-900 border border-slate-700 px-3.5 py-2 text-sm text-white placeholder-slate-500 focus:outline-none focus:border-violet-500';

/** Login / register form shared by the AuthModal and the Diary view. */
export const AuthForm: React.FC<AuthFormProps> = ({ onSuccess, titleId = 'auth-form-title' }) => {
  const { login, register } = useAuth();
  const toast = useToast();
  const [isRegisterMode, setIsRegisterMode] = useState(false);
  const [name, setName] = useState('');
  const [email, setEmail] = useState('demo@vibemovies.app');
  const [password, setPassword] = useState('vibe1234');
  const [city, setCity] = useState('Joinville/SC');
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      if (isRegisterMode) {
        const auth = await register(name, email, password, city);
        toast(`Conta criada para ${auth.name}!`);
      } else {
        const auth = await login(email, password);
        toast(`Bem-vindo(a), ${auth.name}!`);
      }
      onSuccess?.();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Falha na autenticação.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="space-y-6">
      <div className="text-center space-y-2">
        <div className="w-12 h-12 rounded-2xl bg-violet-600/20 border border-violet-500/40 flex items-center justify-center mx-auto text-violet-400">
          <LogIn className="w-6 h-6" aria-hidden="true" />
        </div>
        <h2 id={titleId} className="font-display text-2xl font-bold text-white">
          {isRegisterMode ? 'Criar Conta no Diário' : 'Entrar no Diário de Bordo'}
        </h2>
        <p className="text-xs text-slate-400">Autenticação JWT persistida no banco de dados PostgreSQL / EF Core.</p>
      </div>

      {error && (
        <div role="alert" className="p-3 rounded-xl bg-red-500/10 border border-red-500/30 text-xs text-red-300">
          {error}
        </div>
      )}

      <form onSubmit={handleSubmit} className="space-y-4">
        {isRegisterMode && (
          <>
            <div>
              <label htmlFor="auth-name" className="text-xs font-semibold text-slate-300 block mb-1">
                Seu Nome
              </label>
              <input
                id="auth-name"
                type="text"
                required
                autoComplete="name"
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="Ex: Vitor Hugo"
                className={inputClass}
              />
            </div>
            <div>
              <label htmlFor="auth-city" className="text-xs font-semibold text-slate-300 block mb-1">
                Sua Cidade (para o Radar de Cinemas)
              </label>
              <input
                id="auth-city"
                type="text"
                value={city}
                onChange={(e) => setCity(e.target.value)}
                placeholder="Joinville/SC"
                className={inputClass}
              />
            </div>
          </>
        )}

        <div>
          <label htmlFor="auth-email" className="text-xs font-semibold text-slate-300 block mb-1">
            E-mail
          </label>
          <input
            id="auth-email"
            type="email"
            required
            autoComplete="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            className={inputClass}
            data-autofocus
          />
        </div>

        <div>
          <label htmlFor="auth-password" className="text-xs font-semibold text-slate-300 block mb-1">
            Senha
          </label>
          <input
            id="auth-password"
            type="password"
            required
            autoComplete={isRegisterMode ? 'new-password' : 'current-password'}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            className={inputClass}
          />
        </div>

        <button
          type="submit"
          disabled={submitting}
          className="w-full inline-flex items-center justify-center gap-2 py-2.5 px-4 rounded-xl bg-violet-600 hover:bg-violet-500 disabled:opacity-60 text-white font-bold text-sm shadow-lg shadow-violet-900/40 transition-all"
        >
          {submitting && <Loader2 className="w-4 h-4 animate-spin" aria-hidden="true" />}
          {isRegisterMode ? 'Cadastrar e Entrar' : 'Entrar com JWT'}
        </button>
      </form>

      <div className="pt-4 border-t border-slate-800 text-center">
        <button
          type="button"
          onClick={() => {
            setIsRegisterMode(!isRegisterMode);
            setError(null);
          }}
          className="text-xs text-violet-400 hover:text-violet-300 font-medium"
        >
          {isRegisterMode ? 'Já possui conta? Faça login' : 'Não tem conta? Cadastre-se gratuitamente'}
        </button>
      </div>
    </div>
  );
};
