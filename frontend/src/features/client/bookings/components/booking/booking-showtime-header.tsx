import { Ticket } from "lucide-react";
import { AgeRatingBadge } from "@/components/common/status-badge";
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
      <div aria-hidden className="flex gap-4">
        <Skeleton className="aspect-2/3 w-16 shrink-0 cut-sm" />
        <div className="flex-1 space-y-2">
          <Skeleton className="h-9 w-2/3 cut-xs" />
          <Skeleton className="h-4 w-1/3 cut-xs" />
        </div>
      </div>
    );
  }

  if (!data) return null;

  return (
    <div className="flex gap-4">
      <div className="aspect-2/3 w-16 shrink-0 overflow-hidden cut-sm bg-muted sm:w-20">
        {data.moviePosterUrl ? (
          <img src={data.moviePosterUrl} alt={data.movieTitle} className="h-full w-full object-cover" />
        ) : (
          <div className="flex h-full w-full items-center justify-center text-muted-foreground">
            <Ticket className="size-5" />
          </div>
        )}
      </div>
      <div className="min-w-0">
        <h1 className="t-h1 text-foreground">{data.movieTitle}</h1>
        <div className="mt-2 flex flex-wrap items-center gap-x-3 gap-y-1 text-sm text-muted-foreground">
          {data.ageRatingCode && <AgeRatingBadge code={data.ageRatingCode} />}
          {data.screenType && <span className="font-medium text-foreground">{data.screenType}</span>}
          <span>
            Phòng {data.roomName}, {formatDateTime(data.startTime)}
          </span>
        </div>
      </div>
    </div>
  );
};
