import { ArrowRight, History } from "lucide-react";
import { Link } from "react-router";
import { ROUTES } from "@/config/routes";
import { formatCurrency, formatDateTime } from "@/lib/formatters";
import type { OrderDetailDto } from "../../types";
import { groupExchangeHistory } from "../../utils/order-exchange";

interface OrderExchangeHistorySectionProps {
  order: OrderDetailDto;
  linkTo?: (orderId: number) => string;
}

export const OrderExchangeHistorySection = ({
  order,
  linkTo = ROUTES.account.ticketDetail,
}: OrderExchangeHistorySectionProps) => {
  const groups = groupExchangeHistory(order.exchanges ?? []);
  if (groups.length === 0) return null;

  return (
    <section aria-labelledby="order-exchange-title" className="cut-md bg-card p-4">
      <h2 id="order-exchange-title" className="t-h2 flex items-center gap-2 text-foreground">
        <History className="size-5 text-muted-foreground" aria-hidden />
        Lịch sử đổi suất
      </h2>
      <ol className="mt-3 flex flex-col gap-3">
        {groups.map((group) => {
          const isFromThisOrder = group.originalOrderId === order.orderId;
          return (
            <li key={group.key} className="flex flex-col gap-2 cut-sm bg-muted/40 p-3 text-sm">
              <div className="flex flex-wrap items-center justify-between gap-2">
                <span className="font-semibold text-foreground">
                  Lần đổi {group.exchangeNumber} · {formatDateTime(group.exchangedAt)}
                </span>
                <span className="text-xs text-muted-foreground">
                  {group.channel === "Counter" ? "Đổi tại quầy" : "Đổi trực tuyến"}
                </span>
              </div>
              <p className="font-medium text-foreground">{group.movieTitle}</p>
              <div className="flex flex-col gap-1 sm:flex-row sm:items-center sm:gap-3">
                <span className="text-muted-foreground">
                  {formatDateTime(group.oldStartTime)} · {group.oldRoomName}
                </span>
                <ArrowRight className="hidden size-4 shrink-0 text-muted-foreground sm:block" aria-hidden />
                <span className="text-foreground">
                  {formatDateTime(group.newStartTime)} · {group.newRoomName}
                </span>
              </div>
              <p className="text-muted-foreground">
                Ghế:{" "}
                <span className="t-code text-foreground">
                  {group.seats.map((seat) => `${seat.oldSeat} → ${seat.newSeat}`).join(", ")}
                </span>
              </p>
              <div className="flex flex-wrap items-center justify-between gap-2">
                <span className="font-semibold tabular-nums text-foreground">
                  {group.priceDifference > 0
                    ? `Thu thêm ${formatCurrency(group.priceDifference)}`
                    : group.priceDifference < 0
                      ? `Hoàn lại ${formatCurrency(-group.priceDifference)}`
                      : "Không chênh lệch"}
                </span>
                <span className="text-xs text-muted-foreground">
                  {isFromThisOrder ? "Vé mới ở đơn " : "Đổi từ đơn "}
                  <Link
                    to={linkTo(isFromThisOrder ? group.newOrderId : group.originalOrderId)}
                    className="t-code font-medium text-primary-text underline-offset-2 hover:underline"
                  >
                    {isFromThisOrder ? group.newOrderCode : group.originalOrderCode}
                  </Link>
                </span>
              </div>
            </li>
          );
        })}
      </ol>
    </section>
  );
};
