import { z } from "zod";

import { getBusinessToday } from "@/lib/business-time";
import { formatNumber } from "@/lib/formatters";

import {
  INVENTORY_CODE_MAX_LENGTH,
  INVENTORY_CODE_PATTERN,
  INVENTORY_MAX_QUANTITY,
  INVENTORY_MAX_UNIT_COST,
  INVENTORY_NAME_MAX_LENGTH,
  INVENTORY_QUANTITY_DECIMALS,
  INVENTORY_UNIT_MAX_LENGTH,
  RECEIPT_MAX_LINES,
  RECIPE_MAX_LINES,
  STOCKTAKE_MAX_LINES,
  STOCK_NOTE_MAX_LENGTH,
  SUPPLIER_NAME_MAX_LENGTH,
  WASTE_MAX_LINES,
} from "./inventory-labels";
import { WasteReason } from "../types";

export type InventoryLineMode = "receipt" | "stocktake" | "waste" | "recipe";

export interface InventoryLineValues {
  inventoryItemId: string;
  quantity: string;
  unitCost: string;
}

export interface InventoryLinesFormValues {
  lines: InventoryLineValues[];
}

export const EMPTY_INVENTORY_LINE: InventoryLineValues = {
  inventoryItemId: "",
  quantity: "",
  unitCost: "",
};

const DECIMAL_SCALE = 10 ** INVENTORY_QUANTITY_DECIMALS;
const DECIMAL_TOLERANCE = 1e-6;

export const parseDecimal = (raw: string): number | null => {
  const text = raw.trim();
  if (text === "") return null;
  const value = Number(text);
  return Number.isFinite(value) ? value : null;
};

const hasAllowedDecimals = (value: number) =>
  Math.abs(Math.round(value * DECIMAL_SCALE) - value * DECIMAL_SCALE) < DECIMAL_TOLERANCE;

export const isPositiveQuantity = (raw: string) => {
  const value = parseDecimal(raw);
  return (
    value !== null && value > 0 && value <= INVENTORY_MAX_QUANTITY && hasAllowedDecimals(value)
  );
};

export const isNonNegativeQuantity = (raw: string) => {
  const value = parseDecimal(raw);
  return (
    value !== null && value >= 0 && value <= INVENTORY_MAX_QUANTITY && hasAllowedDecimals(value)
  );
};

export const isUnitCost = (raw: string) => {
  const value = parseDecimal(raw);
  return (
    value !== null && value >= 0 && value <= INVENTORY_MAX_UNIT_COST && Number.isInteger(value)
  );
};

const QUANTITY_MESSAGE = `Số lượng phải lớn hơn 0, tối đa ${formatNumber(INVENTORY_MAX_QUANTITY)} và có tối đa ${INVENTORY_QUANTITY_DECIMALS} chữ số thập phân.`;
const COUNTED_MESSAGE = `Số đếm phải từ 0 đến ${formatNumber(INVENTORY_MAX_QUANTITY)} và có tối đa ${INVENTORY_QUANTITY_DECIMALS} chữ số thập phân.`;
const UNIT_COST_MESSAGE = `Đơn giá nhập phải là số nguyên đồng từ 0 đến ${formatNumber(INVENTORY_MAX_UNIT_COST)}.`;
const DUPLICATE_MESSAGE = "Sản phẩm kho trong phiếu không được trùng nhau.";
const FUTURE_DATE_MESSAGE = "Ngày không được sau hôm nay.";

const LINE_LIMITS: Record<InventoryLineMode, number> = {
  receipt: RECEIPT_MAX_LINES,
  stocktake: STOCKTAKE_MAX_LINES,
  waste: WASTE_MAX_LINES,
  recipe: RECIPE_MAX_LINES,
};

const lineSchema = z.object({
  inventoryItemId: z.string().min(1, "Vui lòng chọn sản phẩm kho."),
  quantity: z.string(),
  unitCost: z.string(),
});

