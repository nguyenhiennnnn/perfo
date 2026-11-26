import { Link } from "react-router";
import { CalendarX } from "lucide-react";
import { EmptyState } from "@/components/common/empty-state";
import { Badge } from "@/components/ui/badge";
import { ROUTES } from "@/config/routes";
import { formatDate, formatTime } from "@/lib/formatters";
import { RoomRentalStatusBadge } from "./room-rental-status-badge";
import type { RoomRentalCalendarItemDto } from "../types";

interface RoomRentalCalendarDayProps {
  date: string;
  isToday: boolean;
  items: RoomRentalCalendarItemDto[];
}

export const RoomRentalCalendarDay = ({ date, isToday, items }: RoomRentalCalendarDayProps) => (
  <section className="cut-md bg-card p-4">
    <header className="mb-3 flex flex-wrap items-center gap-2">
      <h2 className="t-h3 text-foreground">{formatDate(date)}</h2>
      {isToday && <Badge>Hôm nay</Badge>}
      <span className="text-sm text-muted-foreground">{items.length} suất riêng</span>
    </header>
    {items.length === 0 ? (
      <EmptyState
        icon={CalendarX}
        title="Không có suất riêng"
        description="Ngày này chưa có suất thuê phòng nào."
      />
    ) : (
      <ul className="divide-y divide-border">
        {items.map((item) => (
          <li key={item.showtimeId}>
            <Link
              to={ROUTES.admin.roomRentalDetail(item.requestId)}
              className="grid gap-2 py-3 hover:bg-muted/40 sm:grid-cols-[9rem_minmax(0,1fr)_auto] sm:items-center"
            >
              <p className="t-code tabular-nums text-foreground">
                {formatTime(item.startTime)} – {formatTime(item.endTime)}
              </p>
              <div className="min-w-0">
                <p className="truncate text-sm font-medium text-foreground">
                  {item.customer} · <span className="t-code">{item.requestCode}</span>
                </p>
                <p className="truncate text-sm text-muted-foreground">
                  {item.roomName} · {item.attendeeCount} người
                  {item.contentTitle ? ` · ${item.contentTitle}` : ""}
                </p>
              </div>
              <RoomRentalStatusBadge status={item.requestStatus} />
            </Link>
          </li>
        ))}
      </ul>
    )}
  </section>
);
