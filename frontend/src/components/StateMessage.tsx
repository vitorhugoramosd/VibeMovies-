import React from 'react';
import { AlertTriangle, Film, RotateCcw } from 'lucide-react';

interface StateMessageProps {
  tone?: 'empty' | 'error';
  title: string;
  description?: React.ReactNode;
  actionLabel?: string;
  onAction?: () => void;
  icon?: React.ReactNode;
  compact?: boolean;
}

/** Shared empty / error state block. */
export const StateMessage: React.FC<StateMessageProps> = ({
  tone = 'empty',
  title,
  description,
  actionLabel,
  onAction,
  icon,
  compact = false
}) => (
  <div
    role={tone === 'error' ? 'alert' : undefined}
    className={`text-center rounded-3xl border space-y-3 ${compact ? 'p-6' : 'p-8 sm:p-12'} ${
      tone === 'error' ? 'bg-red-950/20 border-red-500/30' : 'bg-[#12141F] border-slate-800'
    }`}
  >
    <div className="mx-auto w-fit">
      {icon ??
        (tone === 'error' ? (
          <AlertTriangle className="w-10 h-10 text-red-400" aria-hidden="true" />
        ) : (
          <Film className="w-10 h-10 text-slate-600" aria-hidden="true" />
        ))}
    </div>
    <h3 className="font-display text-lg font-bold text-white">{title}</h3>
    {description && <p className="text-xs text-slate-400 max-w-md mx-auto">{description}</p>}
    {actionLabel && onAction && (
      <button
        type="button"
        onClick={onAction}
        className={`inline-flex items-center gap-1.5 px-4 py-2 rounded-xl text-white text-xs font-bold ${
          tone === 'error' ? 'bg-red-600 hover:bg-red-500' : 'bg-violet-600 hover:bg-violet-500'
        }`}
      >
        {tone === 'error' && <RotateCcw className="w-3.5 h-3.5" aria-hidden="true" />}
        {actionLabel}
      </button>
    )}
  </div>
);
