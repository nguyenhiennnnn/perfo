import { ButtonLink } from "@/components/common/button-link";
import { ROUTES } from "@/config/routes";
import type { OrderDetailDto } from "../../types";
import { getOrderShowing } from "../../utils/order-lines";
import type { PaymentResultState } from "../../utils/order-payment";

interface BookingResultActionsProps {
  order: OrderDetailDto;
  state: PaymentResultState;
}

export const BookingResultActions = ({ order, state }: BookingResultActionsProps) => {
  const showtimeId = getOrderShowing(order)?.showtimeId;

  return (
    <div className="flex w-full flex-wrap gap-3">
      {state === "confirmed" && (
        <ButtonLink to={ROUTES.account.ticketDetail(order.orderId)}>Xem chi tiết vé</ButtonLink>
      )}
      {(state === "outstanding" || state === "failed") && showtimeId && (
        <ButtonLink to={`${ROUTES.booking.detail(showtimeId)}?resume=${order.orderId}`}>
          Thanh toán phần còn lại
        </ButtonLink>
      )}
      {(state === "expired" || state === "cancelled") && (
        <ButtonLink to={showtimeId ? ROUTES.booking.detail(showtimeId) : ROUTES.schedule}>
          {showtimeId ? "Đặt lại vé" : "Xem lịch chiếu"}
        </ButtonLink>
      )}
      <ButtonLink to={ROUTES.account.tickets} variant="outline">
        Xem vé của tôi
      </ButtonLink>
      <ButtonLink to={ROUTES.home} variant="outline">
        Về trang chủ
      </ButtonLink>
    </div>
  );
};
