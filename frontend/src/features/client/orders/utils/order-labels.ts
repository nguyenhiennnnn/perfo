import { CancelledBy, CancelReason, OrderType, PriceAdjustmentType } from "@/types/enum";
import type { OrderPriceAdjustmentDto } from "../types";

const ORDER_TYPE_LABELS: Record<string, string> = {
  [OrderType.Ticket]: "Đặt vé xem phim",
  [OrderType.Combo]: "Bắp nước",
  [OrderType.GiftCard]: "Thẻ quà tặng",
  [OrderType.Membership]: "Gói thành viên",
  [OrderType.RoomRental]: "Thuê phòng chiếu",
  [OrderType.Supplement]: "Bán thêm",
};

const PAYMENT_METHOD_LABELS: Record<string, string> = {
  VNPay: "VNPay",
  Momo: "MoMo",
  ZaloPay: "ZaloPay",
  Stripe: "Stripe",
  Cash: "Tiền mặt",
  GiftCard: "Thẻ quà tặng",
  BankTransfer: "Chuyển khoản",
  Card: "Thẻ ngân hàng",
};

const CANCEL_REASON_LABELS: Record<string, string> = {
  [CancelReason.CustomerChangedMind]: "Khách đổi ý",
  [CancelReason.DuplicateOrder]: "Trùng đơn",
  [CancelReason.PaymentError]: "Lỗi thanh toán",
  [CancelReason.ShowtimeCancelled]: "Rạp hủy suất chiếu",
  [CancelReason.RoomClosed]: "Rạp đóng phòng chiếu",
  [CancelReason.Other]: "Lý do khác",
};

const CANCELLED_BY_LABELS: Record<string, string> = {
  [CancelledBy.Customer]: "Bạn",
  [CancelledBy.Staff]: "Nhân viên rạp",
  [CancelledBy.Admin]: "Quản trị viên",
  [CancelledBy.System]: "Hệ thống",
};

const ADJUSTMENT_LABELS: Record<string, string> = {
  [PriceAdjustmentType.TierDiscount]: "Ưu đãi hạng thành viên",
  [PriceAdjustmentType.MembershipBenefit]: "Quyền lợi gói thành viên",
  [PriceAdjustmentType.Promotion]: "Mã khuyến mãi",
  [PriceAdjustmentType.PackagePrice]: "Giá gói sản phẩm",
};

export const getOrderTypeLabel = (type: string) => ORDER_TYPE_LABELS[type] ?? type;

export const getPaymentMethodLabel = (method: string) => PAYMENT_METHOD_LABELS[method] ?? method;

export const getCancelReasonLabel = (reason: string) => CANCEL_REASON_LABELS[reason] ?? reason;

export const getCancelledByLabel = (actor: string) => CANCELLED_BY_LABELS[actor] ?? actor;

export const getAdjustmentLabel = (adjustment: OrderPriceAdjustmentDto) => {
  const label = ADJUSTMENT_LABELS[adjustment.type] ?? "Giảm giá";
  return adjustment.referenceName ? `${label} ${adjustment.referenceName}` : label;
};
