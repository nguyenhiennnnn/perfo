import { Link } from "react-router";
import { Ticket } from "lucide-react";
import { SkeletonList } from "@/components/common/skeleton-list";
import { useBookShowtime } from "@/features/client/bookings/hooks/use-book-showtime";
import { useSchedule } from "@/features/client/showtimes/hooks/use-showtimes";
import { getBusinessToday } from "@/lib/business-time";
import { formatShowtimeHour } from "@/lib/formatters";
import { cn } from "@/lib/utils";
import { ROUTES } from "@/config/routes";

const MAX_MOVIES = 6;
const MAX_TIMES_PER_MOVIE = 6;

export const CinemaExperienceSection = () => {
  const today = getBusinessToday();
  const { bookShowtime, isReady } = useBookShowtime();
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
          ? <SkeletonList count={3} className="h-16 w-full cut-md" />
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
                    className="flex min-w-0 items-center gap-3 text-foreground hover:underline"
                  >
                    <span className="aspect-2/3 w-12 shrink-0 overflow-hidden cut-xs bg-muted">
                      {movie.moviePosterUrl ? (
                        <img src={movie.moviePosterUrl} alt="" className="h-full w-full object-cover" />
                      ) : (
                        <span className="flex h-full w-full items-center justify-center text-muted-foreground">
                          <Ticket className="size-4" />
                        </span>
                      )}
                    </span>
                    <span className="t-h3 line-clamp-2">{movie.movieTitle}</span>
                  </Link>
                  <ul className="flex flex-wrap gap-2">
                    {times.map((slot) => {
                      const soldOut = slot.availableSeats === 0;
                      const hour = formatShowtimeHour(slot.startTime);
                      return (
                        <li key={slot.showtimeId}>
                          <button
                            type="button"
                            disabled={soldOut || !isReady}
                            aria-label={soldOut ? `Suất ${hour}, hết ghế` : `Đặt vé suất ${hour}`}
                            onClick={() => bookShowtime(slot.showtimeId)}
                            className={cn(
                              "t-code h-11 min-w-16 cursor-pointer cut-xs bg-secondary px-3 text-sm text-foreground transition-colors hover:bg-primary hover:text-primary-foreground",
                              soldOut && "cursor-not-allowed text-muted-foreground line-through hover:bg-secondary hover:text-muted-foreground",
                            )}
                          >
                            {hour}
                          </button>
                        </li>
                      );
                    })}
                  </ul>
                </div>
              );
            })}
      </div>
    </section>
  );
};
