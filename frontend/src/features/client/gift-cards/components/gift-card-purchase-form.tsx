import { useId, useMemo } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { Controller, useForm, useWatch } from "react-hook-form";
import { Gift } from "lucide-react";
import { Chip, ChipGroup } from "@/components/common/chip";
import { FormDateTimePickerField } from "@/components/common/form/form-datetime-picker-field";
import { FormInputField } from "@/components/common/form/form-input-field";
import { FormTextareaField } from "@/components/common/form/form-textarea-field";
import { SubmitButton } from "@/components/common/form/submit-button";
import { Field, FieldDescription, FieldGroup, FieldLabel } from "@/components/ui/field";
import { GIFT_CARD_MESSAGE_MAX_LENGTH } from "@/config/default-constants";
import { getBusinessTodayDate } from "@/lib/business-time";
import { formatCurrency } from "@/lib/formatters";
import { useServerFieldErrors } from "@/hooks/use-server-field-errors";
import {
  GIFT_SEND_MODE,
  createGiftCardSchema,
  getGiftCardDefaults,
  parseAmount,
  toPurchaseGiftCardRequest,
  type GiftCardFormValues,
  type GiftCardLimits,
} from "../utils/gift-card-form";
import type { PurchaseGiftCardRequest } from "../types";
import { GiftCardAmountField } from "./gift-card-amount-field";

interface GiftCardPurchaseFormProps {
  limits: GiftCardLimits;
  isPending: boolean;
  onSubmit: (body: PurchaseGiftCardRequest, onServerError: (error: unknown) => boolean) => void;
}

export const GiftCardPurchaseForm = ({ limits, isPending, onSubmit }: GiftCardPurchaseFormProps) => {
  const sendLabelId = useId();
  const schema = useMemo(() => createGiftCardSchema(limits), [limits]);
  const form = useForm<GiftCardFormValues>({
    resolver: zodResolver(schema),
    mode: "onTouched",
    defaultValues: getGiftCardDefaults(limits),
  });
  const applyServerErrors = useServerFieldErrors(form);
  const [amount, message, sendMode] = useWatch({
    control: form.control,
    name: ["amount", "message", "sendMode"],
  });
  const total = parseAmount(amount);

  return (
    <form
      noValidate
      onSubmit={form.handleSubmit((values) => onSubmit(toPurchaseGiftCardRequest(values), applyServerErrors))}
      className="flex flex-col gap-6"
    >
      <FieldGroup disabled={isPending} className="gap-5 cut-md bg-card p-4 sm:p-6">
        <GiftCardAmountField control={form.control} limits={limits} disabled={isPending} />
      </FieldGroup>

      <FieldGroup disabled={isPending} className="gap-4 cut-md bg-card p-4 sm:p-6">
        <h2 className="t-h3 text-foreground">Người nhận</h2>
        <div className="grid gap-4 sm:grid-cols-2">
          <FormInputField
            name="recipientName"
            control={form.control}
            label="Tên người nhận"
            autoComplete="off"
          />
          <FormInputField
            name="recipientEmail"
            control={form.control}
            type="email"
            label="Email người nhận"
            autoComplete="off"
          />
        </div>
        <div className="flex flex-col gap-1.5">
          <FormTextareaField
            name="message"
            control={form.control}
            label="Lời nhắn (không bắt buộc)"
            placeholder="Chúc bạn xem phim thật vui!"
            rows={4}
          />
          <p className="text-right text-xs text-muted-foreground tabular-nums">
            {message.length}/{GIFT_CARD_MESSAGE_MAX_LENGTH}
          </p>
        </div>
      </FieldGroup>

      <FieldGroup disabled={isPending} className="gap-4 cut-md bg-card p-4 sm:p-6">
        <Controller
          name="sendMode"
          control={form.control}
          render={({ field }) => (
            <Field>
              <FieldLabel id={sendLabelId}>Thời điểm gửi</FieldLabel>
              <ChipGroup
                role="radiogroup"
                aria-labelledby={sendLabelId}
                className="grid grid-cols-1 gap-2 sm:grid-cols-2"
              >
                <Chip
                  role="radio"
                  selected={field.value === GIFT_SEND_MODE.Now}
                  onClick={() => field.onChange(GIFT_SEND_MODE.Now)}
                >
                  Gửi ngay sau khi thanh toán
                </Chip>
                <Chip
                  role="radio"
                  selected={field.value === GIFT_SEND_MODE.Scheduled}
                  onClick={() => field.onChange(GIFT_SEND_MODE.Scheduled)}
                >
                  Hẹn ngày giờ gửi
                </Chip>
              </ChipGroup>
              <FieldDescription>
                Thẻ được gửi qua email cho người nhận; bạn nhận email xác nhận khi thanh toán xong.
              </FieldDescription>
            </Field>
          )}
        />
        {sendMode === GIFT_SEND_MODE.Scheduled && (
          <FormDateTimePickerField
            name="sendAt"
            control={form.control}
            label="Gửi vào lúc"
            placeholder="Chọn ngày và giờ gửi"
            minuteStep={5}
            calendarDisabled={{ before: getBusinessTodayDate() }}
          />
        )}
      </FieldGroup>

      <div className="flex flex-col gap-3 cut-md bg-card p-4 sm:flex-row sm:items-center sm:justify-between sm:p-6">
        <div>
          <p className="text-sm text-muted-foreground">Tổng thanh toán</p>
          <p className="text-xl font-bold tabular-nums text-primary-text">
            {formatCurrency(Number.isFinite(total) ? total : 0)}
          </p>
          <p className="mt-1 text-xs text-muted-foreground">
            Thẻ quà không áp dụng ưu đãi hạng, mã khuyến mãi và không tích điểm.
          </p>
        </div>
        <SubmitButton
          isPending={isPending}
          label="Tiếp tục thanh toán"
          pendingLabel="Đang tạo đơn..."
          icon={<Gift className="size-4" />}
        />
      </div>
    </form>
  );
};
