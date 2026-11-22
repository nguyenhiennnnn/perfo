import { useEffect, useRef, useState } from "react";
import { useSearchParams } from "react-router";
import { Ticket as TicketIcon } from "lucide-react";

import { ButtonLink } from "@/components/common/button-link";
import { NotFoundState } from "@/components/common/not-found-state";
import { PageContainer } from "@/components/common/page-container";
import { QueryState } from "@/components/common/query-state";
import { OrderStatus } from "@/types/enum";
import { BOOKING_RESULT_POLL_INTERVAL_MS } from "@/config/default-constants";

import { useOrderDetail } from "../hooks/use-orders";
import { BookingResultStatus } from "../components/result/booking-result-status";
import { BookingResultSkeleton } from "../components/result/booking-result-skeleton";
import { BookingResultTickets } from "../components/result/booking-result-tickets";
import { ROUTES } from "@/config/routes";

const BookingResultPage = () => {
  const [searchParams] = useSearchParams();
  const orderId = Number(searchParams.get("bookingId"));

  const orderDetail = useOrderDetail(orderId, {
    pollWhilePendingMs: BOOKING_RESULT_POLL_INTERVAL_MS,
  });
  const status = orderDetail.data?.status;

  const [celebrate, setCelebrate] = useState(false);
  const hasCelebrated = useRef(false);

  useEffect(() => {
    if (status !== OrderStatus.Confirmed || hasCelebrated.current) return;
    hasCelebrated.current = true;
    setCelebrate(true);
  }, [status]);

  return (
    <PageContainer size="sm" className="flex min-h-[60vh] max-w-2xl flex-col items-center justify-center gap-6 py-12">
      {!orderId ? (
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
          query={orderDetail}
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
          {(order) => (
            <>
              <BookingResultStatus status={order.status} orderCode={order.orderCode} />
              {order.status === OrderStatus.Confirmed && <BookingResultTickets order={order} isTorn={celebrate} />}
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
