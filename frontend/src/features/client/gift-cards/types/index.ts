import type { GiftCardStatus } from "@/types/enum";

export interface PurchaseGiftCardRequest {
  amount: number;
  recipientEmail: string;
  recipientName: string;
  message?: string | null;
  sendAt?: string | null;
}

export interface PurchasedGiftCardDto {
  giftCardId: number;
  maskedCode: string;
  status: GiftCardStatus;
  faceValue: number;
  balance: number;
  recipientName?: string | null;
  recipientEmail?: string | null;
  message?: string | null;
  sendAt?: string | null;
  expiresAt: string;
  createdAt: string;
}

export interface PurchasedGiftCardListParams {
  cursor?: string;
  limit?: number;
}

export interface GiftCardLookupDto {
  code: string;
  type: string;
  status: string;
  balance: number;
  availableBalance: number;
  expiresAt: string;
}

export interface OrderGiftCardItemDto {
  giftCardId: number;
  code: string;
  type: string;
  amount: number;
}

export interface OrderGiftCardResponse {
  orderId: number;
  orderCode: string;
  status: string;
  finalAmount: number;
  giftCardAmount: number;
  payableAmount: number;
  isFullyCovered: boolean;
  cards: OrderGiftCardItemDto[];
}
