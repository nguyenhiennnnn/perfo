import { useState } from "react";
import { Armchair, Banknote, Clock, CreditCard, HandCoins, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { formatCurrency } from "@/lib/formatters";
import { cn } from "@/lib/utils";
import { useCountdown } from "@/hooks/use-countdown";
import { useAlertDialogOpen } from "@/stores/alert-dialog-store";
import { FORM_PROCESSING_LABEL } from "@/config/default-constants";
import { CounterPaymentMethod } from "../types";
import type {
  CounterCheckoutRequest,
  CounterCheckoutResponse,
  CounterOrderResponse,
} from "../types";
import { getCashSuggestions, parseMoneyInput } from "../utils/cash";
import { CounterPaymentReceipts } from "./counter-payment-receipts";

const METHOD_OPTIONS = [
  { value: CounterPaymentMethod.Cash, label: "Tiền mặt", icon: Banknote },
  { value: CounterPaymentMethod.Card, label: "Thẻ", icon: CreditCard },
] as const;

interface CounterCheckoutPanelProps {
  order: CounterOrderResponse;
  payments: CounterCheckoutResponse[];
  outstandingAmount: number;
  hasPartialPayment: boolean;
  onPay: (request: CounterCheckoutRequest) => void | Promise<void>;
  onChangeSeats: () => void | Promise<void>;
  onCancelOrder: () => void | Promise<void>;
  isSubmitting?: boolean;
}

export const CounterCheckoutPanel = ({
  order,
  payments,
  outstandingAmount,
  hasPartialPayment,
  onPay,
  onChangeSeats,
  onCancelOrder,
  isSubmitting,
}: CounterCheckoutPanelProps) => {
  const openAlertDialog = useAlertDialogOpen();
  const { remainingMs, label } = useCountdown(order.expiresAt);

  const [method, setMethod] = useState<CounterPaymentMethod>(CounterPaymentMethod.Cash);
  const [amount, setAmount] = useState(outstandingAmount);
  const [receivedInput, setReceivedInput] = useState<number | null>(null);

  const [prevOutstanding, setPrevOutstanding] = useState(outstandingAmount);
  if (prevOutstanding !== outstandingAmount) {
    setPrevOutstanding(outstandingAmount);
    setAmount(outstandingAmount);
    setReceivedInput(null);
  }

  const isCash = method === CounterPaymentMethod.Cash;
  const received = receivedInput ?? amount;
  const isAmountValid = amount > 0 && amount <= outstandingAmount;
  const isInsufficient = isCash && received < amount;
  const change = isCash ? Math.max(0, received - amount) : 0;
  const isExpired = remainingMs <= 0;
  const isPartial = isAmountValid && amount < outstandingAmount;
  const canSubmit = isAmountValid && !isInsufficient && !isExpired && !isSubmitting;

  const handleMethodChange = (next: CounterPaymentMethod) => {
    setMethod(next);
    setReceivedInput(null);
  };

  const handleAmountChange = (value: number) => {
    setAmount(value);
    setReceivedInput(null);
  };

  const handleSubmit = () => {
    void onPay({
      paymentMethod: method,
      amount,
      amountReceived: isCash ? received : undefined,
    });
  };

  const handleCancelOrder = () => {
    openAlertDialog({
      title: `Hủy đơn ${order.orderCode}?`,
      description: "Ghế đang giữ sẽ được nhả ngay và đơn này sẽ bị hủy.",
      confirmText: "Hủy đơn",
      cancelText: "Giữ lại",
      variant: "destructive",
      onConfirm: onCancelOrder,
    });
  };

  return (
    <div className="flex flex-col gap-4 bg-card p-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <p className="t-code text-sm text-foreground">{order.orderCode}</p>
        <div className="flex items-center gap-1.5 text-xs font-medium text-warning-text">
          <Clock className="size-3.5" />
          <span>{isExpired ? "Đã hết hạn giữ ghế" : `Còn ${label}`}</span>
        </div>
      </div>

      <CounterPaymentReceipts payments={payments} />

      <div className="flex items-center justify-between gap-3 text-sm">
        <span className="text-muted-foreground">
          {hasPartialPayment ? "Còn phải thu" : "Số tiền cần thanh toán"}
        </span>
        <span
          data-testid="counter-outstanding-amount"
          className="text-lg font-bold tabular-nums text-primary-text"
        >
          {formatCurrency(outstandingAmount)}
        </span>
      </div>

      <div role="group" aria-label="Phương thức thanh toán" className="grid grid-cols-2 gap-2">
        {METHOD_OPTIONS.map(({ value, label: optionLabel, icon: Icon }) => (
          <Button
            key={value}
            type="button"
            variant={method === value ? "default" : "outline"}
            aria-pressed={method === value}
            onClick={() => handleMethodChange(value)}
            disabled={isSubmitting}
          >
            <Icon className="size-4" />
            <span>{optionLabel}</span>
          </Button>
        ))}
      </div>

      <div className="flex flex-col gap-1.5">
        <div className="flex items-center justify-between gap-2">
          <Label htmlFor="counter-pay-amount">Số tiền thu lần này</Label>
          {amount !== outstandingAmount && (
            <Button
              type="button"
              variant="link"
              size="xs"
              onClick={() => handleAmountChange(outstandingAmount)}
            >
              Thu đủ
            </Button>
          )}
        </div>
        <Input
          id="counter-pay-amount"
          type="number"
          inputMode="numeric"
          min={0}
          max={outstandingAmount}
          step={1000}
          value={amount}
          aria-invalid={!isAmountValid || undefined}
          onChange={(e) => handleAmountChange(parseMoneyInput(e.target.value))}
        />
        {!isAmountValid && (
          <p className="text-xs text-destructive">
            Số tiền thu phải lớn hơn 0 và không vượt quá {formatCurrency(outstandingAmount)}.
          </p>
        )}
      </div>

      {isCash && (
        <>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="counter-amount-received">Khách đưa</Label>
            <Input
              id="counter-amount-received"
              type="number"
              inputMode="numeric"
              min={0}
              step={1000}
              value={received}
              aria-invalid={isInsufficient || undefined}
              onChange={(e) => setReceivedInput(parseMoneyInput(e.target.value))}
            />
            <div className="flex flex-wrap gap-1.5">
              {getCashSuggestions(amount).map((value) => (
                <Button
                  key={value}
                  type="button"
                  size="sm"
                  variant={received === value ? "default" : "outline"}
                  onClick={() => setReceivedInput(value)}
                >
                  {value === amount ? "Vừa đủ" : formatCurrency(value)}
                </Button>
              ))}
            </div>
          </div>

          <div className="flex items-center justify-between text-sm">
            <span className="text-muted-foreground">Tiền thối trả khách</span>
            <span
              data-testid="counter-change-amount"
              className={cn(
                "font-semibold tabular-nums",
                change > 0 ? "text-primary-text" : "text-foreground",
              )}
            >
              {formatCurrency(change)}
            </span>
          </div>

          {isInsufficient && (
            <p className="text-xs text-destructive">
              Số tiền khách đưa chưa đủ, còn thiếu {formatCurrency(amount - received)}.
            </p>
          )}
        </>
      )}

      <Button onClick={handleSubmit} disabled={!canSubmit}>
        <HandCoins className="size-4" />
        <span>
          {isSubmitting
            ? FORM_PROCESSING_LABEL
            : isPartial
              ? "Thu một phần"
              : "Xác nhận thanh toán"}
        </span>
      </Button>

      <div className="grid grid-cols-2 gap-2">
        <Button
          variant="outline"
          onClick={onChangeSeats}
          disabled={isSubmitting || hasPartialPayment}
        >
          <Armchair className="size-4" />
          <span>Đổi ghế</span>
        </Button>
        <Button
          variant="outline"
          onClick={handleCancelOrder}
          disabled={isSubmitting || hasPartialPayment}
        >
          <X className="size-4" />
          <span>Hủy đơn</span>
        </Button>
      </div>

      {hasPartialPayment && (
        <p className="text-xs text-muted-foreground">
          Đơn đã thu một phần nên chưa thể đổi ghế hoặc hủy. Hãy thu nốt phần còn lại trước khi hết thời gian giữ ghế.
        </p>
      )}
    </div>
  );
};
