import { OrderStatus, OrderType, PriceAdjustmentType } from "@/types/enum";
import type { OrderDetailDto } from "../types";
import { isTicketLine, type OrderTicketLine } from "./order-lines";

export const canUseMembershipBenefits = (order: OrderDetailDto) =>
  order.status === OrderStatus.Pending &&
  order.type === OrderType.Ticket &&
  order.payment.netPaidAmount === 0;

export const getMembershipBenefitAmount = (order: OrderDetailDto) =>
  order.priceAdjustments
    .filter((adjustment) => adjustment.type === PriceAdjustmentType.MembershipBenefit)
    .reduce((sum, adjustment) => sum + adjustment.amount, 0);

export const getFreeTicketLineIds = (order: OrderDetailDto): Set<number> =>
  new Set(
    order.priceAdjustments
      .filter((adjustment) => adjustment.type === PriceAdjustmentType.FreeTicket)
      .flatMap((adjustment) => adjustment.allocations ?? [])
      .map((allocation) => allocation.orderLineId),
  );

const getBundledLineIds = (order: OrderDetailDto): Set<number> =>
  new Set(
    order.priceAdjustments
      .filter((adjustment) => adjustment.type === PriceAdjustmentType.PackagePrice)
      .flatMap((adjustment) => adjustment.allocations ?? [])
      .map((allocation) => allocation.orderLineId),
  );

export interface FreeTicketCandidate {
  line: OrderTicketLine;
  isApplied: boolean;
  isEligible: boolean;
}

export const getFreeTicketCandidates = (order: OrderDetailDto): FreeTicketCandidate[] => {
  const applied = getFreeTicketLineIds(order);
  const bundled = getBundledLineIds(order);
  return order.lines.filter(isTicketLine).map((line) => ({
    line,
    isApplied: applied.has(line.orderLineId),
    isEligible: !bundled.has(line.orderLineId) && line.lockedUnitPrice * line.quantity > 0,
  }));
};
