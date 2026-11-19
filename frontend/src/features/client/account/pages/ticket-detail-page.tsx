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
import { useBookingDetail, useCancelBooking } from "@/features/client/bookings/hooks/use-bookings";
import { useCancelPolicy } from "@/features/client/bookings/hooks/use-cancel-policy";
import type { BookingDetailDto } from "@/features/client/bookings/types";
import { useAlertDialogOpen } from "@/stores/alert-dialog-store";
import { useCountdown } from "@/hooks/use-countdown";
import { formatCurrency, formatDateTime } from "@/lib/formatters";
import { BookingStatus } from "@/types/enum";
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

const TicketDetailContent = ({ booking }: { booking: BookingDetailDto }) => {
  const cancelBooking = useCancelBooking();
  const openAlert = useAlertDialogOpen();
  const holdCountdown = useCountdown(booking.holdExpiresAt ?? null);
  const cancelPolicy = useCancelPolicy(booking.status, booking.showtime.startTime, booking.finalAmount);

  const isConfirmed = booking.status === BookingStatus.Confirmed;
  const isPending = booking.status === BookingStatus.Pending;
  const isHoldExpired = isPending && (!booking.holdExpiresAt || holdCountdown.remainingMs <= 0);
  const canResumePayment = isPending && !isHoldExpired;

  const seatPrices = new Map(booking.seats.map((seat) => [`${seat.seatRow}${seat.seatNumber}`, seat.price]));

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
      onConfirm: () => cancelBooking.mutate(booking.bookingId),
    });
  };

  const ticketsSection = booking.tickets.length > 0 && (
    <section aria-labelledby="ticket-section-title">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 id="ticket-section-title" className="t-h2 text-foreground">
          Vé điện tử & Mã QR
        </h2>
        {isConfirmed && (
          <DownloadTicketsPdfButton
            endpoint={ENDPOINTS.bookings.ticketsPdf(booking.bookingId)}
            fallbackFilename={`tickets-${booking.bookingCode}.pdf`}
          />
        )}
      </div>
      <div className="mt-3 flex flex-col gap-4">
        {booking.tickets.map((ticket) => {
          const seatLabel = `${ticket.seatRow}${ticket.seatNumber}`;
          return (
            <PerfoTicket
              key={ticket.ticketId}
              movieTitle={booking.movie.title}
              startTime={booking.showtime.startTime}
              room={booking.showtime.roomName}
              seat={seatLabel}
              price={seatPrices.get(seatLabel) ?? 0}
              ticketCode={`${booking.bookingCode}-${seatLabel}`}
              qrValue={ticket.qrCodeImage}
              isCheckedIn={ticket.isCheckedIn}
            />
          );
        })}
      </div>
    </section>
  );

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3 bg-card p-4">
        <div>
          <div className="flex items-center gap-2">
            <span className="t-code text-lg text-foreground">{booking.bookingCode}</span>
            <CopyButton value={booking.bookingCode} label="mã đặt vé" />
          </div>
          <p className="mt-1 text-xs text-muted-foreground">Đặt lúc {formatDateTime(booking.createdAt)}</p>
        </div>
        <BookingStatusBadge status={booking.status} />
      </div>

      {isConfirmed && ticketsSection}

      {isPending && (
        <div className="flex flex-wrap items-center justify-between gap-3 bg-warning/10 p-4">
          <div>
            <p className="text-sm font-medium text-foreground">
              {canResumePayment
                ? "Booking đang chờ thanh toán — hoàn tất trước khi hết hạn giữ ghế."
                : "Thời gian giữ ghế đã hết hạn."}
            </p>
            {canResumePayment && (
              <p className="mt-0.5 text-xs font-semibold text-warning-text">
                Còn {holdCountdown.label} để hoàn tất thanh toán
              </p>
            )}
          </div>
          {canResumePayment ? (
            <ButtonLink to={`${ROUTES.booking.detail(booking.showtime.showtimeId)}?resume=${booking.bookingId}`}>
              Thanh toán ngay
            </ButtonLink>
          ) : (
            <ButtonLink to={ROUTES.booking.detail(booking.showtime.showtimeId)} variant="outline">
              Đặt vé mới
            </ButtonLink>
          )}
        </div>
      )}

      <div className="flex gap-4 bg-card p-4">
        <div className="aspect-2/3 w-24 shrink-0 overflow-hidden bg-muted">
          {booking.movie.posterUrl ? (
            <img src={booking.movie.posterUrl} alt={booking.movie.title} className="h-full w-full object-cover" />
          ) : (
            <div className="flex h-full w-full items-center justify-center text-muted-foreground">
              <TicketIcon className="size-6" />
            </div>
          )}
        </div>
        <div>
          <h1 className="t-h1 text-foreground">{booking.movie.title}</h1>
          <p className="mt-1 text-sm text-muted-foreground">{booking.showtime.roomName}</p>
          <p className="mt-1 text-sm text-muted-foreground">{formatDateTime(booking.showtime.startTime)}</p>
        </div>
      </div>

      <div className="bg-card p-4">
        <h2 className="t-h2 text-foreground">Ghế & Combo</h2>
        <div className="mt-3 space-y-1.5 text-sm">
          {booking.seats.map((seat, i) => (
            <div key={i} className="flex justify-between text-foreground">
              <span>
                Ghế {seat.seatRow}
                {seat.seatNumber}
              </span>
              <span className="tabular-nums">{formatCurrency(seat.price)}</span>
            </div>
          ))}
          {booking.combos.map((combo, i) => (
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
            <span className="tabular-nums">{formatCurrency(booking.totalAmount)}</span>
          </div>
          {booking.discountAmount > 0 && (
            <div className="flex justify-between text-muted-foreground">
              <span>Giảm giá</span>
              <span className="tabular-nums">-{formatCurrency(booking.discountAmount)}</span>
            </div>
          )}
          <div className="flex justify-between text-base font-semibold text-foreground">
            <span>Thành tiền</span>
            <span className="tabular-nums text-primary-text">{formatCurrency(booking.finalAmount)}</span>
          </div>
        </div>
      </div>

      {booking.payments.length > 0 && (
        <div className="bg-card p-4">
          <h2 className="t-h2 text-foreground">Thanh toán</h2>
          <div className="mt-3 space-y-2">
            {booking.payments.map((payment) => (
              <div key={payment.paymentId} className="flex items-center justify-between text-sm">
                <PaymentMethodBadge method={payment.paymentMethod} />
                <span className="tabular-nums text-foreground">{formatCurrency(payment.amount)}</span>
                <PaymentStatusBadge status={payment.status} />
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
      {isConfirmed && cancelNotice && <p className="text-sm text-muted-foreground">{cancelNotice}</p>}
    </div>
  );
};

const AccountTicketDetailPage = () => {
  const { id } = useParams<{ id: string }>();
  const bookingQuery = useBookingDetail(Number(id));

  return (
    <PageContainer size="sm">
      <QueryState
        query={bookingQuery}
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
        {(booking) => <TicketDetailContent booking={booking} />}
      </QueryState>
    </PageContainer>
  );
};

export default AccountTicketDetailPage;