import { formatCurrency, formatNumber } from "@/lib/formatters";
import type { SegmentConditionNode } from "../types";

export const SEGMENT_MAX_DEPTH = 3;
export const SEGMENT_MAX_GROUP_DEPTH = SEGMENT_MAX_DEPTH - 1;
export const SEGMENT_MAX_CONDITIONS = 20;
export const SEGMENT_MAX_LIST_VALUES = 20;
export const SEGMENT_MAX_VALUE_LENGTH = 50;
export const SEGMENT_NAME_MAX_LENGTH = 150;
export const SEGMENT_DESCRIPTION_MAX_LENGTH = 500;

export type SegmentGroupOperator = "and" | "or";
export type SegmentFieldKind = "number" | "bool" | "stage" | "id";
export type SegmentNumberUnit = "count" | "days" | "money" | "points" | "score";
export type SegmentIdSource = "genre" | "screenType" | "tier";
export type SegmentOp = "eq" | "neq" | "gt" | "gte" | "lt" | "lte" | "in" | "isnull" | "isnotnull";

export interface SegmentOption {
  value: string;
  label: string;
}

export type SegmentLookups = Record<SegmentIdSource, SegmentOption[]>;

export interface SegmentFieldDef {
  key: string;
  label: string;
  group: string;
  kind: SegmentFieldKind;
  nullable: boolean;
  integer: boolean;
  unit?: SegmentNumberUnit;
  idSource?: SegmentIdSource;
}

const GROUP_ACTIVITY = "Mức độ hoạt động";
const GROUP_VALUE = "Giá trị khách hàng";
const GROUP_INTEREST = "Sở thích";
const GROUP_POINTS = "Tình trạng điểm";

export const SEGMENT_FIELDS: SegmentFieldDef[] = [
  { key: "activityStage", label: "Mức độ hoạt động", group: GROUP_ACTIVITY, kind: "stage", nullable: false, integer: false },
  { key: "daysSinceLastOrder", label: "Số ngày từ đơn gần nhất", group: GROUP_ACTIVITY, kind: "number", nullable: true, integer: true, unit: "days" },
  { key: "lookbackOrderCount", label: "Số đơn trong kỳ xét", group: GROUP_ACTIVITY, kind: "number", nullable: false, integer: true, unit: "count" },
  { key: "lookbackSpent", label: "Chi tiêu trong kỳ xét", group: GROUP_ACTIVITY, kind: "number", nullable: false, integer: false, unit: "money" },
  { key: "totalSpent", label: "Tổng chi tiêu", group: GROUP_VALUE, kind: "number", nullable: false, integer: false, unit: "money" },
  { key: "estimatedLifetimeValue", label: "Giá trị vòng đời ước tính", group: GROUP_VALUE, kind: "number", nullable: false, integer: false, unit: "money" },
  { key: "recencyScore", label: "Điểm gần đây (1–5)", group: GROUP_VALUE, kind: "number", nullable: false, integer: true, unit: "score" },
  { key: "frequencyScore", label: "Điểm tần suất (1–5)", group: GROUP_VALUE, kind: "number", nullable: false, integer: true, unit: "score" },
  { key: "monetaryScore", label: "Điểm chi tiêu (1–5)", group: GROUP_VALUE, kind: "number", nullable: false, integer: true, unit: "score" },
  { key: "isHighSpender", label: "Khách chi tiêu cao", group: GROUP_VALUE, kind: "bool", nullable: false, integer: false },
  { key: "isHighFrequency", label: "Khách tần suất cao", group: GROUP_VALUE, kind: "bool", nullable: false, integer: false },
  { key: "preferredGenreId", label: "Thể loại ưa thích", group: GROUP_INTEREST, kind: "id", nullable: true, integer: false, idSource: "genre" },
  { key: "preferredScreenTypeId", label: "Định dạng phòng ưa thích", group: GROUP_INTEREST, kind: "id", nullable: true, integer: false, idSource: "screenType" },
  { key: "expiringPoints", label: "Điểm sắp hết hạn", group: GROUP_POINTS, kind: "number", nullable: false, integer: true, unit: "points" },
  { key: "pointsToNextTier", label: "Điểm còn thiếu để lên hạng", group: GROUP_POINTS, kind: "number", nullable: true, integer: true, unit: "points" },
  { key: "isNearTierUpgrade", label: "Khách sắp lên hạng", group: GROUP_POINTS, kind: "bool", nullable: false, integer: false },
  { key: "nextTierId", label: "Hạng kế tiếp", group: GROUP_POINTS, kind: "id", nullable: true, integer: false, idSource: "tier" },
];

