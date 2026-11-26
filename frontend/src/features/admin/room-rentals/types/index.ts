import type {
  RoomRentalAudienceType,
  RoomRentalQuoteLineKind,
  RoomRentalRequestStatus,
} from "@/types/enum";

export interface RoomRentalAdminListItemDto {
  roomRentalRequestId: number;
  requestCode: string;
  fullName: string;
  organizationName: string | null;
  audienceType: RoomRentalAudienceType;
  contactPhone: string;
  desiredStartAt: string;
  attendeeCount: number;
  status: RoomRentalRequestStatus;
  createdAt: string;
  updatedAt: string;
}

export interface RoomRentalAdminDto extends RoomRentalAdminListItemDto {
  contactEmail: string;
  flexibleFromAt: string | null;
  flexibleToAt: string | null;
  movieId: number | null;
  movieTitle: string | null;
  bringsOwnContent: boolean;
  contentNote: string | null;
  screenTypeId: number | null;
  screenTypeName: string | null;
  comboRequest: string | null;
  notes: string | null;
  statusNote: string | null;
  allowedNextStatuses: RoomRentalRequestStatus[];
}

export interface RoomRentalListParams {
  status?: RoomRentalRequestStatus;
  search?: string;
  page?: number;
  pageSize?: number;
}

export interface ChangeRoomRentalStatusBody {
  status: RoomRentalRequestStatus;
  note: string | null;
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

export interface RoomRentalQuoteComboItem {
  comboId: number;
  quantity: number;
}

export interface RoomRentalQuoteExtraItem {
  label: string;
  amount: number;
  quantity: number;
}

export interface RoomRentalQuotePricingRequest {
  roomId: number;
  startAt: string;
  durationMinutes: number;
  attendeeCount: number;
  combos: RoomRentalQuoteComboItem[];
  extras: RoomRentalQuoteExtraItem[];
  discountAmount: number | null;
  discountPercent: number | null;
  discountReason: string | null;
}

export interface RoomRentalQuoteResultDto {
  roomId: number;
  roomName: string;
  roomRentalRateId: number;
  roomRentalRateName: string;
  startAt: string;
  durationMinutes: number;
  attendeeCount: number;
  lines: RoomRentalQuoteLineDto[];
  subtotal: number;
  discountTotal: number;
  total: number;
}

export interface UpsertRoomRentalQuoteVersionBody {
  pricing: RoomRentalQuotePricingRequest;
  validUntil: string | null;
  depositPercent: number | null;
  balanceDueAt: string | null;
  cancellationTiers: RoomRentalCancellationTierDto[] | null;
  note: string | null;
}

export interface RoomRentalQuoteVersionListItemDto {
  roomRentalQuoteId: number;
  versionNumber: number;
  isCurrent: boolean;
  total: number;
  depositAmount: number;
  validUntil: string;
  sentAt: string | null;
  createdAt: string;
}

export interface RoomRentalQuoteVersionDto {
  roomRentalQuoteId: number;
  roomRentalRequestId: number;
  requestCode: string;
  versionNumber: number;
  isCurrent: boolean;
  roomId: number;
  roomName: string;
  roomRentalRateId: number;
  roomRentalRateName: string;
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
  sentAt: string | null;
  sentToEmail: string | null;
  createdAt: string;
}

export interface RoomRentalRateDto {
  roomRentalRateId: number;
  name: string;
  roomId: number;
  roomName: string;
  startTime: string;
  endTime: string;
  minDurationMinutes: number;
  maxDurationMinutes: number;
  basePrice: number;
  perPersonFee: number;
  includedAttendees: number;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface UpsertRoomRentalRateRequest {
  name: string;
  roomId: number;
  startTime: string;
  endTime: string;
  minDurationMinutes: number;
  maxDurationMinutes: number;
  basePrice: number;
  perPersonFee: number;
  includedAttendees: number;
  isActive: boolean;
}

export interface RoomRentalRateListParams {
  search?: string;
  roomId?: number;
  isActive?: boolean;
  page?: number;
  pageSize?: number;
}
