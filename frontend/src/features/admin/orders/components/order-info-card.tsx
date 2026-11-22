import { Link } from "react-router";
import {
  BookingChannelBadge,
  BookingStatusBadge,
} from "@/components/common/status-badge";
import { ROUTES } from "@/config/routes";
import { getOrderTypeLabel } from "@/features/client/orders/utils/order-labels";
import { formatDateTime } from "@/lib/formatters";
import { OrderStatus } from "@/types/enum";
import type { OrderDetailDto } from "../types";
import { getCancelledByLabel, getCancelReasonLabel } from "../utils/order-cancel";
import { InfoRow } from "./order-detail-parts";

interface OrderInfoCardProps {
  order: OrderDetailDto;
}

export const OrderInfoCard = ({ order }: OrderInfoCardProps) => (
  <section className="cut-md bg-card p-4">
    <h2 className="t-h3 mb-3 text-foreground">Thông tin đơn hàng</h2>
    <dl className="flex flex-col gap-2">
      <InfoRow label="Mã đơn">
        <span className="t-code">{order.orderCode}</span>
      </InfoRow>
      <InfoRow label="Loại đơn">{getOrderTypeLabel(order.type)}</InfoRow>
      <InfoRow label="Kênh bán">
        <BookingChannelBadge channel={order.channel} />
      </InfoRow>
      <InfoRow label="Trạng thái">
        <BookingStatusBadge status={order.status} />
      </InfoRow>
      <InfoRow label="Khách hàng">
        {order.customerName || "Khách vãng lai"}
      </InfoRow>
      <InfoRow label="Số điện thoại">{order.customerPhone || "Chưa có"}</InfoRow>
      <InfoRow label="Loại khách">
        {order.userId ? `Có tài khoản (#${order.userId})` : "Khách vãng lai"}
      </InfoRow>
      {order.createdByEmployee && (
        <InfoRow label="Nhân viên tạo">{order.createdByEmployee.fullName}</InfoRow>
      )}
      <InfoRow label="Thời điểm tạo">{formatDateTime(order.createdAt)}</InfoRow>
      {order.expiresAt && (
        <InfoRow label="Hạn thanh toán">{formatDateTime(order.expiresAt)}</InfoRow>
      )}
      {order.originalOrderId && (
        <InfoRow label="Đơn gốc">
          <Link
            to={ROUTES.admin.orderDetail(order.originalOrderId)}
            className="text-primary-text underline-offset-4 hover:underline"
          >
            Đơn #{order.originalOrderId}
          </Link>
        </InfoRow>
      )}
      {order.status === OrderStatus.Cancelled && (
        <>
          <InfoRow label="Lý do hủy">
            {order.cancelReason
              ? getCancelReasonLabel(order.cancelReason)
              : "Không ghi nhận"}
          </InfoRow>
          <InfoRow label="Người hủy">
            {order.cancelledBy
              ? getCancelledByLabel(order.cancelledBy)
              : "Không ghi nhận"}
          </InfoRow>
          <InfoRow label="Thời điểm hủy">{formatDateTime(order.cancelledAt)}</InfoRow>
        </>
      )}
    </dl>
  </section>
);
