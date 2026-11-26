import type {
  RoomRentalAudienceType,
  RoomRentalPaymentStage,
  RoomRentalQuoteLineKind,
  RoomRentalRequestStatus,
} from "@/types/enum";

export interface CreateRoomRentalRequestRequest {
  fullName: string;
  organizationName: string | null;
  audienceType: RoomRentalAudienceType;
  contactPhone: string;
  contactEmail: string;
  desiredStartAt: string;
  flexibleFromAt: string | null;
  flexibleToAt: string | null;
  attendeeCount: number;
  movieId: number | null;
  bringsOwnContent: boolean;
  contentNote: string | null;
  screenTypeId: number | null;
  comboRequest: string | null;
  notes: string | null;
}

export interface RoomRentalRequestReceiptDto {
  requestCode: string;
  status: RoomRentalRequestStatus;
  createdAt: string;
}

export interface RoomRentalSelectOption {
  value: string;
  label: string;
}

export interface RoomRentalTrackingDto {
  requestCode: string;
  status: RoomRentalRequestStatus;
  fullName: string;
  organizationName: string | null;
  audienceType: RoomRentalAudienceType;
  desiredStartAt: string;
  flexibleFromAt: string | null;
  flexibleToAt: string | null;
  attendeeCount: number;
  movieTitle: string | null;
  bringsOwnContent: boolean;
  contentNote: string | null;
  screenTypeName: string | null;
  comboRequest: string | null;
  notes: string | null;
  createdAt: string;
  updatedAt: string;
  trackingExpiresAt: string;
}

export interface RoomRentalQuoteLineDto {
  kind: RoomRentalQuoteLineKind;
  label: string;
  quantity: number;
  unitPrice: number;
  amount: number;
  referenceId: number | null;
}

export interface RoomRentalCancellationTierDto {
  minHoursBeforeStart: number;
  refundPercent: number;
}

export interface RoomRentalTrackingQuoteDto {
  versionNumber: number;
  isLatest: boolean;
  canRespond: boolean;
  roomName: string;
  startAt: string;
  durationMinutes: number;
  attendeeCount: number;
  lines: RoomRentalQuoteLineDto[];
  subtotal: number;
  discountTotal: number;
  total: number;
  depositPercent: number;
  depositAmount: number;
  balanceAmount: number;
  validUntil: string;
  balanceDueAt: string;
  cancellationTiers: RoomRentalCancellationTierDto[];
  note: string | null;
  sentAt: string;
}

export interface RoomRentalTrackingQuotesDto {
  requestCode: string;
  status: RoomRentalRequestStatus;
  versions: RoomRentalTrackingQuoteDto[];
}

export interface RoomRentalTrackingQuoteDecisionDto {
  requestCode: string;
  status: RoomRentalRequestStatus;
  versionNumber: number;
  updatedAt: string;
}

export interface RoomRentalPaymentOrderDto {
  orderId: number;
  orderCode: string;
  stage: RoomRentalPaymentStage;
  status: string;
  amount: number;
  paidAmount: number;
  outstandingAmount: number;
  expiresAt: string | null;
}

export interface RoomRentalPaymentStageDto {
  stage: RoomRentalPaymentStage;
  amount: number;
  dueAt: string | null;
  order: RoomRentalPaymentOrderDto | null;
  isPaid: boolean;
}

export interface RoomRentalPaymentsDto {
  requestId: number;
  requestCode: string;
  status: RoomRentalRequestStatus;
  total: number;
  paidTotal: number;
  stages: RoomRentalPaymentStageDto[];
}
