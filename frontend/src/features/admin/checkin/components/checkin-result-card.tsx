import { CheckCircle2, XCircle, Film, Armchair, Clock } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { EmptyState } from "@/components/common/empty-state";
import { formatDateTime, formatTime } from "@/lib/formatters";
import type { CheckinResponse } from "@/features/admin/bookings/types";


interface CheckinResultCardProps {
  status: "idle" | "success" | "error";
  result?: CheckinResponse | null;
  errorMessage?: string | null;
}

export const CheckinResultCard = ({ status, result, errorMessage }: CheckinResultCardProps) => {
  if (status === "idle") {
    return (
      <EmptyState icon={Film} title="Chưa có vé nào được soát" className="h-full min-h-55" />
    );
  }

  if (status === "error") {
    return (
      <div className="flex h-full min-h-55 flex-col items-center justify-center gap-3 cut-md bg-destructive/10 px-4 text-center">
        <XCircle className="size-10 text-destructive" />
        <p className="font-heading text-lg font-semibold text-destructive">Soát vé thất bại</p>
        <p className="text-sm text-muted-foreground">
          {errorMessage ?? "Mã vé không hợp lệ hoặc đã được sử dụng. Kiểm tra mã vé rồi quét lại."}
        </p>
      </div>
    );
  }

  if (!result) return null;

  return (
    <div className="flex h-full min-h-55 flex-col gap-3 cut-md bg-success/10 p-4">
      <div className="flex items-center gap-2">
        <CheckCircle2 className="size-8 text-success" />
        <div>
          <p className="font-heading text-lg font-semibold text-success">Check-in thành công</p>
          <p className="text-xs text-muted-foreground">Lúc {formatDateTime(result.checkedInAt)}</p>
        </div>
      </div>

      <div className="flex items-center gap-2 text-sm text-foreground">
        <Film className="size-4 text-muted-foreground" />
        <span className="line-clamp-1 font-medium">{result.movieTitle}</span>
      </div>

      <div className="flex items-center gap-2 text-sm text-foreground">
        <Armchair className="size-4 text-muted-foreground" />
        <span>
          Ghế{" "}
          <Badge variant="secondary" className="ml-1 tabular-nums">
            {result.seatRow}
            {result.seatNumber}
          </Badge>
        </span>
      </div>

      <div className="flex items-center gap-2 text-sm text-foreground">
        <Clock className="size-4 text-muted-foreground" />
        <span>Suất chiếu {formatTime(result.startTime)}</span>
      </div>
    </div>
  );
}