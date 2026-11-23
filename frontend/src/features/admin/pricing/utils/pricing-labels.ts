import { formatCurrency } from "@/lib/formatters";

export const DAY_OF_WEEK_OPTIONS = [
  { value: 1, label: "T2" },
  { value: 2, label: "T3" },
  { value: 3, label: "T4" },
  { value: 4, label: "T5" },
  { value: 5, label: "T6" },
  { value: 6, label: "T7" },
  { value: 0, label: "CN" },
] as const;

const DAY_OF_WEEK_LABELS = new Map<number, string>(
  DAY_OF_WEEK_OPTIONS.map((option) => [option.value, option.label]),
);

export const formatDaysOfWeek = (days: number[]) =>
  DAY_OF_WEEK_OPTIONS.filter((option) => days.includes(option.value))
    .map((option) => option.label)
    .join(", ");

export const getDayOfWeekLabel = (day: number) => DAY_OF_WEEK_LABELS.get(day) ?? String(day);

const RULE_TYPE_LABELS: Record<string, string> = {
  TimeOfDay: "Khung giờ",
  WeekDay: "Thứ trong tuần",
  SpecialDay: "Ngày lễ và sự kiện",
  Occupancy: "Mức lấp đầy",
};

export const getRuleTypeLabel = (ruleType?: string | null) =>
  (ruleType && RULE_TYPE_LABELS[ruleType]) ?? ruleType ?? "";

const PRICING_MODE_LABELS: Record<string, string> = {
  Standard: "Suất thường",
  Private: "Suất riêng",
  Event: "Suất sự kiện",
};

export const getPricingModeLabel = (mode: string) => PRICING_MODE_LABELS[mode] ?? mode;

const ADJUSTMENT_KIND_LABELS: Record<string, string> = {
  Rule: "Quy tắc",
  DynamicCap: "Chạm trần tăng",
  DynamicFloor: "Chạm sàn giảm",
  MinPrice: "Giá tối thiểu",
};

export const getAdjustmentKindLabel = (kind: string) => ADJUSTMENT_KIND_LABELS[kind] ?? kind;

export const formatMultiplier = (value: number) => `×${Number(value.toFixed(4))}`;

export const formatRuleAdjustment = (mode: string, value: number) =>
  mode === "Multiplier"
    ? formatMultiplier(value)
    : `${value > 0 ? "+" : "−"}${formatCurrency(Math.abs(value))}`;

export const formatSignedCurrency = (amount: number) =>
  amount === 0 ? formatCurrency(0) : `${amount > 0 ? "+" : "−"}${formatCurrency(Math.abs(amount))}`;

export const RULE_TYPE_OPTIONS = [
  { value: "TimeOfDay", label: RULE_TYPE_LABELS.TimeOfDay },
  { value: "WeekDay", label: RULE_TYPE_LABELS.WeekDay },
  { value: "SpecialDay", label: RULE_TYPE_LABELS.SpecialDay },
  { value: "Occupancy", label: RULE_TYPE_LABELS.Occupancy },
] as const;

export const ADJUSTMENT_MODE_OPTIONS = [
  { value: "Multiplier", label: "Nhân hệ số" },
  { value: "Amount", label: "Cộng / trừ số tiền" },
] as const;

export const RULE_MIN_MULTIPLIER = 0.1;
export const RULE_MAX_MULTIPLIER = 5;
export const RULE_MAX_AMOUNT = 10_000_000;
export const RULE_MAX_PRIORITY = 1000;
export const RULE_MAX_TIERS = 20;
export const RULE_MAX_HOURS_BEFORE_START = 720;
