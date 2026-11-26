import { OrderLineType, PriceAdjustmentType } from "@/types/enum";
import type { OrderDetailDto, OrderLineDto } from "../types";

export interface OrderBundleComponent {
  line: OrderLineDto;
  listTotal: number;
  splitPrice: number;
}

export interface OrderBundleGroup {
  adjustmentId: number;
  title: string;
  packagePrice: number;
  listTotal: number;
  savings: number;
  components: OrderBundleComponent[];
}

export interface OrderBundleGrouping {
  groups: OrderBundleGroup[];
  bundledLineIds: Set<number>;
}

const hasAllocations = (order: OrderDetailDto) =>
  order.priceAdjustments.some(
    (adjustment) =>
      adjustment.type === PriceAdjustmentType.PackagePrice && (adjustment.allocations?.length ?? 0) > 0,
  );

export const groupBundleLines = (order: OrderDetailDto): OrderBundleGrouping => {
  const bundledLineIds = new Set<number>();
  if (!hasAllocations(order)) return { groups: [], bundledLineIds };

  const lineIndex = new Map(order.lines.map((line, index) => [line.orderLineId, index]));
  const adjustments = order.priceAdjustments.filter(
    (adjustment) =>
      adjustment.type === PriceAdjustmentType.PackagePrice && (adjustment.allocations?.length ?? 0) > 0,
  );
  const unitsPerBundle = new Map<number | null, number>();
  adjustments.forEach((adjustment) => {
    const key = adjustment.referenceId ?? null;
    unitsPerBundle.set(key, (unitsPerBundle.get(key) ?? 0) + 1);
  });
  const seenPerBundle = new Map<number | null, number>();

  const groups = adjustments.map((adjustment): OrderBundleGroup => {
    const key = adjustment.referenceId ?? null;
    const unitNumber = (seenPerBundle.get(key) ?? 0) + 1;
    seenPerBundle.set(key, unitNumber);
    const unitCount = unitsPerBundle.get(key) ?? 1;

    const components = (adjustment.allocations ?? [])
      .flatMap((allocation) => {
        const line = order.lines.find((l) => l.orderLineId === allocation.orderLineId);
        if (!line) return [];
        bundledLineIds.add(line.orderLineId);
        return [{ line, listTotal: line.lineTotal, splitPrice: line.lineTotal - allocation.amount }];
      })
      .sort((a, b) => (lineIndex.get(a.line.orderLineId) ?? 0) - (lineIndex.get(b.line.orderLineId) ?? 0));

    const listTotal = components.reduce((sum, c) => sum + c.listTotal, 0);
    const packagePrice = components.reduce((sum, c) => sum + c.splitPrice, 0);
    const name = adjustment.referenceName ? `Gói ${adjustment.referenceName}` : "Gói sản phẩm";

    return {
      adjustmentId: adjustment.adjustmentId,
      title: unitCount > 1 ? `${name} (${unitNumber}/${unitCount})` : name,
      packagePrice,
      listTotal,
      savings: listTotal - packagePrice,
      components,
    };
  });

  return { groups, bundledLineIds };
};

export const getBundleComponentLabel = (line: OrderLineDto): string => {
  if (line.lineType === OrderLineType.Combo) {
    return `${line.quantity}x ${line.combo?.name ?? line.description ?? ""}`;
  }
  const type = line.description?.trim();
  const seat = line.ticket ? `${line.ticket.seatRow}${line.ticket.seatNumber}` : "";
  return `Vé${seat ? ` ghế ${seat}` : ""}${type ? `, ${type}` : ""}`;
};

export const getVisibleAdjustments = (order: OrderDetailDto, grouping: OrderBundleGrouping) =>
  grouping.groups.length === 0
    ? order.priceAdjustments
    : order.priceAdjustments.filter(
        (adjustment) =>
          adjustment.type !== PriceAdjustmentType.PackagePrice || (adjustment.allocations?.length ?? 0) === 0,
      );
