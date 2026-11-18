import { useId } from "react";
import { Chip, ChipGroup } from "@/components/common/chip";
import { PaymentMethod } from "@/types/enum";

const ONLINE_PAYMENT_METHODS = [
  { value: PaymentMethod.VNPay, label: "VNPay" },
  { value: PaymentMethod.Momo, label: "MoMo" },
  { value: PaymentMethod.Stripe, label: "Thẻ quốc tế (Stripe)" },
] as const;

interface BookingPaymentMethodProps {
  value: PaymentMethod;
  onChange: (value: PaymentMethod) => void;
}

export const BookingPaymentMethod = ({ value, onChange }: BookingPaymentMethodProps) => {
  const legendId = useId();

  return (
    <div className="flex flex-col gap-3 bg-card p-4">
      <h3 id={legendId} className="t-h3 mb-3 text-foreground">Phương thức thanh toán</h3>
      <ChipGroup role="radiogroup" aria-labelledby={legendId} className="grid grid-cols-1 gap-3 sm:grid-cols-3">
        {ONLINE_PAYMENT_METHODS.map((method) => (
          <Chip
            key={method.value}
            role="radio"
            selected={value === method.value}
            onClick={() => onChange(method.value)}
            className="text-center"
          >
            {method.label}
          </Chip>
        ))}
      </ChipGroup>
    </div>
  );
};
