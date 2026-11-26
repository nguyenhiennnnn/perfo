import type { EnumOption } from "@/lib/enum-options";
import { RoomRentalAudienceType } from "@/types/enum";

export const ROOM_RENTAL_AUDIENCE_LABELS: Record<RoomRentalAudienceType, string> = {
  Business: "Doanh nghiệp",
  School: "Trường học",
  FriendGroup: "Nhóm bạn",
};

export const roomRentalAudienceOptions: EnumOption[] = Object.values(
  RoomRentalAudienceType,
).map((value) => ({ value, label: ROOM_RENTAL_AUDIENCE_LABELS[value] }));

export const getRoomRentalOrganizationLabel = (audienceType: string): string => {
  if (audienceType === RoomRentalAudienceType.School) return "Tên trường / đơn vị (không bắt buộc)";
  if (audienceType === RoomRentalAudienceType.FriendGroup) return "Tên nhóm (không bắt buộc)";
  return "Tên công ty / đơn vị (không bắt buộc)";
};
