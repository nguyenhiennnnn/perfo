import { CheckCircle2, Clock, DoorClosed, type LucideIcon } from "lucide-react";

import { Badge, type badgeVariants } from "@/components/ui/badge";
import type { VariantProps } from "class-variance-authority";

import type { RoomClosureStatus } from "../types";
import { CLOSURE_STATUS_LABEL } from "../utils/closure-status";

type BadgeVariant = VariantProps<typeof badgeVariants>["variant"];

const STATUS_STYLE: Record<
  RoomClosureStatus,
  { variant: BadgeVariant; icon: LucideIcon }
> = {
  upcoming: { variant: "warning", icon: Clock },
  ongoing: { variant: "destructive", icon: DoorClosed },
  ended: { variant: "secondary", icon: CheckCircle2 },
};

interface RoomClosureStatusBadgeProps {
  status: RoomClosureStatus;
}

export const RoomClosureStatusBadge = ({ status }: RoomClosureStatusBadgeProps) => {
  const { variant, icon: Icon } = STATUS_STYLE[status];
  return (
    <Badge variant={variant}>
      <Icon />
      {CLOSURE_STATUS_LABEL[status]}
    </Badge>
  );
};
