import { Film } from "lucide-react";
import { MovieCard } from "@/features/client/movies/components/movie-card";
import { MovieCardSkeleton } from "@/features/client/movies/components/movie-card-skeleton";
import { EmptyState } from "@/components/common/empty-state";
import { InfiniteScrollTrigger } from "@/components/common/infinite-scroll-trigger";
import { formatNumber } from "@/lib/formatters";
import { SKELETON_ROW_COUNT } from "@/config/default-constants";
import type { MovieListItemDto } from "@/features/client/movies/types";

interface MoviesGridProps {
  movies: MovieListItemDto[];
  totalCount: number | null;
  hasNextPage: boolean | undefined;
  isFetchingNextPage: boolean;
  fetchNextPage: () => void;
  hasActiveFilters?: boolean;
  onResetFilters?: () => void;
}

export const MOVIES_GRID_CLASS = "grid grid-cols-2 gap-4 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5";

export const MoviesGridSkeleton = () => (
  <div aria-busy="true" className={MOVIES_GRID_CLASS}>
    {Array.from({ length: SKELETON_ROW_COUNT }, (_, index) => (
      <MovieCardSkeleton key={index} />
    ))}
  </div>
);

export const MoviesGrid = ({
  movies,
  totalCount,
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
    <div className="space-y-4">
      <p aria-live="polite" className="text-sm text-muted-foreground">
        {formatNumber(totalCount ?? movies.length)} phim{hasActiveFilters ? " phù hợp" : ""}
      </p>
      <div className={MOVIES_GRID_CLASS}>
        {movies.map((movie) => (
          <MovieCard key={movie.movieId} movie={movie} />
        ))}
      </div>
      <InfiniteScrollTrigger
        hasNextPage={hasNextPage}
        isFetchingNextPage={isFetchingNextPage}
        fetchNextPage={fetchNextPage}
        endMessage={<p className="py-6 text-center text-sm text-muted-foreground">Đã hết danh sách</p>}
      />
    </div>
  );
};
