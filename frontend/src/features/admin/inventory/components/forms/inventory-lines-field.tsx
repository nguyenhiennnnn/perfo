import { ListPlus, Plus, Trash2 } from "lucide-react";
import { useFieldArray, useFormContext, useWatch } from "react-hook-form";

import { Button } from "@/components/ui/button";
import { Field, FieldDescription, FieldError, FieldLabel } from "@/components/ui/field";
import { FormInputField } from "@/components/common/form/form-input-field";
import { formatCurrency } from "@/lib/formatters";

import { useAdminInventoryItemOptions } from "../../hooks/use-admin-inventory";
import type { InventoryItemDto } from "../../types";
import {
  INVENTORY_QUANTITY_STEP,
  RECEIPT_MAX_LINES,
  RECIPE_MAX_LINES,
  STOCKTAKE_MAX_LINES,
  WASTE_MAX_LINES,
  formatQuantity,
  formatSignedQuantity,
} from "../../utils/inventory-labels";
import {
  EMPTY_INVENTORY_LINE,
  parseDecimal,
  type InventoryLineMode,
  type InventoryLineValues,
  type InventoryLinesFormValues,
} from "../../utils/inventory-schema";
import { OptionSelectField } from "./option-select-field";

interface ModeConfig {
  label: string;
  description: string;
  quantityLabel: string;
  maxLines: number;
}

const MODE_CONFIG: Record<InventoryLineMode, ModeConfig> = {
  receipt: {
    label: "Sản phẩm nhập",
    description: "Mỗi lần nhập cập nhật giá vốn bình quân gia quyền của sản phẩm.",
    quantityLabel: "Số lượng nhập",
    maxLines: RECEIPT_MAX_LINES,
  },
  stocktake: {
    label: "Sản phẩm kiểm kê",
    description: "Chênh lệch giữa số đếm và sổ sách được ghi thành điều chỉnh tồn.",
    quantityLabel: "Số đếm thực tế",
    maxLines: STOCKTAKE_MAX_LINES,
  },
  waste: {
    label: "Sản phẩm hao hụt",
    description: "Số lượng hủy được trừ khỏi tồn theo giá vốn hiện hành.",
    quantityLabel: "Số lượng hủy",
    maxLines: WASTE_MAX_LINES,
  },
  recipe: {
    label: "Nguyên liệu cho một combo",
    description: "Số lượng sản phẩm kho tiêu hao khi bán một combo.",
    quantityLabel: "Số lượng tiêu hao",
    maxLines: RECIPE_MAX_LINES,
  },
};

const DECIMAL_SCALE = 1000;

const getLineHint = (
  mode: InventoryLineMode,
  item: InventoryItemDto | undefined,
  line: InventoryLineValues,
): string | null => {
  if (!item) return null;
  const quantity = parseDecimal(line.quantity);

  switch (mode) {
    case "stocktake": {
      const book = `Sổ sách: ${formatQuantity(item.quantityOnHand, item.unit)}`;
      if (quantity === null) return book;
      const difference = Math.round((quantity - item.quantityOnHand) * DECIMAL_SCALE) / DECIMAL_SCALE;
      return `${book}. Chênh lệch: ${formatSignedQuantity(difference, item.unit)}`;
    }
    case "waste":
      return `Tồn hiện có: ${formatQuantity(item.quantityOnHand, item.unit)}, khả dụng ${formatQuantity(item.availableQuantity, item.unit)}`;
    case "receipt": {
      const current = `Giá vốn hiện hành: ${formatCurrency(item.unitCost)}`;
      const cost = parseDecimal(line.unitCost);
      if (quantity === null || cost === null) return current;
      return `${current}. Thành tiền: ${formatCurrency(Math.round(quantity * cost))}`;
    }
    case "recipe":
      return `Giá vốn hiện hành: ${formatCurrency(item.unitCost)} mỗi ${item.unit}`;
  }
};

interface InventoryLinesFieldProps {
  mode: InventoryLineMode;
}

