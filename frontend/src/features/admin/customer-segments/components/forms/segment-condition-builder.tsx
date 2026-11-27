import { Layers, Plus, Trash2 } from "lucide-react";

import { Button } from "@/components/ui/button";
import { useFieldDisabled } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectGroup,
  SelectItem,
  SelectLabel,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { SelectField } from "@/components/common/select-field";
import { cn } from "@/lib/utils";

import {
  GROUP_OPERATOR_OPTIONS,
  SEGMENT_FIELD_GROUPS,
  SEGMENT_MAX_CONDITIONS,
  SEGMENT_MAX_GROUP_DEPTH,
  changeConditionField,
  changeConditionOp,
  countConditions,
  createCondition,
  createGroup,
  getFieldDef,
  getOpOptions,
  getValueOptions,
  isNullOp,
  type DraftCondition,
  type DraftGroup,
  type DraftNode,
  type SegmentFieldDef,
  type SegmentGroupOperator,
  type SegmentLookups,
  type SegmentNumberUnit,
  type SegmentOp,
} from "../../utils/segment-condition-fields";

const UNIT_SUFFIX: Partial<Record<SegmentNumberUnit, string>> = {
  days: "ngày",
  money: "₫",
  points: "điểm",
};

interface SegmentConditionBuilderProps {
  value: DraftGroup;
  onChange: (value: DraftGroup) => void;
  lookups: SegmentLookups;
  error?: string;
}

interface ValueEditorProps {
  condition: DraftCondition;
  def: SegmentFieldDef;
  lookups: SegmentLookups;
  disabled: boolean;
  onChange: (condition: DraftCondition) => void;
}

const ValueEditor = ({ condition, def, lookups, disabled, onChange }: ValueEditorProps) => {
  if (isNullOp(condition.op)) {
    return <span className="self-center text-xs text-muted-foreground">Không cần nhập giá trị</span>;
  }

  if (def.kind === "number") {
    const suffix = def.unit ? UNIT_SUFFIX[def.unit] : undefined;
    return (
      <div className="relative">
        <Input
          type="number"
          inputMode={def.integer ? "numeric" : "decimal"}
          step={def.integer ? 1 : "any"}
          value={condition.value}
          onChange={(event) => onChange({ ...condition, value: event.target.value })}
          placeholder="Nhập số"
          disabled={disabled}
          className={cn(suffix && "pr-14")}
          aria-label={`Giá trị của ${def.label}`}
        />
        {suffix && (
          <span className="pointer-events-none absolute inset-y-0 right-3 flex items-center text-xs text-muted-foreground">
            {suffix}
          </span>
        )}
      </div>
    );
  }

  if (condition.op === "in") {
    return (
      <span className="self-center text-xs text-muted-foreground tabular-nums">
        Đã chọn {condition.values.length}
      </span>
    );
  }

  const options = getValueOptions(def, lookups);
  return (
    <SelectField
      value={condition.value || null}
      onChange={(value) => onChange({ ...condition, value })}
      options={options}
      getOptionValue={(option) => option.value}
      getOptionLabel={(option) => option.label}
      placeholder={options.length === 0 ? "Chưa có dữ liệu" : "Chọn giá trị"}
      disabled={disabled || options.length === 0}
    />
  );
};

interface ConditionRowProps {
  condition: DraftCondition;
  lookups: SegmentLookups;
  canRemove: boolean;
  onChange: (condition: DraftCondition) => void;
  onRemove: () => void;
}

const ConditionRow = ({ condition, lookups, canRemove, onChange, onRemove }: ConditionRowProps) => {
  const disabled = useFieldDisabled();
  const def = getFieldDef(condition.field);

  const toggleListValue = (value: string) => {
    const selected = condition.values.includes(value);
    onChange({
      ...condition,
      values: selected
        ? condition.values.filter((item) => item !== value)
        : [...condition.values, value],
    });
  };

  return (
    <div className="grid grid-cols-1 gap-2 sm:grid-cols-[minmax(0,1.3fr)_minmax(0,1fr)_minmax(0,1.2fr)_auto] sm:items-start">
      <Select
        value={def?.key}
        onValueChange={(value) => {
          if (value) onChange(changeConditionField(condition, value));
        }}
        disabled={disabled}
      >
        <SelectTrigger className="w-full" aria-label="Tiêu chí">
          <SelectValue placeholder="Chọn tiêu chí">{def?.label ?? "Chọn tiêu chí"}</SelectValue>
        </SelectTrigger>
        <SelectContent>
          {SEGMENT_FIELD_GROUPS.map((group) => (
            <SelectGroup key={group.label}>
              <SelectLabel>{group.label}</SelectLabel>
              {group.fields.map((field) => (
                <SelectItem key={field.key} value={field.key}>
                  {field.label}
                </SelectItem>
              ))}
            </SelectGroup>
          ))}
        </SelectContent>
      </Select>

      <SelectField
        value={def ? condition.op : null}
        onChange={(value) => {
          if (value) onChange(changeConditionOp(condition, value as SegmentOp));
        }}
        options={def ? getOpOptions(def) : []}
        getOptionValue={(option) => option.value}
        getOptionLabel={(option) => option.label}
        placeholder="Phép so sánh"
        disabled={disabled || !def}
      />

      {def ? (
        <ValueEditor
          condition={condition}
          def={def}
          lookups={lookups}
          disabled={disabled}
          onChange={onChange}
        />
      ) : (
        <span />
      )}

      <Button
        type="button"
        variant="ghost"
        size="icon-sm"
        onClick={onRemove}
        disabled={disabled || !canRemove}
        aria-label="Xóa điều kiện"
        className="justify-self-end"
      >
        <Trash2 className="size-4" />
      </Button>

      {def && condition.op === "in" && (
        <div
          role="group"
          aria-label={`Giá trị của ${def.label}`}
          className="flex flex-wrap gap-1.5 sm:col-span-4"
        >
          {getValueOptions(def, lookups).length === 0 ? (
            <span className="text-xs text-muted-foreground">Chưa có dữ liệu để chọn.</span>
          ) : (
            getValueOptions(def, lookups).map((option) => {
              const selected = condition.values.includes(option.value);
              return (
                <Button
                  key={option.value}
                  type="button"
                  size="xs"
                  variant={selected ? "default" : "outline"}
                  aria-pressed={selected}
                  disabled={disabled}
                  onClick={() => toggleListValue(option.value)}
                >
                  {option.label}
                </Button>
              );
            })
          )}
        </div>
      )}
    </div>
  );
};

