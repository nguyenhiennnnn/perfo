import { useSearchParams } from "react-router";
import { Gift } from "lucide-react";
import { ButtonLink } from "@/components/common/button-link";
import { EmptyState } from "@/components/common/empty-state";
import { PageContainer } from "@/components/common/page-container";
import { PageTitle } from "@/components/common/page-title";
import { QueryState } from "@/components/common/query-state";
import { Seo } from "@/components/common/seo";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { ROUTES } from "@/config/routes";
import { GiftCardPaymentStep } from "../components/gift-card-payment-step";
import { GiftCardPurchaseForm } from "../components/gift-card-purchase-form";
import { useGiftCardPurchaseFlow } from "../hooks/use-gift-card-purchase-flow";

const PurchaseSkeleton = () => (
  <div aria-busy="true" className="flex flex-col gap-6">
    <CardSkeleton className="h-48" />
    <CardSkeleton className="h-64" />
  </div>
);

const GiftCardPurchasePage = () => {
  const [searchParams] = useSearchParams();
  const resumeOrderId = Number(searchParams.get("resume") ?? 0) || undefined;
  const flow = useGiftCardPurchaseFlow(resumeOrderId);

  const renderBody = () => {
    if (flow.isResumeMode) {
      if (flow.isResumeLoading) return <PurchaseSkeleton />;
      if (flow.isResumeInvalid || !flow.holdResult) {
        return (
          <EmptyState
            icon={Gift}
            title="Đơn thẻ quà không còn chờ thanh toán"
            description="Đơn này đã thanh toán, đã hủy hoặc đã hết thời gian giữ. Bạn có thể mua thẻ mới."
            action={<ButtonLink to={ROUTES.giftCards}>Mua thẻ mới</ButtonLink>}
          />
        );
      }
    }

    if (flow.step === "payment" && flow.holdResult) {
      return (
        <GiftCardPaymentStep
          expiresAt={flow.holdResult.expiresAt}
          orderQuery={flow.paymentOrder}
          paymentMethod={flow.paymentMethod}
          onPaymentMethodChange={flow.setPaymentMethod}
          isSubmitting={flow.isSubmittingPayment}
          onSubmit={flow.submitPayment}
          onCancel={flow.cancelHold}
          isCancelling={flow.isCancelling}
        />
      );
    }

    return (
      <QueryState
        query={flow.settings}
        skeleton={<PurchaseSkeleton />}
        errorTitle="Không tải được cấu hình thẻ quà"
      >
        {(settings) => (
          <GiftCardPurchaseForm
            limits={settings}
            isPending={flow.isPurchasing}
            onSubmit={flow.submitPurchase}
          />
        )}
      </QueryState>
    );
  };

  return (
    <PageContainer size="sm">
      <Seo
        title="Mua thẻ quà tặng"
        description="Tặng thẻ quà xem phim cho người thân, chọn mệnh giá, lời nhắn và thời điểm gửi."
        canonical={ROUTES.giftCards}
      />
      <PageTitle
        title="Mua thẻ quà tặng"
        description="Chọn mệnh giá, nhập thông tin người nhận và hẹn thời điểm gửi."
        action={
          <div className="flex flex-wrap gap-2">
            <ButtonLink to={ROUTES.giftCardBulk} variant="outline">
              Quà doanh nghiệp
            </ButtonLink>
            <ButtonLink to={ROUTES.account.giftCards} variant="outline">
              Xem thẻ đã mua
            </ButtonLink>
          </div>
        }
      />
      {renderBody()}
    </PageContainer>
  );
};

export default GiftCardPurchasePage;
