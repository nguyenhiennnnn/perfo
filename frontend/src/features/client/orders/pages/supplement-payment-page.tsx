import { useParams } from "react-router";
import { Clock, Popcorn, Ticket } from "lucide-react";
import { ButtonLink } from "@/components/common/button-link";
import { EmptyState } from "@/components/common/empty-state";
import { NotFoundState } from "@/components/common/not-found-state";
import { PageContainer } from "@/components/common/page-container";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { ROUTES } from "@/config/routes";
import { BookingPaymentStep } from "../components/booking/booking-payment-step";
import { useSupplementPaymentFlow } from "../hooks/use-supplement-payment-flow";

const SupplementPaymentPage = () => {
  const { orderId } = useParams<{ orderId: string }>();
  const flow = useSupplementPaymentFlow(Number(orderId));

  return (
    <PageContainer size="sm" className="flex flex-col gap-6">
      <div className="mx-auto w-full max-w-lg">
        <h1 className="t-h1 flex items-center gap-2 text-foreground">
          <Popcorn className="size-6 text-primary-text" aria-hidden />
          Thanh toán combo đặt trước
        </h1>
        {flow.order && (
          <p className="mt-1 text-sm text-muted-foreground">
            Đơn <span className="t-code font-medium text-foreground">{flow.order.orderCode}</span>, thanh toán riêng
            với đơn vé.
          </p>
        )}
      </div>

      {flow.isLoading && (
        <div className="mx-auto w-full max-w-lg space-y-3" aria-busy="true">
          <CardSkeleton />
          <CardSkeleton />
        </div>
      )}

      {flow.invalidReason === "load-error" && (
        <div className="mx-auto w-full max-w-lg">
          <NotFoundState
            entity="đơn combo"
            icon={Ticket}
            description="Đơn không tồn tại hoặc không thuộc tài khoản của bạn."
            backLabel="Xem vé của tôi"
            backTo={ROUTES.account.tickets}
          />
        </div>
      )}

      {(flow.invalidReason === "not-pending" || flow.invalidReason === "expired") && (
        <div className="mx-auto w-full max-w-lg">
          <EmptyState
            icon={flow.invalidReason === "expired" ? Clock : Ticket}
            title={flow.invalidReason === "expired" ? "Hết thời gian thanh toán" : "Đơn không còn chờ thanh toán"}
            description={
              flow.invalidReason === "expired"
                ? "Đơn combo đã hết hạn. Quay lại đơn vé để đặt lại combo."
                : "Đơn combo đã được thanh toán hoặc đã hủy. Xem chi tiết đơn để biết trạng thái mới nhất."
            }
            action={
              <div className="flex gap-2">
                <ButtonLink to={flow.backTo} variant="outline">
                  Về đơn vé
                </ButtonLink>
                {flow.invalidReason === "not-pending" && (
                  <ButtonLink to={ROUTES.account.ticketDetail(Number(orderId))}>Xem đơn combo</ButtonLink>
                )}
              </div>
            }
          />
        </div>
      )}

      {flow.order && !flow.invalidReason && flow.order.expiresAt && (
        <BookingPaymentStep
          expiresAt={flow.order.expiresAt}
          orderQuery={flow.orderQuery}
          paymentMethod={flow.paymentMethod}
          onPaymentMethodChange={flow.setPaymentMethod}
          isSubmitting={flow.isSubmittingPayment}
          onSubmit={flow.submitPayment}
          onCancel={flow.cancelHold}
          isCancelling={flow.isCancelling}
        />
      )}
    </PageContainer>
  );
};

export default SupplementPaymentPage;
