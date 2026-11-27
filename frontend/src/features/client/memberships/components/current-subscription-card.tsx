import { ButtonLink } from "@/components/common/button-link";
import { LineSkeleton } from "@/components/common/skeleton-shapes";
import { ROUTES } from "@/config/routes";
import { formatDate } from "@/lib/formatters";
import type { MembershipPlanDto, MembershipSubscriptionDto } from "../types";
import { getDaysLeft, getElapsedPercent, getPeriodLabel } from "../utils/membership-plans";
import { MembershipBenefitList } from "./membership-benefit-list";
import { MembershipStatusBadge } from "./membership-status-badge";

interface CurrentSubscriptionCardProps {
  subscription: MembershipSubscriptionDto;
  plan: MembershipPlanDto | undefined;
  isPlanLoading: boolean;
  now: number;
}

export const CurrentSubscriptionCard = ({
  subscription,
  plan,
  isPlanLoading,
  now,
}: CurrentSubscriptionCardProps) => {
  const daysLeft = getDaysLeft(subscription.validTo, now);
  const elapsed = getElapsedPercent(subscription.validFrom, subscription.validTo, now);
  const renewTo = plan
    ? `${ROUTES.membershipPlans}?plan=${plan.membershipPlanId}`
    : ROUTES.membershipPlans;

  return (
    <section aria-label="Gói đang hiệu lực" className="cut-md bg-card p-5">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <p className="text-xs text-muted-foreground">
            Gói đang hiệu lực{plan ? ` · ${getPeriodLabel(plan.period)}` : ""}
          </p>
          <h2 className="t-h3 mt-1 text-foreground">{subscription.planName}</h2>
        </div>
        <MembershipStatusBadge state="current" />
      </div>

      <dl className="mt-4 grid grid-cols-3 gap-3 text-sm">
        <div>
          <dt className="text-xs text-muted-foreground">Bắt đầu</dt>
          <dd className="mt-1 font-semibold tabular-nums text-foreground">
            {formatDate(subscription.validFrom)}
          </dd>
        </div>
        <div>
          <dt className="text-xs text-muted-foreground">Hết hạn</dt>
          <dd className="mt-1 font-semibold tabular-nums text-foreground">
            {formatDate(subscription.validTo)}
          </dd>
        </div>
        <div>
          <dt className="text-xs text-muted-foreground">Còn lại</dt>
          <dd className="mt-1 font-semibold tabular-nums text-accent-text">{daysLeft} ngày</dd>
        </div>
      </dl>

      <div
        role="progressbar"
        aria-label="Thời gian đã dùng của gói"
        aria-valuemin={0}
        aria-valuemax={100}
        aria-valuenow={Math.round(elapsed)}
        className="mt-4 h-2 w-full overflow-hidden bg-muted"
      >
        <div className="h-full bg-primary" style={{ width: `${elapsed}%` }} />
      </div>

      <div className="mt-5 border-t border-border pt-4">
        <h3 className="mb-3 text-sm font-semibold text-foreground">Quyền lợi của gói</h3>
        {plan ? (
          <MembershipBenefitList plan={plan} />
        ) : isPlanLoading ? (
          <div aria-busy="true" className="flex flex-col gap-2">
            <LineSkeleton className="h-4 w-3/4" />
            <LineSkeleton className="h-4 w-2/3" />
            <LineSkeleton className="h-4 w-1/2" />
          </div>
        ) : (
          <p className="text-sm text-muted-foreground">
            Gói này không còn được bán nên chi tiết quyền lợi không hiển thị ở đây. Quyền lợi bạn
            đã mua vẫn được giữ đến hết thời hạn.
          </p>
        )}
      </div>

      <div className="mt-5">
        <ButtonLink to={renewTo}>{plan ? "Gia hạn gói" : "Xem các gói khác"}</ButtonLink>
      </div>
    </section>
  );
};
