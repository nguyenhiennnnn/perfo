import { Badge } from "@/components/ui/badge";
import type { RoomRentalRequestStatus } from "@/types/enum";
import { ROOM_RENTAL_ADMIN_STATUS } from "../utils/room-rental-admin-labels";

export const RoomRentalStatusBadge = ({
  status,
  className,
}: {
  status: RoomRentalRequestStatus;
  className?: string;
}) => {
  const info = ROOM_RENTAL_ADMIN_STATUS[status];
  return (
    <Badge variant={info.variant} className={className}>
      {info.label}
    </Badge>
  );
};
