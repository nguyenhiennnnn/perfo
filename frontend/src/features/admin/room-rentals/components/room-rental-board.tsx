import { Link } from "react-router";
import { CalendarClock, Users } from "lucide-react";
import { ErrorState } from "@/components/common/error-state";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { ROUTES } from "@/config/routes";
import { ROOM_RENTAL_AUDIENCE_LABELS } from "@/features/client/room-rentals/utils/room-rental-labels";
import { formatDateTime } from "@/lib/formatters";
import type { RoomRentalRequestStatus } from "@/types/enum";
import { useAdminRoomRentalBoard } from "../hooks/use-admin-room-rentals";
import {
  ROOM_RENTAL_ADMIN_STATUS,
  ROOM_RENTAL_BOARD_PAGE_SIZE,
  ROOM_RENTAL_BOARD_STATUSES,
} from "../utils/room-rental-admin-labels";
import type { RoomRentalAdminListItemDto } from "../types";

const BoardCard = ({ item }: { item: RoomRentalAdminListItemDto }) => (
  <Link
    to={ROUTES.admin.roomRentalDetail(item.roomRentalRequestId)}
    className="block cut-md bg-card p-3 transition-colors hover:bg-accent focus-visible:outline-2 focus-visible:outline-ring"
  >
    <div className="flex items-start justify-between gap-2">
      <span className="t-code text-foreground">{item.requestCode}</span>
      <Badge variant="outline">{ROOM_RENTAL_AUDIENCE_LABELS[item.audienceType]}</Badge>
    </div>
    <p className="mt-1 truncate text-sm font-medium text-foreground">{item.fullName}</p>
    {item.organizationName && (
      <p className="truncate text-xs text-muted-foreground">{item.organizationName}</p>
    )}
    <div className="mt-2 flex flex-wrap items-center gap-x-3 gap-y-1 text-xs text-muted-foreground">
      <span className="inline-flex items-center gap-1 tabular-nums">
        <CalendarClock className="size-3.5" />
        {formatDateTime(item.desiredStartAt)}
      </span>
      <span className="inline-flex items-center gap-1 tabular-nums">
        <Users className="size-3.5" />
        {item.attendeeCount}
      </span>
    </div>
  </Link>
);

interface RoomRentalBoardProps {
  search?: string;
  onViewAll: (status: RoomRentalRequestStatus) => void;
}

export const RoomRentalBoard = ({ search, onViewAll }: RoomRentalBoardProps) => {
  const queries = useAdminRoomRentalBoard(
    ROOM_RENTAL_BOARD_STATUSES,
    search,
    ROOM_RENTAL_BOARD_PAGE_SIZE,
  );

  return (
    <div className="-mx-1 flex gap-3 overflow-x-auto px-1 pb-3" data-testid="room-rental-board">
      {ROOM_RENTAL_BOARD_STATUSES.map((status, index) => {
        const query = queries[index];
        const items = query.data?.items ?? [];
        const total = query.data?.totalItems ?? 0;

        return (
          <section
            key={status}
            aria-label={ROOM_RENTAL_ADMIN_STATUS[status].label}
            className="flex w-72 shrink-0 flex-col gap-2 cut-md bg-muted/40 p-2"
          >
            <header className="flex items-center justify-between px-1 py-1">
              <h2 className="text-sm font-semibold text-foreground">
                {ROOM_RENTAL_ADMIN_STATUS[status].label}
              </h2>
              <Badge variant="secondary" className="tabular-nums">
                {total}
              </Badge>
            </header>

            {query.isLoading ? (
              <>
                <CardSkeleton className="h-24" />
                <CardSkeleton className="h-24" />
              </>
            ) : query.isError ? (
              <ErrorState error={query.error} onRetry={() => void query.refetch()} />
            ) : items.length === 0 ? (
              <p className="px-1 py-6 text-center text-xs text-muted-foreground">Chưa có yêu cầu</p>
            ) : (
              <>
                {items.map((item) => (
                  <BoardCard key={item.roomRentalRequestId} item={item} />
                ))}
                {total > items.length && (
                  <Button variant="ghost" size="sm" onClick={() => onViewAll(status)}>
                    Xem tất cả {total} yêu cầu
                  </Button>
                )}
              </>
            )}
          </section>
        );
      })}
    </div>
  );
};
