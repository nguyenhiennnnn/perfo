import { useParams, useSearchParams } from "react-router";
import { CalendarX, Clock, Ticket } from "lucide-react";

import { useBookingFlow } from "../hooks/use-booking-flow";
import { BookingStepper } from "../components/booking/booking-stepper";
import { BookingShowtimeHeader } from "../components/booking/booking-showtime-header";
import { BOOKING_SELECTION_GRID_CLASS, BookingSelectionStep } from "../components/booking/booking-selection-step";
import { BookingPaymentStep } from "../components/booking/booking-payment-step";
import { ButtonLink } from "@/components/common/button-link";
import { EmptyState } from "@/components/common/empty-state";
import { NotFoundState } from "@/components/common/not-found-state";
import { PageContainer } from "@/components/common/page-container";
import { QueryState } from "@/components/common/query-state";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { ROUTES } from "@/config/routes";

const RESUME_NOTICE = {
  "not-pending": {
    icon: Ticket,
    title: "Đơn không còn chờ thanh toán",
    description: "Đơn này đã được thanh toán hoặc đã hủy. Xem vé của bạn hoặc đặt vé mới.",
    restartLabel: "Đặt vé mới",
  },
  expired: {
    icon: Clock,
    title: "Hết thời gian giữ ghế",
    description: "Thời gian giữ ghế cho đơn này đã hết. Chọn lại ghế để tiếp tục.",
    restartLabel: "Chọn lại ghế",
  },
} as const;

const SeatSelectionSkeleton = () => (
  <div aria-busy="true" className={BOOKING_SELECTION_GRID_CLASS}>
    <CardSkeleton className="h-96" cut="lg" />
    <CardSkeleton className="h-48" />
  </div>
);

const BookingPage = () => {
  const { showtimeId } = useParams<{ showtimeId: string }>();
  const [searchParams] = useSearchParams();
  const resumeOrderId = Number(searchParams.get("resume") ?? 0) || undefined;

  const flow = useBookingFlow(Number(showtimeId), resumeOrderId);

  return (
    <PageContainer size="xl" className="flex flex-col gap-6">
      <BookingShowtimeHeader data={flow.showtimeSeats.data} isLoading={flow.showtimeSeats.isLoading} />

      <BookingStepper step={flow.step} />

      {flow.isResumeLoading && (
        <div className="mx-auto w-full max-w-lg space-y-3" aria-busy="true">
          <CardSkeleton />
          <CardSkeleton />
        </div>
      )}

      {flow.isResumeMode && !flow.isResumeLoading && flow.resumeInvalidReason && (
        <div className="mx-auto w-full max-w-lg">
          {flow.resumeInvalidReason === "load-error" ? (
            <NotFoundState
              entity="đơn đặt vé"
              icon={Ticket}
              description="Đơn không tồn tại hoặc không thuộc tài khoản của bạn."
              backLabel="Xem vé của tôi"
              backTo={ROUTES.account.tickets}
            />
          ) : (
            <EmptyState
              icon={RESUME_NOTICE[flow.resumeInvalidReason].icon}
              title={RESUME_NOTICE[flow.resumeInvalidReason].title}
              description={RESUME_NOTICE[flow.resumeInvalidReason].description}
              action={
                <div className="flex gap-2">
                  <ButtonLink to={ROUTES.account.tickets} variant="outline">
                    Xem vé của tôi
                  </ButtonLink>
                  <ButtonLink to={ROUTES.booking.detail(showtimeId)}>
                    {RESUME_NOTICE[flow.resumeInvalidReason].restartLabel}
                  </ButtonLink>
                </div>
              }
            />
          )}
        </div>
      )}

      {flow.step === "select" && !flow.isResumeMode && (
        <QueryState
          query={flow.showtimeSeats}
          skeleton={<SeatSelectionSkeleton />}
          notFound={
            <NotFoundState
              entity="suất chiếu"
              icon={CalendarX}
              backLabel="Xem lịch chiếu"
              backTo={ROUTES.schedule}
            />
          }
          errorTitle="Không tải được sơ đồ ghế"
          isEmpty={flow.showtimeSeats.data?.seats.length === 0}
          empty={
            <EmptyState
              icon={CalendarX}
              title="Suất chiếu chưa có sơ đồ ghế"
              description="Suất chiếu này hiện chưa có ghế để đặt. Chọn suất chiếu khác để tiếp tục."
              action={<ButtonLink to={ROUTES.schedule}>Xem lịch chiếu</ButtonLink>}
            />
          }
        >
          <BookingSelectionStep
            data={flow.showtimeSeats.data}
            statusMap={flow.seatStatusMap}
            selectedIds={flow.selectedSeatIds}
            onToggleSeat={flow.toggleSeat}
            onSelectSeats={flow.selectSeats}
            comboQuantities={flow.comboQuantities}
            onComboChange={flow.setComboQuantity}
            customerName={flow.customerName}
            onCustomerNameChange={flow.setCustomerName}
            customerPhone={flow.customerPhone}
            onCustomerPhoneChange={flow.setCustomerPhone}
            isAuthenticated={flow.isAuthenticated}
            canHold={flow.canHold}
            loneSeats={flow.loneSeats}
            isHolding={flow.isHolding}
            onContinue={flow.holdAndContinue}
          />
        </QueryState>
      )}

      {flow.step === "payment" && flow.holdResult && !flow.resumeInvalidReason && (
        <>
          {flow.isResumeMode && (
            <p className="mx-auto w-full max-w-lg text-center text-xs text-muted-foreground">
              Đang tiếp tục thanh toán cho đơn{" "}
              <span className="tabular-nums font-medium text-foreground">
                {flow.holdResult.orderCode}
              </span>
            </p>
          )}
          <BookingPaymentStep
            seats={flow.selectedSeatsInfo}
            comboItems={flow.holdComboItems}
            hold={flow.holdResult}
            paymentMethod={flow.paymentMethod}
            onPaymentMethodChange={flow.setPaymentMethod}
            isSubmitting={flow.isSubmittingPayment}
            onSubmit={flow.submitPayment}
            onCancel={flow.cancelHold}
            isCancelling={flow.isCancelling}
          />
        </>
      )}
    </PageContainer>
  );
};

export default BookingPage;