const FIELD_MAP = new Map(SEGMENT_FIELDS.map((field) => [field.key.toLowerCase(), field]));

export const getFieldDef = (key: string | null | undefined) =>
  key ? FIELD_MAP.get(key.toLowerCase()) : undefined;

export const SEGMENT_FIELD_GROUPS = SEGMENT_FIELDS.reduce<
  { label: string; fields: SegmentFieldDef[] }[]
>((groups, field) => {
  const existing = groups.find((group) => group.label === field.group);
  if (existing) existing.fields.push(field);
  else groups.push({ label: field.group, fields: [field] });
  return groups;
}, []);

export const ACTIVITY_STAGE_OPTIONS: SegmentOption[] = [
  { value: "New", label: "Khách mới" },
  { value: "Regular", label: "Khách thường xuyên" },
  { value: "Casual", label: "Khách thỉnh thoảng" },
  { value: "Lapsed", label: "Khách lâu chưa quay lại" },
];

export const BOOL_OPTIONS: SegmentOption[] = [
  { value: "true", label: "Có" },
  { value: "false", label: "Không" },
];

export const GROUP_OPERATOR_OPTIONS: { value: SegmentGroupOperator; label: string }[] = [
  { value: "and", label: "Thỏa tất cả điều kiện (VÀ)" },
  { value: "or", label: "Thỏa ít nhất một điều kiện (HOẶC)" },
];

export const SEGMENT_ACTIVE_FILTER_OPTIONS: SegmentOption[] = [
  { value: "true", label: "Đang hoạt động" },
  { value: "false", label: "Tạm dừng" },
];

const OP_LABELS: Record<SegmentOp, string> = {
  eq: "bằng",
  neq: "khác",
  gt: "lớn hơn",
  gte: "từ (≥)",
  lt: "nhỏ hơn",
  lte: "tối đa (≤)",
  in: "thuộc một trong",
  isnull: "chưa có dữ liệu",
  isnotnull: "đã có dữ liệu",
};

const NUMBER_OPS: SegmentOp[] = ["eq", "neq", "gt", "gte", "lt", "lte"];
const LIST_OPS: SegmentOp[] = ["eq", "neq", "in"];
const NULL_OPS: SegmentOp[] = ["isnull", "isnotnull"];

const NUMBER_SYMBOLS: Partial<Record<SegmentOp, string>> = {
  eq: "=",
  neq: "≠",
  gt: ">",
  gte: "≥",
  lt: "<",
  lte: "≤",
};

export const isNullOp = (op: string) => op === "isnull" || op === "isnotnull";

export const getAllowedOps = (def: SegmentFieldDef): SegmentOp[] => {
  const base = def.kind === "number" ? NUMBER_OPS : def.kind === "bool" ? ["eq" as const] : LIST_OPS;
  return def.nullable ? [...base, ...NULL_OPS] : base;
};

export const getOpOptions = (def: SegmentFieldDef): { value: SegmentOp; label: string }[] =>
  getAllowedOps(def).map((op) => ({
    value: op,
    label: def.kind === "bool" && op === "eq" ? "là" : OP_LABELS[op],
  }));

export const getValueOptions = (def: SegmentFieldDef, lookups: SegmentLookups): SegmentOption[] => {
  if (def.kind === "stage") return ACTIVITY_STAGE_OPTIONS;
  if (def.kind === "bool") return BOOL_OPTIONS;
  if (def.kind === "id" && def.idSource) return lookups[def.idSource];
  return [];
};

export interface DraftCondition {
  kind: "condition";
  id: string;
  field: string;
  op: SegmentOp;
  value: string;
  values: string[];
}

export interface DraftGroup {
  kind: "group";
  id: string;
  operator: SegmentGroupOperator;
  items: DraftNode[];
}

