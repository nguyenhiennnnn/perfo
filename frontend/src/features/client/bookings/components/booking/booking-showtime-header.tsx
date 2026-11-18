import { Skeleton } from "@/components/ui/skeleton";
import type { ShowtimeSeatsResponseDto } from "@/features/client/showtimes/types";
import { formatDateTime } from "@/lib/formatters";

export const BookingShowtimeHeader = ({
  data,
  isLoading,
}: {
  data: ShowtimeSeatsResponseDto | undefined;
  isLoading: boolean;
}) => {
  if (isLoading) {
    return (
      <div aria-hidden className="space-y-2">
        <Skeleton className="h-9 w-2/3 cut-xs" />
        <Skeleton className="h-4 w-1/3 cut-xs" />
      </div>
    );
  }

  if (!data) return null;

  return (
    <div>
      <h1 className="t-h1 text-foreground">
        {data.movieTitle}
      </h1>
      <p className="text-sm text-muted-foreground">
        Phòng {data.roomName}, {formatDateTime(data.startTime)}
      </p>
    </div>
  );
}
