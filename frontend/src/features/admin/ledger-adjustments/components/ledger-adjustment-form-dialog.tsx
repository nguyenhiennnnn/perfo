import { useEffect, useState } from "react";
import { subDays } from "date-fns";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { DateFilterPicker } from "@/components/common/filter/date-filter-picker";
import { FORM_PROCESSING_LABEL } from "@/config/default-constants";
import { getBusinessTodayDate } from "@/lib/business-time";
import { formatCurrency, toDateInputValue } from "@/lib/formatters";
import { cn } from "@/lib/utils";
import { useDialogClose, useDialogDismissGuard } from "@/stores/dialog-store";

import { useAdminCreateLedgerAdjustment } from "../hooks/use-admin-ledger-adjustments";
import {
  LEDGER_ADJUSTMENT_MAX_AMOUNT,
  LEDGER_ADJUSTMENT_MAX_COUNT,
  LEDGER_ADJUSTMENT_REASON_MAX_LENGTH,
} from "../types";

type AmountKey =
  | "ticketRevenueDelta"
  | "comboRevenueDelta"
  | "giftCardSoldDelta"
  | "discountDelta"
  | "refundAmountDelta";

const AMOUNT_FIELDS: { key: AmountKey; label: string }[] = [
  { key: "ticketRevenueDelta", label: "Doanh thu vé" },
  { key: "comboRevenueDelta", label: "Doanh thu combo" },
  { key: "giftCardSoldDelta", label: "Thẻ quà tặng bán" },
  { key: "discountDelta", label: "Giảm giá" },
  { key: "refundAmountDelta", label: "Hoàn tiền vé" },
];

const parseSignedInt = (value: string): number => {
  const parsed = Number(value);
  return Number.isFinite(parsed) ? Math.round(parsed) : 0;
};

const parseOptionalId = (value: string): number | null => {
  const parsed = Number(value);
  return value.trim() !== "" && Number.isInteger(parsed) && parsed > 0 ? parsed : null;
};

