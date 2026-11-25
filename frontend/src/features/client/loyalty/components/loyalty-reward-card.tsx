import { Gift, Percent, Popcorn, Ticket, type LucideIcon } from "lucide-react";
import { Button } from "@/components/ui/button";
import { formatPoints } from "@/lib/formatters";
import { LoyaltyRewardType } from "@/types/enum";
import type { LoyaltyRewardCatalogItemDto } from "../types";
import {
  describeCodeValidity,
  describeRewardBenefit,
  describeRewardValidity,
} from "../utils/loyalty-labels";

const LOW_STOCK_THRESHOLD = 10;

const REWARD_ICONS: Record<string, LucideIcon> = {
  [LoyaltyRewardType.FreeTicket]: Ticket,
  [LoyaltyRewardType.FreeCombo]: Popcorn,
  [LoyaltyRewardType.Discount]: Percent,
};

interface LoyaltyRewardCardProps {
  reward: LoyaltyRewardCatalogItemDto;
  balance: number;
  onRedeem: (reward: LoyaltyRewardCatalogItemDto) => void;
}

export const LoyaltyRewardCard = ({ reward, balance, onRedeem }: LoyaltyRewardCardProps) => {
  const Icon = REWARD_ICONS[reward.type] ?? Gift;
  const missing = reward.pointsCost - balance;
  const validity = describeRewardValidity(reward);

  return (
    <article className="flex flex-col gap-3 cut-md bg-card p-4">
      <div className="flex gap-3">
        <div className="flex size-11 shrink-0 items-center justify-center cut-xs bg-primary/10 text-primary-text">
          <Icon className="size-5" aria-hidden />
        </div>
        <div className="min-w-0 flex-1">
          <h3 className="t-h3 text-foreground">{reward.name}</h3>
          <p className="text-sm text-muted-foreground">{describeRewardBenefit(reward)}</p>
        </div>
      </div>

      <div className="space-y-0.5 text-xs text-muted-foreground">
        <p>{describeCodeValidity(reward.codeValidityDays)}</p>
        {validity && <p>{validity}</p>}
        {reward.remainingQuantity <= LOW_STOCK_THRESHOLD && (
          <p className="text-warning-text">Chỉ còn {reward.remainingQuantity} phần quà</p>
        )}
      </div>

      <div className="mt-auto flex items-center justify-between gap-3 border-t border-border pt-3">
        <div>
          <p className="text-lg font-bold tabular-nums text-accent-text">
            {formatPoints(reward.pointsCost)} điểm
          </p>
          {missing > 0 && (
            <p className="text-xs text-muted-foreground">Còn thiếu {formatPoints(missing)} điểm</p>
          )}
        </div>
        <Button disabled={missing > 0} onClick={() => onRedeem(reward)}>
          Đổi quà
        </Button>
      </div>
    </article>
  );
};
