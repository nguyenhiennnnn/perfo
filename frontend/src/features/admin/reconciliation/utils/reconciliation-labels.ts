import type { ImportWizardConfig } from "@/components/common/import-wizard/import-wizard-types";
import { ENDPOINTS } from "@/api/endpoints";
import type { EnumOption } from "@/lib/enum-options";

export const GATEWAY_OPTIONS: EnumOption[] = [
  { value: "VNPay", label: "VNPay" },
  { value: "Momo", label: "MoMo" },
  { value: "Stripe", label: "Stripe" },
];

export const getGatewayLabel = (gateway: string) =>
  GATEWAY_OPTIONS.find((o) => o.value === gateway)?.label ?? gateway;

export const SESSION_STATUS: Record<string, { label: string; variant: "success" | "warning" | "destructive" }> = {
  Running: { label: "Đang chạy", variant: "warning" },
  Completed: { label: "Hoàn tất", variant: "success" },
  Failed: { label: "Thất bại", variant: "destructive" },
};

export const RESULT_STATS: {
  key:
    | "matchedCount"
    | "amountMismatchCount"
    | "statusMismatchCount"
    | "missingInSystemCount"
    | "missingAtGatewayCount"
    | "paidAtGatewayOrderUnconfirmedCount";
  label: string;
  tone: "ok" | "warn";
}[] = [
  { key: "matchedCount", label: "Khớp", tone: "ok" },
  { key: "amountMismatchCount", label: "Lệch số tiền", tone: "warn" },
  { key: "statusMismatchCount", label: "Lệch trạng thái", tone: "warn" },
  { key: "missingInSystemCount", label: "Thiếu trong hệ thống", tone: "warn" },
  { key: "missingAtGatewayCount", label: "Thiếu ở cổng", tone: "warn" },
  { key: "paidAtGatewayOrderUnconfirmedCount", label: "Cổng đã thu, đơn chưa xác nhận", tone: "warn" },
];

export const createStatementImportConfig = (gateway: string): ImportWizardConfig => ({
  title: `Tải sao kê ${getGatewayLabel(gateway)}`,
  description: "Tải file sao kê của cổng thanh toán để đối soát với giao dịch trong hệ thống.",
  baseUrl: ENDPOINTS.admin.reconciliation.import,
  requestParams: { gateway },
  itemLabel: "dòng sao kê",
  templateFilename: `reconciliation-${gateway.toLowerCase()}-template.xlsx`,
  errorsFilename: `reconciliation-${gateway.toLowerCase()}-errors.xlsx`,
  invalidates: "reconciliationChanged",
  columns: [
    { key: "Mã giao dịch", header: "Mã giao dịch" },
    { key: "Số tiền", header: "Số tiền" },
    { key: "Phí", header: "Phí" },
    { key: "Trạng thái", header: "Trạng thái" },
    { key: "Thời gian", header: "Thời gian" },
  ],
});

export const ITEM_RESULT_OPTIONS: EnumOption[] = [
  { value: "MissingInSystem", label: "Có ở cổng, không có ở hệ thống" },
  { value: "MissingAtGateway", label: "Có ở hệ thống, không có ở cổng" },
  { value: "AmountMismatch", label: "Lệch số tiền" },
  { value: "StatusMismatch", label: "Lệch trạng thái" },
  { value: "PaidAtGatewayOrderUnconfirmed", label: "Cổng đã thu, đơn chưa xác nhận" },
];

export const getItemResultLabel = (result: string) =>
  ITEM_RESULT_OPTIONS.find((o) => o.value === result)?.label ?? result;

export const RESOLUTION_OPTIONS: EnumOption[] = [
  { value: "Open", label: "Đang mở" },
  { value: "Resolved", label: "Đã xử lý" },
  { value: "Ignored", label: "Bỏ qua" },
];

export const RESOLUTION_BADGE: Record<string, { label: string; variant: "warning" | "success" | "outline" }> = {
  Open: { label: "Đang mở", variant: "warning" },
  Resolved: { label: "Đã xử lý", variant: "success" },
  Ignored: { label: "Bỏ qua", variant: "outline" },
};
