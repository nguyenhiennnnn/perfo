import { ENDPOINTS } from "@/api/endpoints";
import type { ImportWizardConfig } from "@/components/common/import-wizard/import-wizard-types";
import type { HoldOrderResponse } from "@/features/client/orders/types";
import { formatCurrency } from "@/lib/formatters";

export const createGiftCardBulkImportConfig = (
  onOrderCreated: (order: HoldOrderResponse) => void,
): ImportWizardConfig => ({
  title: "Tải lên danh sách người nhận",
  description:
    "Tải file Excel hoặc CSV gồm tên, email, mệnh giá và lời nhắn của từng người nhận. Hệ thống kiểm tra danh sách rồi tạo một đơn tổng để bạn thanh toán.",
  baseUrl: ENDPOINTS.giftCards.bulkImport,
  itemLabel: "thẻ quà",
  templateFilename: "gift-cards-bulk-template.xlsx",
  errorsFilename: "gift-cards-bulk-errors.xlsx",
  invalidates: "orderGiftCardChanged",
  columns: [
    { key: "RecipientName", header: "Tên người nhận" },
    { key: "RecipientEmail", header: "Email" },
    { key: "Amount", header: "Mệnh giá" },
    { key: "Message", header: "Lời nhắn" },
  ],
  summarizeApply: (data) => {
    const order = data as HoldOrderResponse;
    onOrderCreated(order);
    return {
      message: `Đã tạo đơn ${order.orderCode} với tổng ${formatCurrency(order.finalAmount)}.`,
      metrics: [{ label: "Tổng tiền (VND)", value: order.finalAmount }],
    };
  },
});
