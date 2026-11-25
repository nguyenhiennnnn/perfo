import { SupportMessageAuthorType, SupportRequestStatus, SupportRequestType } from "@/types/enum";
import type { EnumOption } from "@/lib/enum-options";

const TYPE_LABELS: Record<SupportRequestType, string> = {
  [SupportRequestType.TicketEmailNotReceived]: "Không nhận được email vé",
  [SupportRequestType.ChargedOrderNotConfirmed]: "Bị trừ tiền nhưng đơn chưa xác nhận",
  [SupportRequestType.RefundOutsidePolicy]: "Đề nghị hoàn tiền ngoài chính sách",
  [SupportRequestType.ShowtimeFeedback]: "Góp ý về suất chiếu",
  [SupportRequestType.Other]: "Vấn đề khác",
};

export const getSupportTypeLabel = (type: string) =>
  TYPE_LABELS[type as SupportRequestType] ?? type;

export const supportTypeOptions: EnumOption[] = Object.values(SupportRequestType).map((value) => ({
  value,
  label: TYPE_LABELS[value],
}));

export const SUPPORT_STATUS_TABS: { value: SupportRequestStatus | null; label: string }[] = [
  { value: null, label: "Tất cả" },
  { value: SupportRequestStatus.New, label: "Mới" },
  { value: SupportRequestStatus.InProgress, label: "Đang xử lý" },
  { value: SupportRequestStatus.WaitingCustomer, label: "Chờ bạn phản hồi" },
  { value: SupportRequestStatus.Resolved, label: "Đã giải quyết" },
];

export const getSupportAuthorLabel = (authorType: string, authorName?: string | null) => {
  if (authorType === SupportMessageAuthorType.Customer) return "Bạn";
  if (authorType === SupportMessageAuthorType.System) return "Hệ thống";
  return authorName?.trim() || "Nhân viên rạp";
};
