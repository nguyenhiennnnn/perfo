import { ButtonLink } from "@/components/common/button-link";
import { ROUTES } from "@/config/routes";
import type { OrderDetailDto } from "../../types";
import { OrderType } from "@/types/enum";
import { getOrderShowing } from "../../utils/order-lines";
import type { PaymentResultState } from "../../utils/order-payment";

interface BookingResultActionsProps {
  order: OrderDetailDto;
  state: PaymentResultState;
}

const MembershipResultActions = ({ order, state }: BookingResultActionsProps) => (
  <div className="flex w-full flex-wrap gap-3">
    {state === "confirmed" && (
      <ButtonLink to={ROUTES.account.membership}>Xem gói của tôi</ButtonLink>
    )}
    {(state === "outstanding" || state === "failed") && (
      <ButtonLink to={ROUTES.membershipPlanResume(order.orderId)}>Thanh toán phần còn lại</ButtonLink>
    )}
    {(state === "expired" || state === "cancelled") && (
      <ButtonLink to={ROUTES.membershipPlans}>Mua lại gói</ButtonLink>
    )}
    <ButtonLink to={ROUTES.membershipPlans} variant="outline">
      Xem các gói
    </ButtonLink>
    <ButtonLink to={ROUTES.home} variant="outline">
      Về trang chủ
    </ButtonLink>
  </div>
);

export const BookingResultActions = ({ order, state }: BookingResultActionsProps) => {
  if (order.type === OrderType.Membership) {
    return <MembershipResultActions order={order} state={state} />;
  }

  const showtimeId = getOrderShowing(order)?.showtimeId;
  const isSupplement = order.type === OrderType.Supplement;
  const resumeTo = isSupplement
    ? ROUTES.booking.supplement(order.orderId)
    : showtimeId
      ? `${ROUTES.booking.detail(showtimeId)}?resume=${order.orderId}`
      : null;
  const restartTo = isSupplement
    ? order.originalOrderId
      ? ROUTES.account.ticketDetail(order.originalOrderId)
      : ROUTES.account.tickets
    : showtimeId
      ? ROUTES.booking.detail(showtimeId)
      : ROUTES.schedule;
  const restartLabel = isSupplement ? "Về đơn vé" : showtimeId ? "Đặt lại vé" : "Xem lịch chiếu";

  return (
    <div className="flex w-full flex-wrap gap-3">
      {state === "confirmed" && (
        <ButtonLink to={ROUTES.account.ticketDetail(order.orderId)}>Xem chi tiết vé</ButtonLink>
      )}
      {(state === "outstanding" || state === "failed") && resumeTo && (
        <ButtonLink to={resumeTo}>Thanh toán phần còn lại</ButtonLink>
      )}
      {(state === "expired" || state === "cancelled") && (
        <ButtonLink to={restartTo}>{restartLabel}</ButtonLink>
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
