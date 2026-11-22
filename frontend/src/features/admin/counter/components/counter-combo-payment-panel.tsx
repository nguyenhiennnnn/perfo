import { useState } from "react";
import { Banknote, Clock, CreditCard, HandCoins, Pencil, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  COUNTER_CASH_QUICK_AMOUNTS,
  FORM_PROCESSING_LABEL,
} from "@/config/default-constants";
import { useCountdown } from "@/hooks/use-countdown";
import { formatCurrency } from "@/lib/formatters";
import { useAlertDialogOpen } from "@/stores/alert-dialog-store";
import { cn } from "@/lib/utils";
import type { CounterComboLine } from "../hooks/use-counter-combo-sale";
import { CounterPaymentMethod, type CounterOrderResponse } from "../types";
import { CounterComboSummary } from "./counter-combo-summary";

interface CounterComboPaymentPanelProps {
  order: CounterOrderResponse;
  lines: CounterComboLine[];
  onPay: (method: CounterPaymentMethod, amountReceived?: number) => void;
  onBack: () => void | Promise<void>;
  onCancel: () => void | Promise<void>;
  isSubmitting?: boolean;
}

const METHOD_OPTIONS = [
  { value: CounterPaymentMethod.Cash, label: "Tiền mặt", icon: Banknote },
  { value: CounterPaymentMethod.Card, label: "Thẻ", icon: CreditCard },
] as const;

export const CounterComboPaymentPanel = ({
  order,
  lines,
  onPay,
  onBack,
  onCancel,
  isSubmitting,
}: CounterComboPaymentPanelProps) => {
  const openAlertDialog = useAlertDialogOpen();
  const { remainingMs, label } = useCountdown(order.expiresAt);
  const [method, setMethod] = useState<CounterPaymentMethod>(
    CounterPaymentMethod.Cash,
  );
  const [amountReceived, setAmountReceived] = useState(order.finalAmount);

  const isCash = method === CounterPaymentMethod.Cash;
  const isExpired = remainingMs <= 0;
  const isInsufficient = isCash && amountReceived < order.finalAmount;
  const change = Math.max(0, amountReceived - order.finalAmount);
  const quickAmounts = COUNTER_CASH_QUICK_AMOUNTS.filter(
    (amount) => amount > order.finalAmount,
  );

  const handleCancel = () => {
    openAlertDialog({
      title: `Hủy đơn ${order.orderCode}?`,
      description: "Đơn combo này sẽ bị hủy và chưa thu tiền khách.",
      confirmText: "Hủy đơn",
      cancelText: "Giữ lại",
      variant: "destructive",
      onConfirm: onCancel,
    });
  };

  return (
    <div className="mx-auto flex w-full max-w-md flex-col gap-4">
      <CounterComboSummary lines={lines} total={order.finalAmount} />

      <div className="flex flex-col gap-4 bg-card p-4">
        <div className="flex items-center justify-between">
          <p className="t-code text-sm text-foreground">{order.orderCode}</p>
          <div className="flex items-center gap-1.5 text-xs font-medium text-warning-text">
            <Clock className="size-3.5" />
            <span>{isExpired ? "Đơn đã hết hạn" : `Còn ${label}`}</span>
          </div>
        </div>

        <div className="grid grid-cols-2 gap-2" role="group" aria-label="Phương thức thanh toán">
          {METHOD_OPTIONS.map(({ value, label: methodLabel, icon: Icon }) => (
            <Button
              key={value}
              type="button"
              variant={method === value ? "default" : "outline"}
              aria-pressed={method === value}
              disabled={isSubmitting}
              onClick={() => setMethod(value)}
            >
              <Icon className="size-4" />
              <span>{methodLabel}</span>
            </Button>
          ))}
        </div>

        {isCash ? (
          <>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="combo-amount-received">Số tiền khách đưa</Label>
              <Input
                id="combo-amount-received"
                type="number"
                inputMode="numeric"
                min={0}
                value={amountReceived}
                onChange={(e) => setAmountReceived(Number(e.target.value) || 0)}
              />
              <div className="flex flex-wrap gap-2">
                <Button
                  type="button"
                  size="sm"
                  variant="outline"
                  onClick={() => setAmountReceived(order.finalAmount)}
                >
                  Vừa đủ
                </Button>
                {quickAmounts.map((amount) => (
                  <Button
                    key={amount}
                    type="button"
                    size="sm"
                    variant="outline"
                    className={cn(amountReceived === amount && "border-primary")}
                    onClick={() => setAmountReceived(amount)}
                  >
                    {formatCurrency(amount)}
                  </Button>
                ))}
              </div>
            </div>

            <div className="flex items-center justify-between text-sm">
              <span className="text-muted-foreground">Tiền thừa trả khách</span>
              <span className="font-semibold tabular-nums text-foreground">
                {formatCurrency(change)}
              </span>
            </div>
            {isInsufficient && (
              <p className="text-xs text-destructive">
                Số tiền khách đưa chưa đủ so với số tiền cần thanh toán.
              </p>
            )}
          </>
        ) : (
          <p className="text-sm text-muted-foreground">
            Xác nhận sau khi máy POS báo giao dịch thành công.
          </p>
        )}

        <Button
          onClick={() => onPay(method, isCash ? amountReceived : undefined)}
          disabled={isSubmitting || isInsufficient || isExpired}
        >
          <HandCoins className="size-4" />
          <span>{isSubmitting ? FORM_PROCESSING_LABEL : "Xác nhận thanh toán"}</span>
        </Button>

        <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
          <Button variant="outline" onClick={() => void onBack()} disabled={isSubmitting}>
            <Pencil className="size-4" />
            <span>Sửa đơn</span>
          </Button>
          <Button variant="outline" onClick={handleCancel} disabled={isSubmitting}>
            <X className="size-4" />
            <span>Hủy đơn</span>
          </Button>
        </div>
      </div>
    </div>
  );
};
