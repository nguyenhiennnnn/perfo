import { cn } from "@/lib/utils";
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
  return (
    <fieldset className="flex flex-col gap-3 bg-card p-4">
      <legend className="t-h3 float-left mb-3 w-full text-foreground">Phương thức thanh toán</legend>
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
        {ONLINE_PAYMENT_METHODS.map((method) => (
          <label
            key={method.value}
            className={cn(
              "flex h-12 cursor-pointer items-center justify-center cut-sm px-4 text-center text-sm font-medium transition-colors has-focus-visible:outline-2 has-focus-visible:outline-offset-2 has-focus-visible:outline-ring",
              value === method.value
                ? "bg-primary text-primary-foreground"
                : "bg-secondary text-secondary-foreground hover:bg-muted",
            )}
          >
            <input
              type="radio"
              name="payment-method"
              value={method.value}
              checked={value === method.value}
              onChange={() => onChange(method.value)}
              className="sr-only"
            />
            {method.label}
          </label>
        ))}
      </div>
    </fieldset>
  );
};
