import { useEffect, useRef, useState } from "react";
import { useSearchParams } from "react-router";
import { Ticket as TicketIcon } from "lucide-react";

import { NotFoundState } from "@/components/common/not-found-state";
import { PageContainer } from "@/components/common/page-container";
import { QueryState } from "@/components/common/query-state";
import {
  BOOKING_HOLD_POLL_INTERVAL_MS,
  BOOKING_RESULT_POLL_INTERVAL_MS,
} from "@/config/default-constants";
import { ROUTES } from "@/config/routes";

import { useOrderDetail } from "../hooks/use-orders";
import { BookingResultActions } from "../components/result/booking-result-actions";
import { BookingResultPayment } from "../components/result/booking-result-payment";
import { BookingResultSkeleton } from "../components/result/booking-result-skeleton";
import { BookingResultStatus } from "../components/result/booking-result-status";
import { OrderComboSupplementSection } from "../components/supplement/order-combo-supplement-section";
import { SupplementPickupSection } from "../components/supplement/supplement-pickup-section";
import { OrderTicketCards } from "../components/detail/order-tickets-section";
import { getPaymentResultState } from "../utils/order-payment";

const BookingResultPage = () => {
  const [searchParams] = useSearchParams();
  const orderId = Number(searchParams.get("bookingId"));
  const gatewayCanceled = searchParams.get("canceled") === "true";

  const orderDetail = useOrderDetail(orderId, {
    alwaysRefetchOnMount: true,
    getRefetchInterval: (order) => {
      const state = getPaymentResultState(order, gatewayCanceled);
      if (state === "processing") return BOOKING_RESULT_POLL_INTERVAL_MS;
      if (state === "outstanding" || state === "failed") return BOOKING_HOLD_POLL_INTERVAL_MS;
      return false;
    },
  });
  const order = orderDetail.data;
  const state = order ? getPaymentResultState(order, gatewayCanceled) : null;

  const [celebrate, setCelebrate] = useState(false);
  const hasCelebrated = useRef(false);

  useEffect(() => {
    if (state !== "confirmed" || hasCelebrated.current) return;
    hasCelebrated.current = true;
    setCelebrate(true);
  }, [state]);

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
          {(loadedOrder) => {
            const resultState = getPaymentResultState(loadedOrder, gatewayCanceled);
            return (
              <>
                <BookingResultStatus state={resultState} orderCode={loadedOrder.orderCode} />
                {resultState === "confirmed" && <OrderTicketCards order={loadedOrder} isTorn={celebrate} />}
                {resultState === "confirmed" && <SupplementPickupSection order={loadedOrder} />}
                {resultState === "confirmed" && <OrderComboSupplementSection order={loadedOrder} />}
                <BookingResultPayment order={loadedOrder} state={resultState} />
                <BookingResultActions order={loadedOrder} state={resultState} />
              </>
            );
          }}
        </QueryState>
      )}
    </PageContainer>
  );
};

export default BookingResultPage;
