import type { ShowtimeSeatItemDto } from "@/features/client/showtimes/types";
import type { ApplicableBundleDto } from "../types";

export const MAX_BUNDLE_QUANTITY = 10;

export const getBundleMaxQuantity = (bundle: ApplicableBundleDto): number =>
  Math.max(1, Math.min(MAX_BUNDLE_QUANTITY, bundle.remainingToday ?? MAX_BUNDLE_QUANTITY));

export const getBundleTicketItems = (bundle: ApplicableBundleDto) =>
  bundle.items.filter((item) => item.ticketTypeId != null);

export const getBundleComboItems = (bundle: ApplicableBundleDto) =>
  bundle.items.filter((item) => item.comboId != null);

export const expandBundleTicketTypeIds = (bundle: ApplicableBundleDto, quantity: number): number[] =>
  Array.from({ length: quantity }).flatMap(() =>
    getBundleTicketItems(bundle).flatMap((item) =>
      Array.from({ length: item.quantity }, () => item.ticketTypeId as number),
    ),
  );

export const assignBundleTicketTypes = (
  seatIds: number[],
  bundle: ApplicableBundleDto,
  quantity: number,
): Record<number, number> => {
  const ticketTypeIds = expandBundleTicketTypeIds(bundle, quantity);
  return Object.fromEntries(
    seatIds.slice(0, ticketTypeIds.length).map((seatId, index) => [seatId, ticketTypeIds[index]]),
  );
};

export const describeBundleItems = (bundle: ApplicableBundleDto, quantity = 1): string =>
  bundle.items
    .map((item) => {
      const count = item.quantity * quantity;
      return item.ticketTypeId != null ? `${count} vé ${item.name}` : `${count} ${item.name}`;
    })
    .join(", ");

export const getBundleTotal = (bundle: ApplicableBundleDto, quantity: number) => bundle.price * quantity;

export const getBundleSavings = (bundle: ApplicableBundleDto, quantity: number) =>
  bundle.savingsAmount * quantity;

export const getBundleSeatTypeNames = (
  bundle: ApplicableBundleDto,
  seats: ShowtimeSeatItemDto[] | undefined,
): string[] => {
  const allowed = bundle.allowedSeatTypeIds ?? [];
  if (allowed.length === 0) return [];
  const names = new Map<number, string>();
  seats?.forEach((seat) => {
    if (seat.seatTypeId != null && seat.seatType) names.set(seat.seatTypeId, seat.seatType);
  });
  return allowed.flatMap((id) => (names.has(id) ? [names.get(id) as string] : []));
};

export const isSeatAllowedForBundle = (bundle: ApplicableBundleDto, seat: ShowtimeSeatItemDto): boolean => {
  const allowed = bundle.allowedSeatTypeIds ?? [];
  return allowed.length === 0 || (seat.seatTypeId != null && allowed.includes(seat.seatTypeId));
};

export type BundleSeatRejection = "seat-type" | "limit";

export const trimSeatSelection = (
  selected: Set<number>,
  seats: ShowtimeSeatItemDto[],
  bundle: ApplicableBundleDto,
  quantity: number,
): Set<number> => {
  const limit = bundle.ticketCount * quantity;
  const bySeat = new Map(seats.map((seat) => [seat.showtimeSeatId, seat]));
  const next = new Set<number>();
  for (const id of selected) {
    if (next.has(id)) continue;
    const seat = bySeat.get(id);
    if (!seat || !isSeatAllowedForBundle(bundle, seat)) continue;
    const group = seat.coupleId
      ? seats.filter((s) => s.coupleId === seat.coupleId).map((s) => s.showtimeSeatId)
      : [id];
    if (next.size + group.length > limit) continue;
    group.forEach((groupId) => next.add(groupId));
  }
  return next;
};

export const findBundleSeatRejection = (
  bundle: ApplicableBundleDto,
  requiredSeats: number,
  current: Set<number>,
  next: Set<number>,
  seats: ShowtimeSeatItemDto[],
): BundleSeatRejection | null => {
  const added = [...next].filter((id) => !current.has(id));
  if (added.length === 0) return null;
  const bySeat = new Map(seats.map((seat) => [seat.showtimeSeatId, seat]));
  if (added.some((id) => !bySeat.has(id) || !isSeatAllowedForBundle(bundle, bySeat.get(id)!))) return "seat-type";
  return next.size > requiredSeats ? "limit" : null;
};

export const getBundleSeatRejectionMessage = (
  bundle: ApplicableBundleDto,
  reason: BundleSeatRejection,
  requiredSeats: number,
): string =>
  reason === "seat-type"
    ? `Gói ${bundle.name} không áp dụng cho loại ghế này.`
    : `Gói ${bundle.name} gồm ${requiredSeats} vé. Bỏ bớt ghế hoặc tăng số lượng gói để chọn thêm.`;
