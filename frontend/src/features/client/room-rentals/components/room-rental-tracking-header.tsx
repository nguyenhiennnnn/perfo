import { CopyButton } from "@/components/common/copy-button";
import { Badge } from "@/components/ui/badge";
import type { RoomRentalTrackingDto } from "../types";
import { ROOM_RENTAL_STATUS_INFO } from "../utils/room-rental-status";
import { RoomRentalProgress } from "./room-rental-progress";

interface RoomRentalTrackingHeaderProps {
  tracking: RoomRentalTrackingDto;
}

export const RoomRentalTrackingHeader = ({ tracking }: RoomRentalTrackingHeaderProps) => {
  const info = ROOM_RENTAL_STATUS_INFO[tracking.status];
  const Icon = info.icon;

  return (
    <section className="cut-md flex flex-col gap-4 bg-card p-4" aria-label="Trạng thái yêu cầu">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <p className="text-xs text-muted-foreground">Mã yêu cầu</p>
          <p className="tabular truncate font-mono text-lg font-semibold text-foreground">
            {tracking.requestCode}
          </p>
        </div>
        <div className="flex shrink-0 items-center gap-2">
          <Badge variant={info.variant}>
            <Icon className="size-3" aria-hidden />
            {info.label}
          </Badge>
          <CopyButton value={tracking.requestCode} label="mã yêu cầu" />
        </div>
      </div>

      <RoomRentalProgress status={tracking.status} />

      <p className="text-sm leading-relaxed text-foreground" role="status">
        {info.message}
      </p>
    </section>
  );
};
