import { useMutation, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import { useAuthRole, useIsAuthenticated } from "@/stores/auth-store";
import { Role } from "@/types/enum";
import { movieInterestsApi } from "../api/movie-interests-api";
import type { MovieInterestDto } from "../types";

const toInterestedIds = (res: { data: MovieInterestDto[] }) =>
  new Set(res.data.map((item) => item.movieId));

export function useMyMovieInterests() {
  const isAuthenticated = useIsAuthenticated();
  const role = useAuthRole();

  return useQuery({
    queryKey: queryKeys.movieInterests.mine(),
    queryFn: () => movieInterestsApi.getMine(),
    enabled: isAuthenticated && role === Role.Customer,
    retry: false,
    meta: { skipErrorToast: true },
    select: toInterestedIds,
  });
}

export function useAddMovieInterest() {
  return useMutation({
    meta: { invalidates: "movieInterestChanged" },
    mutationFn: (movieId: number) => movieInterestsApi.add(movieId),
  });
}

export function useRemoveMovieInterest() {
  return useMutation({
    meta: { invalidates: "movieInterestChanged" },
    mutationFn: (movieId: number) => movieInterestsApi.remove(movieId),
  });
}
