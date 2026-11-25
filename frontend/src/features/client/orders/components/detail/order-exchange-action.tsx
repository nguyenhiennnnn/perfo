import { Store } from "lucide-react";
import { ButtonLink } from "@/components/common/button-link";
import { ROUTES } from "@/config/routes";
import type { OrderDetailDto } from "../../types";
import { getExchangeableLines, hasCashPayment } from "../../utils/order-exchange";

export const OrderExchangeAction = ({ order }: { order: OrderDetailDto }) => {
  if (getExchangeableLines(order).length === 0) return null;

  if (hasCashPayment(order)) {
    return (
      <div className="flex items-start gap-3 cut-md bg-card p-4 text-sm text-muted-foreground">
        <Store className="mt-0.5 size-5 shrink-0 text-foreground" />
        <p>
          Đơn có thanh toán tiền mặt nên chỉ đổi suất được tại quầy. Vui lòng mang mã đơn{" "}
          <span className="t-code font-medium text-foreground">{order.orderCode}</span> đến quầy vé.
        </p>
      </div>
    );
  }

  return (
    <div className="flex justify-end">
      <ButtonLink to={ROUTES.account.ticketExchange(order.orderId)} variant="outline">
        Đổi suất
      </ButtonLink>
    </div>
  );
};
