import { useMutation, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import { moviesApi } from "@/features/client/movies/api/movies-api";
import { MovieStatus } from "@/types/enum";
import { roomRentalsApi } from "../api/room-rentals-api";
import {
  ROOM_RENTAL_MOVIE_OPTIONS_LIMIT,
  ROOM_RENTAL_OPTIONS_STALE_MS,
} from "../constants";
import type {
  CreateRoomRentalRequestRequest,
  RoomRentalSelectOption,
} from "../types";

export function useCreateRoomRentalRequest() {
  return useMutation({
    meta: { skipErrorToast: true, skipSuccessToast: true },
    mutationFn: (body: CreateRoomRentalRequestRequest) => roomRentalsApi.create(body),
  });
}

export function useRentalMovieOptions() {
  return useQuery({
    queryKey: queryKeys.roomRentals.movieOptions(),
    queryFn: async () => {
      const [nowShowing, comingSoon] = await Promise.all([
        moviesApi.getMovies({
          status: MovieStatus.NowShowing,
          limit: ROOM_RENTAL_MOVIE_OPTIONS_LIMIT,
        }),
        moviesApi.getMovies({
          status: MovieStatus.ComingSoon,
          limit: ROOM_RENTAL_MOVIE_OPTIONS_LIMIT,
        }),
      ]);
      return [...nowShowing.data.items, ...comingSoon.data.items];
    },
    select: (movies): RoomRentalSelectOption[] =>
      movies.map((movie) => ({ value: String(movie.movieId), label: movie.title })),
    staleTime: ROOM_RENTAL_OPTIONS_STALE_MS,
    meta: { skipErrorToast: true },
  });
}

export function useRentalScreenTypes() {
  return useQuery({
    queryKey: queryKeys.catalog.screenTypes(),
    queryFn: () => roomRentalsApi.getScreenTypes(),
    select: (res): RoomRentalSelectOption[] =>
      res.data.items.map((item) => ({
        value: String(item.screenTypeId),
        label: item.name,
      })),
    staleTime: ROOM_RENTAL_OPTIONS_STALE_MS,
    meta: { skipErrorToast: true },
  });
}
