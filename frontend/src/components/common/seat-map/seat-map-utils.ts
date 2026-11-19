import { SeatGapRuleMode, ShowtimeSeatStatus } from "@/types/enum";
import type { ShowtimeSeatItemDto } from "@/features/client/showtimes/types";
import { rowLabelToIndex, type SeatCellState } from "./seat-map-types";

export function toCellState(status: ShowtimeSeatStatus, isSelected: boolean): SeatCellState {
  if (isSelected) return "selected";
  if (status === ShowtimeSeatStatus.Booked) return "booked";
  if (status === ShowtimeSeatStatus.Held) return "held";
  return "available";
}

export function findLoneSeats(
  seats: ShowtimeSeatItemDto[],
  statusMap: Map<number, ShowtimeSeatStatus>,
  selectedIds: Set<number>,
  mode: SeatGapRuleMode,
): string[] {
  if (mode === SeatGapRuleMode.Off || selectedIds.size === 0) return [];

  const rows = new Map<string, ShowtimeSeatItemDto[]>();
  for (const seat of seats) {
    const list = rows.get(seat.seatRow);
    if (list) list.push(seat);
    else rows.set(seat.seatRow, [seat]);
  }

  const isFree = (seat: ShowtimeSeatItemDto) =>
    !selectedIds.has(seat.showtimeSeatId) &&
    (statusMap.get(seat.showtimeSeatId) ?? seat.status) === ShowtimeSeatStatus.Available;

  const result: string[] = [];
  for (const [row, rowSeats] of rows) {
    const picked = new Set(
      rowSeats.filter((s) => selectedIds.has(s.showtimeSeatId)).map((s) => s.seatNumber),
    );
    if (picked.size === 0) continue;

    const sorted = [...rowSeats].sort((a, b) => a.seatNumber - b.seatNumber);
    const numbers = new Set(sorted.map((s) => s.seatNumber));

    sorted.forEach((seat, i) => {
      if (!isFree(seat)) return;
      const n = seat.seatNumber;
      const prev = sorted[i - 1];
      const next = sorted[i + 1];
      if (prev && prev.seatNumber === n - 1 && isFree(prev)) return;
      if (next && next.seatNumber === n + 1 && isFree(next)) return;

      const pickedLeft = picked.has(n - 1);
      const pickedRight = picked.has(n + 1);
      if (!pickedLeft && !pickedRight) return;
      if (mode === SeatGapRuleMode.AllowEdge && !numbers.has(pickedLeft ? n + 1 : n - 1)) return;

      result.push(`${row}${n}`);
    });
  }
  return result;
}

export function formatSeatCodes(
  seats: { seatRow: string; seatNumber: number; coupleId?: string | null }[],
): string[] {
  const sorted = [...seats].sort(
    (a, b) => rowLabelToIndex(a.seatRow) - rowLabelToIndex(b.seatRow) || a.seatNumber - b.seatNumber,
  );
  const codes: string[] = [];
  for (let i = 0; i < sorted.length; i++) {
    const seat = sorted[i];
    const next = sorted[i + 1];
    if (seat.coupleId && next && next.coupleId === seat.coupleId && next.seatRow === seat.seatRow) {
      codes.push(`${seat.seatRow}${seat.seatNumber}-${next.seatNumber}`);
      i++;
    } else {
      codes.push(`${seat.seatRow}${seat.seatNumber}`);
    }
  }
  return codes;
}

/**
 * Trả về toàn bộ ID ghế cùng nhóm với ghế được bấm.
 * Ghế đôi (có coupleId) luôn được chọn/bỏ chọn theo cả cặp; ghế thường trả về chính nó.
 */
export function getSeatGroupIds(
  seats: Pick<ShowtimeSeatItemDto, "showtimeSeatId" | "coupleId">[],
  showtimeSeatId: number,
): number[] {
  const coupleId = seats.find((s) => s.showtimeSeatId === showtimeSeatId)?.coupleId;
  if (!coupleId) return [showtimeSeatId];
  const group = seats.filter((s) => s.coupleId === coupleId).map((s) => s.showtimeSeatId);
  return group.length > 0 ? group : [showtimeSeatId];
}

/**
 * Tính tập ghế đã chọn mới sau khi bấm 1 ghế (áp dụng cho cả cặp ghế đôi).
 * Trả về chính `prev` nếu có ghế trong nhóm không còn trống (đang giữ/đã bán).
 */
export function toggleSeatSelection(
  prev: Set<number>,
  seats: Pick<ShowtimeSeatItemDto, "showtimeSeatId" | "coupleId">[],
  statusMap: Map<number, ShowtimeSeatStatus>,
  showtimeSeatId: number,
): Set<number> {
  const group = getSeatGroupIds(seats, showtimeSeatId);
  const blocked = group.some((id) => {
    const status = statusMap.get(id);
    return status !== undefined && status !== ShowtimeSeatStatus.Available;
  });
  if (blocked) return prev;

  const next = new Set(prev);
  const remove = next.has(showtimeSeatId);
  group.forEach((id) => (remove ? next.delete(id) : next.add(id)));
  return next;
}
