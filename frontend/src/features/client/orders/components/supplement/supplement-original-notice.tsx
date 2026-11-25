import { AlertTriangle } from "lucide-react";
import { Link } from "react-router";
import { ROUTES } from "@/config/routes";
import { OrderStatus } from "@/types/enum";
import { useOrderDetail } from "../../hooks/use-orders";
import type { OrderDetailDto } from "../../types";
import { hasSupplementToCollect, isSupplementOrder } from "../../utils/order-supplement";

type OriginalChange = "cancelled" | "exchanged";

const getOriginalChange = (original: OrderDetailDto): OriginalChange | null => {
  if (original.status === OrderStatus.Cancelled) return "cancelled";
  const exchanged = (original.exchanges ?? []).some((exchange) => exchange.originalOrderId === original.orderId);
  return exchanged ? "exchanged" : null;
};

const NOTICE_COPY: Record<OriginalChange, string> = {
  cancelled:
    "Đơn vé gốc đã bị hủy nhưng combo đặt trước chưa nhận vẫn còn hiệu lực. Bạn có thể hủy đơn combo bên dưới để được hoàn tiền, hoặc đến quầy nhận combo bằng mã QR.",
  exchanged:
    "Đơn vé gốc đã đổi sang suất chiếu khác. Combo đặt trước vẫn giữ nguyên, bạn có thể nhận tại quầy vào suất mới bằng mã QR, hoặc hủy đơn combo bên dưới để được hoàn tiền.",
};

interface SupplementOriginalNoticeProps {
  order: OrderDetailDto;
}

export const SupplementOriginalNotice = ({ order }: SupplementOriginalNoticeProps) => {
  const shouldCheck = isSupplementOrder(order) && hasSupplementToCollect(order);
  const originalQuery = useOrderDetail(shouldCheck ? (order.originalOrderId ?? 0) : 0);

  if (!shouldCheck || !originalQuery.data) return null;

  const change = getOriginalChange(originalQuery.data);
  if (!change) return null;

  return (
    <div role="status" className="flex items-start gap-3 cut-md bg-warning/10 p-4">
      <AlertTriangle className="mt-0.5 size-5 shrink-0 text-warning-text" aria-hidden />
      <div className="text-sm">
        <p className="font-medium text-foreground">Cần xử lý combo đặt trước</p>
        <p className="mt-1 text-muted-foreground">{NOTICE_COPY[change]}</p>
        <Link
          to={ROUTES.account.ticketDetail(originalQuery.data.orderId)}
          className="mt-2 inline-block font-medium text-primary-text underline-offset-4 hover:underline"
        >
          Xem đơn gốc {originalQuery.data.orderCode}
        </Link>
      </div>
    </div>
  );
};
