import { Film } from "lucide-react";
import { ShowtimeStatus } from "@/types/enum";
import { Label } from "@/components/ui/label";
import { EmptyState } from "@/components/common/empty-state";
import { QueryState } from "@/components/common/query-state";
import { SkeletonList } from "@/components/common/skeleton-list";
import { SKELETON_LIST_COUNT } from "@/config/default-constants";
import { formatCurrency } from "@/lib/formatters";
import { useSchedule } from "@/features/client/showtimes/hooks/use-showtimes";
import { formatShowtimeRange } from "@/lib/cinema-format";
import { DatePickerField } from "@/components/common/date-time/date-picker-field";

interface CounterShowtimePickerProps {
  date: string;
  onDateChange: (date: string) => void;
  onPickShowtime: (showtimeId: number) => void;
}

export const CounterShowtimePicker = ({ date, onDateChange, onPickShowtime }: CounterShowtimePickerProps) => {
  const schedule = useSchedule(date);

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-col gap-1.5 sm:w-56">
        <Label htmlFor="counter-date">Ngày chiếu</Label>
        <DatePickerField
          id="counter-date"
          value={date}
          onChange={(value) => onDateChange(value ?? date)}
          placeholder="Chọn ngày chiếu"
          clearable={false}
        />
      </div>

      <QueryState
        query={schedule}
        skeleton={
          <div aria-busy="true" className="flex flex-col gap-4">
            <SkeletonList count={SKELETON_LIST_COUNT} className="h-40" />
          </div>
        }
        isEmpty={(data) => data.movies.length === 0}
        empty={
          <EmptyState
            icon={Film}
            title="Chưa có suất chiếu trong ngày này"
            description="Chọn ngày khác để xem suất chiếu."
          />
        }
        errorTitle="Không tải được lịch chiếu"
      >
        {(data) => (
      <div className="flex flex-col gap-4">
        {data.movies.map((movie) => (
          <div key={movie.movieId} className="bg-card p-4">
            <div className="mb-3 flex items-center gap-3">
              {movie.moviePosterUrl && (
                <img
                  src={movie.moviePosterUrl}
                  alt={movie.movieTitle}
                  className="h-16 w-11 object-cover"
                />
              )}
              <h3 className="t-h3 text-foreground">
                {movie.movieTitle}
              </h3>
            </div>
            <div className="flex flex-wrap gap-2">
              {movie.showtimes.map((st) => (
                <button
                  key={st.showtimeId}
                  type="button"
                  data-testid={`counter-showtime-${st.showtimeId}`}
                  disabled={st.availableSeats <= 0 || st.status !== ShowtimeStatus.Scheduled}
                  onClick={() => onPickShowtime(st.showtimeId)}
                  className="flex flex-col items-start cut-sm bg-secondary px-3 py-2 text-left text-xs font-medium transition-all hover:bg-muted disabled:cursor-not-allowed disabled:opacity-40"
                >
                  <span>
                    {formatShowtimeRange(st.startTime, st.endTime)}
                  </span>
                  <span className="text-muted-foreground">
                    P.{st.roomName}, {st.availableSeats}/{st.totalSeats} ghế trống
                  </span>
                  <span className="font-semibold text-primary-text">Từ {formatCurrency(st.fromPrice ?? st.basePrice)}</span>
                </button>
              ))}
            </div>
          </div>
        ))}
      </div>
        )}
      </QueryState>
    </div>
  );
}
