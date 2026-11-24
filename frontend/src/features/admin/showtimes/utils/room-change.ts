import type {
  RoomChangeMatchKind,
  RoomChangeSeatMapping,
  RoomChangeSeatOverride,
  RoomChangeSeatRef,
  RoomChangeTargetSeat,
} from "../types";

export const ROOM_CHANGE_NOTE_MAX_LENGTH = 500;

export const MATCH_KIND_LABEL: Record<RoomChangeMatchKind, string> = {
  Exact: "Cùng vị trí",
  Nearest: "Gần nhất",
  Manual: "Chọn tay",
  Unmapped: "Chưa có ghế",
};

const SEAT_STATUS_LABEL: Record<string, string> = {
  Available: "Trống",
  Held: "Đang giữ",
  Booked: "Đã đặt",
  Blocked: "Đã khóa",
};

export const getSeatStatusLabel = (status: string) => SEAT_STATUS_LABEL[status] ?? status;

export const formatSeatRef = (seat: Pick<RoomChangeSeatRef, "row" | "number">) =>
  `${seat.row}${seat.number}`;

export const describeSeat = (seat: RoomChangeSeatRef | RoomChangeTargetSeat) => {
  const parts = [formatSeatRef(seat)];
  if (seat.seatType) parts.push(seat.seatType);
  return parts.join(" · ");
};

export const isRequiredMapping = (mapping: RoomChangeSeatMapping) =>
  (mapping.status !== "Available" && mapping.status !== "Blocked") || mapping.hasOrders;

export const overridesToList = (
  overrides: Record<number, number>,
): RoomChangeSeatOverride[] =>
  Object.entries(overrides).map(([showtimeSeatId, targetSeatId]) => ({
    showtimeSeatId: Number(showtimeSeatId),
    targetSeatId,
  }));

export const buildTargetOwners = (mappings: RoomChangeSeatMapping[]) => {
  const owners = new Map<number, number>();
  for (const mapping of mappings) {
    if (mapping.targetSeatId != null && isRequiredMapping(mapping)) {
      owners.set(mapping.targetSeatId, mapping.showtimeSeatId);
    }
  }
  return owners;
};

export const getSelectableTargetSeats = (
  targetSeats: RoomChangeTargetSeat[],
  owners: Map<number, number>,
  showtimeSeatId: number,
) =>
  targetSeats.filter((seat) => {
    const owner = owners.get(seat.seatId);
    return owner === undefined || owner === showtimeSeatId;
  });