export const LedgerAdjustmentFormDialogContent = () => {
  const closeDialog = useDialogClose();
  const { block, release } = useDialogDismissGuard();
  const create = useAdminCreateLedgerAdjustment();

  const [originalDate, setOriginalDate] = useState<string | null>(
    toDateInputValue(subDays(getBusinessTodayDate(), 1)),
  );
  const [amounts, setAmounts] = useState<Record<AmountKey, number>>({
    ticketRevenueDelta: 0,
    comboRevenueDelta: 0,
    giftCardSoldDelta: 0,
    discountDelta: 0,
    refundAmountDelta: 0,
  });
  const [cancelledOrders, setCancelledOrders] = useState(0);
  const [orderId, setOrderId] = useState("");
  const [reason, setReason] = useState("");

  useEffect(() => {
    if (!create.isPending) return;
    block();
    return release;
  }, [create.isPending, block, release]);

  const trimmedReason = reason.trim();
  const hasAmount = Object.values(amounts).some((v) => v !== 0) || cancelledOrders !== 0;
  const amountOutOfRange = Object.values(amounts).some(
    (v) => Math.abs(v) > LEDGER_ADJUSTMENT_MAX_AMOUNT,
  );
  const countOutOfRange = Math.abs(cancelledOrders) > LEDGER_ADJUSTMENT_MAX_COUNT;
  const orderIdInvalid = orderId.trim() !== "" && parseOptionalId(orderId) === null;
  const reasonTooLong = reason.length > LEDGER_ADJUSTMENT_REASON_MAX_LENGTH;
  const canSubmit =
    Boolean(originalDate) &&
    hasAmount &&
    !amountOutOfRange &&
    !countOutOfRange &&
    !orderIdInvalid &&
    trimmedReason.length > 0 &&
    !reasonTooLong &&
    !create.isPending;

  const handleSubmit = async (event: React.SyntheticEvent) => {
    event.preventDefault();
    if (!canSubmit || !originalDate) return;
    try {
      await create.mutateAsync({
        originalDate,
        reason: trimmedReason,
        ...amounts,
        cancelledOrdersDelta: cancelledOrders,
        orderId: parseOptionalId(orderId),
      });
      closeDialog();
    } catch {
      return;
    }
  };

  return (
    <form onSubmit={(e) => void handleSubmit(e)} className="flex flex-col gap-4">
      <p className="text-sm text-muted-foreground">
        Bút toán được ghi vào ngày kinh doanh đang mở, còn số liệu của ngày gốc đã chốt giữ nguyên.
        Nhập số dương để tăng, số âm để giảm.
      </p>

      <div className="flex flex-col gap-1.5">
        <Label>Ngày gốc (phải là ngày đã chốt)</Label>
        <DateFilterPicker value={originalDate} onChange={setOriginalDate} placeholder="Chọn ngày gốc" />
      </div>

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
        {AMOUNT_FIELDS.map(({ key, label }) => (
          <div key={key} className="flex flex-col gap-1.5">
            <Label htmlFor={`adj-${key}`}>{label} (đ)</Label>
            <Input
              id={`adj-${key}`}
              type="number"
              inputMode="numeric"
              step={1000}
              value={amounts[key]}
              aria-invalid={Math.abs(amounts[key]) > LEDGER_ADJUSTMENT_MAX_AMOUNT || undefined}
              onChange={(e) =>
                setAmounts((prev) => ({ ...prev, [key]: parseSignedInt(e.target.value) }))
              }
            />
          </div>
        ))}
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="adj-cancelled">Số đơn hủy</Label>
          <Input
            id="adj-cancelled"
            type="number"
            inputMode="numeric"
            step={1}
            value={cancelledOrders}
            aria-invalid={countOutOfRange || undefined}
            onChange={(e) => setCancelledOrders(parseSignedInt(e.target.value))}
          />
        </div>
      </div>

      {amountOutOfRange ? (
        <p className="text-xs text-destructive">
          Mỗi khoản tiền không được vượt quá {formatCurrency(LEDGER_ADJUSTMENT_MAX_AMOUNT)}.
        </p>
      ) : null}
      {countOutOfRange ? (
        <p className="text-xs text-destructive">
          Số đơn hủy không được vượt quá {LEDGER_ADJUSTMENT_MAX_COUNT}.
        </p>
      ) : null}
      {!hasAmount ? (
        <p className="text-xs text-muted-foreground">Cần điều chỉnh ít nhất một chỉ tiêu.</p>
      ) : null}

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="adj-order">Mã đơn liên quan (nếu có)</Label>
        <Input
          id="adj-order"
          inputMode="numeric"
          value={orderId}
          aria-invalid={orderIdInvalid || undefined}
          onChange={(e) => setOrderId(e.target.value)}
        />
        {orderIdInvalid ? <p className="text-xs text-destructive">Mã đơn không hợp lệ.</p> : null}
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="adj-reason">Lý do (bắt buộc)</Label>
        <Textarea
          id="adj-reason"
          rows={3}
          className="min-h-20 resize-none"
          value={reason}
          aria-invalid={reasonTooLong || undefined}
          onChange={(e) => setReason(e.target.value)}
        />
        <p className={cn("text-xs", reasonTooLong ? "text-destructive" : "text-muted-foreground")}>
          {reason.length}/{LEDGER_ADJUSTMENT_REASON_MAX_LENGTH}
        </p>
      </div>

      <div className="grid grid-cols-2 gap-2">
        <Button type="button" variant="outline" onClick={closeDialog} disabled={create.isPending}>
          Quay lại
        </Button>
        <Button type="submit" disabled={!canSubmit}>
          {create.isPending ? FORM_PROCESSING_LABEL : "Tạo bút toán"}
        </Button>
      </div>
    </form>
  );
};
