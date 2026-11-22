import { useParams } from "react-router";
import { Ticket as TicketIcon } from "lucide-react";
import { CardSkeleton, LineSkeleton, TicketSkeleton } from "@/components/common/skeleton-shapes";
import { NotFoundState } from "@/components/common/not-found-state";
import { QueryState } from "@/components/common/query-state";
import { Button } from "@/components/ui/button";
import { ButtonLink } from "@/components/common/button-link";
import { PageContainer } from "@/components/common/page-container";
import { BookingStatusBadge, PaymentMethodBadge, PaymentStatusBadge } from "@/components/common/status-badge";
import { CopyButton } from "@/components/common/copy-button";
import { PerfoTicket } from "@/components/common/perfo-ticket";
import { DownloadTicketsPdfButton } from "@/components/common/download-tickets-pdf-button";
import { useCancelOrder, useOrderDetail } from "@/features/client/orders/hooks/use-orders";
import { useCancelPolicy } from "@/features/client/orders/hooks/use-cancel-policy";
import type { OrderDetailDto } from "@/features/client/orders/types";
import { getOrderCombos, getOrderSeats, getOrderShowing, getTicketLines } from "@/features/client/orders/utils/order-lines";
import { useAlertDialogOpen } from "@/stores/alert-dialog-store";
import { useCountdown } from "@/hooks/use-countdown";
import { formatCurrency, formatDateTime } from "@/lib/formatters";
import { CancelReason, OrderStatus, type PaymentStatus } from "@/types/enum";
import { ROUTES } from "@/config/routes";
import { ENDPOINTS } from "@/api/endpoints";

const TicketDetailSkeleton = () => (
  <div className="space-y-6" aria-busy="true">
    <CardSkeleton className="h-20" />
    <div>
      <LineSkeleton className="h-7 w-1/3" />
      <TicketSkeleton className="mt-3" />
    </div>
    <CardSkeleton className="h-44" />
    <CardSkeleton className="h-40" />
  </div>
);

const TicketDetailContent = ({ order }: { order: OrderDetailDto }) => {
  const cancelOrder = useCancelOrder();
  const showing = getOrderShowing(order);
  const seats = getOrderSeats(order);
  const combos = getOrderCombos(order);
  const ticketLines = getTicketLines(order);
  const openAlert = useAlertDialogOpen();
  const holdCountdown = useCountdown(order.expiresAt ?? null);
  const cancelPolicy = useCancelPolicy(order.status, showing?.startTime, order.finalAmount);

  const isConfirmed = order.status === OrderStatus.Confirmed;
  const isPending = order.status === OrderStatus.Pending;
  const isHoldExpired = isPending && (!order.expiresAt || holdCountdown.remainingMs <= 0);
  const canResumePayment = isPending && !isHoldExpired;

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

  if (!showing) return null;

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3 bg-card p-4">
        <div>
          <div className="flex items-center gap-2">
            <span className="t-code text-lg text-foreground">{order.orderCode}</span>
            <CopyButton value={order.orderCode} label="mã đặt vé" />
          </div>
          <p className="mt-1 text-xs text-muted-foreground">Đặt lúc {formatDateTime(order.createdAt)}</p>
        </div>
        <span data-testid="ticket-detail-status">
          <BookingStatusBadge status={order.status} />
        </span>
      </div>

      {isConfirmed && ticketsSection}

      {isPending && (
        <div className="flex flex-wrap items-center justify-between gap-3 bg-warning/10 p-4">
          <div>
            <p className="text-sm font-medium text-foreground">
              {canResumePayment
                ? "Đơn đang chờ thanh toán — hoàn tất trước khi hết hạn giữ ghế."
                : "Thời gian giữ ghế đã hết hạn."}
            </p>
            {canResumePayment && (
              <p className="mt-0.5 text-xs font-semibold text-warning-text">
                Còn {holdCountdown.label} để hoàn tất thanh toán
              </p>
            )}
          </div>
          {canResumePayment ? (
            <ButtonLink to={`${ROUTES.booking.detail(showing.showtimeId)}?resume=${order.orderId}`}>
              Thanh toán ngay
            </ButtonLink>
          ) : (
            <ButtonLink to={ROUTES.booking.detail(showing.showtimeId)} variant="outline">
              Đặt vé mới
            </ButtonLink>
          )}
        </div>
      )}

      <div className="flex gap-4 bg-card p-4">
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
          <h1 className="t-h1 text-foreground">{showing.movieTitle}</h1>
          <p className="mt-1 text-sm text-muted-foreground">{showing.roomName}</p>
          <p className="mt-1 text-sm text-muted-foreground">{formatDateTime(showing.startTime)}</p>
        </div>
      </div>

      <div className="bg-card p-4">
        <h2 className="t-h2 text-foreground">Ghế & Combo</h2>
        <div className="mt-3 space-y-1.5 text-sm">
          {seats.map((seat, i) => (
            <div key={i} className="flex justify-between text-foreground">
              <span>
                Ghế {seat.seatRow}
                {seat.seatNumber}
              </span>
              <span className="tabular-nums">{formatCurrency(seat.price)}</span>
            </div>
          ))}
          {combos.map((combo, i) => (
            <div key={i} className="flex justify-between text-foreground">
              <span>
                {combo.quantity}x {combo.name}
              </span>
              <span className="tabular-nums">{formatCurrency(combo.unitPrice * combo.quantity)}</span>
            </div>
          ))}
        </div>
        <div className="mt-3 space-y-1.5 border-t border-border pt-3 text-sm">
          <div className="flex justify-between text-muted-foreground">
            <span>Tổng tiền</span>
            <span className="tabular-nums">{formatCurrency(order.totalAmount)}</span>
          </div>
          {order.discountAmount > 0 && (
            <div className="flex justify-between text-muted-foreground">
              <span>Giảm giá</span>
              <span className="tabular-nums">-{formatCurrency(order.discountAmount)}</span>
            </div>
          )}
          <div className="flex justify-between text-base font-semibold text-foreground">
            <span>Thành tiền</span>
            <span className="tabular-nums text-primary-text">{formatCurrency(order.finalAmount)}</span>
          </div>
        </div>
      </div>

      {order.transactions.length > 0 && (
        <div className="bg-card p-4">
          <h2 className="t-h2 text-foreground">Thanh toán</h2>
          <div className="mt-3 space-y-2">
            {order.transactions.map((transaction) => (
              <div key={transaction.transactionId} className="flex items-center justify-between text-sm">
                <PaymentMethodBadge method={transaction.method} />
                <span className="tabular-nums text-foreground">{formatCurrency(transaction.amount)}</span>
                <PaymentStatusBadge status={transaction.status as PaymentStatus} />
              </div>
            ))}
          </div>
        </div>
      )}

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
        skeleton={<TicketDetailSkeleton />}
        notFound={
          <NotFoundState
            entity="đơn đặt vé"
            icon={TicketIcon}
            backLabel="Xem vé của tôi"
            backTo={ROUTES.account.tickets}
          />
        }
        errorTitle="Không tải được đơn đặt vé"
      >
        {(order) => <TicketDetailContent order={order} />}
      </QueryState>
    </PageContainer>
  );
};

export default AccountTicketDetailPage;