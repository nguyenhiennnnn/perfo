export const GiftCardCancelBalanceAction = {
  WriteOff: 0,
  ConvertToCredit: 1,
} as const;
export type GiftCardCancelBalanceAction =
  (typeof GiftCardCancelBalanceAction)[keyof typeof GiftCardCancelBalanceAction];

export interface AdminGiftCard {
  giftCardId: number;
  maskedCode: string;
  type: string;
  status: string;
  faceValue: number;
  balance: number;
  reservedAmount: number;
  purchaserUserId: number;
  purchaserName: string;
  purchaserEmail: string;
  recipientName: string | null;
  recipientEmail: string | null;
  sendAt: string | null;
  expiresAt: string;
  createdAt: string;
}

export interface AdminGiftCardLedgerEntry {
  giftCardLedgerId: number;
  entryType: string;
  balanceChange: number;
  reservedChange: number;
  balanceAfter: number;
  reservedAfter: number;
  orderId: number | null;
  occurredAt: string;
}

export interface AdminGiftCardListParams {
  page?: number;
  pageSize?: number;
  search?: string;
  status?: string;
  type?: string;
}

export interface CancelGiftCardRequest {
  reason: string;
  balanceAction: GiftCardCancelBalanceAction | null;
}

export interface CancelGiftCardResponse {
  giftCardId: number;
  cancelledBalance: number;
  balanceAction: GiftCardCancelBalanceAction | null;
  creditGiftCardId: number | null;
}

export const GIFT_CARD_CANCEL_REASON_MAX_LENGTH = 500;
