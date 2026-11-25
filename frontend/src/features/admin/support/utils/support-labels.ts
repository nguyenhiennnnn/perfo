import { SupportMessageAuthorType, SupportRequestStatus, SupportRequestType } from "@/types/enum";
import type { EnumOption } from "@/lib/enum-options";

const TYPE_LABELS: Record<SupportRequestType, string> = {
  [SupportRequestType.TicketEmailNotReceived]: "Không nhận được email vé",
  [SupportRequestType.ChargedOrderNotConfirmed]: "Bị trừ tiền nhưng đơn chưa xác nhận",
  [SupportRequestType.RefundOutsidePolicy]: "Đề nghị hoàn tiền ngoài chính sách",
  [SupportRequestType.ShowtimeFeedback]: "Góp ý về suất chiếu",
  [SupportRequestType.Other]: "Vấn đề khác",
};

export const getStaffSupportTypeLabel = (type: string) =>
  TYPE_LABELS[type as SupportRequestType] ?? type;

export const SUPPORT_STATUS_TABS: { value: SupportRequestStatus | null; label: string }[] = [
  { value: null, label: "Tất cả" },
  { value: SupportRequestStatus.New, label: "Mới" },
  { value: SupportRequestStatus.InProgress, label: "Đang xử lý" },
  { value: SupportRequestStatus.WaitingCustomer, label: "Chờ khách phản hồi" },
  { value: SupportRequestStatus.Resolved, label: "Đã giải quyết" },
];

export const SUPPORT_ASSIGNEE_FILTER_OPTIONS: EnumOption[] = [
  { value: "mine", label: "Của tôi" },
  { value: "unassigned", label: "Chưa có người phụ trách" },
];

export const SUPPORT_SETTABLE_STATUS_OPTIONS: EnumOption[] = [
  { value: SupportRequestStatus.InProgress, label: "Đang xử lý" },
  { value: SupportRequestStatus.WaitingCustomer, label: "Chờ khách phản hồi" },
  { value: SupportRequestStatus.Resolved, label: "Đã giải quyết" },
];

export const getStaffAuthorLabel = (authorType: string, authorName?: string | null) => {
  if (authorType === SupportMessageAuthorType.Customer) return authorName?.trim() || "Khách hàng";
  if (authorType === SupportMessageAuthorType.System) return "Hệ thống";
  return authorName?.trim() || "Nhân viên rạp";
};
