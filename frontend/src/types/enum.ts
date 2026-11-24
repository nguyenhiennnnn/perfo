export const Gender = {
  Male: "Male",
  Female: "Female",
  Other: "Other",
} as const;
export type Gender = (typeof Gender)[keyof typeof Gender];

export const MovieStatus = {
  ComingSoon: "ComingSoon",
  NowShowing: "NowShowing",
  Ended: "Ended",
} as const;
export type MovieStatus = (typeof MovieStatus)[keyof typeof MovieStatus];

export const ShowtimeStatus = {
  Scheduled: "Scheduled",
  Cancelled: "Cancelled",
  Completed: "Completed",
} as const;
export type ShowtimeStatus =
  (typeof ShowtimeStatus)[keyof typeof ShowtimeStatus];

export const ShowtimeSeatStatus = {
  Available: "Available",
  Held: "Held",
  Booked: "Booked",
  Blocked: "Blocked",
} as const;
export type ShowtimeSeatStatus =
  (typeof ShowtimeSeatStatus)[keyof typeof ShowtimeSeatStatus];

export const SeatGapRuleMode = {
  Strict: "Strict",
  AllowEdge: "AllowEdge",
  Off: "Off",
} as const;
export type SeatGapRuleMode =
  (typeof SeatGapRuleMode)[keyof typeof SeatGapRuleMode];

export const OrderChannel = {
  Online: "Online",
  Counter: "Counter",
} as const;
export type OrderChannel = (typeof OrderChannel)[keyof typeof OrderChannel];

export const OrderStatus = {
  Pending: "Pending",
  Confirmed: "Confirmed",
  Cancelled: "Cancelled",
  Expired: "Expired",
} as const;
export type OrderStatus = (typeof OrderStatus)[keyof typeof OrderStatus];

export const BookingStatus = OrderStatus;
export type BookingStatus = OrderStatus;

export const OrderLineType = {
  Ticket: "Ticket",
  Combo: "Combo",
  GiftCard: "GiftCard",
  Fee: "Fee",
} as const;
export type OrderLineType = (typeof OrderLineType)[keyof typeof OrderLineType];

export const OrderType = {
  Ticket: "Ticket",
  Combo: "Combo",
  GiftCard: "GiftCard",
  Membership: "Membership",
  RoomRental: "RoomRental",
  Supplement: "Supplement",
} as const;
export type OrderType = (typeof OrderType)[keyof typeof OrderType];

export const CancelledBy = {
  Customer: "Customer",
  Staff: "Staff",
  Admin: "Admin",
  System: "System",
} as const;
export type CancelledBy = (typeof CancelledBy)[keyof typeof CancelledBy];

export const CancelReason = {
  CustomerChangedMind: "CustomerChangedMind",
  DuplicateOrder: "DuplicateOrder",
  PaymentError: "PaymentError",
  ShowtimeCancelled: "ShowtimeCancelled",
  RoomClosed: "RoomClosed",
  Other: "Other",
} as const;
export type CancelReason = (typeof CancelReason)[keyof typeof CancelReason];

export const PaymentMethod = {
  VNPay: "VNPay",
  Momo: "Momo",
  Cash: "Cash",
  Stripe: "Stripe",
  GiftCard: "GiftCard",
  BankTransfer: "BankTransfer",
  Card: "Card",
} as const;
export type PaymentMethod = (typeof PaymentMethod)[keyof typeof PaymentMethod];

export const PaymentStatus = {
  Pending: "Pending",
  Success: "Success",
  Failed: "Failed",
  Refunded: "Refunded",
} as const;
export type PaymentStatus = (typeof PaymentStatus)[keyof typeof PaymentStatus];

export const PaymentTransactionType = {
  Charge: "Charge",
  Refund: "Refund",
} as const;
export type PaymentTransactionType =
  (typeof PaymentTransactionType)[keyof typeof PaymentTransactionType];

export const PriceAdjustmentType = {
  TierDiscount: "TierDiscount",
  MembershipBenefit: "MembershipBenefit",
  Promotion: "Promotion",
  PackagePrice: "PackagePrice",
} as const;
export type PriceAdjustmentType =
  (typeof PriceAdjustmentType)[keyof typeof PriceAdjustmentType];

export const PromotionDiscountType = {
  Percent: "Percent",
  FixedAmount: "FixedAmount",
} as const;
export type PromotionDiscountType =
  (typeof PromotionDiscountType)[keyof typeof PromotionDiscountType];

export const PromotionType = {
  ManualCode: "ManualCode",
  AutoApply: "AutoApply",
  PersonalCode: "PersonalCode",
} as const;
export type PromotionType = (typeof PromotionType)[keyof typeof PromotionType];

export const PromotionRedemptionStatus = {
  Reserved: "Reserved",
  Redeemed: "Redeemed",
  Released: "Released",
} as const;
export type PromotionRedemptionStatus =
  (typeof PromotionRedemptionStatus)[keyof typeof PromotionRedemptionStatus];

export const PromotionScope = {
  TicketsOnly: "TicketsOnly",
  CombosOnly: "CombosOnly",
  WholeOrder: "WholeOrder",
} as const;
export type PromotionScope = (typeof PromotionScope)[keyof typeof PromotionScope];

export const VerificationTokenPurpose = {
  EmailConfirmation: "EmailConfirmation",
  PasswordReset: "PasswordReset",
} as const;
export type VerificationTokenPurpose =
  (typeof VerificationTokenPurpose)[keyof typeof VerificationTokenPurpose];

export const Role = {
  Customer: "Customer",
  Admin: "Admin",
  Employee: "Employee",
} as const;
export type Role = (typeof Role)[keyof typeof Role];

export const ReportGroupBy = {
  Day: "day",
  Week: "week",
  Month: "month",
} as const;
export type ReportGroupBy = (typeof ReportGroupBy)[keyof typeof ReportGroupBy];

export const DayOfWeek = {
  Mon: "Mon",
  Tue: "Tue",
  Wed: "Wed",
  Thu: "Thu",
  Fri: "Fri",
  Sat: "Sat",
  Sun: "Sun",
} as const;
export type DayOfWeek = (typeof DayOfWeek)[keyof typeof DayOfWeek];

export const ImportExportJobType = {
  Import: "Import",
  Export: "Export",
  Rebuild: "Rebuild",
} as const;
export type ImportExportJobType =
  (typeof ImportExportJobType)[keyof typeof ImportExportJobType];

export const ImportExportJobStatus = {
  Queued: "Queued",
  Running: "Running",
  Completed: "Completed",
  Failed: "Failed",
} as const;
export type ImportExportJobStatus =
  (typeof ImportExportJobStatus)[keyof typeof ImportExportJobStatus];

export const ImportExportModule = {
  Movies: "movies",
  Showtimes: "showtimes",
  Orders: "bookings",
  Payments: "payments",
  PromotionRedemptions: "promotion-redemptions",
} as const;
export type ImportExportModule =
  (typeof ImportExportModule)[keyof typeof ImportExportModule];
