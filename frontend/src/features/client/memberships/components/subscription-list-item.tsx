import { formatDate } from "@/lib/formatters";
import type { MembershipSubscriptionDto, MembershipSubscriptionState } from "../types";
import { MembershipStatusBadge } from "./membership-status-badge";

interface SubscriptionListItemProps {
  subscription: MembershipSubscriptionDto;
  state: MembershipSubscriptionState;
}

export const SubscriptionListItem = ({ subscription, state }: SubscriptionListItemProps) => (
  <li className="flex flex-wrap items-center justify-between gap-2 cut-sm bg-card px-4 py-3">
    <div className="min-w-0">
      <p className="truncate text-sm font-medium text-foreground">{subscription.planName}</p>
      <p className="mt-0.5 text-xs tabular-nums text-muted-foreground">
        {formatDate(subscription.validFrom)} – {formatDate(subscription.validTo)}
      </p>
    </div>
    <MembershipStatusBadge state={state} />
  </li>
);
