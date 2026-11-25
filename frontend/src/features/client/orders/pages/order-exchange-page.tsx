import { useParams } from "react-router";
import { Ticket } from "lucide-react";
import { NotFoundState } from "@/components/common/not-found-state";
import { PageContainer } from "@/components/common/page-container";
import { QueryState } from "@/components/common/query-state";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { ROUTES } from "@/config/routes";
import { ordersApi } from "../api/orders-api";
import { OrderExchangeFlow } from "../components/exchange/order-exchange-flow";
import { useOrderDetail } from "../hooks/use-orders";
import type { OrderExchangeAdapter } from "../types";

const customerAdapter: OrderExchangeAdapter = {
  channel: "customer",
  preview: ordersApi.previewExchange,
  start: ordersApi.startExchange,
};

const OrderExchangePage = () => {
  const { id } = useParams<{ id: string }>();
  const orderId = Number(id);
  const orderQuery = useOrderDetail(orderId);

  return (
    <PageContainer size="xl" className="flex flex-col gap-6">
      <h1 className="t-h1 text-foreground">Đổi suất chiếu</h1>
      <QueryState
        query={orderQuery}
        skeleton={<CardSkeleton className="h-64" />}
        notFound={
          <NotFoundState entity="đơn hàng" icon={Ticket} backLabel="Xem lịch sử đơn" backTo={ROUTES.account.tickets} />
        }
        errorTitle="Không tải được đơn hàng"
      >
        {(order) => (
          <OrderExchangeFlow
            order={order}
            adapter={customerAdapter}
            backTo={ROUTES.account.ticketDetail(orderId)}
            getPaymentRoute={(exchange) =>
              `${ROUTES.booking.detail(exchange.newShowtimeId)}?resume=${exchange.exchangeOrderId}`
            }
          />
        )}
      </QueryState>
    </PageContainer>
  );
};

export default OrderExchangePage;
