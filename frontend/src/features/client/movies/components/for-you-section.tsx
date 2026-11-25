import { LineSkeleton } from "@/components/common/skeleton-shapes";
import { MovieCard } from "@/features/client/movies/components/movie-card";
import { MOVIES_GRID_CLASS, MoviesGridSkeleton } from "@/features/client/movies/components/movies-grid";
import { useMovieRecommendations } from "@/features/client/movies/hooks/use-movies";
import { FOR_YOU_SECTION_LIMIT } from "@/config/default-constants";
import { cn } from "@/lib/utils";

interface ForYouSectionProps {
  excludeMovieId?: number;
  className?: string;
}

export const ForYouSection = ({ excludeMovieId, className }: ForYouSectionProps) => {
  const limit = excludeMovieId ? FOR_YOU_SECTION_LIMIT + 1 : FOR_YOU_SECTION_LIMIT;
  const query = useMovieRecommendations(limit);

  if (query.isError) return null;

  if (query.isLoading) {
    return (
      <section className={cn("space-y-5", className)} aria-busy="true">
        <div className="border-b border-border pb-3">
          <LineSkeleton className="h-7 w-40" />
        </div>
        <MoviesGridSkeleton />
      </section>
    );
  }

  const items = (query.data?.items ?? [])
    .filter((item) => item.movie.movieId !== excludeMovieId)
    .slice(0, FOR_YOU_SECTION_LIMIT);
  if (items.length === 0) return null;

  const title = query.data?.personalized ? "Dành cho bạn" : "Phim được xem nhiều";

  return (
    <section className={className}>
      <div className="border-b border-border pb-3">
        <h2 className="t-h2 text-foreground">{title}</h2>
      </div>
      <div className={cn("mt-5", MOVIES_GRID_CLASS)}>
        {items.map((item) => (
          <MovieCard key={item.movie.movieId} movie={item.movie} reason={item.reason} />
        ))}
      </div>
    </section>
  );
};
