import { useEffect, useMemo, useRef, useState } from "react";
import { endOfWeek, startOfWeek } from "date-fns";
import { CalendarX, TriangleAlert, Wrench } from "lucide-react";

import { DateFilterPicker } from "@/components/common/filter/date-filter-picker";
import { EnumSelectFilter } from "@/components/common/filter/enum-select-filter";
import { EmptyState } from "@/components/common/empty-state";
import { ErrorState } from "@/components/common/error-state";
import { SelectField } from "@/components/common/select-field";
import { SkeletonList } from "@/components/common/skeleton-list";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { getBusinessTodayDate } from "@/lib/business-time";
import { showtimeStatusOptions } from "@/lib/enum-options";
import { formatWeekdayShort } from "@/lib/cinema-format";
import { toDateInputValue } from "@/lib/formatters";
import { useAdminShowtimeCalendarParams } from "@/hooks/use-query-params";
import { useAuthRole } from "@/stores/auth-store";
import { Role } from "@/types/enum";
import { useAdminRooms } from "@/features/admin/rooms/hooks/use-admin-rooms";

import { useAdminShowtimesCalendar } from "@/features/admin/showtimes/hooks/use-admin-showtimes";
import type {
  ShowtimeCalendarDayDto,
  ShowtimeCalendarItemDto,
} from "@/features/admin/showtimes/types";
import {
  buildDayLayout,
  listClosureDays,
  parseCalendarDay,
  placeClosuresForDay,
} from "@/features/admin/showtimes/utils/calendar-layout";
import { ShowtimeCalendarDay } from "./showtime-calendar-day";
import { ShowtimeCalendarLegend } from "./showtime-calendar-legend";
import { LOOKUP_PAGE_SIZE, SKELETON_LIST_COUNT } from "@/config/default-constants";
import { FilterBar } from "@/components/common/filter/filter-bar";

interface ShowtimeCalendarViewProps {
  onSelectShowtime?: (showtime: ShowtimeCalendarItemDto) => void;
}

