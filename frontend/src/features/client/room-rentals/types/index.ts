import type { RoomRentalAudienceType, RoomRentalRequestStatus } from "@/types/enum";

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
