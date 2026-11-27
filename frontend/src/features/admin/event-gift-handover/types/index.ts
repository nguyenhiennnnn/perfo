export interface EventGiftHandoverLineDto {
  reservationId: number;
  inventoryItemId: number;
  name: string;
  unit: string;
  quantity: number;
  given: number;
  remaining: number;
}

export interface EventGiftHandoverHistoryDto {
  handoverId: number;
  giftName: string;
  quantity: number;
  handedAt: string;
  handedByUserId: number;
  handedByName: string;
}

export interface EventGiftHandoverTicketDto {
  orderId: number;
  orderCode: string;
  orderLineId: number;
  showtimeId: number;
  orderStatus: string;
  customerName?: string | null;
  totalRemaining: number;
  gifts: EventGiftHandoverLineDto[];
  history: EventGiftHandoverHistoryDto[];
}

export interface EventGiftHandoverResponse {
  ticket: EventGiftHandoverTicketDto;
  handedQuantity: number;
  handedAt: string;
}
