import { useEffect } from "react";
import { useParams } from "react-router";
import { ArrowLeft } from "lucide-react";
import { ButtonLink } from "@/components/common/button-link";
import { NotFoundState } from "@/components/common/not-found-state";
import { PageHeader } from "@/components/common/page-header";
import { QueryState } from "@/components/common/query-state";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { ROUTES } from "@/config/routes";
import { CancelReason, OrderStatus } from "@/types/enum";
import { CounterCheckoutPanel } from "@/features/admin/counter/components/counter-checkout-panel";
import { useCounterCheckoutFlow } from "@/features/admin/counter/hooks/use-counter-checkout";
import { useCounterCancelOrder } from "@/features/admin/counter/hooks/use-counter-orders";
import type { CounterOrderResponse } from "@/features/admin/counter/types";
import {
  OrderExchangeFlow,
  type ExchangePaymentContext,
} from "@/features/client/orders/components/exchange/order-exchange-flow";
import type { OrderExchangeAdapter } from "@/features/client/orders/types";
import { ordersAdminApi } from "../api/orders-admin-api";
import { useAdminOrderDetail } from "../hooks/use-admin-orders";

const counterAdapter: OrderExchangeAdapter = {
  channel: "counter",
  preview: ordersAdminApi.previewExchange,
  start: ordersAdminApi.startExchange,
};

const ExchangePayment = ({ exchange, order, onCompleted, onBackToSeats, onExit }: ExchangePaymentContext) => {
  const cancel = useCounterCancelOrder();
  const counterOrder: CounterOrderResponse = {
    orderId: exchange.exchangeOrderId,
    orderCode: exchange.exchangeOrderCode,
    type: "Exchange",
    status: exchange.status,
    totalAmount: exchange.newAmount,
    discountAmount: exchange.newAmount - exchange.amountDue,
    finalAmount: exchange.amountDue,
    expiresAt: exchange.expiresAt,
    channel: "Counter",
    userId: order.userId,
    customerName: order.customerName,
    customerPhone: order.customerPhone,
  };
  const flow = useCounterCheckoutFlow(counterOrder, true);
  const confirmed = flow.orderDetail.data?.status === OrderStatus.Confirmed;

  useEffect(() => {
    if (confirmed) onCompleted();
  }, [confirmed, onCompleted]);

  const release = async () => {
    try {
      await cancel.mutateAsync({ orderId: exchange.exchangeOrderId, reason: CancelReason.CustomerChangedMind });
    } catch {
      return false;
    }
    return true;
  };

  return (
    <div className="mx-auto w-full max-w-lg">
      <CounterCheckoutPanel
        order={counterOrder}
        payments={flow.payments}
        outstandingAmount={flow.outstandingAmount}
        hasPartialPayment={flow.totalPaid > 0}
        isSubmitting={flow.isPaying}
        onPay={(request) => flow.pay(request, onCompleted)}
        onChangeSeats={async () => {
          if (await release()) onBackToSeats();
        }}
        onCancelOrder={async () => {
          if (await release()) onExit();
        }}
      />
    </div>
  );
};

const AdminOrderExchangePage = () => {
  const { id } = useParams();
  const orderId = Number(id);
  const detail = useAdminOrderDetail(orderId);
  const backTo = ROUTES.admin.orderDetail(orderId);

  return (
    <div>
      <PageHeader
        title="Đổi suất giúp khách"
        action={
          <ButtonLink to={backTo} variant="outline">
            <ArrowLeft /> Quay lại
          </ButtonLink>
        }
      />
      <QueryState
        query={detail}
        skeleton={<CardSkeleton className="h-64" />}
        notFound={<NotFoundState entity="đơn hàng" backLabel="Về danh sách đơn hàng" backTo={ROUTES.admin.orders} />}
        errorTitle="Không tải được chi tiết đơn hàng"
      >
        {(order) => (
          <OrderExchangeFlow
            order={order}
            adapter={counterAdapter}
            backTo={backTo}
            renderPayment={(context) => <ExchangePayment {...context} />}
          />
        )}
      </QueryState>
    </div>
  );
};

export default AdminOrderExchangePage;
