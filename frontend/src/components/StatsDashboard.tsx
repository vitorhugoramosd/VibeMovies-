import React from 'react';
import { BarChart3, CheckCircle2, Clock, Film, Star, Trophy } from 'lucide-react';
import { WatchlistStatsDto } from '../types/api';

interface StatsDashboardProps {
  stats: WatchlistStatsDto;
}

const Kpi: React.FC<{
  icon: React.ReactNode;
  label: string;
  value: React.ReactNode;
  accent: string;
}> = ({ icon, label, value, accent }) => (
  <div className="p-4 sm:p-5 rounded-2xl bg-[#12141F] border border-slate-800 flex items-center gap-3 sm:gap-4 min-w-0">
    <div className={`p-2.5 sm:p-3 rounded-xl border shrink-0 ${accent}`}>{icon}</div>
    <div className="min-w-0">
      <span className="text-[11px] sm:text-xs text-slate-400 block">{label}</span>
      <span className="font-display text-xl sm:text-2xl font-extrabold text-white block truncate">{value}</span>
    </div>
  </div>
);

const Card: React.FC<{ title: string; children: React.ReactNode; className?: string }> = ({
  title,
  children,
  className = ''
}) => (
  <section className={`p-4 sm:p-5 rounded-2xl bg-[#12141F] border border-slate-800 min-w-0 ${className}`}>
    <h3 className="text-xs font-bold uppercase tracking-wider text-slate-400 mb-4">{title}</h3>
    {children}
  </section>
);

const EmptyChart: React.FC = () => (
  <p className="text-xs text-slate-500 py-6 text-center">Registre filmes em &ldquo;Já Vi&rdquo; para ver este gráfico.</p>
);

