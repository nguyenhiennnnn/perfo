import { useMemo, useState } from "react";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { ErrorState } from "@/components/common/error-state";
import { PageHeader } from "@/components/common/page-header";
import { SelectField } from "@/components/common/select-field";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { Button } from "@/components/ui/button";
import { LOOKUP_PAGE_SIZE } from "@/config/default-constants";
import { useAdminRooms } from "@/features/admin/rooms/hooks/use-admin-rooms";
import { getBusinessToday } from "@/lib/business-time";
import { formatDate, toDateInputValue } from "@/lib/formatters";
import { RoomRentalCalendarDay } from "../components/room-rental-calendar-day";
import { useAdminRoomRentalCalendar } from "../hooks/use-admin-room-rentals";
import { addDays, listDays } from "../utils/room-rental-dates";
import { ROOM_RENTAL_CALENDAR_DAYS } from "../utils/room-rental-admin-labels";
import type { RoomRentalCalendarItemDto } from "../types";

const ALL_ROOMS = "all";

const AdminRoomRentalCalendarPage = () => {
  const today = getBusinessToday();
  const [from, setFrom] = useState(today);
  const [roomId, setRoomId] = useState<string>(ALL_ROOMS);
  const to = addDays(from, ROOM_RENTAL_CALENDAR_DAYS - 1);

  const rooms = useAdminRooms({ pageSize: LOOKUP_PAGE_SIZE });
  const calendar = useAdminRoomRentalCalendar({
    from,
    to,
    roomId: roomId === ALL_ROOMS ? undefined : Number(roomId),
  });

  const roomOptions = useMemo(
    () => [
      { roomId: ALL_ROOMS, roomName: "Tất cả phòng" },
      ...(rooms.data?.items ?? []).map((room) => ({
        roomId: String(room.roomId),
        roomName: room.roomName,
      })),
    ],
    [rooms.data],
  );

  const grouped = useMemo(() => {
    const map = new Map<string, RoomRentalCalendarItemDto[]>();
    for (const day of listDays(from, ROOM_RENTAL_CALENDAR_DAYS)) map.set(day, []);
    for (const item of calendar.data ?? []) {
      const key = toDateInputValue(item.startTime) ?? from;
      const list = map.get(key);
      if (list) list.push(item);
      else map.set(key, [item]);
    }
    return [...map.entries()].sort(([a], [b]) => a.localeCompare(b));
  }, [calendar.data, from]);

  return (
    <div>
      <PageHeader
        title="Lịch phòng thuê"
        description="Các suất riêng đã chốt trong từng phòng. Suất riêng không hiện ở lịch chiếu công khai."
        action={
          <div className="flex flex-wrap items-center gap-2">
            <Button
              size="icon-sm"
              variant="outline"
              aria-label="Tuần trước"
              onClick={() => setFrom(addDays(from, -ROOM_RENTAL_CALENDAR_DAYS))}
            >
              <ChevronLeft className="size-4" />
            </Button>
            <Button size="sm" variant="outline" onClick={() => setFrom(today)}>
              Hôm nay
            </Button>
            <Button
              size="icon-sm"
              variant="outline"
              aria-label="Tuần sau"
              onClick={() => setFrom(addDays(from, ROOM_RENTAL_CALENDAR_DAYS))}
            >
              <ChevronRight className="size-4" />
            </Button>
          </div>
        }
      />

      <div className="mb-4 flex flex-wrap items-center gap-3">
        <p className="text-sm text-muted-foreground tabular-nums">
          {formatDate(from)} – {formatDate(to)}
        </p>
        <SelectField
          className="w-52"
          value={roomId}
          onChange={setRoomId}
          options={roomOptions}
          getOptionValue={(option) => option.roomId}
          getOptionLabel={(option) => option.roomName}
          placeholder="Phòng chiếu"
        />
      </div>

      {calendar.isLoading ? (
        <div className="space-y-4">
          <CardSkeleton className="h-32" />
          <CardSkeleton className="h-32" />
        </div>
      ) : calendar.isError && !calendar.data ? (
        <ErrorState error={calendar.error} onRetry={() => void calendar.refetch()} />
      ) : (
        <div className="space-y-4">
          {grouped.map(([date, items]) => (
            <RoomRentalCalendarDay key={date} date={date} isToday={date === today} items={items} />
          ))}
        </div>
      )}
    </div>
  );
};

export default AdminRoomRentalCalendarPage;
