import { Plus, Trash2 } from "lucide-react";
import { useFieldArray, type Control, type FieldErrors } from "react-hook-form";

import { Button } from "@/components/ui/button";
import { Field, FieldDescription, FieldError, FieldLabel } from "@/components/ui/field";
import { FormInputField } from "@/components/common/form/form-input-field";

import { RULE_MAX_TIERS } from "../../utils/pricing-labels";
import type { PricingRuleFormValues } from "./pricing-rule-schema";

interface OccupancyTiersFieldProps {
  control: Control<PricingRuleFormValues>;
  errors: FieldErrors<PricingRuleFormValues>;
  mode: PricingRuleFormValues["adjustmentMode"];
}

const EMPTY_TIER = { fromPercent: "", toPercent: "", maxHoursBeforeStart: "", value: "" };

export const OccupancyTiersField = ({ control, errors, mode }: OccupancyTiersFieldProps) => {
  const { fields, append, remove } = useFieldArray({ control, name: "tiers" });
  const rootError = errors.tiers?.root ?? errors.tiers;

  return (
    <Field data-invalid={Boolean(rootError?.message)}>
      <FieldLabel>Các bậc mức lấp đầy</FieldLabel>
      <FieldDescription>
        Mỗi bậc áp dụng khi tỷ lệ ghế đã bán và đang giữ nằm trong khoảng đã chọn. Để trống "Đến (%)" ở
        bậc cuối cùng. Điền "Còn tối đa (giờ)" để bậc chỉ áp dụng khi suất gần giờ chiếu, dùng cho bậc
        giảm giá suất vắng.
      </FieldDescription>

      <div className="space-y-3">
        {fields.map((item, index) => (
          <div
            key={item.id}
            className="grid grid-cols-2 items-start gap-2 border border-border p-3 sm:grid-cols-[1fr_1fr_1fr_1fr_auto]"
          >
            <FormInputField
              name={`tiers.${index}.fromPercent`}
              control={control}
              label="Từ (%)"
              type="number"
              min={0}
              max={100}
              step={1}
            />
            <FormInputField
              name={`tiers.${index}.toPercent`}
              control={control}
              label="Đến (%)"
              type="number"
              min={0}
              max={100}
              step={1}
              placeholder="Không giới hạn"
            />
            <FormInputField
              name={`tiers.${index}.maxHoursBeforeStart`}
              control={control}
              label="Còn tối đa (giờ)"
              type="number"
              min={1}
              step={1}
              placeholder="Mọi lúc"
            />
            <FormInputField
              name={`tiers.${index}.value`}
              control={control}
              label={mode === "Multiplier" ? "Hệ số" : "Số tiền"}
              type="number"
              step={mode === "Multiplier" ? 0.05 : 1000}
            />
            <Button
              type="button"
              variant="ghost"
              size="icon"
              className="mt-6 size-8 text-destructive"
              onClick={() => remove(index)}
              aria-label={`Xóa bậc ${index + 1}`}
            >
              <Trash2 className="size-4" />
            </Button>
          </div>
        ))}

        <Button
          type="button"
          variant="outline"
          size="sm"
          disabled={fields.length >= RULE_MAX_TIERS}
          onClick={() => append(EMPTY_TIER)}
        >
          <Plus className="size-4" />
          Thêm bậc
        </Button>
      </div>

      {rootError?.message && <FieldError errors={[{ message: rootError.message }]} />}
    </Field>
  );
};
