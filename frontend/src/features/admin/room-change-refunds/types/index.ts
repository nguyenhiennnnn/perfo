export interface RoomChangeRefundListParams {
  status?: string;
  showtimeId?: number;
  orderId?: number;
  orderCode?: string;
  page?: number;
  pageSize?: number;
}

export interface RoomChangeRefundListItemDto {
  roomChangeRefundDueId: number;
  showtimeRoomChangeId: number;
  showtimeId: number;
  movieTitle: string;
  showtimeStartTime: string;
  orderId: number;
  orderCode: string;
  customerUserId?: number | null;
  customerName: string;
  customerPhone?: string | null;
  isGuest: boolean;
  orderLineId: number;
  fromSeat: string;
  toSeat: string;
  amountDue: number;
  status: string;
  createdAt: string;
}

export interface RoomChangeRefundTransactionDto {
  refundTransactionId: number;
  method: string;
  amount: number;
  status: string;
}

export interface RoomChangeRefundConfirmResultDto {
  roomChangeRefundDueId: number;
  orderId: number;
  orderCode: string;
  amount: number;
  status: string;
  alreadyRefunded: boolean;
  transactions: RoomChangeRefundTransactionDto[];
}

export const RoomChangeRefundStatus = {
  Pending: "Pending",
  Refunded: "Refunded",
  Voided: "Voided",
  All: "All",
} as const;
export type RoomChangeRefundStatus =
  (typeof RoomChangeRefundStatus)[keyof typeof RoomChangeRefundStatus];
