import { Crown } from "lucide-react";
import { ButtonLink } from "@/components/common/button-link";
import { EmptyState } from "@/components/common/empty-state";
import { PageTitle } from "@/components/common/page-title";
import { QueryState } from "@/components/common/query-state";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { ROUTES } from "@/config/routes";
import { useCinemaSettings } from "@/features/client/settings/hooks/use-settings";
import { MyMembershipContent } from "../components/my-membership-content";
import { useMembershipPlans, useMySubscriptions } from "../hooks/use-membership-plans";
import { MEMBERSHIP_EXPIRY_REMINDER_FALLBACK_DAYS } from "../utils/membership-plans";

const MySkeleton = () => (
  <div aria-busy="true" className="space-y-6">
    <CardSkeleton className="h-64" />
    <CardSkeleton className="h-40" />
  </div>
);

const MyMembershipPage = () => {
  const subscriptionsQuery = useMySubscriptions();
  const plansQuery = useMembershipPlans();
  const settingsQuery = useCinemaSettings();

  return (
    <div>
      <PageTitle
        title="Gói thành viên của tôi"
        description="Thời hạn, quyền lợi và số vé miễn phí còn lại của gói bạn đã mua."
        action={
          <ButtonLink to={ROUTES.membershipPlans} variant="outline">
            Xem các gói
          </ButtonLink>
        }
      />

      <QueryState
        query={subscriptionsQuery}
        skeleton={<MySkeleton />}
        isEmpty={(subscriptions) => subscriptions.length === 0}
        empty={
          <EmptyState
            icon={Crown}
            title="Bạn chưa mua gói thành viên nào"
            description="Mua gói để nhận giảm giá vé, vé miễn phí mỗi tháng và nhiều quyền lợi khác."
            action={<ButtonLink to={ROUTES.membershipPlans}>Xem các gói thành viên</ButtonLink>}
          />
        }
        errorTitle="Không tải được gói thành viên của bạn"
      >
        {(subscriptions) => (
          <MyMembershipContent
            subscriptions={subscriptions}
            plans={plansQuery.data}
            isPlansLoading={plansQuery.isLoading}
            reminderDays={
              settingsQuery.data?.membershipExpiryReminderDays ??
              MEMBERSHIP_EXPIRY_REMINDER_FALLBACK_DAYS
            }
            perShowtimeLimit={settingsQuery.data?.membershipFreeTicketsPerShowtime ?? 0}
          />
        )}
      </QueryState>
    </div>
  );
};

export default MyMembershipPage;
