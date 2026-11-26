export interface ApplicableBundleItemDto {
  ticketTypeId?: number | null;
  comboId?: number | null;
  name: string;
  quantity: number;
  retailUnitPrice: number;
}

export interface ApplicableBundleDto {
  bundleId: number;
  name: string;
  description?: string | null;
  price: number;
  ticketCount: number;
  items: ApplicableBundleItemDto[];
  retailTotal: number;
  savingsAmount: number;
  savingsPercent: number;
  excludesPromotions: boolean;
  remainingToday?: number | null;
  allowedSeatTypeIds?: number[] | null;
}

export interface BundleSelectionRequest {
  bundleId: number;
  quantity: number;
}
