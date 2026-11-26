import {
  CalendarCheck,
  CheckCircle2,
  FileText,
  Hourglass,
  Loader2,
  PartyPopper,
  Send,
  Wallet,
  XCircle,
  type LucideIcon,
} from "lucide-react";
import type { BadgeVariant } from "@/components/common/status-badge.config";
import { RoomRentalRequestStatus } from "@/types/enum";

export interface RoomRentalStatusInfo {
  label: string;
  variant: BadgeVariant;
  icon: LucideIcon;
  message: string;
}

export const ROOM_RENTAL_STATUS_INFO: Record<RoomRentalRequestStatus, RoomRentalStatusInfo> = {
  New: {
    label: "Đã gửi yêu cầu",
    variant: "secondary",
    icon: Send,
    message: "Rạp đã nhận yêu cầu và sẽ liên hệ với bạn sớm nhất.",
  },
  Processing: {
    label: "Đang xử lý",
    variant: "warning",
    icon: Loader2,
    message: "Rạp đang kiểm tra phòng và chuẩn bị báo giá cho bạn.",
  },
  Quoted: {
    label: "Đã có báo giá",
    variant: "default",
    icon: FileText,
    message: "Xem báo giá bên dưới, sau đó chấp nhận hoặc từ chối trước khi báo giá hết hạn.",
  },
  Accepted: {
    label: "Đã chấp nhận, chờ đặt cọc",
    variant: "warning",
    icon: CalendarCheck,
    message: "Bạn đã chấp nhận báo giá. Thanh toán tiền cọc để rạp giữ phòng cho buổi chiếu.",
  },
  Declined: {
    label: "Đã từ chối",
    variant: "secondary",
    icon: XCircle,
    message: "Bạn đã từ chối báo giá. Nếu cần điều chỉnh, hãy liên hệ rạp hoặc gửi yêu cầu mới.",
  },
  DepositPaid: {
    label: "Đã đặt cọc",
    variant: "success",
    icon: Wallet,
    message: "Rạp đã giữ phòng cho bạn. Thanh toán phần còn lại trước hạn để hoàn tất.",
  },
  FullyPaid: {
    label: "Đã thanh toán đủ",
    variant: "success",
    icon: CheckCircle2,
    message: "Bạn đã thanh toán đủ. Hẹn gặp bạn tại buổi chiếu.",
  },
  Completed: {
    label: "Hoàn tất",
    variant: "success",
    icon: PartyPopper,
    message: "Buổi chiếu đã hoàn tất. Cảm ơn bạn đã chọn Perfo Cinema.",
  },
  Cancelled: {
    label: "Đã hủy",
    variant: "destructive",
    icon: XCircle,
    message: "Yêu cầu đã bị hủy. Liên hệ rạp nếu bạn cần hỗ trợ thêm.",
  },
  QuoteExpired: {
    label: "Báo giá hết hạn",
    variant: "secondary",
    icon: Hourglass,
    message: "Báo giá đã hết hiệu lực. Liên hệ rạp để nhận báo giá mới.",
  },
};

export const ROOM_RENTAL_PROGRESS_STEPS = [
  { label: "Gửi yêu cầu", icon: Send },
  { label: "Báo giá", icon: FileText },
  { label: "Chấp nhận", icon: CalendarCheck },
  { label: "Đặt cọc", icon: Wallet },
  { label: "Thanh toán đủ", icon: CheckCircle2 },
  { label: "Hoàn tất", icon: PartyPopper },
] as const;

const PROGRESS_INDEX: Partial<Record<RoomRentalRequestStatus, number>> = {
  New: 0,
  Processing: 0,
  Quoted: 1,
  Accepted: 2,
  DepositPaid: 3,
  FullyPaid: 4,
  Completed: 5,
};

export const getRoomRentalProgressIndex = (status: RoomRentalRequestStatus): number | null =>
  PROGRESS_INDEX[status] ?? null;

const STATUSES_WITH_QUOTE: RoomRentalRequestStatus[] = [
  RoomRentalRequestStatus.Quoted,
  RoomRentalRequestStatus.Accepted,
  RoomRentalRequestStatus.Declined,
  RoomRentalRequestStatus.DepositPaid,
  RoomRentalRequestStatus.FullyPaid,
  RoomRentalRequestStatus.Completed,
  RoomRentalRequestStatus.QuoteExpired,
  RoomRentalRequestStatus.Cancelled,
];

const STATUSES_WITH_PAYMENT: RoomRentalRequestStatus[] = [
  RoomRentalRequestStatus.Accepted,
  RoomRentalRequestStatus.DepositPaid,
  RoomRentalRequestStatus.FullyPaid,
  RoomRentalRequestStatus.Completed,
];

export const hasRoomRentalQuote = (status: RoomRentalRequestStatus) =>
  STATUSES_WITH_QUOTE.includes(status);

export const hasRoomRentalPayments = (status: RoomRentalRequestStatus) =>
  STATUSES_WITH_PAYMENT.includes(status);
