import type { Control, FieldPath, FieldValues } from "react-hook-form";
import { Controller } from "react-hook-form";

import { Field, FieldError, FieldLabel } from "@/components/ui/field";
import { SelectField } from "@/components/common/select-field";

export interface SelectOption {
  value: string;
  label: string;
}

interface OptionSelectFieldProps<TFieldValues extends FieldValues> {
  name: FieldPath<TFieldValues>;
  control: Control<TFieldValues>;
  label?: string;
  options: SelectOption[];
  placeholder?: string;
  disabled?: boolean;
}

export const OptionSelectField = <TFieldValues extends FieldValues>({
  name,
  control,
  label,
  options,
  placeholder,
  disabled,
}: OptionSelectFieldProps<TFieldValues>) => (
  <Controller
    name={name}
    control={control}
    render={({ field, fieldState }) => (
      <Field data-invalid={fieldState.invalid} className="group">
        {label && <FieldLabel htmlFor={`field-${name}`}>{label}</FieldLabel>}
        <SelectField
          value={field.value || null}
          onChange={(value) => field.onChange(value)}
          options={options}
          getOptionValue={(option) => option.value}
          getOptionLabel={(option) => option.label}
          placeholder={placeholder}
          disabled={disabled}
        />
        {fieldState.invalid && <FieldError errors={[fieldState.error]} />}
      </Field>
    )}
  />
);
