import { useParams } from "react-router";
import { Ban, ArrowLeft, TriangleAlert } from "lucide-react";

import { ENDPOINTS } from "@/api/endpoints";
import { ButtonLink } from "@/components/common/button-link";
import { DownloadTicketsPdfButton } from "@/components/common/download-tickets-pdf-button";
import { NotFoundState } from "@/components/common/not-found-state";
import { PageHeader } from "@/components/common/page-header";
import { QueryState } from "@/components/common/query-state";
import { Button } from "@/components/ui/button";
import { ROUTES } from "@/config/routes";
import { formatDateTime } from "@/lib/formatters";
import { OrderStatus } from "@/types/enum";

import { OrderCancelErrorNotice } from "../components/order-cancel-error-notice";
import { OrderCancelResult } from "../components/order-cancel-result";
import { OrderDetailSkeleton } from "../components/order-detail-skeleton";
import { OrderInfoCard } from "../components/order-info-card";
import { OrderLinesCard } from "../components/order-lines-card";
import { OrderSummaryCard } from "../components/order-summary-card";
import { OrderTransactionsCard } from "../components/order-transactions-card";
import { useAdminOrderDetail } from "../hooks/use-admin-orders";
import { useOrderCancelFlow } from "../hooks/use-order-cancel-flow";
import type { OrderDetailDto } from "../types";
import { canCancelOrder, hasFailedRefund } from "../utils/order-cancel";
import { hasTicketLines } from "../utils/order-detail";

const BackButton = () => (
  <ButtonLink to={ROUTES.admin.orders} variant="outline">
    <ArrowLeft /> Quay lại
  </ButtonLink>
);

interface OrderDetailViewProps {
  order: OrderDetailDto;
  onReload: () => void;
}

const OrderDetailView = ({ order, onReload }: OrderDetailViewProps) => {
  const { cancel, start } = useOrderCancelFlow();
  const result = cancel.data?.data;
  const showCancel = canCancelOrder(order.status) && !cancel.isSuccess;
  const showPdf = order.status === OrderStatus.Confirmed && hasTicketLines(order);

  return (
    <div>
      <PageHeader
        title={`Đơn hàng ${order.orderCode}`}
        description={`Tạo lúc ${formatDateTime(order.createdAt)}`}
        action={
          <div className="flex flex-wrap items-center gap-2">
            {showPdf && (
              <DownloadTicketsPdfButton
                size="default"
                endpoint={ENDPOINTS.admin.orders.ticketsPdf(order.orderId)}
                fallbackFilename={`tickets-${order.orderCode}.pdf`}
              />
            )}
            {showCancel && (
              <Button
                variant="destructive"
                disabled={cancel.isPending}
                onClick={() => start(order, onReload)}
              >
                <Ban /> Hủy đơn
              </Button>
            )}
            <BackButton />
          </div>
        }
      />

      {(result || cancel.error) && (
        <div className="mb-4 flex flex-col gap-4 cut-md bg-card p-4">
          {result && <OrderCancelResult result={result} />}
          {result && hasFailedRefund(result.refunds) && (
            <div
              role="alert"
              className="flex items-start gap-3 cut-sm bg-destructive/10 p-3 text-destructive"
            >
              <TriangleAlert className="mt-0.5 size-4 shrink-0" />
              <p className="text-sm">
                Có khoản hoàn không thực hiện được, cần xử lý thủ công.
              </p>
            </div>
          )}
          {cancel.error && <OrderCancelErrorNotice error={cancel.error} />}
        </div>
      )}

      <div className="grid gap-4 lg:grid-cols-3">
        <div className="min-w-0 space-y-4 lg:col-span-2">
          <OrderInfoCard order={order} />
          <OrderLinesCard lines={order.lines} />
          <OrderTransactionsCard transactions={order.transactions} />
        </div>
        <div className="min-w-0 space-y-4">
          <OrderSummaryCard order={order} />
        </div>
      </div>
    </div>
  );
};

const AdminOrderDetailPage = () => {
  const { id } = useParams();
  const detail = useAdminOrderDetail(Number(id));

  return (
    <QueryState
      query={detail}
      skeleton={
        <div>
          <PageHeader title="Chi tiết đơn hàng" action={<BackButton />} />
          <OrderDetailSkeleton />
        </div>
      }
      notFound={
        <NotFoundState
          entity="đơn hàng"
          backLabel="Về danh sách đơn hàng"
          backTo={ROUTES.admin.orders}
        />
      }
      errorTitle="Không tải được chi tiết đơn hàng"
    >
      {(order) => <OrderDetailView order={order} onReload={() => void detail.refetch()} />}
    </QueryState>
  );
};

export default AdminOrderDetailPage;
