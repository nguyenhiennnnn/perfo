import type { EnumOption } from "@/lib/enum-options";

import { SeatBlockType } from "../types";

export const SEAT_BLOCK_MAX_SEATS = 200;
export const SEAT_BLOCK_REASON_MAX_LENGTH = 500;

export const MANUAL_SEAT_BLOCK_TYPES: readonly string[] = [
  SeatBlockType.Maintenance,
  SeatBlockType.Broken,
];

export const SEAT_BLOCK_TYPE_LABEL: Record<string, string> = {
  [SeatBlockType.Maintenance]: "Bảo trì",
  [SeatBlockType.Broken]: "Hỏng",
  [SeatBlockType.RoomRental]: "Thuê phòng",
  [SeatBlockType.WaitlistReserve]: "Giữ chỗ danh sách chờ",
};

export const seatBlockTypeOptions: EnumOption[] = MANUAL_SEAT_BLOCK_TYPES.map(
  (value) => ({ value, label: SEAT_BLOCK_TYPE_LABEL[value] }),
);
