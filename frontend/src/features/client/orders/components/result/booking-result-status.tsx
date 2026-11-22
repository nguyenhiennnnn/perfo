import { cn } from "@/lib/utils";
import type { PaymentResultState } from "../../utils/order-payment";
import { paymentResultConfig } from "./payment-result-config";

interface BookingResultStatusProps {
  state: PaymentResultState;
  orderCode: string;
}

export const BookingResultStatus = ({ state, orderCode }: BookingResultStatusProps) => {
  const config = paymentResultConfig[state];
  const Icon = config.icon;

  return (
    <div className="flex w-full items-start gap-4">
      <Icon className={cn("mt-1 size-10 shrink-0", config.className)} aria-hidden />
      <div className="space-y-1">
        <h1 className="t-h2 text-foreground">{config.label}</h1>
        <p className="max-w-md text-sm text-muted-foreground">{config.description}</p>
        <p className="t-code text-sm text-foreground">Mã đặt vé: {orderCode}</p>
      </div>
    </div>
  );
};
