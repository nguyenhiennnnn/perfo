import { Film } from "lucide-react";
import { MovieCard } from "@/features/client/movies/components/movie-card";
import { MovieCardSkeleton } from "@/features/client/movies/components/movie-card-skeleton";
import { EmptyState } from "@/components/common/empty-state";
import { InfiniteScrollTrigger } from "@/components/common/infinite-scroll-trigger";
import type { MovieListItemDto } from "@/features/client/movies/types";

interface MoviesGridProps {
  movies: MovieListItemDto[];
  hasNextPage: boolean | undefined;
  isFetchingNextPage: boolean;
  fetchNextPage: () => void;
  hasActiveFilters?: boolean;
  onResetFilters?: () => void;
}

const MOVIES_GRID_CLASS = "grid grid-cols-2 gap-4 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5";

export const MoviesGridSkeleton = () => (
  <div className={MOVIES_GRID_CLASS}>
    {Array.from({ length: 5 }).map((_, i) => (
      <MovieCardSkeleton key={i} />
    ))}
  </div>
);

export const MoviesGrid = ({
  movies,
  hasNextPage,
  isFetchingNextPage,
  fetchNextPage,
  hasActiveFilters,
  onResetFilters,
}: MoviesGridProps) => {
  if (movies.length === 0) {
    return hasActiveFilters ? (
      <EmptyState variant="filtered" title="Không có phim phù hợp" onClearFilters={onResetFilters} />
    ) : (
      <EmptyState
        icon={Film}
        title="Chưa có phim nào"
        description="Phim đang chiếu và sắp chiếu sẽ hiện ở đây khi rạp cập nhật lịch."
      />
    );
  }

  return (
    <div className="space-y-6">
      <div className={MOVIES_GRID_CLASS}>
        {movies.map((movie) => (
          <MovieCard key={movie.movieId} movie={movie} />
        ))}
      </div>
      <InfiniteScrollTrigger hasNextPage={hasNextPage} isFetchingNextPage={isFetchingNextPage} fetchNextPage={fetchNextPage} />
    </div>
  );
};
