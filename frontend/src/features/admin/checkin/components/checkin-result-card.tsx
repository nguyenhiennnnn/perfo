import { CheckCircle2, XCircle, Film, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/common/empty-state";
import { formatDateTime, formatTime } from "@/lib/formatters";
import type { CheckinResponse } from "@/features/admin/bookings/types";

interface CheckinResultCardProps {
  status: "idle" | "success" | "error";
  result?: CheckinResponse | null;
  errorMessage?: string | null;
  onClose: () => void;
}

export const CheckinResultCard = ({ status, result, errorMessage, onClose }: CheckinResultCardProps) => {
  if (status === "idle") {
    return (
      <Card className="p-5">
        <EmptyState icon={Film} title="Chưa có vé nào được soát" className="min-h-55" />
      </Card>
    );
  }

  if (status === "error") {
    return (
      <div role="alert" className="cut-lg bg-destructive p-6 text-destructive-foreground md:p-8">
        <div className="flex items-start justify-between gap-4">
          <div className="flex items-center gap-3">
            <XCircle className="size-10 shrink-0" />
            <p className="t-h2">Soát vé thất bại</p>
          </div>
          <Button
            type="button"
            variant="ghost"
            size="icon"
            aria-label="Đóng kết quả"
            className="hover:bg-destructive-foreground/15 hover:text-destructive-foreground"
            onClick={onClose}
          >
            <X className="size-5" />
          </Button>
        </div>
        <p className="mt-6 text-xl font-bold">
          {errorMessage ?? "Mã vé không hợp lệ hoặc đã được sử dụng. Kiểm tra mã vé rồi quét lại."}
        </p>
      </div>
    );
  }

  if (!result) return null;

  return (
    <div role="status" className="cut-lg bg-success p-6 text-success-foreground md:p-8">
      <div className="flex items-start justify-between gap-4">
        <div className="flex items-center gap-3">
          <CheckCircle2 className="size-10 shrink-0" />
          <div>
            <p className="t-h2">Check-in thành công</p>
            <p className="text-sm">Lúc {formatDateTime(result.checkedInAt)}</p>
          </div>
        </div>
        <Button
          type="button"
          variant="ghost"
          size="icon"
          aria-label="Đóng kết quả"
          className="hover:bg-success-foreground/15 hover:text-success-foreground"
          onClick={onClose}
        >
          <X className="size-5" />
        </Button>
      </div>

      <dl className="mt-6 grid grid-cols-2 gap-6">
        <div className="min-w-0">
          <dt className="text-sm font-medium">Ghế</dt>
          <dd className="t-numeral text-7xl wrap-break-word md:text-9xl">
            {result.seatRow}
            {result.seatNumber}
          </dd>
        </div>
        <div className="min-w-0">
          <dt className="text-sm font-medium">Phòng</dt>
          <dd className="t-numeral text-7xl wrap-break-word md:text-9xl">{result.roomName}</dd>
        </div>
      </dl>

      <div className="mt-6 flex flex-wrap items-baseline justify-between gap-x-6 gap-y-2">
        <p className="t-h2 min-w-0 wrap-break-word">{result.movieTitle}</p>
        <p className="t-numeral text-4xl">{formatTime(result.startTime)}</p>
      </div>
    </div>
  );
};
