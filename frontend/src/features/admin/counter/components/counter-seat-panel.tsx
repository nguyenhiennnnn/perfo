import type { UseQueryResult } from "@tanstack/react-query";
import { CalendarX } from "lucide-react";
import { Skeleton } from "@/components/ui/skeleton";
import { NotFoundState } from "@/components/common/not-found-state";
import { QueryState } from "@/components/common/query-state";
import { SelectableSeatMap } from "@/components/common/seat-map/selectable-seat-map";
import { ShowtimeSeatStatus } from "@/types/enum";
import type { ShowtimeSeatsResponseDto } from "@/features/client/showtimes/types";
import { formatDateTime } from "@/lib/formatters";

interface CounterSeatPanelProps {
  query: UseQueryResult<ShowtimeSeatsResponseDto>;
  statusMap: Map<number, ShowtimeSeatStatus>;
  selectedIds: Set<number>;
  onToggleSeat: (showtimeSeatId: number) => void;
}

export const CounterSeatPanel = ({
  query,
  statusMap,
  selectedIds,
  onToggleSeat,
}: CounterSeatPanelProps) => (
  <QueryState
    query={query}
    skeleton={<Skeleton className="h-96 w-full" />}
    notFound={<NotFoundState entity="suất chiếu" icon={CalendarX} />}
    errorTitle="Không tải được sơ đồ ghế"
  >
    {(data) => (
      <div className="flex flex-col gap-2">
        <p className="text-sm text-muted-foreground">
          {data.movieTitle}, Phòng {data.roomName}, {formatDateTime(data.startTime)}
        </p>
        <SelectableSeatMap
          rows={data.rows}
          cols={data.cols}
          aisles={data.aisles}
          seats={data.seats}
          statusMap={statusMap}
          selectedIds={selectedIds}
          onToggle={(seat) => onToggleSeat(seat.showtimeSeatId)}
          fitWidth
        />
      </div>
    )}
  </QueryState>
);
