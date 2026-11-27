import { Crown } from "lucide-react";
import { Skeleton } from "@/components/ui/skeleton";
import { useActiveMembership } from "@/features/client/memberships/hooks/use-active-membership";
import { useMyFreeTickets } from "@/features/client/memberships/hooks/use-membership-plans";
import { describePlanDiscounts } from "@/features/client/memberships/utils/membership-plans";
import { useCinemaSettings } from "@/features/client/settings/hooks/use-settings";

interface BookingMembershipHintProps {
  enabled: boolean;
  selectedSeatCount: number;
  hasBundle: boolean;
}

export const BookingMembershipHint = ({
  enabled,
  selectedSeatCount,
  hasBundle,
}: BookingMembershipHintProps) => {
  const { subscription, plan, isLoading } = useActiveMembership(enabled);
  const freeTickets = useMyFreeTickets(enabled && subscription !== null);
  const perShowtimeLimit = useCinemaSettings().data?.membershipFreeTicketsPerShowtime ?? 0;

  if (!enabled) return null;
  if (isLoading) return <Skeleton className="h-24 w-full" />;
  if (!subscription) return null;

  const planName = plan?.name ?? subscription.planName;
  const discountText = plan ? describePlanDiscounts(plan) : null;
  const remaining = freeTickets.data?.remaining ?? 0;
  const usable = Math.min(
    remaining,
    selectedSeatCount,
    perShowtimeLimit > 0 ? perShowtimeLimit : Number.POSITIVE_INFINITY,
  );

  return (
    <section aria-label="Quyền lợi gói thành viên" className="flex flex-col gap-2 bg-card p-4">
      <h3 className="t-h3 flex items-center gap-2 text-foreground">
        <Crown className="size-4 text-accent-text" aria-hidden />
        Gói {planName}
      </h3>
      {discountText && (
        <p className="text-sm text-foreground">
          Gói {discountText}. Số tiền giảm hiển thị trong đơn sau khi giữ ghế.
        </p>
      )}
      {(plan?.freeTicketsPerMonth ?? 0) > 0 && (
        <p className="text-sm text-foreground">
          Còn <span className="font-semibold tabular-nums">{remaining}</span> vé miễn phí trong kỳ này.
          {remaining > 0 && " Chọn dùng ở bước thanh toán."}
        </p>
      )}
      {remaining > 0 && selectedSeatCount > 0 && !hasBundle && (
        <p className="text-xs text-muted-foreground">
          Đơn này dùng được tối đa {usable} vé miễn phí
          {perShowtimeLimit > 0 ? ` (giới hạn ${perShowtimeLimit} vé mỗi suất)` : ""}.
        </p>
      )}
      {hasBundle && (
        <p className="text-xs text-muted-foreground">Vé trong gói sản phẩm không dùng được vé miễn phí.</p>
      )}
      {plan && !plan.stackableWithPromotions && (
        <p className="text-xs text-muted-foreground">Gói này không dùng chung với mã khuyến mãi.</p>
      )}
    </section>
  );
};
