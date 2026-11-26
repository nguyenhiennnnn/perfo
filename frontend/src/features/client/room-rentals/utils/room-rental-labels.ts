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

export const ROOM_RENTAL_STAGE_LABELS = {
  Deposit: "Đặt cọc",
  Balance: "Thanh toán phần còn lại",
} as const;

export const ROOM_RENTAL_QUOTE_LINE_KIND_LABELS = {
  RoomRental: "Thuê phòng",
  PerPerson: "Theo đầu người",
  Combo: "Combo",
  Extra: "Khoản phụ",
  Discount: "Chiết khấu",
} as const;

export const formatRoomRentalDuration = (minutes: number): string => {
  const hours = Math.floor(minutes / 60);
  const rest = minutes % 60;
  if (hours === 0) return `${rest} phút`;
  return rest === 0 ? `${hours} giờ` : `${hours} giờ ${rest} phút`;
};

export const formatRoomRentalCancellationTier = (
  minHoursBeforeStart: number,
  refundPercent: number,
): string => {
  const lead =
    minHoursBeforeStart >= 48 && minHoursBeforeStart % 24 === 0
      ? `${minHoursBeforeStart / 24} ngày`
      : `${minHoursBeforeStart} giờ`;
  return refundPercent >= 100
    ? `Hủy trước ${lead} so với giờ chiếu: hoàn toàn bộ tiền cọc`
    : `Hủy trước ${lead} so với giờ chiếu: hoàn ${refundPercent}% tiền cọc`;
};
