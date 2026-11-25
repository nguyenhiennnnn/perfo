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
