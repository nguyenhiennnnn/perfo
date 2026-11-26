import { apiDelete, apiGet, apiPost } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse } from "@/types/api";
import type { MovieInterestDto, RemoveMovieInterestResponse } from "../types";

export const movieInterestsApi = {
  getMine: () => apiGet<ApiResponse<MovieInterestDto[]>>(ENDPOINTS.movieInterests.root),

  add: (movieId: number) =>
    apiPost<ApiResponse<MovieInterestDto>>(ENDPOINTS.movieInterests.byMovie(movieId)),

  remove: (movieId: number) =>
    apiDelete<ApiResponse<RemoveMovieInterestResponse>>(ENDPOINTS.movieInterests.byMovie(movieId)),
};
