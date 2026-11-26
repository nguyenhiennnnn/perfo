export const API_ERROR_CODES = {
  passwordChangeRequired: "PASSWORD_CHANGE_REQUIRED",
  priceChangeConfirmationRequired: "PRICE_CHANGE_CONFIRMATION_REQUIRED",
  comboOutOfStock: "COMBO_OUT_OF_STOCK",
  giftCardAttemptsLocked: "GIFT_CARD_ATTEMPTS_LOCKED",
  pendingOrdersLimitExceeded: "PENDING_ORDERS_LIMIT_EXCEEDED",
  verificationRequired: "VERIFICATION_REQUIRED",
} as const;

export const SEAT_CONFLICT_ERROR_KEY = "conflictSeatIds";
