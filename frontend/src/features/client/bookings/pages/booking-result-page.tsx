import { useEffect, useRef, useState } from "react";
import { useSearchParams } from "react-router";
import { Ticket as TicketIcon } from "lucide-react";

import { ButtonLink } from "@/components/common/button-link";
import { NotFoundState } from "@/components/common/not-found-state";
import { PageContainer } from "@/components/common/page-container";
import { QueryState } from "@/components/common/query-state";
import { BookingStatus } from "@/types/enum";
import { BOOKING_RESULT_POLL_INTERVAL_MS } from "@/config/default-constants";

import { useBookingDetail } from "../hooks/use-bookings";
import { BookingResultStatus } from "../components/result/booking-result-status";
import { BookingResultSkeleton } from "../components/result/booking-result-skeleton";
import { BookingResultTickets } from "../components/result/booking-result-tickets";
import { ROUTES } from "@/config/routes";

const BookingResultPage = () => {
  const [searchParams] = useSearchParams();
  const bookingId = Number(searchParams.get("bookingId"));

  const bookingDetail = useBookingDetail(bookingId, {
    pollWhilePendingMs: BOOKING_RESULT_POLL_INTERVAL_MS,
  });
  const status = bookingDetail.data?.status;

  const [celebrate, setCelebrate] = useState(false);
  const hasCelebrated = useRef(false);

  useEffect(() => {
    if (status !== BookingStatus.Confirmed || hasCelebrated.current) return;
    hasCelebrated.current = true;
    setCelebrate(true);
  }, [status]);

  return (
    <PageContainer size="sm" className="flex min-h-[60vh] max-w-2xl flex-col items-center justify-center gap-6 py-12">
      {!bookingId ? (
        <NotFoundState
          entity="đơn đặt vé"
          icon={TicketIcon}
          description="Đường dẫn thiếu mã đơn đặt vé. Mở lại đơn từ danh sách vé của bạn."
          backLabel="Xem vé của tôi"
          backTo={ROUTES.account.tickets}
          className="w-full"
        />
      ) : (
        <QueryState
          query={bookingDetail}
          skeleton={<BookingResultSkeleton />}
          notFound={
            <NotFoundState
              entity="đơn đặt vé"
              icon={TicketIcon}
              backLabel="Xem vé của tôi"
              backTo={ROUTES.account.tickets}
              className="w-full"
            />
          }
          errorTitle="Không tải được đơn đặt vé"
          errorClassName="w-full"
        >
          {(booking) => (
            <>
              <BookingResultStatus status={booking.status} bookingCode={booking.bookingCode} />
              {booking.status === BookingStatus.Confirmed && <BookingResultTickets booking={booking} isTorn={celebrate} />}
              <div className="flex w-full flex-wrap gap-3">
                <ButtonLink to={ROUTES.account.tickets}>Xem vé của tôi</ButtonLink>
                <ButtonLink to={ROUTES.home} variant="outline">
                  Về trang chủ
                </ButtonLink>
              </div>
            </>
          )}
        </QueryState>
      )}
    </PageContainer>
  );
};

export default BookingResultPage;
