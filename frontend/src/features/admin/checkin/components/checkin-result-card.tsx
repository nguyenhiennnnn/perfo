import { CheckCircle2, Copy, Film, QrCode, TicketX, X, XCircle } from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/common/empty-state";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { formatDateTime, formatTime } from "@/lib/formatters";
import { cn } from "@/lib/utils";
import type {
  CheckinErrorKind,
  CheckinFailure,
  CheckinResponse,
} from "../types";

type CheckinCardStatus = "idle" | "pending" | "success" | "error";

interface CheckinResultCardProps {
  status: CheckinCardStatus;
  result?: CheckinResponse | null;
  failure?: CheckinFailure | null;
  onClose: () => void;
}

interface FailureStyle {
  title: string;
  icon: LucideIcon;
  container: string;
  closeButton: string;
  divider: string;
}

const DESTRUCTIVE_STYLE = {
  container: "bg-destructive text-destructive-foreground",
  closeButton: "hover:bg-destructive-foreground/15 hover:text-destructive-foreground",
  divider: "border-destructive-foreground/25",
} as const;

const FAILURE_STYLES: Record<CheckinErrorKind, FailureStyle> = {
  duplicate: {
    title: "Vé đã được soát",
    icon: Copy,
    container: "bg-warning text-warning-foreground",
    closeButton: "hover:bg-warning-foreground/15 hover:text-warning-foreground",
    divider: "border-warning-foreground/25",
  },
  notFound: { title: "Mã QR không hợp lệ", icon: QrCode, ...DESTRUCTIVE_STYLE },
  invalid: { title: "Vé không hợp lệ", icon: TicketX, ...DESTRUCTIVE_STYLE },
  unknown: { title: "Soát vé thất bại", icon: XCircle, ...DESTRUCTIVE_STYLE },
};

const CloseButton = ({ className, onClick }: { className: string; onClick: () => void }) => (
  <Button
    type="button"
    variant="ghost"
    size="icon"
    aria-label="Đóng kết quả"
    className={className}
    onClick={onClick}
  >
    <X className="size-5" />
  </Button>
);

const FailureCard = ({
  failure,
  onClose,
}: {
  failure: CheckinFailure;
  onClose: () => void;
}) => {
  const style = FAILURE_STYLES[failure.kind];
  const Icon = style.icon;
  const previous = failure.previous;

  return (
    <div role="alert" className={cn("cut-lg p-6 md:p-8", style.container)}>
      <div className="flex items-start justify-between gap-4">
        <div className="flex items-center gap-3">
          <Icon className="size-10 shrink-0" />
          <p className="t-h2">{style.title}</p>
        </div>
        <CloseButton className={style.closeButton} onClick={onClose} />
      </div>
      <p className="mt-6 text-xl font-bold">{failure.message}</p>
      {previous && (
        <p
          className={cn(
            "mt-4 border-t pt-3 text-sm font-medium",
            style.divider,
          )}
        >
          Ghế {previous.seatRow}
          {previous.seatNumber}, phòng {previous.roomName}, đã soát lúc{" "}
          {formatDateTime(previous.checkedInAt)}.
        </p>
      )}
    </div>
  );
};

export const CheckinResultCard = ({
  status,
  result,
  failure,
  onClose,
}: CheckinResultCardProps) => {
  if (status === "pending") {
    return (
      <div aria-busy="true">
        <CardSkeleton cut="lg" className="h-56" />
      </div>
    );
  }

  if (status === "idle") {
    return (
      <Card className="p-5">
        <EmptyState icon={Film} title="Chưa có vé nào được soát" className="min-h-55" />
      </Card>
    );
  }

  if (status === "error") {
    if (!failure) return null;
    return <FailureCard failure={failure} onClose={onClose} />;
  }

  if (!result) return null;

  return (
    <div role="status" className="cut-lg bg-success p-4 text-success-foreground md:p-5">
      <div className="flex items-start justify-between gap-4">
        <div className="flex items-center gap-3">
          <CheckCircle2 className="size-8 shrink-0" />
          <div>
            <p className="t-h3">Check-in thành công</p>
            <p className="text-sm">Lúc {formatDateTime(result.checkedInAt)}</p>
          </div>
        </div>
        <CloseButton
          className="hover:bg-success-foreground/15 hover:text-success-foreground"
          onClick={onClose}
        />
      </div>

      <dl className="mt-4 grid grid-cols-[auto_1fr] items-end gap-x-8 gap-y-2">
        <div className="min-w-0">
          <dt className="text-sm font-medium">Ghế</dt>
          <dd className="t-numeral text-6xl wrap-break-word md:text-7xl">
            {result.seatRow}
            {result.seatNumber}
          </dd>
        </div>
        <div className="min-w-0">
          <dt className="text-sm font-medium">Phòng</dt>
          <dd className="t-numeral text-3xl wrap-break-word md:text-4xl">{result.roomName}</dd>
        </div>
      </dl>

      <div className="mt-4 flex flex-wrap items-baseline justify-between gap-x-6 gap-y-1 border-t border-success-foreground/25 pt-3">
        <p className="t-h3 min-w-0 wrap-break-word">{result.movieTitle}</p>
        <p className="t-numeral text-3xl">{formatTime(result.startTime)}</p>
      </div>

      <div className="mt-3 flex flex-wrap items-center gap-x-4 gap-y-2 text-sm">
        <span>
          Mã đơn <span className="t-code font-bold">{result.orderCode}</span>
        </span>
        {result.ticketTypeName && (
          <Badge variant="secondary">{result.ticketTypeName}</Badge>
        )}
      </div>
    </div>
  );
};
