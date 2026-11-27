import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { formatCurrency } from "@/lib/formatters";
import { cn } from "@/lib/utils";
import type { MembershipPlanDto } from "../types";
import { getMonthlyPrice, getPeriodLabel } from "../utils/membership-plans";
import { MembershipBenefitList } from "./membership-benefit-list";

interface MembershipPlanCardProps {
  plan: MembershipPlanDto;
  isOwned: boolean;
  isHighlighted: boolean;
  isPending: boolean;
  isDisabled: boolean;
  onPurchase: (plan: MembershipPlanDto) => void;
}

export const MembershipPlanCard = ({
  plan,
  isOwned,
  isHighlighted,
  isPending,
  isDisabled,
  onPurchase,
}: MembershipPlanCardProps) => (
  <article
    aria-label={`Gói ${plan.name}`}
    className={cn(
      "flex flex-col gap-5 cut-md bg-card p-5 text-left",
      isHighlighted && "ring-2 ring-primary",
    )}
  >
    <header>
      <div className="flex items-start justify-between gap-2">
        <h2 className="t-h3 text-foreground">{plan.name}</h2>
        <Badge variant="secondary">{getPeriodLabel(plan.period)}</Badge>
      </div>
      {plan.description && (
        <p className="mt-1 text-sm text-muted-foreground">{plan.description}</p>
      )}
    </header>

    <div>
      <p className="t-numeral text-3xl text-primary">{formatCurrency(plan.price)}</p>
      <p className="mt-1 text-xs text-muted-foreground">
        {plan.durationMonths} tháng
        {plan.durationMonths > 1 && ` · khoảng ${formatCurrency(getMonthlyPrice(plan))}/tháng`}
      </p>
    </div>

    <MembershipBenefitList plan={plan} className="flex-1" />

    <Button disabled={isDisabled || isPending} onClick={() => onPurchase(plan)}>
      {isPending ? "Đang tạo đơn..." : isOwned ? "Gia hạn gói" : "Mua gói"}
    </Button>
  </article>
);
