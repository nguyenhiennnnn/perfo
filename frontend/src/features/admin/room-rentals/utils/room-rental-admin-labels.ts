import type { BadgeVariant } from "@/components/common/status-badge.config";
import type { EnumOption } from "@/lib/enum-options";
import { RoomRentalRequestStatus } from "@/types/enum";

export interface RoomRentalAdminStatusInfo {
  label: string;
  variant: BadgeVariant;
}

export const ROOM_RENTAL_ADMIN_STATUS: Record<RoomRentalRequestStatus, RoomRentalAdminStatusInfo> = {
  New: { label: "Mới", variant: "secondary" },
  Processing: { label: "Đang xử lý", variant: "warning" },
  Quoted: { label: "Đã báo giá", variant: "default" },
  Accepted: { label: "Khách chấp nhận", variant: "warning" },
  Declined: { label: "Khách từ chối", variant: "secondary" },
  DepositPaid: { label: "Đã đặt cọc", variant: "success" },
  FullyPaid: { label: "Đã thanh toán đủ", variant: "success" },
  Completed: { label: "Hoàn tất", variant: "success" },
  Cancelled: { label: "Đã hủy", variant: "destructive" },
  QuoteExpired: { label: "Hết hạn báo giá", variant: "secondary" },
};

export const ROOM_RENTAL_BOARD_STATUSES: RoomRentalRequestStatus[] = [
  RoomRentalRequestStatus.New,
  RoomRentalRequestStatus.Processing,
  RoomRentalRequestStatus.Quoted,
  RoomRentalRequestStatus.QuoteExpired,
  RoomRentalRequestStatus.Accepted,
  RoomRentalRequestStatus.DepositPaid,
  RoomRentalRequestStatus.FullyPaid,
  RoomRentalRequestStatus.Completed,
  RoomRentalRequestStatus.Declined,
  RoomRentalRequestStatus.Cancelled,
];

export const roomRentalStatusOptions: EnumOption[] = ROOM_RENTAL_BOARD_STATUSES.map((value) => ({
  value,
  label: ROOM_RENTAL_ADMIN_STATUS[value].label,
}));

export const ROOM_RENTAL_STATUS_ACTION_LABELS: Partial<Record<RoomRentalRequestStatus, string>> = {
  Processing: "Bắt đầu xử lý",
  Completed: "Đánh dấu hoàn tất",
  Cancelled: "Hủy yêu cầu",
};

const PAID_STAGE_STATUSES: RoomRentalRequestStatus[] = [
  RoomRentalRequestStatus.Accepted,
  RoomRentalRequestStatus.DepositPaid,
  RoomRentalRequestStatus.FullyPaid,
];

export const getManualNextStatuses = (
  current: RoomRentalRequestStatus,
  allowed: RoomRentalRequestStatus[],
): RoomRentalRequestStatus[] =>
  allowed.filter(
    (status) =>
      !(status === RoomRentalRequestStatus.Cancelled && PAID_STAGE_STATUSES.includes(current)),
  );

export const QUOTABLE_STATUSES: RoomRentalRequestStatus[] = [
  RoomRentalRequestStatus.New,
  RoomRentalRequestStatus.Processing,
  RoomRentalRequestStatus.Quoted,
  RoomRentalRequestStatus.QuoteExpired,
];

export const ROOM_RENTAL_BOARD_PAGE_SIZE = 20;
export const ROOM_RENTAL_NOTE_MAX_LENGTH = 500;
export const ROOM_RENTAL_QUOTE_MAX_EXTRAS = 20;
export const ROOM_RENTAL_QUOTE_MAX_COMBOS = 20;
export const ROOM_RENTAL_QUOTE_MAX_TIERS = 10;
export const ROOM_RENTAL_CALCULATE_DEBOUNCE_MS = 500;
