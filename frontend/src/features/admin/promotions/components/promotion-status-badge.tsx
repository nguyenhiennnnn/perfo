import { Badge } from "@/components/ui/badge";

import { PROMOTION_STATUS_BADGES, type PromotionStatus } from "../utils/promotion-admin-labels";

interface PromotionStatusBadgeProps {
  status: PromotionStatus;
  className?: string;
}

export const PromotionStatusBadge = ({ status, className }: PromotionStatusBadgeProps) => {
  const item = PROMOTION_STATUS_BADGES[status];
  return (
    <Badge variant={item.variant} className={className}>
      {item.label}
    </Badge>
  );
};
