import { Store } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useDialogOpen } from "@/stores/dialog-store";
import type { OrderDetailDto } from "../../types";
import { getOrderCancelMode } from "../../utils/order-cancel";
import { OrderCancelDialogContent } from "./order-cancel-dialog-content";

interface OrderCancelActionProps {
  order: OrderDetailDto;
}

export const OrderCancelAction = ({ order }: OrderCancelActionProps) => {
  const openDialog = useDialogOpen();
  const mode = getOrderCancelMode(order);

  if (mode === "none") return null;

  if (mode === "counter") {
    return (
      <div
        data-testid="order-cancel-counter-guide"
        className="flex items-start gap-3 cut-md bg-card p-4 text-sm text-muted-foreground"
      >
        <Store className="mt-0.5 size-5 shrink-0 text-foreground" />
        <p>
          Đơn có khoản thanh toán tiền mặt nên không hủy được trực tuyến. Vui lòng mang mã đơn{" "}
          <span className="t-code font-medium text-foreground">{order.orderCode}</span> đến quầy vé để nhân viên hỗ
          trợ hủy và hoàn tiền.
        </p>
      </div>
    );
  }

  const handleOpen = () =>
    openDialog({
      title: "Hủy đơn",
      description: `Đơn ${order.orderCode}`,
      size: "md",
      content: <OrderCancelDialogContent order={order} mode={mode} />,
    });

  return (
    <div className="flex justify-end">
      <Button variant="destructive" onClick={handleOpen}>
        Hủy đơn
      </Button>
    </div>
  );
};
