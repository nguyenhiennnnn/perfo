export interface ComboPickupLineDto {
  orderLineId: number;
  comboId?: number | null;
  comboName?: string | null;
  quantity: number;
  refundedQuantity: number;
  quantityFulfilled: number;
  remainingQuantity: number;
}

export interface ComboPickupHistoryDto {
  comboPickupId: number;
  orderLineId: number;
  comboName?: string | null;
  quantity: number;
  pickedUpAt: string;
  staffUserId: number;
  staffName?: string | null;
}

export interface ComboPickupOrderDto {
  orderId: number;
  orderCode: string;
  orderType: string;
  status: string;
  customerName?: string | null;
  customerPhone?: string | null;
  createdAt: string;
  isComboOnly: boolean;
  isUnclaimed: boolean;
  isFullyFulfilled: boolean;
  totalRemaining: number;
  lines: ComboPickupLineDto[];
  history: ComboPickupHistoryDto[];
}

export interface ComboPickupItemRequest {
  orderLineId: number;
  quantity: number;
}

export interface ComboPickupRequest {
  code: string;
  items?: ComboPickupItemRequest[];
}

export interface ComboPickupResponse {
  order: ComboPickupOrderDto;
  quantityPickedUp: number;
  pickedUpAt: string;
}
