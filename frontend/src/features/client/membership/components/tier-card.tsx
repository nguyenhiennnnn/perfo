import { cn } from "@/lib/utils";
import { TIER_ROLE_LAYOUT } from "../constants";
import { getTierRole, getVisual } from "../utils";
import { formatPoints } from "@/lib/formatters";
import type { MembershipTierDto } from "@/features/admin/catalog/types";

interface TierCardProps {
  tier: MembershipTierDto;
  index: number;
  count: number;
}

export const TierCard = ({ tier, index, count }: TierCardProps) => {
  const visual = getVisual(index);
  const role = getTierRole(index, count);
  const size = TIER_ROLE_LAYOUT[role];
  const featured = role === "featured";

  return (
    <div
      className={cn(
        "flex flex-col text-left",
        size.shape,
        size.pad,
        featured
          ? "bg-primary text-primary-foreground"
          : role === "last"
            ? "bg-secondary text-foreground"
            : "bg-card text-foreground",
      )}
    >
      <h3 className="t-h3">Hạng {tier.tierName}</h3>

      <p className={cn("mt-1 text-xs", featured ? "text-primary-foreground" : "text-muted-foreground")}>
        Yêu cầu{" "}
        <span className={cn("font-medium", !featured && "text-foreground")}>
          {formatPoints(tier.minPoints)} điểm
        </span>
      </p>

      <div className="mt-6 flex items-baseline gap-2">
        <span className={cn("t-numeral", size.number, !featured && visual.accent)}>
          {tier.discountPercent}%
        </span>
        <span className={cn("text-sm", featured ? "text-primary-foreground" : "text-muted-foreground")}>
          chiết khấu
        </span>
      </div>

      <p
        className={cn(
          "mt-4 text-xs leading-relaxed",
          featured ? "text-primary-foreground" : "text-muted-foreground",
        )}
      >
        {visual.perk}
      </p>
    </div>
  );
};
