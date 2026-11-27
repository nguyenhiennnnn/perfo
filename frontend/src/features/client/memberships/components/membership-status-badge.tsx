import { Badge } from "@/components/ui/badge";
import type { MembershipSubscriptionState } from "../types";

const STATE_BADGES: Record<
  MembershipSubscriptionState,
  { label: string; variant: "success" | "outline" | "secondary" | "warning" }
> = {
  current: { label: "Đang hiệu lực", variant: "success" },
  upcoming: { label: "Sắp hiệu lực", variant: "outline" },
  expired: { label: "Đã hết hạn", variant: "secondary" },
  refunded: { label: "Đã hoàn tiền", variant: "warning" },
};

interface MembershipStatusBadgeProps {
  state: MembershipSubscriptionState;
}

export const MembershipStatusBadge = ({ state }: MembershipStatusBadgeProps) => {
  const { label, variant } = STATE_BADGES[state];
  return <Badge variant={variant}>{label}</Badge>;
};