export const InventoryLinesField = ({ mode }: InventoryLinesFieldProps) => {
  const config = MODE_CONFIG[mode];
  const {
    control,
    formState: { errors },
  } = useFormContext<InventoryLinesFormValues>();
  const { fields, append, remove, replace } = useFieldArray({ control, name: "lines" });
  const lines = useWatch({ control, name: "lines" }) ?? [];
  const itemsQuery = useAdminInventoryItemOptions();
  const items = itemsQuery.data ?? [];
  const rootError = errors.lines?.root ?? errors.lines;
  const showUnitCost = mode === "receipt";

  const total = lines.reduce((sum, line) => {
    const quantity = parseDecimal(line.quantity);
    if (quantity === null) return sum;
    if (mode === "receipt") {
      const cost = parseDecimal(line.unitCost);
      return cost === null ? sum : sum + quantity * cost;
    }
    const item = items.find((candidate) => String(candidate.inventoryItemId) === line.inventoryItemId);
    return item ? sum + quantity * item.unitCost : sum;
  }, 0);

  const fillAllItems = () =>
    replace(
      items
        .filter((item) => item.isActive)
        .slice(0, config.maxLines)
        .map((item) => ({ ...EMPTY_INVENTORY_LINE, inventoryItemId: String(item.inventoryItemId) })),
    );

  return (
    <Field data-invalid={Boolean(rootError?.message)}>
      <FieldLabel>{config.label}</FieldLabel>
      <FieldDescription>{config.description}</FieldDescription>

      {itemsQuery.isError && (
        <FieldError errors={[{ message: "Không tải được danh sách sản phẩm kho. Tải lại trang để thử lại." }]} />
      )}

      <div className="space-y-3">
        {fields.map((field, index) => {
          const line = lines[index] ?? EMPTY_INVENTORY_LINE;
          const selectedId = line.inventoryItemId;
          const item = items.find((candidate) => String(candidate.inventoryItemId) === selectedId);
          const takenByOthers = new Set(
            lines.filter((_, lineIndex) => lineIndex !== index).map((other) => other.inventoryItemId),
          );
          const options = items
            .filter((candidate) => {
              const id = String(candidate.inventoryItemId);
              return !takenByOthers.has(id) && (candidate.isActive || id === selectedId);
            })
            .map((candidate) => ({
              value: String(candidate.inventoryItemId),
              label: `${candidate.name} (${candidate.code})`,
            }));
          const hint = getLineHint(mode, item, line);
          const quantityLabel = item ? `${config.quantityLabel} (${item.unit})` : config.quantityLabel;

          return (
            <div key={field.id} className="space-y-1.5 bg-secondary/50 p-3 cut-sm">
              <div
                className={
                  showUnitCost
                    ? "grid grid-cols-1 items-start gap-2 sm:grid-cols-[2fr_1fr_1fr_auto]"
                    : "grid grid-cols-1 items-start gap-2 sm:grid-cols-[2fr_1fr_auto]"
                }
              >
                <OptionSelectField
                  name={`lines.${index}.inventoryItemId`}
                  control={control}
                  label="Sản phẩm kho"
                  placeholder="Chọn sản phẩm kho"
                  options={options}
                />
                <FormInputField
                  name={`lines.${index}.quantity`}
                  control={control}
                  label={quantityLabel}
                  type="number"
                  min={0}
                  step={INVENTORY_QUANTITY_STEP}
                />
                {showUnitCost && (
                  <FormInputField
                    name={`lines.${index}.unitCost`}
                    control={control}
                    label="Đơn giá nhập"
                    type="number"
                    min={0}
                    step={1}
                  />
                )}
                <Button
                  type="button"
                  variant="ghost"
                  size="icon"
                  className="mt-6 size-8 text-destructive"
                  onClick={() => remove(index)}
                  aria-label={`Xóa dòng ${index + 1}`}
                >
                  <Trash2 className="size-4" />
                </Button>
              </div>
              {hint && <p className="text-xs text-muted-foreground">{hint}</p>}
            </div>
          );
        })}

        <div className="flex flex-wrap items-center justify-between gap-2">
          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              variant="outline"
              size="sm"
              disabled={fields.length >= config.maxLines}
              onClick={() => append(EMPTY_INVENTORY_LINE)}
            >
              <Plus className="size-4" />
              Thêm dòng
            </Button>
            {mode === "stocktake" && (
              <Button
                type="button"
                variant="outline"
                size="sm"
                disabled={!items.some((item) => item.isActive)}
                onClick={fillAllItems}
              >
                <ListPlus className="size-4" />
                Thêm toàn bộ sản phẩm
              </Button>
            )}
          </div>
          {(mode === "receipt" || mode === "recipe") && total > 0 && (
            <p className="text-sm text-muted-foreground">
              {mode === "receipt" ? "Tổng tiền nhập" : "Giá vốn ước tính một combo"}:{" "}
              <span className="font-semibold tabular-nums text-foreground">
                {formatCurrency(Math.round(total))}
              </span>
            </p>
          )}
        </div>
      </div>

      {rootError?.message && <FieldError errors={[{ message: rootError.message }]} />}
    </Field>
  );
};
