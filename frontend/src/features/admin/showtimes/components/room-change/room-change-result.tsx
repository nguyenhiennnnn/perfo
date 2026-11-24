import { CheckCircle2 } from "lucide-react";

import { ButtonLink } from "@/components/common/button-link";
import { Button } from "@/components/ui/button";
import { ROUTES } from "@/config/routes";
import { formatCurrency, formatNumber } from "@/lib/formatters";

import type { ShowtimeRoomChangeResponse } from "../../types";

interface ResultStatProps {
  label: string;
  value: string;
  testId: string;
}

const ResultStat = ({ label, value, testId }: ResultStatProps) => (
  <div className="flex flex-col gap-1 cut-sm bg-secondary p-4">
    <p data-testid={testId} className="t-numeral text-2xl tabular-nums text-foreground">
      {value}
    </p>
    <p className="text-sm text-muted-foreground">{label}</p>
  </div>
);

interface RoomChangeResultProps {
  movieTitle: string;
  result: ShowtimeRoomChangeResponse;
  onClose: () => void;
}

export const RoomChangeResult = ({ movieTitle, result, onClose }: RoomChangeResultProps) => (
  <section className="flex flex-col gap-4">
    <div className="flex items-start gap-3">
      <CheckCircle2 className="mt-0.5 size-5 shrink-0 text-success" />
      <div className="min-w-0">
        <p className="font-heading text-base font-semibold text-foreground">
          Đã chuyển suất &quot;{movieTitle}&quot; sang {result.toRoomName}
        </p>
        <p className="text-sm text-muted-foreground">
          Từ {result.fromRoomName}. Các đơn được giữ nguyên và khách sẽ nhận thông báo đổi phòng.
        </p>
      </div>
    </div>

    <div className="grid grid-cols-2 gap-3 md:grid-cols-4">
      <ResultStat
        label="Ghế đã chuyển"
        value={formatNumber(result.seatsMoved)}
        testId="room-change-result-seats"
      />
      <ResultStat
        label="Đơn liên quan"
        value={formatNumber(result.orderCount)}
        testId="room-change-result-orders"
      />
      <ResultStat
        label="Vé cần hoàn chênh lệch"
        value={formatNumber(result.refundDueCount)}
        testId="room-change-result-refund-count"
      />
      <ResultStat
        label="Tổng tiền hoàn"
        value={formatCurrency(result.refundDueTotal)}
        testId="room-change-result-refund-total"
      />
    </div>

    {result.refundDueCount > 0 && (
      <div className="flex flex-col gap-3 cut-sm bg-warning/10 p-4 sm:flex-row sm:items-center sm:justify-between">
        <p className="text-sm text-warning-text">
          Có {formatNumber(result.refundDueCount)} vé cần hoàn chênh lệch, tổng{" "}
          {formatCurrency(result.refundDueTotal)}. Xác nhận hoàn tại trang hoàn chênh lệch đổi phòng.
        </p>
        <ButtonLink
          size="sm"
          to={ROUTES.admin.roomChangeRefundsByShowtime(result.showtimeId)}
          onClick={onClose}
        >
          Xử lý hoàn chênh lệch
        </ButtonLink>
      </div>
    )}

    <div className="flex flex-col gap-2 sm:flex-row sm:justify-end">
      <ButtonLink
        variant="outline"
        to={ROUTES.admin.ordersByShowtime(result.showtimeId)}
        onClick={onClose}
      >
        Xem đơn của suất chiếu
      </ButtonLink>
      <Button onClick={onClose}>Đóng</Button>
    </div>
  </section>
);
