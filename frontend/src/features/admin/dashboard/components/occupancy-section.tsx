import { RotateCcw } from "lucide-react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { DateFilterPicker } from "@/components/common/filter/date-filter-picker";
import { useAdminDashboardOccupancyParams } from "@/hooks/use-query-params";
import { getDefaultReportRange } from "@/features/admin/reports/utils/default-range";
import type { OccupancyGroupBy, ShowtimeKind } from "@/types/enum";
import { useDashboardOccupancy } from "../hooks/use-dashboard";
import { OCCUPANCY_GROUP_OPTIONS, SHOWTIME_KIND_OPTIONS } from "../utils/occupancy";
import { OccupancyChart } from "./occupancy-chart";

const DEFAULT_RANGE_DAYS = 14;

export const OccupancySection = () => {
  const [params, setParams] = useAdminDashboardOccupancyParams();

  const defaultRange = getDefaultReportRange(DEFAULT_RANGE_DAYS);
  const from = params.from ?? defaultRange.fromDate;
  const to = params.to ?? defaultRange.toDate;

  const query = useDashboardOccupancy({ from, to, groupBy: params.groupBy, kind: params.kind });

  const toggleKind = (kind: ShowtimeKind) => {
    const next = params.kind.includes(kind)
      ? params.kind.filter((k) => k !== kind)
      : SHOWTIME_KIND_OPTIONS.map((o) => o.value).filter(
          (k) => k === kind || params.kind.includes(k),
        );
    if (next.length === 0) return;
    void setParams({ kind: next });
  };

  const handleReset = () => void setParams({ from: null, to: null, groupBy: null, kind: null });

  return (
    <Card>
      <CardHeader>
        <CardTitle>Lấp đầy</CardTitle>
        <CardDescription>
          Tỉ lệ vé bán trên ghế bán được, so với kỳ liền trước có cùng độ dài.
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="flex flex-wrap items-center gap-3">
          <DateFilterPicker
            value={from}
            onChange={(value) => void setParams({ from: value })}
            placeholder="Từ ngày"
          />
          <DateFilterPicker
            value={to}
            onChange={(value) => void setParams({ to: value })}
            placeholder="Đến ngày"
          />
          <div className="flex flex-wrap gap-2" role="group" aria-label="Loại suất">
            {SHOWTIME_KIND_OPTIONS.map((option) => {
              const selected = params.kind.includes(option.value);
              return (
                <Button
                  key={option.value}
                  type="button"
                  size="sm"
                  variant={selected ? "default" : "outline"}
                  aria-pressed={selected}
                  onClick={() => toggleKind(option.value)}
                >
                  {option.label}
                </Button>
              );
            })}
          </div>
          <Button variant="outline" onClick={handleReset} className="h-10">
            <RotateCcw className="size-4" />
            <span>Đặt lại</span>
          </Button>
        </div>

        <Tabs
          value={params.groupBy}
          onValueChange={(value) => void setParams({ groupBy: value as OccupancyGroupBy })}
        >
          <TabsList>
            {OCCUPANCY_GROUP_OPTIONS.map((option) => (
              <TabsTrigger key={option.value} value={option.value}>
                {option.label}
              </TabsTrigger>
            ))}
          </TabsList>
        </Tabs>

        <OccupancyChart query={query} groupBy={params.groupBy} />
      </CardContent>
    </Card>
  );
};
