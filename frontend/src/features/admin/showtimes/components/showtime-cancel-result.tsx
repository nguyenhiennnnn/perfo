import { Banknote, CheckCircle2, CreditCard } from "lucide-react";
import type { ReactNode } from "react";

import { ButtonLink } from "@/components/common/button-link";
import { ROUTES } from "@/config/routes";

import type { DeleteShowtimeResponse } from "../types";

interface ResultStatProps {
  icon: ReactNode;
  label: string;
  value: number;
  hint: string;
  testId: string;
}

const ResultStat = ({ icon, label, value, hint, testId }: ResultStatProps) => (
  <div className="flex flex-col gap-1 cut-sm bg-secondary p-4">
    <p className="flex items-center gap-2 text-sm font-medium text-foreground">
      {icon}
      {label}
    </p>
    <p
      data-testid={testId}
      className="font-heading text-3xl font-bold tabular-nums text-foreground"
    >
      {value}
    </p>
    <p className="text-xs text-muted-foreground">{hint}</p>
  </div>
);

interface ShowtimeCancelResultProps {
  movieTitle: string;
  result: DeleteShowtimeResponse;
  onNavigate: () => void;
}

export const ShowtimeCancelResult = ({
  movieTitle,
  result,
  onNavigate,
}: ShowtimeCancelResultProps) => {
  const autoCount = result.autoRefundQueuedCount ?? 0;
  const cashCount = result.manualRefundRequiredCount;
  const hasRefunds = autoCount > 0 || cashCount > 0;

  return (
    <section className="flex flex-col gap-4">
      <div className="flex items-start gap-3">
        <CheckCircle2 className="mt-0.5 size-5 shrink-0 text-success" />
        <div className="min-w-0">
          <p className="font-heading text-base font-semibold text-foreground">
            Đã hủy suất chiếu &quot;{movieTitle}&quot;
          </p>
          <p className="text-sm text-muted-foreground">
            {hasRefunds
              ? "Các đơn của suất chiếu đã bị hủy và được hoàn đầy đủ."
              : "Suất chiếu không có đơn nào cần hoàn tiền."}
          </p>
        </div>
      </div>

      {hasRefunds && (
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <ResultStat
            icon={<CreditCard className="size-4 text-success" />}
            label="Hoàn tự động"
            value={autoCount}
            hint="Đơn qua cổng thanh toán, hệ thống đang hoàn về cho khách."
            testId="showtime-cancel-auto-count"
          />
          <ResultStat
            icon={<Banknote className="size-4 text-warning-text" />}
            label="Cần hoàn tiền mặt"
            value={cashCount}
            hint="Đơn tiền mặt, hoàn cho khách tại quầy rồi xác nhận."
            testId="showtime-cancel-cash-count"
          />
        </div>
      )}

      <div className="flex flex-col gap-2 sm:flex-row sm:justify-end">
        <ButtonLink
          variant="outline"
          to={ROUTES.admin.ordersByShowtime(result.showtimeId)}
          onClick={onNavigate}
        >
          Xem đơn của suất chiếu
        </ButtonLink>
        {cashCount > 0 && (
          <ButtonLink to={ROUTES.admin.cashRefunds} onClick={onNavigate}>
            Xem đơn tiền mặt cần hoàn
          </ButtonLink>
        )}
      </div>
    </section>
  );
};
