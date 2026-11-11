import React from 'react';
import { AlertTriangle, Info, Sparkles, X } from 'lucide-react';

interface ToastProps {
  message: string | null;
  tone: 'success' | 'error' | 'info';
  toastKey: number;
  onDismiss: () => void;
}

const TONES = {
  success: 'bg-violet-600 border-violet-400/40 shadow-violet-950/70',
  error: 'bg-red-600 border-red-400/40 shadow-red-950/70',
  info: 'bg-slate-800 border-slate-600 shadow-black/60'
};

export const Toast: React.FC<ToastProps> = ({ message, tone, toastKey, onDismiss }) => {
  const Icon = tone === 'error' ? AlertTriangle : tone === 'info' ? Info : Sparkles;
  return (
    <div
      role="status"
      aria-live="polite"
      className="fixed bottom-4 right-4 left-4 sm:left-auto z-[60] flex justify-end pointer-events-none"
    >
      {message && (
        <div
          key={toastKey}
          className={`pointer-events-auto flex items-center gap-2.5 max-w-sm px-4 py-3 rounded-2xl text-white text-xs sm:text-sm font-semibold shadow-2xl border animate-slide-up ${TONES[tone]}`}
        >
          <Icon className="w-4 h-4 shrink-0" aria-hidden="true" />
          <span className="flex-1">{message}</span>
          <button
            type="button"
            onClick={onDismiss}
            aria-label="Fechar notificação"
            className="p-1 -mr-1 rounded-lg hover:bg-white/15"
          >
            <X className="w-3.5 h-3.5" aria-hidden="true" />
          </button>
        </div>
      )}
    </div>
  );
};