export type DraftNode = DraftCondition | DraftGroup;

let sequence = 0;
const nextId = () => `segment-node-${++sequence}`;

const defaultOpFor = (def: SegmentFieldDef): SegmentOp => (def.kind === "number" ? "gte" : "eq");
const defaultValueFor = (def: SegmentFieldDef) => (def.kind === "bool" ? "true" : "");

export const createCondition = (fieldKey = SEGMENT_FIELDS[0].key): DraftCondition => {
  const def = getFieldDef(fieldKey) ?? SEGMENT_FIELDS[0];
  return {
    kind: "condition",
    id: nextId(),
    field: def.key,
    op: defaultOpFor(def),
    value: defaultValueFor(def),
    values: [],
  };
};

export const createGroup = (
  operator: SegmentGroupOperator = "and",
  withCondition = true,
): DraftGroup => ({
  kind: "group",
  id: nextId(),
  operator,
  items: withCondition ? [createCondition()] : [],
});

export const changeConditionField = (
  condition: DraftCondition,
  fieldKey: string,
): DraftCondition => ({ ...createCondition(fieldKey), id: condition.id });

export const changeConditionOp = (condition: DraftCondition, op: SegmentOp): DraftCondition => {
  const wasList = condition.op === "in";
  const isList = op === "in";
  const isNull = isNullOp(op);
  const single = wasList ? (condition.values[0] ?? "") : condition.value;
  return {
    ...condition,
    op,
    value: isList || isNull ? "" : single,
    values: isList ? (wasList ? condition.values : single ? [single] : []) : [],
  };
};

export const toDraftRoot = (node: SegmentConditionNode | null | undefined): DraftGroup => {
  if (!node) return createGroup();
  const draft = toDraft(node);
  return draft.kind === "group" ? draft : { ...createGroup("and", false), items: [draft] };
};

const toDraft = (node: SegmentConditionNode): DraftNode => {
  if (node.operator) {
    return {
      kind: "group",
      id: nextId(),
      operator: node.operator.toLowerCase() === "or" ? "or" : "and",
      items: (node.items ?? []).map(toDraft),
    };
  }
  return {
    kind: "condition",
    id: nextId(),
    field: getFieldDef(node.field)?.key ?? node.field ?? "",
    op: (node.op ?? "eq").toLowerCase() as SegmentOp,
    value: node.value ?? "",
    values: node.values ?? [],
  };
};

export const toRequestNode = (node: DraftNode): SegmentConditionNode => {
  if (node.kind === "group") {
    return { operator: node.operator, items: node.items.map(toRequestNode) };
  }
  if (isNullOp(node.op)) return { field: node.field, op: node.op };
  if (node.op === "in") return { field: node.field, op: node.op, values: node.values };
  return { field: node.field, op: node.op, value: node.value.trim() };
};

export const countConditions = (node: DraftNode): number =>
  node.kind === "group" ? node.items.reduce((sum, item) => sum + countConditions(item), 0) : 1;

const INTEGER_PATTERN = /^-?\d+$/;
const DECIMAL_PATTERN = /^-?\d+(\.\d+)?$/;
const ID_PATTERN = /^\d+$/;

const validateSingleValue = (def: SegmentFieldDef, raw: string): string | null => {
  const value = raw.trim();
  if (value === "") return `Vui lòng nhập giá trị cho "${def.label}".`;
  if (value.length > SEGMENT_MAX_VALUE_LENGTH) {
    return `Giá trị của "${def.label}" tối đa ${SEGMENT_MAX_VALUE_LENGTH} ký tự.`;
  }
  switch (def.kind) {
    case "bool":
      return value === "true" || value === "false"
        ? null
        : `Vui lòng chọn giá trị cho "${def.label}".`;
    case "stage":
      return ACTIVITY_STAGE_OPTIONS.some((option) => option.value === value)
        ? null
        : `Vui lòng chọn giá trị cho "${def.label}".`;
    case "id":
      return ID_PATTERN.test(value) && Number(value) > 0
        ? null
        : `Vui lòng chọn giá trị cho "${def.label}".`;
    default:
      return (def.integer ? INTEGER_PATTERN : DECIMAL_PATTERN).test(value)
        ? null
        : def.integer
          ? `"${def.label}" phải là số nguyên.`
          : `"${def.label}" phải là số.`;
  }
};

