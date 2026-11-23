import type { BadgeVariant } from "@/components/common/status-badge.config";

const ACTION_LABELS: Record<string, string> = {
  Create: "Tạo mới",
  Update: "Cập nhật",
  Delete: "Xóa",
};

const ACTION_VARIANTS: Record<string, BadgeVariant> = {
  Create: "success",
  Update: "secondary",
  Delete: "destructive",
};

const ENTITY_TYPE_LABELS: Record<string, string> = {
  CinemaSettings: "Cấu hình rạp",
  MembershipTier: "Hạng thành viên",
  TicketType: "Loại vé",
  PricingRule: "Quy tắc giá",
  SpecialDay: "Ngày đặc biệt",
  Promotion: "Khuyến mãi",
};

export const SYSTEM_ACTOR_LABEL = "Hệ thống";

export const getAuditActionLabel = (action: string) => ACTION_LABELS[action] ?? action;

export const getAuditActionVariant = (action: string): BadgeVariant =>
  ACTION_VARIANTS[action] ?? "outline";

export const getAuditEntityTypeLabel = (entityType: string) =>
  ENTITY_TYPE_LABELS[entityType] ?? entityType;
