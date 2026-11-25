import { OrderStatus, OrderType } from "@/types/enum";
import type { ComboSupplementRequest, OrderComboSuggestionDto, OrderDetailDto } from "../types";

export const getSupplementTotal = (
  items: OrderComboSuggestionDto[],
  quantities: Record<number, number>,
) => items.reduce((sum, item) => sum + item.preorderPrice * (quantities[item.comboId] ?? 0), 0);

export const toSupplementRequest = (quantities: Record<number, number>): ComboSupplementRequest => ({
  combos: Object.entries(quantities)
    .filter(([, quantity]) => quantity > 0)
    .map(([comboId, quantity]) => ({ comboId: Number(comboId), quantity })),
});

export const isSupplementOrder = (order: OrderDetailDto) =>
  order.type === OrderType.Supplement && order.originalOrderId != null;

export const getSupplementRemainingCombos = (order: OrderDetailDto) =>
  order.lines
    .filter((line) => line.combo != null)
    .map((line) => ({
      name: line.combo?.name ?? line.description ?? "",
      remaining: line.quantity - line.refundedQuantity - (line.combo?.quantityFulfilled ?? 0),
    }))
    .filter((combo) => combo.remaining > 0);

export const hasSupplementToCollect = (order: OrderDetailDto) =>
  order.status === OrderStatus.Confirmed && getSupplementRemainingCombos(order).length > 0;
