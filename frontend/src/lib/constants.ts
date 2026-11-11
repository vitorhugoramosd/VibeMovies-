import { SortOption } from '../types/api';

/** Streamings the user can mark as "Meus streamings" in the profile. */
export const PROFILE_PROVIDERS: string[] = [
  'Netflix',
  'Amazon Prime Video',
  'Max',
  'Disney+',
  'Globoplay',
  'Apple TV+',
  'Paramount+'
];

/** Streamings offered in the discover filter. */
export const FILTER_PROVIDERS: string[] = [...PROFILE_PROVIDERS, 'MUBI'];

export const RUNTIME_OPTIONS: { label: string; value: number | null }[] = [
  { label: 'Qualquer', value: null },
  { label: '≤ 90 min', value: 90 },
  { label: '≤ 120 min', value: 120 },
  { label: '≤ 150 min', value: 150 }
];

export const DECADE_OPTIONS: { label: string; value: number | null }[] = [
  { label: 'Qualquer década', value: null },
  { label: 'Anos 2020', value: 2020 },
  { label: 'Anos 2010', value: 2010 },
  { label: 'Anos 2000', value: 2000 },
  { label: 'Anos 90', value: 1990 },
  { label: 'Anos 80', value: 1980 },
  { label: 'Anos 70', value: 1970 }
];

export const MIN_RATING_OPTIONS: { label: string; value: number | null }[] = [
  { label: 'Qualquer nota', value: null },
  { label: 'Nota ≥ 6', value: 6 },
  { label: 'Nota ≥ 7', value: 7 },
  { label: 'Nota ≥ 8', value: 8 }
];

export const SORT_OPTIONS: { label: string; value: SortOption }[] = [
  { label: 'Mais populares', value: 'popularity.desc' },
  { label: 'Melhor avaliados', value: 'vote_average.desc' },
  { label: 'Mais recentes', value: 'primary_release_date.desc' },
  { label: 'Mais antigos', value: 'primary_release_date.asc' }
];

export const DEFAULT_CITIES = [
  'Joinville/SC',
  'Florianópolis/SC',
  'Curitiba/PR',
  'São Paulo/SP',
  'Porto Alegre/RS',
  'Rio de Janeiro/RJ'
];

// Sem pré-seleção: os ids de vibe do modo offline e da API são diferentes.
export const DEFAULT_VIBES: string[] = [];

/** Normalises provider names so "Apple TV" matches "Apple TV+", "HBO Max" matches "Max", etc. */
export function normalizeProvider(name: string): string {
  return name.toLowerCase().replace(/[^a-z0-9]/g, '');
}

export function providerMatches(providerName: string, wanted: string): boolean {
  const a = normalizeProvider(providerName);
  const b = normalizeProvider(wanted);
  if (!a || !b) return false;
  return a === b || a.includes(b) || b.includes(a);
}
