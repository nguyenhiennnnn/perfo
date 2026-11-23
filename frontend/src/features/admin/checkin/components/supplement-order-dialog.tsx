import { useEffect, useState } from "react";
import { Banknote, CreditCard, HandCoins } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { FORM_PROCESSING_LABEL } from "@/config/default-constants";
import { formatCurrency } from "@/lib/formatters";
import { CancelReason } from "@/types/enum";
import { useDialogClose, useDialogDismissGuard } from "@/stores/dialog-store";
import { useCounterCancelOrder, useCounterCheckout } from "@/features/admin/counter/hooks/use-counter-orders";
import { getCashSuggestions, parseMoneyInput } from "@/features/admin/counter/utils/cash";
import {
  CounterPaymentMethod,
  type CounterCheckoutResponse,
  type CounterOrderResponse,
} from "@/features/admin/counter/types";
import { useCreateSupplementOrder } from "../hooks/use-checkin";
import type { CheckinResponse } from "../types";

const METHOD_OPTIONS = [
  { value: CounterPaymentMethod.Cash, label: "Tiền mặt", icon: Banknote },
  { value: CounterPaymentMethod.Card, label: "Thẻ", icon: CreditCard },
] as const;

interface SupplementOrderDialogContentProps {
  ticket: CheckinResponse;
  onPaid: () => void;
}

export const SupplementOrderDialogContent = ({
  ticket,
  onPaid,
}: SupplementOrderDialogContentProps) => {
  const closeDialog = useDialogClose();
  const { block, release } = useDialogDismissGuard();
  const createOrder = useCreateSupplementOrder();
  const checkout = useCounterCheckout();
  const cancelOrder = useCounterCancelOrder();

  const [order, setOrder] = useState<CounterOrderResponse | null>(null);
  const [receipt, setReceipt] = useState<CounterCheckoutResponse | null>(null);
  const [method, setMethod] = useState<CounterPaymentMethod>(CounterPaymentMethod.Cash);
  const [receivedInput, setReceivedInput] = useState<number | null>(null);

  const isLocked = order !== null && receipt === null;
  useEffect(() => {
    if (!isLocked) return;
    block();
    return release;
  }, [isLocked, block, release]);

  const seatCode = `${ticket.seatRow}${ticket.seatNumber}`;
  const isBusy = createOrder.isPending || checkout.isPending || cancelOrder.isPending;

  const handleCreate = async () => {
    try {
      const res = await createOrder.mutateAsync({ orderLineId: ticket.orderLineId });
      setOrder(res.data);
    } catch {
      return;
    }
  };

  if (receipt) {
    return (
      <div className="flex flex-col gap-4">
        <p role="status" className="bg-success/10 p-3 text-sm text-foreground">
          Đã thu chênh lệch {formatCurrency(receipt.amountPaid)} cho ghế {seatCode}. Khách có thể vào rạp.
        </p>
        {receipt.change > 0 && (
          <div className="flex items-center justify-between text-sm">
            <span className="text-muted-foreground">Tiền thối trả khách</span>
            <span className="font-semibold tabular-nums">{formatCurrency(receipt.change)}</span>
          </div>
        )}
        <Button onClick={closeDialog}>Đóng</Button>
      </div>
    );
  }

  if (!order) {
    return (
      <div className="flex flex-col gap-4">
        <dl className="grid grid-cols-[auto_1fr] gap-x-4 gap-y-1.5 text-sm">
          <dt className="text-muted-foreground">Mã đơn gốc</dt>
          <dd className="t-code text-foreground">{ticket.orderCode}</dd>
          <dt className="text-muted-foreground">Ghế</dt>
          <dd className="font-medium text-foreground">{seatCode}</dd>
          <dt className="text-muted-foreground">Loại vé đã mua</dt>
          <dd className="font-medium text-foreground">{ticket.ticketTypeName ?? "Vé thường"}</dd>
        </dl>
        <p className="text-sm text-muted-foreground">
          Khách không xuất trình được thẻ nên chuyển sang giá vé thường. Hệ thống tạo đơn thu phần
          chênh lệch so với số tiền khách đã trả.
        </p>
        <div className="grid grid-cols-2 gap-2">
          <Button variant="outline" onClick={closeDialog} disabled={isBusy}>
            Đóng
          </Button>
          <Button onClick={() => void handleCreate()} disabled={isBusy}>
            {createOrder.isPending ? FORM_PROCESSING_LABEL : "Tạo đơn thu chênh lệch"}
          </Button>
        </div>
      </div>
    );
  }

  const amount = order.finalAmount;
  const isCash = method === CounterPaymentMethod.Cash;
  const received = receivedInput ?? amount;
  const isInsufficient = isCash && received < amount;
  const change = isCash ? Math.max(0, received - amount) : 0;

  const handlePay = async () => {
    try {
      const res = await checkout.mutateAsync({
        orderId: order.orderId,
        paymentMethod: method,
        amount,
        amountReceived: isCash ? received : undefined,
      });
      if (res.data.isFullyPaid) {
        setReceipt(res.data);
        onPaid();
      }
    } catch {
      return;
    }
  };

  const handleCancel = async () => {
    try {
      await cancelOrder.mutateAsync({ orderId: order.orderId, reason: CancelReason.Other });
    } catch {
      return;
    }
    setOrder(null);
    closeDialog();
  };

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between gap-3 text-sm">
        <span className="text-muted-foreground">
          Đơn <span className="t-code text-foreground">{order.orderCode}</span>, thu chênh lệch ghế{" "}
          {seatCode}
        </span>
        <span data-testid="supplement-amount" className="text-lg font-bold tabular-nums text-primary-text">
          {formatCurrency(amount)}
        </span>
      </div>

      <div role="group" aria-label="Phương thức thanh toán" className="grid grid-cols-2 gap-2">
        {METHOD_OPTIONS.map(({ value, label, icon: Icon }) => (
          <Button
            key={value}
            type="button"
            variant={method === value ? "default" : "outline"}
            aria-pressed={method === value}
            disabled={isBusy}
            onClick={() => {
              setMethod(value);
              setReceivedInput(null);
            }}
          >
            <Icon className="size-4" />
            <span>{label}</span>
          </Button>
        ))}
      </div>

      {isCash && (
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="supplement-received">Khách đưa</Label>
          <Input
            id="supplement-received"
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
          <p className="text-sm text-muted-foreground">
            Tiền thối: <span className="font-semibold tabular-nums text-foreground">{formatCurrency(change)}</span>
          </p>
          {isInsufficient && (
            <p className="text-xs text-destructive">
              Số tiền khách đưa chưa đủ, còn thiếu {formatCurrency(amount - received)}.
            </p>
          )}
        </div>
      )}

      <Button onClick={() => void handlePay()} disabled={isBusy || isInsufficient}>
        <HandCoins className="size-4" />
        <span>{checkout.isPending ? FORM_PROCESSING_LABEL : "Xác nhận thu tiền"}</span>
      </Button>
      <Button variant="outline" onClick={() => void handleCancel()} disabled={isBusy}>
        Hủy đơn chênh lệch
      </Button>
    </div>
  );
};
