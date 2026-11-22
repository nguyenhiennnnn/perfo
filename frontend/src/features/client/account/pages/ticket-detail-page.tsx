import { useParams } from "react-router";
import { Ticket as TicketIcon } from "lucide-react";
import { NotFoundState } from "@/components/common/not-found-state";
import { QueryState } from "@/components/common/query-state";
import { Button } from "@/components/ui/button";
import { PageContainer } from "@/components/common/page-container";
import { PerfoTicket } from "@/components/common/perfo-ticket";
import { DownloadTicketsPdfButton } from "@/components/common/download-tickets-pdf-button";
import { useCancelOrder, useOrderDetail } from "@/features/client/orders/hooks/use-orders";
import { useCancelPolicy } from "@/features/client/orders/hooks/use-cancel-policy";
import type { OrderDetailDto } from "@/features/client/orders/types";
import { getOrderShowing, getTicketLines } from "@/features/client/orders/utils/order-lines";
import { OrderCancelSection } from "@/features/client/orders/components/detail/order-cancel-section";
import { OrderDetailHeader } from "@/features/client/orders/components/detail/order-detail-header";
import { OrderDetailSkeleton } from "@/features/client/orders/components/detail/order-detail-skeleton";
import { OrderLinesSection } from "@/features/client/orders/components/detail/order-lines-section";
import { OrderPaymentPrompt } from "@/features/client/orders/components/detail/order-payment-prompt";
import { OrderPriceSection } from "@/features/client/orders/components/detail/order-price-section";
import { OrderTransactionsSection } from "@/features/client/orders/components/detail/order-transactions-section";
import { useAlertDialogOpen } from "@/stores/alert-dialog-store";
import { formatCurrency, formatDateTime } from "@/lib/formatters";
import { CancelReason, OrderStatus } from "@/types/enum";
import { ROUTES } from "@/config/routes";
import { ENDPOINTS } from "@/api/endpoints";

const TicketDetailContent = ({ order }: { order: OrderDetailDto }) => {
  const cancelOrder = useCancelOrder();
  const showing = getOrderShowing(order);
  const ticketLines = getTicketLines(order);
  const openAlert = useAlertDialogOpen();
  const cancelPolicy = useCancelPolicy(order.status, showing?.startTime, order.finalAmount);

  const isConfirmed = order.status === OrderStatus.Confirmed;

  const cancelNotice =
    cancelPolicy.blockReason === "nonRefundable"
      ? "Rạp hiện không hoàn tiền khi hủy vé nên đơn này không thể hủy."
      : cancelPolicy.blockReason === "deadlinePassed"
        ? `Đã quá hạn hủy vé. Bạn chỉ có thể hủy trước giờ chiếu ít nhất ${cancelPolicy.deadlineHours} giờ.`
        : null;

  const handleCancel = () => {
    openAlert({
      title: "Hủy vé & hoàn tiền",
      description: `Bạn sẽ được hoàn ${formatCurrency(cancelPolicy.refundAmount)} (${cancelPolicy.refundPercent}% giá trị đơn) theo chính sách của rạp. Thao tác này không thể hoàn tác.`,
      confirmText: "Hủy vé",
      variant: "destructive",
      onConfirm: () => cancelOrder.mutate({ orderId: order.orderId, reason: CancelReason.CustomerChangedMind }),
    });
  };

  const ticketsSection = showing && ticketLines.length > 0 && (
    <section aria-labelledby="ticket-section-title">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 id="ticket-section-title" className="t-h2 text-foreground">
          Vé điện tử & Mã QR
        </h2>
        {isConfirmed && (
          <DownloadTicketsPdfButton
            endpoint={ENDPOINTS.orders.ticketsPdf(order.orderId)}
            fallbackFilename={`tickets-${order.orderCode}.pdf`}
          />
        )}
      </div>
      <div className="mt-3 flex flex-col gap-4">
        {ticketLines.map((line) => {
          const { ticket } = line;
          const seatLabel = `${ticket.seatRow}${ticket.seatNumber}`;
          return (
            <PerfoTicket
              key={line.orderLineId}
              movieTitle={ticket.movieTitle}
              startTime={ticket.startTime}
              room={ticket.roomName}
              seat={seatLabel}
              price={line.lockedUnitPrice}
              ticketCode={`${order.orderCode}-${seatLabel}`}
              qrValue={ticket.qrCodeImage ?? order.orderCode}
              isCheckedIn={ticket.isCheckedIn}
            />
          );
        })}
      </div>
    </section>
  );

  return (
    <div className="space-y-6">
      <OrderDetailHeader order={order} />

      <OrderPaymentPrompt order={order} />

      <OrderCancelSection order={order} />

      {isConfirmed && ticketsSection}

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
          <div>
            <h2 className="t-h1 text-foreground">{showing.movieTitle}</h2>
            <p className="mt-1 text-sm text-muted-foreground">{showing.roomName}</p>
            <p className="mt-1 text-sm text-muted-foreground">{formatDateTime(showing.startTime)}</p>
          </div>
        </div>
      )}

      <OrderLinesSection order={order} />

      <OrderPriceSection order={order} />

      <OrderTransactionsSection order={order} />

      {!isConfirmed && ticketsSection}

      {cancelPolicy.canCancel && (
        <div className="flex justify-end">
          <Button variant="destructive" onClick={handleCancel}>
            Hủy vé & Hoàn tiền
          </Button>
        </div>
      )}
      {isConfirmed && cancelNotice && (
        <p data-testid="ticket-cancel-notice" className="text-sm text-muted-foreground">
          {cancelNotice}
        </p>
      )}
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