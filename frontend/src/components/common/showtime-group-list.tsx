import { useMemo } from "react";
import { cn } from "@/lib/utils";
import { ShowtimeRow } from "@/components/common/showtime-row";
import { useBookShowtime } from "@/features/client/orders/hooks/use-book-showtime";
import { DEFAULT_SCREEN_TYPE } from "@/config/default-constants";

export interface ShowtimeSlotItem {
  showtimeId: number;
  roomName: string;
  screenType?: string | null;
  startTime: string;
  endTime: string;
  availableSeats: number;
  totalSeats: number;
  basePrice: number;
  fromPrice?: number | null;
}

interface ShowtimeGroupListProps {
  showtimes: ShowtimeSlotItem[];
  className?: string;
}

export const ShowtimeGroupList = ({ showtimes, className }: ShowtimeGroupListProps) => {
  const { bookShowtime, isReady } = useBookShowtime();

  const groups = useMemo(() => {
    const map = new Map<string, ShowtimeSlotItem[]>();
    for (const item of showtimes) {
      const key = item.screenType ?? DEFAULT_SCREEN_TYPE;
      if (!map.has(key)) map.set(key, []);
      map.get(key)!.push(item);
    }
    return Array.from(map.entries());
  }, [showtimes]);

  if (groups.length === 0) return null;

  return (
    <div className={cn("@container space-y-6", className)}>
      {groups.map(([format, items]) => (
        <section key={format} aria-label={`Suất chiếu ${format}`}>
          <h3 className="t-h3 text-foreground">{format}</h3>
          <div className="mt-3 grid gap-3 @5xl:grid-cols-2">
            {items.map((item) => (
              <ShowtimeRow key={item.showtimeId} item={item} disabled={!isReady} onBook={bookShowtime} />
            ))}
          </div>
        </section>
      ))}
    </div>
  );
};
