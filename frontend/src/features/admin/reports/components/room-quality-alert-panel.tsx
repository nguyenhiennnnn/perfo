import { useState } from "react";
import { CircleCheck, TriangleAlert } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Label } from "@/components/ui/label";
import { Switch } from "@/components/ui/switch";
import { QueryState } from "@/components/common/query-state";
import { LineSkeleton } from "@/components/common/skeleton-shapes";
import { cn } from "@/lib/utils";
import { formatDate, formatDateTime, formatRating } from "@/lib/formatters";
import { useRoomQualityAlerts } from "../hooks/use-reports";
import type { RoomQualityAlertDto } from "../types";

const AlertRow = ({ alert }: { alert: RoomQualityAlertDto }) => {
  const resolved = alert.resolvedAt != null;

  return (
    <li
      className={cn(
        "flex flex-col gap-2 border-l-2 p-3 sm:flex-row sm:items-start sm:justify-between",
        resolved ? "border-border bg-muted/40" : "border-destructive bg-destructive/5",
      )}
    >
      <div className="min-w-0 space-y-1">
        <div className="flex flex-wrap items-center gap-2">
          <p className="font-medium text-foreground">{alert.roomName}</p>
          <Badge variant={resolved ? "secondary" : "destructive"}>
            {resolved ? "Đã kết thúc" : "Đang cảnh báo"}
          </Badge>
        </div>
        <p className="text-sm text-foreground">
          Điểm trung bình {formatRating(alert.averageScore)}/5, thấp hơn ngưỡng{" "}
          {formatRating(alert.threshold)} trong {alert.consecutiveDays} ngày liên tiếp (
          {formatDate(alert.fromDate)} – {formatDate(alert.toDate)}).
        </p>
      </div>
      <div className="shrink-0 text-xs tabular-nums text-muted-foreground sm:text-right">
        <p>Phát hiện: {formatDateTime(alert.detectedAt)}</p>
        {resolved && <p>Kết thúc: {formatDateTime(alert.resolvedAt)}</p>}
      </div>
    </li>
  );
};

export const RoomQualityAlertPanel = () => {
  const [showResolved, setShowResolved] = useState(false);
  const query = useRoomQualityAlerts(!showResolved);
  const activeCount = query.data?.filter((a) => a.resolvedAt == null).length ?? 0;

  return (
    <Card>
      <CardHeader className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <CardTitle className="flex items-center gap-2">
          <TriangleAlert
            className={cn("size-4", activeCount > 0 ? "text-destructive" : "text-muted-foreground")}
          />
          Cảnh báo chất lượng phòng
          {activeCount > 0 && <Badge variant="destructive">{activeCount}</Badge>}
        </CardTitle>
        <div className="flex items-center gap-2">
          <Switch id="room-quality-show-resolved" checked={showResolved} onCheckedChange={setShowResolved} />
          <Label htmlFor="room-quality-show-resolved" className="text-sm text-muted-foreground">
            Hiện cả cảnh báo đã kết thúc
          </Label>
        </div>
      </CardHeader>
      <CardContent>
        <QueryState
          query={query}
          skeleton={
            <div className="space-y-2">
              <LineSkeleton className="h-14" />
              <LineSkeleton className="h-14" />
            </div>
          }
          isEmpty={(data) => data.length === 0}
          empty={
            <div className="flex items-center gap-2 text-sm text-muted-foreground">
              <CircleCheck className="size-4 text-success" />
              {showResolved
                ? "Chưa có cảnh báo chất lượng phòng nào."
                : "Không có phòng nào bị điểm thấp liên tục."}
            </div>
          }
          errorTitle="Không tải được cảnh báo chất lượng phòng"
        >
          {(data) => (
            <ul className="space-y-2">
              {data.map((alert) => (
                <AlertRow key={alert.roomQualityAlertId} alert={alert} />
              ))}
            </ul>
          )}
        </QueryState>
      </CardContent>
    </Card>
  );
};
