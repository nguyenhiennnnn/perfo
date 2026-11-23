import { formatShowtimeTime } from "@/lib/cinema-format";
import { formatCurrency } from "@/lib/formatters";
import { cn } from "@/lib/utils";
import type { ShowtimeSlotItem } from "@/components/common/showtime-group-list";

interface ShowtimeRowProps {
  item: ShowtimeSlotItem;
  disabled?: boolean;
  onBook: (showtimeId: number) => void;
}

export const ShowtimeRow = ({ item, disabled = false, onBook }: ShowtimeRowProps) => {
  const soldOut = item.availableSeats === 0;
  const startLabel = formatShowtimeTime(item.startTime);

  return (
    <div className="@container">
      <div
        data-slot="showtime-row"
        className="notch-stub relative grid grid-rows-[1fr_var(--stub-h)] bg-card cut-lg @md:grid-cols-[1fr_var(--stub-w)] @md:grid-rows-1"
      >
        <div className="flex items-center gap-4 p-4">
          <p className="t-numeral shrink-0 text-4xl text-foreground">{startLabel}</p>
          <div className="min-w-0 text-sm">
            <p className="font-medium text-foreground">
              Phòng {item.roomName}
              {item.screenType ? `, ${item.screenType}` : ""}
            </p>
            <p className="text-muted-foreground">
              {soldOut ? "Hết ghế" : `Còn ${item.availableSeats}/${item.totalSeats} ghế`}
            </p>
            <p className="text-muted-foreground">Kết thúc {formatShowtimeTime(item.endTime)}</p>
          </div>
        </div>

        <div
          aria-hidden
          className={cn(
            "perf-h absolute inset-x-4 bottom-[calc(var(--stub-h)-0.5rem)] @md:hidden",
            soldOut ? "text-muted-foreground!" : "text-primary-foreground!",
          )}
        />
        <div
          aria-hidden
          className={cn(
            "perf-v absolute inset-y-4 right-[calc(var(--stub-w)-0.5rem)] hidden @md:block",
            soldOut ? "text-muted-foreground!" : "text-primary-foreground!",
          )}
        />

        <div
          className={cn(
            "flex items-center justify-between gap-3 px-4 @md:flex-col @md:justify-center @md:gap-3",
            soldOut ? "bg-secondary text-secondary-foreground" : "bg-primary text-primary-foreground",
          )}
        >
          {soldOut ? (
            <p className="t-h3">Hết ghế</p>
          ) : (
            <>
              <p className="flex flex-col items-start @md:items-center">
                <span className="text-xs leading-none">Từ</span>
                <span className="t-numeral text-2xl">
                  {formatCurrency(item.fromPrice ?? item.basePrice)}
                </span>
              </p>
              <button
                type="button"
                disabled={disabled}
                aria-label={`Đặt vé suất ${startLabel}, phòng ${item.roomName}`}
                onClick={() => onBook(item.showtimeId)}
                className="h-11 cursor-pointer cut-sm bg-primary-foreground px-5 text-sm font-medium text-primary transition-colors hover:bg-primary-foreground/90 disabled:cursor-not-allowed disabled:opacity-50"
              >
                Đặt vé
              </button>
            </>
          )}
        </div>
      </div>
    </div>
  );
};
