import { Check } from "lucide-react";
import { cn } from "@/lib/utils";
import type { RoomRentalRequestStatus } from "@/types/enum";
import {
  getRoomRentalProgressIndex,
  ROOM_RENTAL_PROGRESS_STEPS,
} from "../utils/room-rental-status";

interface RoomRentalProgressProps {
  status: RoomRentalRequestStatus;
}

export const RoomRentalProgress = ({ status }: RoomRentalProgressProps) => {
  const current = getRoomRentalProgressIndex(status);
  if (current === null) return null;

  return (
    <ol className="grid grid-cols-6 gap-1" aria-label="Tiến trình yêu cầu thuê phòng">
      {ROOM_RENTAL_PROGRESS_STEPS.map((step, index) => {
        const isDone = index < current;
        const isActive = index === current;
        const Icon = step.icon;
        return (
          <li
            key={step.label}
            aria-current={isActive ? "step" : undefined}
            className="flex flex-col items-center gap-1.5 text-center"
          >
            <span
              className={cn(
                "flex size-8 items-center justify-center rounded-full border",
                isDone && "border-primary bg-primary text-primary-foreground",
                isActive && "border-primary bg-primary/10 text-primary-text",
                !isDone && !isActive && "border-border text-muted-foreground",
              )}
            >
              {isDone ? <Check className="size-4" aria-hidden /> : <Icon className="size-4" aria-hidden />}
            </span>
            <span
              className={cn(
                "text-[11px] leading-tight",
                isActive ? "font-semibold text-foreground" : "text-muted-foreground",
              )}
            >
              {step.label}
            </span>
          </li>
        );
      })}
    </ol>
  );
};
