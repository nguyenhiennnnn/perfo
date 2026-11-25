import { useId, useState } from "react";
import { Controller } from "react-hook-form";
import type { Control } from "react-hook-form";
import { Chip, ChipGroup } from "@/components/common/chip";
import { Field, FieldDescription, FieldError, FieldLabel } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { formatCurrency } from "@/lib/formatters";
import { parseAmount } from "../utils/gift-card-form";
import type { GiftCardFormValues, GiftCardLimits } from "../utils/gift-card-form";

interface GiftCardAmountFieldProps {
  control: Control<GiftCardFormValues>;
  limits: GiftCardLimits;
  disabled?: boolean;
}

export const GiftCardAmountField = ({ control, limits, disabled }: GiftCardAmountFieldProps) => {
  const labelId = useId();
  const customInputId = useId();
  const denominations = limits.giftCardDenominations;
  const [isCustomMode, setIsCustomMode] = useState(false);

  return (
    <Controller
      name="amount"
      control={control}
      render={({ field, fieldState }) => {
        const matched = denominations.find((value) => String(value) === field.value);
        const isCustom = isCustomMode || (matched === undefined && field.value !== "");

        return (
          <Field data-invalid={fieldState.invalid}>
            <FieldLabel id={labelId}>Mệnh giá</FieldLabel>
            <ChipGroup
              role="radiogroup"
              aria-labelledby={labelId}
              className="grid grid-cols-2 gap-2 sm:grid-cols-3"
            >
              {denominations.map((value) => (
                <Chip
                  key={value}
                  role="radio"
                  disabled={disabled}
                  selected={!isCustom && matched === value}
                  onClick={() => {
                    setIsCustomMode(false);
                    field.onChange(String(value));
                  }}
                  className="tabular-nums"
                >
                  {formatCurrency(value)}
                </Chip>
              ))}
              <Chip
                role="radio"
                disabled={disabled}
                selected={isCustom}
                onClick={() => {
                  setIsCustomMode(true);
                  if (matched !== undefined) field.onChange("");
                }}
              >
                Số khác
              </Chip>
            </ChipGroup>

            {isCustom && (
              <>
                <FieldLabel htmlFor={customInputId} className="sr-only">
                  Mệnh giá tự nhập (VND)
                </FieldLabel>
                <Input
                  id={customInputId}
                  inputMode="numeric"
                  autoComplete="off"
                  autoFocus={isCustomMode}
                  placeholder="Nhập số tiền, ví dụ 300000"
                  disabled={disabled}
                  value={field.value}
                  onBlur={field.onBlur}
                  onChange={(event) => field.onChange(event.target.value.replace(/\D/g, ""))}
                  aria-invalid={fieldState.invalid}
                />
                <FieldDescription>
                  Từ {formatCurrency(limits.giftCardMinAmount)} đến {formatCurrency(limits.giftCardMaxAmount)}
                  {field.value ? ` · Bạn nhập ${formatCurrency(parseAmount(field.value))}` : ""}
                </FieldDescription>
              </>
            )}

            {fieldState.invalid && <FieldError errors={[fieldState.error]} />}
          </Field>
        );
      }}
    />
  );
};
