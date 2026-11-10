export interface VibeDefinition {
  id: string;
  name: string;
  subtitle: string;
  description: string;
  emoji: string;
  accentColor: string;
  tmdbKeywordIds: number[];
  suggestedGenreIds: number[];
  releaseDateGte?: string;
  releaseDateLte?: string;
  minVoteAverage?: number;
  maxRuntimeMinutes?: number;
  sampleTags: string[];
}

export interface GenreDto {
  id: number;
  name: string;
  icon: string;
}

export interface StreamingProviderBadgeDto {
  providerId: number;
  providerName: string;
  logoUrl: string;
  displayPriority: number;
}

export interface WatchProvidersDto {
  region: string;
  tmdbWatchLink?: string;
  flatrate: StreamingProviderBadgeDto[];
  rent: StreamingProviderBadgeDto[];
  buy: StreamingProviderBadgeDto[];
}

export interface MovieCardDto {
  tmdbId: number;
  title: string;
  originalTitle: string;
  overview: string;
  posterUrl: string;
  backdropUrl: string;
  releaseDate: string;
  releaseYear?: number;
  runtimeMinutes?: number;
  voteAverage: number;
  voteCount: number;
  genres: string[];
  genreIds: number[];
  matchedVibes: string[];
  keywords: string[];
  director?: string;
  cast: string[];
  watchProviders: WatchProvidersDto;
  vibeMatchScore: number;
}

export interface DiscoverResultDto {
  page: number;
  totalResults: number;
  totalPages: number;
  usingLiveTmdbApi: boolean;
  appliedFormulaSummary: string;
  appliedTmdbQueryParameters: Record<string, string>;
  movies: MovieCardDto[];
}

export interface CinemaSessionDto {
  time: string;
  roomType: string;
  audioType: string;
  ticketUrl: string;
}

export interface CinemaVenueShowtimesDto {
  cinemaChain: string;
  cinemaName: string;
  address: string;
  sessions: CinemaSessionDto[];
}

export interface NowPlayingMovieWithShowtimesDto {
  movie: MovieCardDto;
  ageRating: string;
  venues: CinemaVenueShowtimesDto[];
}

export interface CinemaRadarResponseDto {
  city: string;
  state: string;
  scrapedAt: string;
  scraperSource: string;
  moviesInTheaters: NowPlayingMovieWithShowtimesDto[];
}

export enum WatchlistType {
  WantToWatch = 1,
  Watched = 2
}

export interface WatchlistItem {
  id: string;
  userId: string;
  tmdbMovieId: number;
  title: string;
  originalTitle: string;
  posterPath: string;
  backdropPath: string;
  releaseYear?: number;
  overview: string;
  listType: WatchlistType;
  personalRating?: number;
  personalNotes?: string;
  watchedAt?: string;
  addedAt: string;
  genresCsv: string;
  runtimeMinutes?: number;
  tmdbVoteAverage: number;
}

export interface WatchlistStatsDto {
  totalWantToWatch: number;
  totalWatched: number;
  totalMinutesWatched: number;
  averagePersonalRating: number;
  favoriteGenre: string;
}

export interface AuthResponseDto {
  token: string;
  userId: string;
  name: string;
  email: string;
  preferredRegion: string;
  defaultCity: string;
}
