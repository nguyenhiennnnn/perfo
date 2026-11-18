import { useEffect, useRef, useState } from "react";
import { useSearchParams } from "react-router";
import { Ticket as TicketIcon } from "lucide-react";

import { Skeleton } from "@/components/ui/skeleton";
import { ButtonLink } from "@/components/common/button-link";
import { Confetti } from "@/components/common/confetti";
import { EmptyState } from "@/components/common/empty-state";
import { PageContainer } from "@/components/common/page-container";
import { QueryState } from "@/components/common/query-state";
import { BookingStatus } from "@/types/enum";
import { BOOKING_RESULT_POLL_INTERVAL_MS } from "@/config/default-constants";

import { useBookingDetail } from "../hooks/use-bookings";
import { BookingResultStatus } from "../components/result/booking-result-status";
import { BookingResultTickets } from "../components/result/booking-result-tickets";
import { ROUTES } from "@/config/routes";

const BookingResultPage = () => {
  const [searchParams] = useSearchParams();
  const bookingId = Number(searchParams.get("bookingId"));

  const bookingDetail = useBookingDetail(bookingId);
  const { refetch } = bookingDetail;
  const status = bookingDetail.data?.status;
  const isPending = status === BookingStatus.Pending;

  const [celebrate, setCelebrate] = useState(false);
  const hasCelebrated = useRef(false);

  useEffect(() => {
    if (!isPending) return;
    const interval = setInterval(
      () => refetch(),
      BOOKING_RESULT_POLL_INTERVAL_MS,
    );
    return () => clearInterval(interval);
  }, [isPending, refetch]);

  useEffect(() => {
    if (status !== BookingStatus.Confirmed || hasCelebrated.current) return;
    hasCelebrated.current = true;
    setCelebrate(true);
  }, [status]);

  return (
    <PageContainer size="sm" className="flex min-h-[60vh] max-w-lg flex-col items-center justify-center gap-6 py-12">
      <Confetti active={celebrate} />
      {!bookingId ? (
        <p className="text-sm text-muted-foreground">Thiếu thông tin đơn đặt vé.</p>
      ) : (
        <QueryState
          query={bookingDetail}
          skeleton={<Skeleton className="h-64 w-full" />}
          empty={
            <EmptyState
              icon={TicketIcon}
              title="Không tìm thấy đơn đặt vé này"
              actionLabel="Thử lại"
              onAction={() => void refetch()}
              className="w-full"
            />
          }
          errorTitle="Không tải được đơn đặt vé"
          errorDescription="Kiểm tra kết nối mạng rồi thử lại."
          errorClassName="w-full"
        >
          {(booking) => (
            <>
              <BookingResultStatus status={booking.status} bookingCode={booking.bookingCode} />
              {booking.status === BookingStatus.Confirmed && <BookingResultTickets booking={booking} />}
              <div className="flex gap-3">
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
