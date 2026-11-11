import React from 'react';
import { Dices, Sparkles } from 'lucide-react';
import { VibeDefinition } from '../types/api';

interface VibeFilterBarProps {
  vibes: VibeDefinition[];
  selectedVibes: string[];
  onToggleVibe: (id: string) => void;
  onSurprise: () => void;
}

export const VibeFilterBar: React.FC<VibeFilterBarProps> = ({
  vibes,
  selectedVibes,
  onToggleVibe,
  onSurprise
}) => (
  <section
    aria-labelledby="vibe-hero-title"
    className="relative overflow-hidden rounded-3xl bg-gradient-to-br from-[#171332] via-[#12141F] to-[#0D101A] border border-violet-500/25 p-5 sm:p-8 shadow-2xl"
  >
    <div className="flex flex-col lg:flex-row lg:items-end justify-between gap-5">
      <div className="max-w-3xl space-y-3">
        <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full text-[11px] sm:text-xs font-bold bg-violet-500/20 text-violet-300 border border-violet-500/30">
          <Sparkles className="w-3.5 h-3.5 shrink-0" aria-hidden="true" />
          Descoberta Inteligente por Atmosfera & Tempo Disponível
        </div>
        <h1 id="vibe-hero-title" className="font-display text-2xl sm:text-4xl font-extrabold text-white leading-tight">
          Qual é a <span className="text-violet-400">vibe do filme</span> que você quer sentir hoje?
        </h1>
        <p className="text-xs sm:text-sm text-slate-300 leading-relaxed">
          Combine uma ou mais <strong>Vibes</strong> (convertidas em fórmulas de{' '}
          <code className="text-violet-300 bg-black/40 px-1.5 py-0.5 rounded">with_keywords</code>,{' '}
          <code className="text-violet-300 bg-black/40 px-1.5 py-0.5 rounded">primary_release_date</code> e{' '}
          <code className="text-violet-300 bg-black/40 px-1.5 py-0.5 rounded">with_runtime.lte</code> no backend
          C#), filtre pelos seus serviços de streaming no Brasil e salve no seu Diário.
        </p>
      </div>

      <button
        type="button"
        onClick={onSurprise}
        aria-keyshortcuts="s"
        className="group shrink-0 inline-flex items-center justify-center gap-2.5 px-5 py-3.5 rounded-2xl bg-gradient-to-r from-fuchsia-600 to-violet-600 hover:from-fuchsia-500 hover:to-violet-500 text-white font-display font-bold text-base shadow-xl shadow-fuchsia-950/50 transition-all hover:scale-[1.02]"
      >
        <Dices className="w-5 h-5 group-hover:rotate-12 transition-transform" aria-hidden="true" />
        Me surpreenda 🎲
        <kbd className="hidden sm:inline text-[10px] font-mono px-1.5 py-0.5 rounded bg-black/30 border border-white/20">
          S
        </kbd>
      </button>
    </div>

    <div
      role="group"
      aria-label="Vibes"
      className="mt-6 grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3 sm:gap-3.5"
    >
      {vibes.length === 0
        ? Array.from({ length: 6 }, (_, i) => <div key={i} className="h-36 rounded-2xl skeleton" aria-hidden="true" />)
        : vibes.map((vibe) => {
            const isSelected = selectedVibes.includes(vibe.id);
            return (
              <button
                key={vibe.id}
                type="button"
                onClick={() => onToggleVibe(vibe.id)}
                aria-pressed={isSelected}
                className={`text-left p-4 rounded-2xl border transition-all duration-200 flex flex-col justify-between gap-2.5 ${
                  isSelected
                    ? 'bg-violet-600/20 border-violet-500 shadow-lg shadow-violet-950/50 ring-1 ring-violet-400/50'
                    : 'bg-[#0E101A]/90 border-slate-800/90 hover:border-slate-700 hover:bg-[#141726]'
                }`}
              >
                <span className="block">
                  <span className="flex items-center justify-between gap-2 mb-1">
                    <span className="text-2xl" aria-hidden="true">
                      {vibe.emoji}
                    </span>
                    <span
                      className={`text-[10px] font-bold uppercase tracking-wider px-2 py-0.5 rounded-full ${
                        isSelected ? 'bg-violet-500 text-white' : 'bg-slate-800 text-slate-400'
                      }`}
                    >
                      {isSelected ? 'Vibe Ativa' : 'Selecionar'}
                    </span>
                  </span>
                  <span className="block font-display font-bold text-base text-white">{vibe.name}</span>
                  <span className="block text-[11px] font-semibold text-violet-300/90">{vibe.subtitle}</span>
                  <span className="block text-xs text-slate-400 mt-1 line-clamp-2">{vibe.description}</span>
                </span>

                <span className="flex flex-wrap gap-1 pt-2 border-t border-slate-800/80">
                  {vibe.sampleTags.map((tag) => (
                    <span key={tag} className="text-[10px] px-2 py-0.5 rounded bg-black/40 text-slate-300">
                      #{tag}
                    </span>
                  ))}
                </span>
              </button>
            );
          })}
    </div>
  </section>
);