export const ShowtimeCalendarView = ({
  onSelectShowtime,
}: ShowtimeCalendarViewProps) => {
  const [params, setParams] = useAdminShowtimeCalendarParams();
  const [activeDay, setActiveDay] = useState<string>();

  const defaultRangeAppliedRef = useRef(false);
  useEffect(() => {
    if (defaultRangeAppliedRef.current) return;
    defaultRangeAppliedRef.current = true;
    if (!params.from || !params.to) {
      const today = getBusinessTodayDate();
      void setParams({
        from: toDateInputValue(startOfWeek(today, { weekStartsOn: 1 })) ?? "",
        to: toDateInputValue(endOfWeek(today, { weekStartsOn: 1 })) ?? "",
      });
    }
  }, [params.from, params.to, setParams]);

  const canFilterByRoom = useAuthRole() === Role.Admin;
  const rooms = useAdminRooms(
    { page: 1, pageSize: LOOKUP_PAGE_SIZE },
    { enabled: canFilterByRoom },
  );

  const queryParams = {
    from: params.from ?? "",
    to: params.to ?? "",
    roomId: params.roomId ?? undefined,
    status: params.status ?? undefined,
  };

  const calendar = useAdminShowtimesCalendar(queryParams);
  const hasActiveFilters = params.roomId != null || params.status != null;

  const closures = useMemo(() => {
    const all = calendar.data?.closures ?? [];
    return params.roomId != null
      ? all.filter((c) => c.roomId === params.roomId)
      : all;
  }, [calendar.data, params.roomId]);

  const days = useMemo(() => {
    const byDate = new Map<string, ShowtimeCalendarDayDto>(
      (calendar.data?.days ?? []).map((d) => [d.date, d]),
    );
    for (const date of listClosureDays(closures, params.from ?? "", params.to ?? "")) {
      if (!byDate.has(date)) byDate.set(date, { date, showtimes: [] });
    }
    const toTime = (value: string) => parseCalendarDay(value)?.getTime() ?? 0;
    return [...byDate.values()].sort((a, b) => toTime(a.date) - toTime(b.date));
  }, [calendar.data, closures, params.from, params.to]);

  const layouts = useMemo(
    () =>
      new Map(
        days.map((d) => [
          d.date,
          buildDayLayout(d.showtimes, placeClosuresForDay(closures, d.date)),
        ]),
      ),
    [days, closures],
  );
  const currentDay = days.find((d) => d.date === activeDay) ?? days[0];

  return (
    <div className="flex flex-col gap-4">
      <FilterBar>
        <DateFilterPicker
          value={params.from}
          onChange={(from) => setParams({ from })}
          placeholder="Từ ngày"
        />
        <DateFilterPicker
          value={params.to}
          onChange={(to) => setParams({ to })}
          placeholder="Đến ngày"
        />
        {canFilterByRoom && (
          <SelectField
            value={params.roomId ?? null}
            onChange={(value) => setParams({ roomId: value ? Number(value) : undefined })}
            options={rooms.data?.items ?? []}
            getOptionValue={(room) => room.roomId}
            getOptionLabel={(room) => room.roomName}
            placeholder="Phòng chiếu"
            className="sm:w-48"
          />
        )}
        <EnumSelectFilter
          value={params.status}
          onChange={(status) => setParams({ status })}
          options={showtimeStatusOptions}
          placeholder="Trạng thái suất chiếu"
        />
      </FilterBar>

      <ShowtimeCalendarLegend editable={Boolean(onSelectShowtime)} />

      {calendar.isLoading && (
        <div className="flex flex-col gap-2" aria-busy="true">
          <CardSkeleton className="h-12" />
          <SkeletonList count={SKELETON_LIST_COUNT} className="h-20" />
        </div>
      )}

      {calendar.isError && (
        <ErrorState
          title="Không tải được lịch chiếu"
          error={calendar.error}
          onRetry={() => void calendar.refetch()}
        />
      )}

      {!calendar.isLoading && !calendar.isError && days.length === 0 && (
        hasActiveFilters ? (
          <EmptyState
            variant="filtered"
            icon={CalendarX}
            onClearFilters={() => void setParams({ roomId: null, status: null })}
          />
        ) : (
          <EmptyState
            icon={CalendarX}
            title="Chưa có suất chiếu"
            description="Chưa có suất chiếu nào trong khoảng ngày đã chọn. Chọn khoảng ngày khác hoặc tạo suất chiếu mới."
          />
        )
      )}

      {!calendar.isLoading && days.length > 0 && currentDay && (
        <Tabs
          value={currentDay.date}
          onValueChange={(value) => setActiveDay(value as string)}
        >
          <TabsList
            variant="line"
            className="flex-nowrap overflow-x-auto overflow-y-hidden [scrollbar-width:none] [&::-webkit-scrollbar]:hidden"
          >
            {days.map((day) => {
              const parsed = parseCalendarDay(day.date);
              const conflicts = layouts.get(day.date)?.conflictCount ?? 0;
              const closureCount = layouts.get(day.date)?.closureCount ?? 0;
              return (
                <TabsTrigger
                  key={day.date}
                  value={day.date}
                  className="shrink-0 flex-col gap-0 leading-tight"
                >
                  <span className="text-xs font-normal">
                    {parsed ? formatWeekdayShort(parsed) : "Ngày"}
                  </span>
                  <span className="t-code text-base">
                    {parsed ? day.date.slice(0, 5) : day.date}
                  </span>
                  <span className="flex items-center gap-1 text-xs font-normal">
                    {day.showtimes.length} suất
                    {closureCount > 0 && (
                      <Wrench
                        className="size-3"
                        role="img"
                        aria-label={`${closureCount} đợt bảo trì`}
                      />
                    )}
                    {conflicts > 0 && (
                      <TriangleAlert
                        className="size-3"
                        role="img"
                        aria-label={`${conflicts} suất trùng giờ`}
                      />
                    )}
                  </span>
                </TabsTrigger>
              );
            })}
          </TabsList>

          {days.map((day) => (
            <TabsContent key={day.date} value={day.date}>
              <ShowtimeCalendarDay
                day={day}
                layout={
                  layouts.get(day.date) ??
                  buildDayLayout(day.showtimes, placeClosuresForDay(closures, day.date))
                }
                onSelectShowtime={onSelectShowtime}
              />
            </TabsContent>
          ))}
        </Tabs>
      )}
    </div>
  );
};
