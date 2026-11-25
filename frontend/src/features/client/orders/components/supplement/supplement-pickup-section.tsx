import { QRCodeSVG } from "qrcode.react";
import { CopyButton } from "@/components/common/copy-button";
import { OrderType } from "@/types/enum";
import type { OrderDetailDto } from "../../types";
import { getSupplementRemainingCombos, hasSupplementToCollect } from "../../utils/order-supplement";

interface SupplementPickupSectionProps {
  order: OrderDetailDto;
}

export const SupplementPickupSection = ({ order }: SupplementPickupSectionProps) => {
  if (order.type !== OrderType.Supplement || !hasSupplementToCollect(order)) return null;

  const remaining = getSupplementRemainingCombos(order);

  return (
    <section
      aria-labelledby="supplement-pickup-title"
      className="flex flex-col items-center gap-4 cut-md bg-card p-4 sm:flex-row"
    >
      <div className="size-36 shrink-0 bg-primary-foreground p-2 text-scrim">
        <QRCodeSVG
          value={order.orderCode}
          title={`Mã QR nhận combo ${order.orderCode}`}
          bgColor="transparent"
          fgColor="currentColor"
          level="M"
          className="size-full"
        />
      </div>
      <div className="min-w-0 flex-1">
        <h2 id="supplement-pickup-title" className="t-h2 text-foreground">
          Nhận combo tại quầy
        </h2>
        <p className="mt-1 flex items-center gap-2 text-sm text-muted-foreground">
          Đưa mã QR hoặc đọc mã đơn
          <span className="t-code font-medium text-foreground">{order.orderCode}</span>
          <CopyButton value={order.orderCode} label="mã đơn" />
        </p>
        <ul className="mt-2 flex flex-col gap-0.5 text-sm text-foreground">
          {remaining.map((combo) => (
            <li key={combo.name}>
              {combo.remaining}x {combo.name}
            </li>
          ))}
        </ul>
      </div>
    </section>
  );
};
