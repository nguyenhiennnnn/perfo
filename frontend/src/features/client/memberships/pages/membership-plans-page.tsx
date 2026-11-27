import { useMemo, useState } from "react";
import { useSearchParams } from "react-router";
import { Crown } from "lucide-react";
import { ButtonLink } from "@/components/common/button-link";
import { EmptyState } from "@/components/common/empty-state";
import { PageContainer } from "@/components/common/page-container";
import { PageTitle } from "@/components/common/page-title";
import { QueryState } from "@/components/common/query-state";
import { Seo } from "@/components/common/seo";
import { SkeletonList } from "@/components/common/skeleton-list";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { ROUTES } from "@/config/routes";
import { formatCurrency, formatDate } from "@/lib/formatters";
import { getServerNow } from "@/lib/server-clock";
import { useAlertDialogOpen } from "@/stores/alert-dialog-store";
import { MembershipPaymentStep } from "../components/membership-payment-step";
import { MembershipPlanCard } from "../components/membership-plan-card";
import { useMembershipPlans, useMySubscriptions } from "../hooks/use-membership-plans";
import { useMembershipPurchaseFlow } from "../hooks/use-membership-purchase-flow";
import type { MembershipPlanDto } from "../types";
import {
  MEMBERSHIP_PLAN_SKELETON_COUNT,
  getExpectedValidity,
  groupSubscriptions,
} from "../utils/membership-plans";

const PLAN_GRID_CLASS = "grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3";

const PaymentSkeleton = () => (
  <div aria-busy="true" className="mx-auto flex w-full max-w-lg flex-col gap-4">
    <CardSkeleton className="h-12" />
    <CardSkeleton className="h-64" />
  </div>
);

