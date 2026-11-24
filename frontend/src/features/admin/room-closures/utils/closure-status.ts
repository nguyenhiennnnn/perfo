import { getBusinessNow, toWallDate } from "@/lib/business-time";
import type { RoomClosureDto, RoomClosureStatus } from "../types";

export const CLOSURE_STATUS_LABEL: Record<RoomClosureStatus, string> = {
  upcoming: "Sắp tới",
  ongoing: "Đang đóng",
  ended: "Đã kết thúc",
};

export function getClosureStatus(
  closure: Pick<RoomClosureDto, "startTime" | "endTime">,
): RoomClosureStatus {
  const now = getBusinessNow().getTime();
  const start = toWallDate(closure.startTime)?.getTime() ?? 0;
  const end = toWallDate(closure.endTime)?.getTime() ?? 0;
  if (now < start) return "upcoming";
  if (now < end) return "ongoing";
  return "ended";
}

export const isClosureOpen = (
  closure: Pick<RoomClosureDto, "startTime" | "endTime">,
) => getClosureStatus(closure) !== "ended";
