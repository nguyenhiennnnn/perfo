import { Badge } from "@/components/ui/badge";
import type { CampaignStatus } from "@/types/enum";

import { CAMPAIGN_STATUS_BADGES } from "../utils/campaign-labels";

interface CampaignStatusBadgeProps {
  status: CampaignStatus;
  className?: string;
}

export const CampaignStatusBadge = ({ status, className }: CampaignStatusBadgeProps) => {
  const item = CAMPAIGN_STATUS_BADGES[status];
  if (!item) return <Badge variant="secondary" className={className}>{status}</Badge>;
  return (
    <Badge variant={item.variant} className={className}>
      {item.label}
    </Badge>
  );
};
