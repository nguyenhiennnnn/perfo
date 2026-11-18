import { Link } from "react-router";
import { Skeleton } from "@/components/ui/skeleton";
import { useSchedule } from "@/features/client/showtimes/hooks/use-showtimes";
import { getBusinessToday } from "@/lib/business-time";
import { formatShowtimeHour } from "@/lib/formatters";
import { ROUTES } from "@/config/routes";

const MAX_MOVIES = 6;
const MAX_TIMES_PER_MOVIE = 6;

export const CinemaExperienceSection = () => {
  const today = getBusinessToday();
  const { data, isLoading, isError } = useSchedule(today);
  const movies = (data?.movies ?? []).filter((movie) => movie.showtimes.length > 0).slice(0, MAX_MOVIES);

  if (!isLoading && (isError || movies.length === 0)) return null;

  return (
    <section>
      <div className="flex items-end justify-between gap-3 border-b border-border pb-3">
        <h2 className="t-h2 text-foreground">Lịch chiếu hôm nay</h2>
        <Link to={ROUTES.schedule} className="shrink-0 text-sm font-medium text-primary hover:underline">
          Xem toàn bộ lịch chiếu
        </Link>
      </div>

      <div className="mt-5 flex flex-col gap-3" aria-busy={isLoading || undefined}>
        {isLoading
          ? Array.from({ length: 3 }, (_, i) => <Skeleton key={i} className="h-16 w-full cut-md" />)
          : movies.map((movie) => {
              const times = [...movie.showtimes]
                .sort((a, b) => a.startTime.localeCompare(b.startTime))
                .slice(0, MAX_TIMES_PER_MOVIE);
              return (
                <div
                  key={movie.movieId}
                  className="flex flex-col gap-3 cut-md bg-card p-4 sm:flex-row sm:items-center sm:justify-between"
                >
                  <Link
                    to={ROUTES.movies.detail(movie.movieId)}
                    className="t-h3 line-clamp-1 text-foreground hover:underline"
                  >
                    {movie.movieTitle}
                  </Link>
                  <ul className="flex flex-wrap gap-2">
                    {times.map((slot) => (
                      <li key={slot.showtimeId} className="t-code cut-xs bg-secondary px-3 py-1.5 text-sm text-foreground">
                        {formatShowtimeHour(slot.startTime)}
                      </li>
                    ))}
                  </ul>
                </div>
              );
            })}
      </div>
    </section>
  );
};
