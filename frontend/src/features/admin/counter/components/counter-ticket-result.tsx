import { CheckCircle2, PlusCircle } from "lucide-react";
import type { UseQueryResult } from "@tanstack/react-query";
import { Button } from "@/components/ui/button";
import { DownloadTicketsPdfButton } from "@/components/common/download-tickets-pdf-button";
import { QueryState } from "@/components/common/query-state";
import { TicketSkeleton } from "@/components/common/skeleton-shapes";
import { ENDPOINTS } from "@/api/endpoints";
import { formatCurrency } from "@/lib/formatters";
import { OrderStatus } from "@/types/enum";
import { OrderTicketCards } from "@/features/client/orders/components/detail/order-tickets-section";
import type { OrderDetailDto } from "@/features/admin/orders/types";
import type { CounterCheckoutResponse } from "../types";
import { CounterPaymentReceipts } from "./counter-payment-receipts";

interface CounterTicketResultProps {
  orderQuery: UseQueryResult<OrderDetailDto>;
  payments: CounterCheckoutResponse[];
  totalChange: number;
  onNewSale: () => void;
}

export const CounterTicketResult = ({
  orderQuery,
  payments,
  totalChange,
  onNewSale,
}: CounterTicketResultProps) => (
  <div
    className="flex flex-col items-center gap-6 py-6 text-center"
    aria-busy={orderQuery.isLoading || undefined}
  >
    <CheckCircle2 className="size-10 text-success" />
    <div>
      <p className="font-heading text-lg font-semibold text-foreground">Thanh toán thành công</p>
      <p className="text-sm text-muted-foreground">
        Tiền thối trả khách: {formatCurrency(totalChange)}
      </p>
    </div>

    <div className="w-full max-w-md text-left">
      <CounterPaymentReceipts payments={payments} />
    </div>

    <QueryState
      query={orderQuery}
      skeleton={<TicketSkeleton className="w-full max-w-3xl" />}
      errorTitle="Không tải được vé vừa bán"
      errorClassName="w-full max-w-3xl"
    >
      {(order) =>
        order.status === OrderStatus.Confirmed ? (
          <div className="flex w-full max-w-3xl flex-col gap-4 text-left">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <p className="t-code text-sm text-foreground">{order.orderCode}</p>
              <DownloadTicketsPdfButton
                endpoint={ENDPOINTS.admin.orders.ticketsPdf(order.orderId)}
                fallbackFilename={`tickets-${order.orderCode}.pdf`}
              />
            </div>
            <OrderTicketCards order={order} />
          </div>
        ) : (
          <div className="flex w-full max-w-3xl flex-col gap-2">
            <p className="text-sm text-muted-foreground">Đang xác nhận đơn và phát vé...</p>
            <TicketSkeleton className="w-full" />
          </div>
        )
      }
    </QueryState>

    <Button onClick={onNewSale}>
      <PlusCircle className="size-4" />
      <span>Bán vé mới</span>
    </Button>
  </div>
);
