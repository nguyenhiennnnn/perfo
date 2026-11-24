import { CheckCircle2, PackageCheck, ScanLine } from "lucide-react";
import { BookingStatusBadge } from "@/components/common/status-badge";
import { ComboQuantityStepper } from "@/components/common/combo-picker/combo-quantity-stepper";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Separator } from "@/components/ui/separator";
import { FORM_PROCESSING_LABEL } from "@/config/default-constants";
import { formatDateTime } from "@/lib/formatters";
import { OrderStatus } from "@/types/enum";
import type { ComboPickupFlow } from "../hooks/use-combo-pickup-flow";
import { ComboPickupHistory } from "./combo-pickup-history";

interface ComboPickupOrderCardProps {
  flow: ComboPickupFlow;
  order: NonNullable<ComboPickupFlow["order"]>;
}

export const ComboPickupOrderCard = ({ flow, order }: ComboPickupOrderCardProps) => {
  const isPickable = order.status === OrderStatus.Confirmed;
  const canConfirm =
    isPickable && flow.selectedTotal > 0 && !flow.isSubmitting && !flow.isLooking;

  return (
    <Card className="gap-4 p-5">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="min-w-0">
          <p className="t-code text-lg font-bold text-foreground">{order.orderCode}</p>
          <p className="text-sm text-muted-foreground">
            {order.customerName || "Khách lẻ"}
            {order.customerPhone ? ` · ${order.customerPhone}` : ""}
          </p>
          <p className="text-xs text-muted-foreground">Tạo lúc {formatDateTime(order.createdAt)}</p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <BookingStatusBadge status={order.status} />
          {order.isComboOnly && <Badge variant="secondary">Đơn chỉ combo</Badge>}
          {order.isUnclaimed && <Badge variant="warning">Chưa nhận</Badge>}
        </div>
      </div>

      <Separator />

      {order.isFullyFulfilled ? (
        <div className="flex flex-col items-center gap-2 py-4 text-center">
          <CheckCircle2 className="size-8 text-success" />
          <p className="font-heading text-base font-semibold text-foreground">
            {flow.lastPickedUp
              ? `Đã giao ${flow.lastPickedUp} phần, đơn đã nhận đủ combo`
              : "Đơn đã nhận đủ combo"}
          </p>
        </div>
      ) : (
        <div className="flex flex-col gap-3">
          {!isPickable && (
            <p role="alert" className="bg-warning/10 px-3 py-2 text-sm text-warning-text">
              Đơn này chưa ở trạng thái đã thanh toán nên chưa thể giao combo.
            </p>
          )}
          <ul className="flex flex-col gap-2">
            {order.lines.map((line) => (
              <li
                key={line.orderLineId}
                className="flex flex-wrap items-center justify-between gap-3 bg-muted/40 p-3"
              >
                <div className="min-w-0">
                  <p className="wrap-break-word text-sm font-medium text-foreground">
                    {line.comboName ?? "Combo"}
                  </p>
                  <p className="text-xs tabular-nums text-muted-foreground">
                    Đã nhận {line.quantityFulfilled}/{line.quantity - line.refundedQuantity}
                    {line.refundedQuantity > 0 ? ` (đã hoàn ${line.refundedQuantity})` : ""}
                    {" · "}còn {line.remainingQuantity}
                  </p>
                </div>
                {line.remainingQuantity > 0 && isPickable ? (
                  <ComboQuantityStepper
                    value={flow.selection[line.orderLineId] ?? 0}
                    max={line.remainingQuantity}
                    onChange={(quantity) => flow.setLineQuantity(line.orderLineId, quantity)}
                  />
                ) : (
                  <Badge variant="success">Đã đủ</Badge>
                )}
              </li>
            ))}
          </ul>

          {isPickable && (
            <Button onClick={flow.confirmPickup} disabled={!canConfirm}>
              <PackageCheck className="size-4" />
              <span>
                {flow.isSubmitting
                  ? FORM_PROCESSING_LABEL
                  : `Xác nhận giao ${flow.selectedTotal} phần`}
              </span>
            </Button>
          )}
        </div>
      )}

      <Separator />

      <ComboPickupHistory history={order.history} />

      <Button variant="outline" onClick={flow.reset} disabled={flow.isSubmitting}>
        <ScanLine className="size-4" />
        <span>Nhận đơn khác</span>
      </Button>
    </Card>
  );
};