const validateCondition = (condition: DraftCondition): string | null => {
  const def = getFieldDef(condition.field);
  if (!def) return "Vui lòng chọn tiêu chí cho mọi điều kiện.";
  if (!getAllowedOps(def).includes(condition.op)) {
    return `Phép so sánh của "${def.label}" không hợp lệ.`;
  }
  if (isNullOp(condition.op)) return null;
  if (condition.op === "in") {
    if (condition.values.length === 0) return `Vui lòng chọn ít nhất một giá trị cho "${def.label}".`;
    if (condition.values.length > SEGMENT_MAX_LIST_VALUES) {
      return `"${def.label}" chọn tối đa ${SEGMENT_MAX_LIST_VALUES} giá trị.`;
    }
    for (const value of condition.values) {
      const issue = validateSingleValue(def, value);
      if (issue) return issue;
    }
    return null;
  }
  return validateSingleValue(def, condition.value);
};

const validateNode = (node: DraftNode, depth: number): string | null => {
  if (node.kind === "condition") return validateCondition(node);
  if (depth > SEGMENT_MAX_GROUP_DEPTH) {
    return `Điều kiện lồng tối đa ${SEGMENT_MAX_DEPTH} cấp.`;
  }
  if (node.items.length === 0) return "Mỗi nhóm cần ít nhất một điều kiện.";
  for (const item of node.items) {
    const issue = validateNode(item, depth + 1);
    if (issue) return issue;
  }
  return null;
};

export const validateDraft = (root: DraftGroup): string | null => {
  if (countConditions(root) > SEGMENT_MAX_CONDITIONS) {
    return `Tối đa ${SEGMENT_MAX_CONDITIONS} điều kiện cho một phân khúc.`;
  }
  return validateNode(root, 1);
};

export const getOptionLabel = (
  def: SegmentFieldDef,
  raw: string,
  lookups: SegmentLookups,
): string => getValueOptions(def, lookups).find((option) => option.value === raw)?.label ?? `#${raw}`;

const formatNumberValue = (def: SegmentFieldDef, raw: string) => {
  const value = Number(raw);
  if (!Number.isFinite(value)) return raw;
  switch (def.unit) {
    case "money":
      return formatCurrency(value);
    case "days":
      return `${formatNumber(value)} ngày`;
    case "points":
      return `${formatNumber(value)} điểm`;
    default:
      return formatNumber(value);
  }
};

const describeCondition = (node: SegmentConditionNode, lookups: SegmentLookups): string => {
  const def = getFieldDef(node.field);
  if (!def) return node.field ?? "";
  const op = (node.op ?? "eq").toLowerCase() as SegmentOp;

  if (op === "isnull") return `${def.label}: chưa có dữ liệu`;
  if (op === "isnotnull") return `${def.label}: đã có dữ liệu`;

  if (def.kind === "number") {
    return `${def.label} ${NUMBER_SYMBOLS[op] ?? "="} ${formatNumberValue(def, node.value ?? "")}`;
  }
  if (def.kind === "bool") {
    return `${def.label}: ${node.value === "false" ? "Không" : "Có"}`;
  }
  if (op === "in") {
    const labels = (node.values ?? []).map((value) => getOptionLabel(def, value, lookups));
    return `${def.label}: ${labels.join(", ")}`;
  }
  const label = getOptionLabel(def, node.value ?? "", lookups);
  return op === "neq" ? `${def.label} ≠ ${label}` : `${def.label}: ${label}`;
};

export const describeSegmentConditions = (
  node: SegmentConditionNode,
  lookups: SegmentLookups,
  nested = false,
): string => {
  if (!node.operator) return describeCondition(node, lookups);
  const parts = (node.items ?? []).map((item) => describeSegmentConditions(item, lookups, true));
  const text = parts.join(node.operator.toLowerCase() === "or" ? " hoặc " : " và ");
  return nested && parts.length > 1 ? `(${text})` : text;
};