/** Diary statistics: KPI tiles + pure CSS/SVG charts. */
export const StatsDashboard: React.FC<StatsDashboardProps> = ({ stats }) => {
  const genres = (stats.genreBreakdown ?? []).slice(0, 6);
  const maxGenre = Math.max(1, ...genres.map((g) => g.count));

  const ratingEntries = [5, 4, 3, 2, 1].map((star) => ({
    star,
    count: Number(stats.ratingDistribution?.[String(star)] ?? 0)
  }));
  const maxRating = Math.max(1, ...ratingEntries.map((r) => r.count));
  const totalRated = ratingEntries.reduce((a, r) => a + r.count, 0);

  const months = stats.watchedPerMonth ?? [];
  const maxMonth = Math.max(1, ...months.map((m) => m.count));
  const hours = stats.totalHoursWatched ?? (stats.totalMinutesWatched || 0) / 60;

  // SVG column chart geometry
  const chartW = 360;
  const chartH = 120;
  const gap = 6;
  const colW = months.length ? (chartW - gap * (months.length - 1)) / months.length : 0;

  return (
    <div className="space-y-4">
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-3 sm:gap-4">
        <Kpi
          icon={<CheckCircle2 className="w-5 h-5 sm:w-6 sm:h-6" aria-hidden="true" />}
          accent="bg-emerald-500/10 text-emerald-400 border-emerald-500/20"
          label="Filmes vistos"
          value={stats.totalWatched}
        />
        <Kpi
          icon={<Clock className="w-5 h-5 sm:w-6 sm:h-6" aria-hidden="true" />}
          accent="bg-sky-500/10 text-sky-400 border-sky-500/20"
          label="Horas de tela"
          value={`${hours.toLocaleString('pt-BR', { maximumFractionDigits: 1 })} h`}
        />
        <Kpi
          icon={<Star className="w-5 h-5 sm:w-6 sm:h-6 fill-amber-400" aria-hidden="true" />}
          accent="bg-amber-500/10 text-amber-400 border-amber-500/20"
          label="Nota média"
          value={stats.averagePersonalRating ? `${stats.averagePersonalRating.toLocaleString('pt-BR')} ★` : '—'}
        />
        <Kpi
          icon={<Trophy className="w-5 h-5 sm:w-6 sm:h-6" aria-hidden="true" />}
          accent="bg-violet-500/10 text-violet-400 border-violet-500/20"
          label="Gênero favorito"
          value={<span className="text-base sm:text-lg">{stats.favoriteGenre || '—'}</span>}
        />
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
        <Card title="Gêneros mais vistos">
          {genres.length === 0 ? (
            <EmptyChart />
          ) : (
            <ul className="space-y-2.5">
              {genres.map((g) => (
                <li key={g.name} className="text-xs">
                  <div className="flex justify-between gap-2 mb-1">
                    <span className="text-slate-200 font-medium truncate">{g.name}</span>
                    <span className="text-slate-400 tabular-nums">{g.count}</span>
                  </div>
                  <div className="h-2 rounded-full bg-slate-800 overflow-hidden" aria-hidden="true">
                    <div
                      className="h-full rounded-full bg-gradient-to-r from-violet-600 to-fuchsia-500"
                      style={{ width: `${(g.count / maxGenre) * 100}%` }}
                    />
                  </div>
                </li>
              ))}
            </ul>
          )}
        </Card>

        <Card title="Distribuição das suas notas">
          {totalRated === 0 ? (
            <EmptyChart />
          ) : (
            <ul className="space-y-2">
              {ratingEntries.map((r) => (
                <li key={r.star} className="flex items-center gap-2 text-xs">
                  <span className="w-14 shrink-0 text-amber-400 font-semibold tracking-tight" aria-label={`${r.star} estrelas`}>
                    {'★'.repeat(r.star)}
                    <span className="text-slate-700">{'★'.repeat(5 - r.star)}</span>
                  </span>
                  <div className="flex-1 h-2.5 rounded-full bg-slate-800 overflow-hidden" aria-hidden="true">
                    <div className="h-full rounded-full bg-amber-400" style={{ width: `${(r.count / maxRating) * 100}%` }} />
                  </div>
                  <span className="w-6 text-right text-slate-400 tabular-nums">{r.count}</span>
                </li>
              ))}
            </ul>
          )}
        </Card>

        <Card title="Filmes por mês (12 meses)">
          {months.every((m) => m.count === 0) ? (
            <EmptyChart />
          ) : (
            <figure>
              <svg
                viewBox={`0 0 ${chartW} ${chartH + 22}`}
                className="w-full h-auto"
                role="img"
                aria-label={`Filmes vistos por mês: ${months.map((m) => `${m.name} ${m.count}`).join(', ')}`}
              >
                {[0.5, 1].map((f) => (
                  <line
                    key={f}
                    x1={0}
                    x2={chartW}
                    y1={chartH - chartH * f}
                    y2={chartH - chartH * f}
                    stroke="#1E2235"
                    strokeDasharray="3 4"
                  />
                ))}
                {months.map((m, i) => {
                  const h = m.count === 0 ? 2 : Math.max(6, (m.count / maxMonth) * (chartH - 14));
                  const x = i * (colW + gap);
                  return (
                    <g key={`${m.name}-${i}`}>
                      <title>{`${m.name}: ${m.count} filme${m.count === 1 ? '' : 's'}`}</title>
                      <rect
                        x={x}
                        y={chartH - h}
                        width={colW}
                        height={h}
                        rx={3}
                        fill={m.count === 0 ? '#1E2235' : i === months.length - 1 ? '#D946EF' : '#7C3AED'}
                      />
                      {m.count > 0 && (
                        <text x={x + colW / 2} y={chartH - h - 4} textAnchor="middle" fontSize="10" fill="#CBD5E1">
                          {m.count}
                        </text>
                      )}
                      <text x={x + colW / 2} y={chartH + 15} textAnchor="middle" fontSize="9" fill="#64748B">
                        {m.name.split('/')[0]}
                      </text>
                    </g>
                  );
                })}
              </svg>
            </figure>
          )}
        </Card>
      </div>

      <p className="flex items-center gap-1.5 text-[11px] text-slate-500">
        <BarChart3 className="w-3.5 h-3.5" aria-hidden="true" />
        {stats.totalWantToWatch} filme{stats.totalWantToWatch === 1 ? '' : 's'} na fila &ldquo;Quero Ver&rdquo;
        <Film className="w-3.5 h-3.5 ml-2" aria-hidden="true" />
        {Math.floor((stats.totalMinutesWatched || 0) / 60)}h {(stats.totalMinutesWatched || 0) % 60}m no total
      </p>
    </div>
  );
};