const MembershipPlansPage = () => {
  const [searchParams] = useSearchParams();
  const resumeOrderId = Number(searchParams.get("resume") ?? 0) || undefined;
  const highlightPlanId = Number(searchParams.get("plan") ?? 0) || undefined;
  const [purchasingPlanId, setPurchasingPlanId] = useState<number | null>(null);

  const flow = useMembershipPurchaseFlow(resumeOrderId);
  const plansQuery = useMembershipPlans();
  const subscriptionsQuery = useMySubscriptions(flow.isAuthenticated);
  const openAlertDialog = useAlertDialogOpen();

  const groups = useMemo(
    () => groupSubscriptions(subscriptionsQuery.data ?? [], getServerNow()),
    [subscriptionsQuery.data],
  );
  const ownedPlanIds = useMemo(() => {
    const ids = new Set<number>();
    if (groups.current) ids.add(groups.current.membershipPlanId);
    groups.upcoming.forEach((subscription) => ids.add(subscription.membershipPlanId));
    return ids;
  }, [groups]);

  const handlePurchase = (plan: MembershipPlanDto) => {
    const startPurchase = () => {
      setPurchasingPlanId(plan.membershipPlanId);
      flow.purchase(plan.membershipPlanId);
    };
    if (!flow.isAuthenticated) {
      startPurchase();
      return;
    }
    const validity = getExpectedValidity(plan, groups.renewalStart, getServerNow());
    openAlertDialog({
      title: `${validity.isRenewal ? "Gia hạn" : "Mua"} gói ${plan.name}`,
      content: (
        <div className="flex flex-col gap-2 text-sm">
          <dl className="flex flex-col gap-2">
            <div className="flex justify-between gap-3">
              <dt className="text-muted-foreground">Giá gói</dt>
              <dd className="font-semibold tabular-nums text-foreground">
                {formatCurrency(plan.price)}
              </dd>
            </div>
            <div className="flex justify-between gap-3">
              <dt className="text-muted-foreground">Hiệu lực dự kiến</dt>
              <dd className="font-semibold tabular-nums text-foreground">
                {formatDate(validity.from)} – {formatDate(validity.to)}
              </dd>
            </div>
          </dl>
          <p className="text-xs text-muted-foreground">
            {validity.isRenewal
              ? "Gói mới nối tiếp ngay sau khi gói hiện tại kết thúc. "
              : "Hiệu lực tính từ lúc thanh toán thành công. "}
            Đơn mua gói không áp dụng ưu đãi hạng thành viên, mã khuyến mãi và không tích điểm.
          </p>
        </div>
      ),
      confirmText: "Tiếp tục thanh toán",
      onConfirm: startPurchase,
    });
  };

  const renderBody = () => {
    if (flow.isResumeMode) {
      if (flow.isResumeLoading) return <PaymentSkeleton />;
      if (flow.isResumeInvalid || !flow.holdResult) {
        return (
          <EmptyState
            icon={Crown}
            title="Đơn gói thành viên không còn chờ thanh toán"
            description="Đơn này đã thanh toán, đã hủy hoặc đã hết thời gian giữ. Bạn có thể chọn gói khác."
            action={<ButtonLink to={ROUTES.membershipPlans}>Xem các gói</ButtonLink>}
          />
        );
      }
    }

    if (flow.holdResult) {
      return (
        <MembershipPaymentStep
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
      <div className="flex flex-col gap-6">
        {flow.isAuthenticated && groups.renewalStart && (
          <div role="status" className="flex flex-wrap items-center justify-between gap-3 cut-md bg-card p-4">
            <p className="text-sm text-foreground">
              {groups.current
                ? `Bạn đang có gói ${groups.current.planName}, hiệu lực đến ${formatDate(groups.current.validTo)}. `
                : "Bạn đã có gói sắp hiệu lực. "}
              Gói mua thêm sẽ nối tiếp từ {formatDate(groups.renewalStart)}.
            </p>
            <ButtonLink to={ROUTES.account.membership} variant="outline">
              Xem gói của tôi
            </ButtonLink>
          </div>
        )}

        <QueryState
          query={plansQuery}
          skeleton={
            <div aria-busy="true" className={PLAN_GRID_CLASS}>
              <SkeletonList count={MEMBERSHIP_PLAN_SKELETON_COUNT} className="h-80" />
            </div>
          }
          isEmpty={(plans) => plans.length === 0}
          empty={
            <EmptyState
              icon={Crown}
              title="Chưa có gói thành viên"
              description="Các gói thành viên trả phí sẽ hiện ở đây khi rạp mở bán."
            />
          }
          errorTitle="Không tải được các gói thành viên"
        >
          {(plans) => (
            <div className={PLAN_GRID_CLASS}>
              {plans.map((plan) => (
                <MembershipPlanCard
                  key={plan.membershipPlanId}
                  plan={plan}
                  isOwned={ownedPlanIds.has(plan.membershipPlanId)}
                  isHighlighted={plan.membershipPlanId === highlightPlanId}
                  isPending={flow.isPurchasing && purchasingPlanId === plan.membershipPlanId}
                  isDisabled={flow.isPurchasing}
                  onPurchase={handlePurchase}
                />
              ))}
            </div>
          )}
        </QueryState>
      </div>
    );
  };

  return (
    <PageContainer size="lg">
      <Seo
        title="Gói thành viên"
        description="Mua gói thành viên theo tháng hoặc theo quý để giảm giá vé, nhận vé miễn phí mỗi tháng và nhiều quyền lợi khác."
        canonical={ROUTES.membershipPlans}
      />
      <PageTitle
        title="Gói thành viên"
        description="Chọn gói theo tháng hoặc theo quý. Hết hạn bạn tự gia hạn, hệ thống không tự trừ tiền."
        action={
          flow.isAuthenticated ? (
            <ButtonLink to={ROUTES.account.membership} variant="outline">
              Gói của tôi
            </ButtonLink>
          ) : undefined
        }
      />
      {renderBody()}
    </PageContainer>
  );
};

export default MembershipPlansPage;
