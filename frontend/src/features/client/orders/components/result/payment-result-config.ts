import { Wallet, XCircle } from "lucide-react";
import { bookingResultStatusConfig, type StatusResultConfig } from "@/components/common/status-badge";
import { OrderStatus } from "@/types/enum";
import type { PaymentResultState } from "../../utils/order-payment";

export const paymentResultConfig: Record<PaymentResultState, StatusResultConfig> = {
  confirmed: bookingResultStatusConfig[OrderStatus.Confirmed],
  processing: bookingResultStatusConfig[OrderStatus.Pending],
  expired: bookingResultStatusConfig[OrderStatus.Expired],
  cancelled: bookingResultStatusConfig[OrderStatus.Cancelled],
  outstanding: {
    icon: Wallet,
    label: "Đơn chưa thanh toán đủ",
    description: "Số tiền đã nhận chưa đủ giá trị đơn. Thanh toán phần còn lại trước khi hết hạn giữ ghế để nhận vé.",
    className: "text-warning",
  },
  failed: {
    icon: XCircle,
    label: "Thanh toán không thành công",
    description: "Giao dịch với cổng thanh toán không hoàn tất. Chọn lại phương thức và thanh toán trước khi hết hạn giữ ghế.",
    className: "text-destructive",
  },
};
