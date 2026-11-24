import type { EnumOption } from "@/lib/enum-options";
import { InvoiceRequestStatus } from "@/types/enum";

export const INVOICE_STATUS_OPTIONS: EnumOption[] = [
  { value: InvoiceRequestStatus.Pending, label: "Chờ xuất" },
  { value: InvoiceRequestStatus.Issued, label: "Đã xuất" },
  { value: InvoiceRequestStatus.Rejected, label: "Từ chối" },
];
