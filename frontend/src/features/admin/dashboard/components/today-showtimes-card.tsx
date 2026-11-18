import type { UseQueryResult } from "@tanstack/react-query";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { SkeletonList } from "@/components/common/skeleton-list";
import { EmptyState } from "@/components/common/empty-state";
import { QueryState } from "@/components/common/query-state";
import { RowSkeleton } from "@/components/common/skeleton-shapes";
import type { DashboardInsightsDto } from "@/features/admin/dashboard/types";
import { Film } from "lucide-react";
import { formatShowtimeHour } from "@/lib/formatters";

interface TodayShowtimesCardProps {
  query: UseQueryResult<DashboardInsightsDto>;
}


export const TodayShowtimesCard = ({ query }: TodayShowtimesCardProps) => {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Suất chiếu hôm nay</CardTitle>
        <CardDescription>Tình trạng lấp đầy theo suất chiếu</CardDescription>
      </CardHeader>
      <CardContent className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
        <QueryState
          query={query}
          skeleton={<SkeletonList count={6} shape={RowSkeleton} className="h-16" />}
          isEmpty={(insights) => insights.todayShowtimes.length === 0}
          empty={
            <div className="col-span-full">
              <EmptyState icon={Film} title="Chưa có suất chiếu hôm nay" description="Suất chiếu sẽ hiện ở đây khi được lên lịch." />
            </div>
          }
          errorTitle="Không tải được suất chiếu hôm nay"
          errorClassName="col-span-full"
        >
          {(insights) => insights.todayShowtimes.map((s) => {
            const percent = s.totalSeats > 0 ? Math.round((s.bookedSeats / s.totalSeats) * 100) : 0;
            return (
              <div key={s.showtimeId} className="cut-sm bg-secondary/50 p-3">
                <div className="flex items-center justify-between gap-2">
                  <div className="min-w-0">
                    <p className="truncate text-sm font-medium text-foreground">{s.movieTitle}</p>
                    <p className="text-xs text-muted-foreground">
                      {formatShowtimeHour(s.startTime)}, Phòng {s.roomName}
                    </p>
                  </div>
                  <span className="shrink-0 text-xs font-semibold tabular-nums text-foreground">
                    {s.bookedSeats}/{s.totalSeats}
                  </span>
                </div>
                <div className="mt-2 h-1.5 w-full overflow-hidden bg-muted">
                  <div className="h-full bg-primary transition-all" style={{ width: `${percent}%` }} />
                </div>
              </div>
            );
          })}
        </QueryState>
      </CardContent>
    </Card>
  );
}