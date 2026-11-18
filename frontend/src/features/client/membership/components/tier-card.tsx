import { cn } from "@/lib/utils";
import { getVisual } from "../utils";
import { formatPoints } from "@/lib/formatters";
import type { MembershipTierDto } from "@/features/admin/catalog/types";

interface TierCardProps {
  tier: MembershipTierDto;
  index: number;
}

const FEATURED_INDEX = 2;

const SIZE_BY_INDEX = [
  { shape: "cut-sm", pad: "p-4", number: "text-4xl" },
  { shape: "cut-md", pad: "p-5", number: "text-5xl" },
  { shape: "cut-lg", pad: "p-6 md:p-8", number: "text-6xl md:text-7xl" },
  { shape: "cut-md", pad: "p-5 md:p-6", number: "text-5xl" },
] as const;

export const TierCard = ({ tier, index }: TierCardProps) => {
  const visual = getVisual(index);
  const size = SIZE_BY_INDEX[index] ?? SIZE_BY_INDEX[SIZE_BY_INDEX.length - 1];
  const featured = index === FEATURED_INDEX;

  return (
    <div
      className={cn(
        "flex flex-col text-left",
        size.shape,
        size.pad,
        featured
          ? "bg-primary text-primary-foreground"
          : index === SIZE_BY_INDEX.length - 1
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
