import type { BadgeVariant } from "@/components/common/status-badge.config";
import type { EnumOption } from "@/lib/enum-options";
import { RoomRentalRequestStatus, type RoomRentalPaymentStage } from "@/types/enum";

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

export const ROOM_RENTAL_PAYMENT_STAGE_LABELS: Record<RoomRentalPaymentStage, string> = {
  Deposit: "Đặt cọc",
  Balance: "Thanh toán nốt",
};

export const ROOM_RENTAL_PAYMENT_ORDER_STATUS: Record<string, RoomRentalAdminStatusInfo> = {
  Pending: { label: "Chờ thanh toán", variant: "warning" },
  Confirmed: { label: "Đã thanh toán", variant: "success" },
  Cancelled: { label: "Đã hủy", variant: "destructive" },
  Expired: { label: "Hết hạn", variant: "secondary" },
};

export const PAYMENT_VISIBLE_STATUSES: RoomRentalRequestStatus[] = [
  RoomRentalRequestStatus.Accepted,
  RoomRentalRequestStatus.DepositPaid,
  RoomRentalRequestStatus.FullyPaid,
  RoomRentalRequestStatus.Completed,
  RoomRentalRequestStatus.Cancelled,
];

export const CANCELLABLE_BY_POLICY_STATUSES: RoomRentalRequestStatus[] = PAID_STAGE_STATUSES;

export const ROOM_RENTAL_BANK_REFERENCE_MAX_LENGTH = 100;
export const ROOM_RENTAL_CANCEL_NOTE_MAX_LENGTH = 2000;
export const ROOM_RENTAL_CALENDAR_DAYS = 7;
export const ROOM_RENTAL_REPORT_DEFAULT_DAYS = 90;
export const ROOM_RENTAL_REPORT_MAX_DAYS = 92;