interface GroupEditorProps {
  group: DraftGroup;
  depth: number;
  lookups: SegmentLookups;
  totalConditions: number;
  onChange: (group: DraftGroup) => void;
  onRemove?: () => void;
}

const GroupEditor = ({
  group,
  depth,
  lookups,
  totalConditions,
  onChange,
  onRemove,
}: GroupEditorProps) => {
  const disabled = useFieldDisabled();
  const canAddCondition = totalConditions < SEGMENT_MAX_CONDITIONS;

  const updateItem = (index: number, next: DraftNode) =>
    onChange({ ...group, items: group.items.map((item, i) => (i === index ? next : item)) });

  const removeItem = (index: number) =>
    onChange({ ...group, items: group.items.filter((_, i) => i !== index) });

  return (
    <div
      className={cn(
        "space-y-3 rounded-lg border border-border p-3",
        depth === 1 ? "bg-card" : "bg-muted/40",
      )}
    >
      <div className="flex items-center justify-between gap-2">
        <div className="w-full sm:w-72">
          <SelectField
            value={group.operator}
            onChange={(value) => {
              if (value) onChange({ ...group, operator: value as SegmentGroupOperator });
            }}
            options={GROUP_OPERATOR_OPTIONS}
            getOptionValue={(option) => option.value}
            getOptionLabel={(option) => option.label}
            disabled={disabled}
          />
        </div>
        {onRemove && (
          <Button
            type="button"
            variant="ghost"
            size="sm"
            onClick={onRemove}
            disabled={disabled}
            aria-label="Xóa nhóm điều kiện"
          >
            <Trash2 className="size-4" />
            <span className="hidden sm:inline">Xóa nhóm</span>
          </Button>
        )}
      </div>

      <div className="space-y-3">
        {group.items.map((item, index) =>
          item.kind === "group" ? (
            <GroupEditor
              key={item.id}
              group={item}
              depth={depth + 1}
              lookups={lookups}
              totalConditions={totalConditions}
              onChange={(next) => updateItem(index, next)}
              onRemove={() => removeItem(index)}
            />
          ) : (
            <ConditionRow
              key={item.id}
              condition={item}
              lookups={lookups}
              canRemove={group.items.length > 1}
              onChange={(next) => updateItem(index, next)}
              onRemove={() => removeItem(index)}
            />
          ),
        )}
      </div>

      <div className="flex flex-wrap gap-2">
        <Button
          type="button"
          variant="outline"
          size="sm"
          disabled={disabled || !canAddCondition}
          onClick={() => onChange({ ...group, items: [...group.items, createCondition()] })}
        >
          <Plus className="size-3.5" />
          <span>Thêm điều kiện</span>
        </Button>
        {depth < SEGMENT_MAX_GROUP_DEPTH && (
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={disabled || !canAddCondition}
            onClick={() => onChange({ ...group, items: [...group.items, createGroup("or")] })}
          >
            <Layers className="size-3.5" />
            <span>Thêm nhóm</span>
          </Button>
        )}
      </div>
    </div>
  );
};

export const SegmentConditionBuilder = ({
  value,
  onChange,
  lookups,
  error,
}: SegmentConditionBuilderProps) => {
  const totalConditions = countConditions(value);

  return (
    <div className="space-y-2">
      <div className="flex items-center justify-between gap-2">
        <p className="text-sm font-medium text-foreground">Điều kiện phân khúc</p>
        <span className="text-xs text-muted-foreground tabular-nums">
          {totalConditions}/{SEGMENT_MAX_CONDITIONS} điều kiện
        </span>
      </div>
      <GroupEditor
        group={value}
        depth={1}
        lookups={lookups}
        totalConditions={totalConditions}
        onChange={onChange}
      />
      {error && (
        <p role="alert" className="text-xs text-destructive">
          {error}
        </p>
      )}
    </div>
  );
};