const createLinesSchema = (mode: InventoryLineMode) =>
  z
    .array(lineSchema)
    .min(1, "Cần ít nhất một dòng sản phẩm.")
    .max(LINE_LIMITS[mode], `Tối đa ${LINE_LIMITS[mode]} dòng sản phẩm.`)
    .superRefine((lines, ctx) => {
      const seen = new Set<string>();
      lines.forEach((line, index) => {
        if (mode === "stocktake") {
          if (!isNonNegativeQuantity(line.quantity)) {
            ctx.addIssue({ code: "custom", path: [index, "quantity"], message: COUNTED_MESSAGE });
          }
        } else if (!isPositiveQuantity(line.quantity)) {
          ctx.addIssue({ code: "custom", path: [index, "quantity"], message: QUANTITY_MESSAGE });
        }

        if (mode === "receipt" && !isUnitCost(line.unitCost)) {
          ctx.addIssue({ code: "custom", path: [index, "unitCost"], message: UNIT_COST_MESSAGE });
        }

        if (line.inventoryItemId === "") return;
        if (seen.has(line.inventoryItemId)) {
          ctx.addIssue({
            code: "custom",
            path: [index, "inventoryItemId"],
            message: DUPLICATE_MESSAGE,
          });
        }
        seen.add(line.inventoryItemId);
      });
    });

const sheetDateSchema = (requiredMessage: string) =>
  z
    .string()
    .min(1, requiredMessage)
    .refine((value) => value <= getBusinessToday(), FUTURE_DATE_MESSAGE);

const noteSchema = z
  .string()
  .trim()
  .max(STOCK_NOTE_MAX_LENGTH, `Ghi chú tối đa ${STOCK_NOTE_MAX_LENGTH} ký tự.`);

export const inventoryItemFormSchema = z.object({
  code: z
    .string()
    .trim()
    .min(1, "Vui lòng nhập mã sản phẩm kho.")
    .max(INVENTORY_CODE_MAX_LENGTH, `Mã tối đa ${INVENTORY_CODE_MAX_LENGTH} ký tự.`)
    .regex(INVENTORY_CODE_PATTERN, "Mã gồm 2 đến 30 ký tự chữ, số, gạch dưới hoặc gạch ngang."),
  name: z
    .string()
    .trim()
    .min(1, "Vui lòng nhập tên sản phẩm kho.")
    .max(INVENTORY_NAME_MAX_LENGTH, `Tên tối đa ${INVENTORY_NAME_MAX_LENGTH} ký tự.`),
  unit: z
    .string()
    .trim()
    .min(1, "Vui lòng nhập đơn vị tính.")
    .max(INVENTORY_UNIT_MAX_LENGTH, `Đơn vị tính tối đa ${INVENTORY_UNIT_MAX_LENGTH} ký tự.`),
  lowStockThreshold: z.string().refine(isNonNegativeQuantity, {
    message: `Ngưỡng cảnh báo phải từ 0 đến ${formatNumber(INVENTORY_MAX_QUANTITY)} và có tối đa ${INVENTORY_QUANTITY_DECIMALS} chữ số thập phân.`,
  }),
  isActive: z.boolean(),
});
export type InventoryItemFormValues = z.infer<typeof inventoryItemFormSchema>;

export const comboRecipeFormSchema = z.object({
  comboId: z.string().min(1, "Vui lòng chọn combo."),
  lines: createLinesSchema("recipe"),
});
export type ComboRecipeFormValues = z.infer<typeof comboRecipeFormSchema>;

export const stockReceiptFormSchema = z.object({
  supplierName: z
    .string()
    .trim()
    .min(1, "Vui lòng nhập nhà cung cấp.")
    .max(SUPPLIER_NAME_MAX_LENGTH, `Tên nhà cung cấp tối đa ${SUPPLIER_NAME_MAX_LENGTH} ký tự.`),
  receivedDate: sheetDateSchema("Vui lòng chọn ngày nhập."),
  note: noteSchema,
  lines: createLinesSchema("receipt"),
});
export type StockReceiptFormValues = z.infer<typeof stockReceiptFormSchema>;

export const stocktakeFormSchema = z.object({
  stocktakeDate: sheetDateSchema("Vui lòng chọn ngày kiểm kê."),
  note: noteSchema,
  lines: createLinesSchema("stocktake"),
});
export type StocktakeFormValues = z.infer<typeof stocktakeFormSchema>;

export const wasteFormSchema = z
  .object({
    wasteDate: sheetDateSchema("Vui lòng chọn ngày hủy hàng."),
    reason: z.enum([
      WasteReason.Expired,
      WasteReason.Damaged,
      WasteReason.Spilled,
      WasteReason.Other,
    ]),
    note: noteSchema,
    lines: createLinesSchema("waste"),
  })
  .superRefine((values, ctx) => {
    if (values.reason === WasteReason.Other && values.note === "") {
      ctx.addIssue({
        code: "custom",
        path: ["note"],
        message: "Lý do khác cần có ghi chú.",
      });
    }
  });
export type WasteFormValues = z.infer<typeof wasteFormSchema>;
