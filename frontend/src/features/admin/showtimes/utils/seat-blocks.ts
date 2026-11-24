import { SEAT_CONFLICT_ERROR_KEY } from "@/config/api-error-codes";
import { HTTP_STATUS } from "@/config/http-status";
import { getBusinessNow, toWallDate } from "@/lib/business-time";
import { extractApiError, getDisplayMessage } from "@/lib/error";
import { getSeatGroupIds } from "@/components/common/seat-map/seat-map-utils";
import { ShowtimeSeatStatus, ShowtimeStatus } from "@/types/enum";

import {
  MANUAL_SEAT_BLOCK_TYPES,
  SEAT_BLOCK_TYPE_LABEL,
} from "../config/seat-block-config";
import type { SeatBlockDto } from "../types";

export type SeatManageKind = "block" | "unblock";

export type ToggleSeatResult = "ok" | "limit" | "ignored";

export const canManageShowtimeSeats = (showtime: {
  status: string;
  startTime: string;
}): boolean => {
  if (showtime.status !== ShowtimeStatus.Scheduled) return false;
  const start = toWallDate(showtime.startTime);
  return start !== undefined && start.getTime() > getBusinessNow().getTime();
};

export const isManualSeatBlock = (block: SeatBlockDto | undefined): boolean =>
  block !== undefined && MANUAL_SEAT_BLOCK_TYPES.includes(block.type);

export const resolveSeatManageKind = (
  status: ShowtimeSeatStatus,
  block: SeatBlockDto | undefined,
): SeatManageKind | null => {
  if (status === ShowtimeSeatStatus.Available) return "block";
  if (status === ShowtimeSeatStatus.Blocked && isManualSeatBlock(block)) {
    return "unblock";
  }
  return null;
};

export const describeSeatManageNote = (
  status: ShowtimeSeatStatus,
  block: SeatBlockDto | undefined,
  blocksLoaded: boolean,
): string | undefined => {
  if (status === ShowtimeSeatStatus.Held) {
    return "Ghế đang được giữ, chưa thể khóa";
  }
  if (status === ShowtimeSeatStatus.Booked) {
    return "Ghế đã có đơn, xử lý đơn trước khi khóa";
  }
  if (status === ShowtimeSeatStatus.Available) {
    return "Nhấn để chọn khóa ghế";
  }
  if (status !== ShowtimeSeatStatus.Blocked) return undefined;
  if (!block) {
    return blocksLoaded ? "Ghế đang khóa" : "Đang tải thông tin khóa";
  }
  const typeLabel = SEAT_BLOCK_TYPE_LABEL[block.type] ?? block.type;
  return isManualSeatBlock(block)
    ? `Ghế đang khóa (${typeLabel}). Nhấn để chọn mở khóa`
    : `Ghế khóa do hệ thống (${typeLabel}), không mở khóa thủ công`;
};

export const toggleManageSelection = (
  prev: Set<number>,
  seats: { showtimeSeatId: number; coupleId?: string | null }[],
  kindOf: (showtimeSeatId: number) => SeatManageKind | null,
  showtimeSeatId: number,
  maxSeats: number,
): { next: Set<number>; result: ToggleSeatResult } => {
  const group = getSeatGroupIds(seats, showtimeSeatId);
  const kinds = group.map(kindOf);
  const kind = kinds[0];
  if (!kind || kinds.some((item) => item !== kind)) {
    return { next: prev, result: "ignored" };
  }

  if (prev.has(showtimeSeatId)) {
    const next = new Set(prev);
    group.forEach((id) => next.delete(id));
    return { next, result: "ok" };
  }

  const currentKind = prev.size > 0 ? kindOf([...prev][0]) : null;
  if (currentKind && currentKind !== kind) {
    return { next: new Set(group), result: "ok" };
  }

  const next = new Set(prev);
  group.forEach((id) => next.add(id));
  if (next.size > maxSeats) return { next: prev, result: "limit" };
  return { next, result: "ok" };
};

export const getConflictSeatIds = (error: unknown): number[] => {
  const { statusCode, errors } = extractApiError(error);
  if (statusCode !== HTTP_STATUS.conflict || !errors) return [];
  const key = Object.keys(errors).find(
    (item) => item.toLowerCase() === SEAT_CONFLICT_ERROR_KEY.toLowerCase(),
  );
  if (!key) return [];
  return errors[key].map(Number).filter((id) => Number.isFinite(id));
};

export const describeSeatBlockConflict = (
  error: unknown,
  hasOrder: boolean,
): string =>
  hasOrder
    ? "Ghế đã có đơn, xử lý đơn trước rồi khóa lại."
    : getDisplayMessage(error, { preferServerMessage: true });
