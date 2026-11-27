import { Crown } from "lucide-react";
import { ButtonLink } from "@/components/common/button-link";
import { EmptyState } from "@/components/common/empty-state";
import { ROUTES } from "@/config/routes";
import { getServerNow } from "@/lib/server-clock";
import type { MembershipPlanDto, MembershipSubscriptionDto } from "../types";
import {
  getDaysLeft,
  getSubscriptionState,
  groupSubscriptions,
} from "../utils/membership-plans";
import { CurrentSubscriptionCard } from "./current-subscription-card";
import { FreeTicketSection } from "./free-ticket-section";
import { MembershipExpiryBanner } from "./membership-expiry-banner";
import { SubscriptionListItem } from "./subscription-list-item";

interface MyMembershipContentProps {
  subscriptions: MembershipSubscriptionDto[];
  plans: MembershipPlanDto[] | undefined;
  isPlansLoading: boolean;
  reminderDays: number;
  perShowtimeLimit: number;
}

export const MyMembershipContent = ({
  subscriptions,
  plans,
  isPlansLoading,
  reminderDays,
  perShowtimeLimit,
}: MyMembershipContentProps) => {
  const now = getServerNow();
  const { current, upcoming, history } = groupSubscriptions(subscriptions, now);
  const currentPlan = current
    ? plans?.find((plan) => plan.membershipPlanId === current.membershipPlanId)
    : undefined;
  const hasLive = current !== null || upcoming.length > 0;
  const daysLeft = current ? getDaysLeft(current.validTo, now) : 0;
  const showExpiryBanner = current !== null && upcoming.length === 0 && daysLeft <= reminderDays;

  return (
    <div className="space-y-6">
      {!hasLive && (
        <EmptyState
          icon={Crown}
          title="Bạn chưa có gói thành viên nào đang hiệu lực"
          description="Mua gói để nhận giảm giá vé, vé miễn phí mỗi tháng và nhiều quyền lợi khác."
          action={<ButtonLink to={ROUTES.membershipPlans}>Xem các gói thành viên</ButtonLink>}
        />
      )}

      {current && showExpiryBanner && (
        <MembershipExpiryBanner
          planName={current.planName}
          validTo={current.validTo}
          daysLeft={daysLeft}
          renewTo={
            currentPlan
              ? `${ROUTES.membershipPlans}?plan=${currentPlan.membershipPlanId}`
              : ROUTES.membershipPlans
          }
        />
      )}

      {current && (
        <CurrentSubscriptionCard
          subscription={current}
          plan={currentPlan}
          isPlanLoading={isPlansLoading}
          now={now}
        />
      )}

      {hasLive && <FreeTicketSection perShowtimeLimit={perShowtimeLimit} />}

      {upcoming.length > 0 && (
        <section aria-label="Gói sắp hiệu lực">
          <h2 className="t-h3 mb-3 text-foreground">Gói sắp hiệu lực</h2>
          <ul className="flex flex-col gap-2">
            {upcoming.map((subscription) => (
              <SubscriptionListItem
                key={subscription.membershipSubscriptionId}
                subscription={subscription}
                state="upcoming"
              />
            ))}
          </ul>
        </section>
      )}

      {history.length > 0 && (
        <section aria-label="Lịch sử gói">
          <h2 className="t-h3 mb-3 text-foreground">Lịch sử gói</h2>
          <ul className="flex flex-col gap-2">
            {history.map((subscription) => (
              <SubscriptionListItem
                key={subscription.membershipSubscriptionId}
                subscription={subscription}
                state={getSubscriptionState(subscription, now)}
              />
            ))}
          </ul>
        </section>
      )}
    </div>
  );
};
