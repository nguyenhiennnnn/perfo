import type { MovieStatus } from "@/types/enum";

export interface MovieInterestDto {
  movieId: number;
  title: string;
  posterUrl?: string | null;
  releaseDate?: string | null;
  status: MovieStatus;
  createdAt: string;
}

export interface RemoveMovieInterestResponse {
  movieId: number;
  removed: boolean;
}
