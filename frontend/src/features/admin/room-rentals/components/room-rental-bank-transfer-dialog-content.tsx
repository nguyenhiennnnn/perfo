import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Field, FieldDescription, FieldLabel } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { formatCurrency } from "@/lib/formatters";
import { useRecordRoomRentalBankTransfer } from "../hooks/use-admin-room-rentals";
import {
  ROOM_RENTAL_BANK_REFERENCE_MAX_LENGTH,
  ROOM_RENTAL_NOTE_MAX_LENGTH,
  ROOM_RENTAL_PAYMENT_STAGE_LABELS,
} from "../utils/room-rental-admin-labels";
import type { RoomRentalPaymentStageDto } from "../types";

interface RoomRentalBankTransferDialogContentProps {
  requestId: number;
  stage: RoomRentalPaymentStageDto;
  onDone: () => void;
}

export const RoomRentalBankTransferDialogContent = ({
  requestId,
  stage,
  onDone,
}: RoomRentalBankTransferDialogContentProps) => {
  const outstanding = stage.order?.outstandingAmount ?? stage.amount;
  const [amount, setAmount] = useState(String(outstanding));
  const [reference, setReference] = useState("");
  const [note, setNote] = useState("");
  const record = useRecordRoomRentalBankTransfer(requestId);

  const parsedAmount = Number(amount);
  const amountValid = Number.isInteger(parsedAmount) && parsedAmount > 0;
  const canSubmit = amountValid && reference.trim().length > 0 && !record.isPending;

  return (
    <form
      className="space-y-4"
      onSubmit={(event) => {
        event.preventDefault();
        if (!canSubmit) return;
        record.mutate(
          {
            stage: stage.stage,
            amount: parsedAmount === outstanding ? null : parsedAmount,
            reference: reference.trim(),
            note: note.trim() || null,
          },
          { onSuccess: onDone },
        );
      }}
    >
      <p className="text-sm text-muted-foreground">
        Ghi nhận khoản chuyển khoản cho đợt <strong>{ROOM_RENTAL_PAYMENT_STAGE_LABELS[stage.stage]}</strong>
        . Còn phải thu: <strong className="tabular-nums">{formatCurrency(outstanding)}</strong>.
      </p>
      <Field>
        <FieldLabel htmlFor="room-rental-bank-amount">Số tiền đã nhận (VND)</FieldLabel>
        <Input
          id="room-rental-bank-amount"
          inputMode="numeric"
          value={amount}
          aria-invalid={!amountValid}
          disabled={record.isPending}
          onChange={(event) => setAmount(event.target.value.replace(/\D/g, ""))}
        />
        <FieldDescription>Nhận thiếu hoặc thừa so với số phải thu sẽ do hệ thống kiểm tra khi ghi nhận.</FieldDescription>
      </Field>
      <Field>
        <FieldLabel htmlFor="room-rental-bank-reference">Mã giao dịch ngân hàng</FieldLabel>
        <Input
          id="room-rental-bank-reference"
          value={reference}
          maxLength={ROOM_RENTAL_BANK_REFERENCE_MAX_LENGTH}
          disabled={record.isPending}
          onChange={(event) => setReference(event.target.value)}
        />
      </Field>
      <Field>
        <FieldLabel htmlFor="room-rental-bank-note">Ghi chú (không bắt buộc)</FieldLabel>
        <Textarea
          id="room-rental-bank-note"
          value={note}
          rows={2}
          maxLength={ROOM_RENTAL_NOTE_MAX_LENGTH}
          className="min-h-16 resize-none"
          disabled={record.isPending}
          onChange={(event) => setNote(event.target.value)}
        />
      </Field>
      <div className="flex justify-end gap-2">
        <Button type="button" variant="outline" onClick={onDone} disabled={record.isPending}>
          Đóng
        </Button>
        <Button type="submit" disabled={!canSubmit}>
          {record.isPending ? "Đang ghi nhận..." : "Ghi nhận thanh toán"}
        </Button>
      </div>
    </form>
  );
};
