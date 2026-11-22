import { bookingResultStatusConfig } from "@/components/common/status-badge";
import type { OrderStatus } from "@/types/enum";
import { cn } from "@/lib/utils";

interface BookingResultStatusProps {
  status: OrderStatus;
  orderCode: string;
}

export const BookingResultStatus = ({ status, orderCode }: BookingResultStatusProps) => {
  const config = bookingResultStatusConfig[status];
  const Icon = config.icon;

  return (
    <div className="flex w-full items-start gap-4">
      <Icon className={cn("mt-1 size-10 shrink-0", config.className)} />
      <div className="space-y-1">
        <h1 className="t-h2 text-foreground">{config.label}</h1>
        <p className="max-w-md text-sm text-muted-foreground">{config.description}</p>
        <p className="t-code text-sm text-foreground">Mã đặt vé: {orderCode}</p>
      </div>
    </div>
  );
};