import { useParams } from "react-router";
import { Ticket as TicketIcon } from "lucide-react";
import { NotFoundState } from "@/components/common/not-found-state";
import { QueryState } from "@/components/common/query-state";
import { PageContainer } from "@/components/common/page-container";
import { useOrderDetail } from "@/features/client/orders/hooks/use-orders";
import type { OrderDetailDto } from "@/features/client/orders/types";
import { getOrderShowing } from "@/features/client/orders/utils/order-lines";
import { OrderCancelAction } from "@/features/client/orders/components/detail/order-cancel-action";
import { OrderCancelSection } from "@/features/client/orders/components/detail/order-cancel-section";
import { OrderExchangeAction } from "@/features/client/orders/components/detail/order-exchange-action";
import { OrderExchangeHistorySection } from "@/features/client/orders/components/exchange/order-exchange-history-section";
import { OrderDetailHeader } from "@/features/client/orders/components/detail/order-detail-header";
import { OrderDetailSkeleton } from "@/features/client/orders/components/detail/order-detail-skeleton";
import { OrderInvoiceSection } from "@/features/client/invoices/components/order-invoice-section";
import { OrderLinesSection } from "@/features/client/orders/components/detail/order-lines-section";
import { OrderPaymentPrompt } from "@/features/client/orders/components/detail/order-payment-prompt";
import { OrderPriceSection } from "@/features/client/orders/components/detail/order-price-section";
import { OrderTicketsSection } from "@/features/client/orders/components/detail/order-tickets-section";
import { OrderTransactionsSection } from "@/features/client/orders/components/detail/order-transactions-section";
import { formatDateTime } from "@/lib/formatters";
import { ROUTES } from "@/config/routes";

const TicketDetailContent = ({ order }: { order: OrderDetailDto }) => {
  const showing = getOrderShowing(order);

  return (
    <div className="space-y-6">
      <OrderDetailHeader order={order} />

      <OrderPaymentPrompt order={order} />

      <OrderCancelSection order={order} />

      <OrderTicketsSection order={order} />

      {showing && (
        <div className="flex gap-4 cut-md bg-card p-4">
          <div className="aspect-2/3 w-24 shrink-0 overflow-hidden bg-muted">
            {showing.posterUrl ? (
              <img src={showing.posterUrl} alt={showing.movieTitle} className="h-full w-full object-cover" />
            ) : (
              <div className="flex h-full w-full items-center justify-center text-muted-foreground">
                <TicketIcon className="size-6" />
              </div>
            )}
          </div>
          <div className="min-w-0">
            <h2 className="t-h1 text-foreground">{showing.movieTitle}</h2>
            <p className="mt-1 text-sm text-muted-foreground">{showing.roomName}</p>
            <p className="mt-1 text-sm text-muted-foreground">{formatDateTime(showing.startTime)}</p>
          </div>
        </div>
      )}

      <OrderLinesSection order={order} />

      <OrderPriceSection order={order} />

      <OrderTransactionsSection order={order} />

      <OrderInvoiceSection order={order} />

      <OrderExchangeHistorySection order={order} />

      <OrderExchangeAction order={order} />

      <OrderCancelAction order={order} />
    </div>
  );
};

const AccountTicketDetailPage = () => {
  const { id } = useParams<{ id: string }>();
  const orderQuery = useOrderDetail(Number(id));

  return (
    <PageContainer size="sm">
      <QueryState
        query={orderQuery}
        skeleton={<OrderDetailSkeleton />}
        notFound={
          <NotFoundState
            entity="đơn hàng"
            icon={TicketIcon}
            backLabel="Xem lịch sử đơn"
            backTo={ROUTES.account.tickets}
          />
        }
        errorTitle="Không tải được đơn hàng"
      >
        {(order) => <TicketDetailContent order={order} />}
      </QueryState>
    </PageContainer>
  );
};

export default AccountTicketDetailPage;
