import { useMemo, useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";

import { Button } from "@/components/ui/button";
import {
  Field,
  FieldDescription,
  FieldError,
  FieldGroup,
  FieldLabel,
} from "@/components/ui/field";
import { FormInputField } from "@/components/common/form/form-input-field";
import { FormSelectField } from "@/components/common/form/form-select-field";
import { FormSwitchField } from "@/components/common/form/form-switch-field";
import { FormDatePickerField } from "@/components/common/form/form-date-picker-field";
import { FormTimePickerField } from "@/components/common/form/form-time-picker-field";
import { FormMultiSelectField } from "@/components/common/form/form-search-select-field";
import { FormActions } from "@/components/common/form/form-actions";
import { useServerFieldErrors } from "@/hooks/use-server-field-errors";
import { useDebouncedValue } from "@/hooks/use-debounced-value";
import { toDateInputValue, formatDate } from "@/lib/formatters";
import { useAdminRooms } from "@/features/admin/rooms/hooks/use-admin-rooms";
import { useAdminScreenTypes } from "@/features/admin/catalog/hooks/use-admin-screen-type";
import { useAdminMovies } from "@/features/admin/movies/hooks/use-admin-movies";
import {
  FORM_SAVE_CHANGES_LABEL,
  LOOKUP_PAGE_SIZE,
  SEARCH_LOOKUP_PAGE_SIZE,
} from "@/config/default-constants";

import {
  useAdminCreatePricingRule,
  useAdminUpdatePricingRule,
} from "../../hooks/use-admin-pricing-rules";
import { useAdminSpecialDays } from "../../hooks/use-admin-special-days";
import type { PricingRuleDto, UpsertPricingRuleRequest } from "../../types";
import {
  ADJUSTMENT_MODE_OPTIONS,
  DAY_OF_WEEK_OPTIONS,
  RULE_MAX_AMOUNT,
  RULE_MAX_MULTIPLIER,
  RULE_MAX_PRIORITY,
  RULE_MIN_MULTIPLIER,
  RULE_TYPE_OPTIONS,
} from "../../utils/pricing-labels";
import { OccupancyTiersField } from "./occupancy-tiers-field";
import { pricingRuleFormSchema, type PricingRuleFormValues } from "./pricing-rule-schema";

interface PricingRuleFormProps {
  rule?: PricingRuleDto;
  onSuccess: () => void;
}

const toText = (value?: number | null) => (value === null || value === undefined ? "" : String(value));

const toNumberOrNull = (value: string) => (value.trim() === "" ? null : Number(value));

export interface LookupOption {
  id: number;
  label: string;
}

export const buildOptions = <T,>(
  selectedIds: number[],
  items: T[],
  getId: (item: T) => number,
  getLabel: (item: T) => string,
  placeholderPrefix: string,
): LookupOption[] => {
  const options = items.map((item) => ({ id: getId(item), label: getLabel(item) }));
  const known = new Set(options.map((o) => o.id));
  const missing = selectedIds
    .filter((id) => !known.has(id))
    .map((id) => ({ id, label: `${placeholderPrefix} #${id}` }));
  return [...options, ...missing];
};

export const PricingRuleForm = ({ rule, onSuccess }: PricingRuleFormProps) => {
  const isEdit = Boolean(rule);
  const createRule = useAdminCreatePricingRule();
  const updateRule = useAdminUpdatePricingRule();
  const isPending = createRule.isPending || updateRule.isPending;

  const [movieSearch, setMovieSearch] = useState("");
  const [roomSearch, setRoomSearch] = useState("");
  const [screenTypeSearch, setScreenTypeSearch] = useState("");
  const [specialDaySearch, setSpecialDaySearch] = useState("");

  const movies = useAdminMovies({
    search: useDebouncedValue(movieSearch),
    pageSize: SEARCH_LOOKUP_PAGE_SIZE,
  });
  const rooms = useAdminRooms({ pageSize: LOOKUP_PAGE_SIZE });
  const screenTypes = useAdminScreenTypes({ pageSize: LOOKUP_PAGE_SIZE });
  const specialDays = useAdminSpecialDays({ pageSize: LOOKUP_PAGE_SIZE });

  const form = useForm<PricingRuleFormValues>({
    resolver: zodResolver(pricingRuleFormSchema),
    defaultValues: {
      name: rule?.name ?? "",
      ruleType: rule?.ruleType ?? "TimeOfDay",
      adjustmentMode: rule?.adjustmentMode ?? "Multiplier",
      adjustmentValue: toText(rule?.adjustmentValue),
      priority: rule?.priority ?? 0,
      validFrom: toDateInputValue(rule?.validFrom) ?? "",
      validTo: toDateInputValue(rule?.validTo) ?? "",
      startTime: rule?.condition.startTime ?? "",
      endTime: rule?.condition.endTime ?? "",
      daysOfWeek: rule?.condition.daysOfWeek ?? [],
      specialDayIds: rule?.condition.specialDayIds ?? [],
      tiers: (rule?.condition.tiers ?? []).map((tier) => ({
        fromPercent: toText(tier.fromPercent),
        toPercent: toText(tier.toPercent),
        maxHoursBeforeStart: toText(tier.maxHoursBeforeStart),
        value: toText(tier.value),
      })),
      roomIds: rule?.roomIds ?? [],
      screenTypeIds: rule?.screenTypeIds ?? [],
      movieIds: rule?.movieIds ?? [],
      isActive: rule?.isActive ?? true,
    },
  });

  const applyServerErrors = useServerFieldErrors(form);
  const ruleType = form.watch("ruleType");
  const mode = form.watch("adjustmentMode");
  const selectedDays = form.watch("daysOfWeek");
  const movieIds = form.watch("movieIds");
  const specialDayIds = form.watch("specialDayIds");

  const roomIds = form.watch("roomIds");
  const screenTypeIds = form.watch("screenTypeIds");

  const movieOptions = useMemo(
    () => buildOptions(movieIds, movies.data?.items ?? [], (m) => m.movieId, (m) => m.title, "Phim"),
    [movieIds, movies.data],
  );
  const specialDayOptions = useMemo(
    () =>
      buildOptions(
        specialDayIds,
        specialDays.data?.items ?? [],
        (d) => d.specialDayId,
        (d) =>
          d.startDate === d.endDate
            ? `${d.name} (${formatDate(d.startDate)})`
            : `${d.name} (${formatDate(d.startDate)} - ${formatDate(d.endDate)})`,
        "Ngày lễ",
      ),
    [specialDayIds, specialDays.data],
  );
  const roomOptions = useMemo(
    () => buildOptions(roomIds, rooms.data?.items ?? [], (r) => r.roomId, (r) => r.roomName, "Phòng"),
    [roomIds, rooms.data],
  );
  const screenTypeOptions = useMemo(
    () =>
      buildOptions(
        screenTypeIds,
        screenTypes.data?.items ?? [],
        (s) => s.screenTypeId,
        (s) => s.name,
        "Định dạng",
      ),
    [screenTypeIds, screenTypes.data],
  );

  const toggleDay = (day: number) => {
    const next = selectedDays.includes(day)
      ? selectedDays.filter((d) => d !== day)
      : [...selectedDays, day];
    form.setValue("daysOfWeek", next, { shouldDirty: true, shouldValidate: form.formState.isSubmitted });
  };

  const onSubmit = (values: PricingRuleFormValues) => {
    const isOccupancy = values.ruleType === "Occupancy";

    const body: UpsertPricingRuleRequest = {
      name: values.name,
      ruleType: values.ruleType,
      adjustmentMode: values.adjustmentMode,
      adjustmentValue: isOccupancy ? null : Number(values.adjustmentValue),
      priority: values.priority,
      validFrom: values.validFrom || null,
      validTo: values.validTo || null,
      roomIds: values.roomIds,
      screenTypeIds: values.screenTypeIds,
      movieIds: values.movieIds,
      isActive: values.isActive,
      condition: {
        startTime: values.ruleType === "TimeOfDay" ? values.startTime : null,
        endTime: values.ruleType === "TimeOfDay" ? values.endTime : null,
        daysOfWeek: values.ruleType === "WeekDay" ? values.daysOfWeek : null,
        specialDayIds: values.ruleType === "SpecialDay" ? values.specialDayIds : null,
        tiers: isOccupancy
          ? values.tiers.map((tier) => ({
              fromPercent: Number(tier.fromPercent),
              toPercent: toNumberOrNull(tier.toPercent),
              maxHoursBeforeStart: toNumberOrNull(tier.maxHoursBeforeStart),
              value: Number(tier.value),
            }))
          : null,
      },
    };
    const options = { onSuccess: () => onSuccess(), onError: applyServerErrors };

    if (rule) {
      updateRule.mutate({ id: rule.pricingRuleId, body }, options);
    } else {
      createRule.mutate(body, options);
    }
  };

  return (
    <form
      id="pricing-rule-form"
      onSubmit={form.handleSubmit(onSubmit)}
      className="max-h-[70vh] space-y-4 overflow-y-auto px-0.5"
    >
      <FieldGroup disabled={isPending}>
        <FormInputField
          name="name"
          control={form.control}
          label="Tên quy tắc"
          placeholder="Giờ vàng buổi tối, Cuối tuần..."
          autoComplete="off"
          autoFocus
        />

        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <FormSelectField
            name="ruleType"
            control={form.control}
            label="Loại quy tắc"
            options={[...RULE_TYPE_OPTIONS]}
            getOptionValue={(o) => o.value}
            getOptionLabel={(o) => o.label}
          />
          <FormSelectField
            name="adjustmentMode"
            control={form.control}
            label="Cách điều chỉnh"
            options={[...ADJUSTMENT_MODE_OPTIONS]}
            getOptionValue={(o) => o.value}
            getOptionLabel={(o) => o.label}
          />
        </div>

        {ruleType === "TimeOfDay" && (
          <div className="grid grid-cols-2 gap-3">
            <FormTimePickerField name="startTime" control={form.control} label="Từ giờ" />
            <FormTimePickerField name="endTime" control={form.control} label="Đến giờ" />
          </div>
        )}

        {ruleType === "WeekDay" && (
          <Field data-invalid={Boolean(form.formState.errors.daysOfWeek)}>
            <FieldLabel>Các thứ áp dụng</FieldLabel>
            <div className="flex flex-wrap gap-1.5">
              {DAY_OF_WEEK_OPTIONS.map((day) => (
                <Button
                  key={day.value}
                  type="button"
                  size="sm"
                  variant={selectedDays.includes(day.value) ? "default" : "outline"}
                  onClick={() => toggleDay(day.value)}
                >
                  {day.label}
                </Button>
              ))}
            </div>
            {form.formState.errors.daysOfWeek && (
              <FieldError errors={[{ message: form.formState.errors.daysOfWeek.message }]} />
            )}
          </Field>
        )}

        {ruleType === "SpecialDay" && (
          <FormMultiSelectField
            name="specialDayIds"
            control={form.control}
            label="Ngày lễ áp dụng (để trống là mọi ngày lễ đang hoạt động)"
            options={specialDayOptions}
            selectedOptions={specialDayOptions}
            getOptionValue={(o) => o.id}
            getOptionLabel={(o) => o.label}
            searchValue={specialDaySearch}
            onSearchChange={setSpecialDaySearch}
            isLoading={specialDays.isLoading}
            placeholder="Mọi ngày lễ"
          />
        )}

        {ruleType === "Occupancy" ? (
          <OccupancyTiersField
            control={form.control}
            errors={form.formState.errors}
            mode={mode}
          />
        ) : (
          <FormInputField
            name="adjustmentValue"
            control={form.control}
            label={
              mode === "Multiplier"
                ? `Hệ số (${RULE_MIN_MULTIPLIER} - ${RULE_MAX_MULTIPLIER}, ví dụ 1.2 là tăng 20%)`
                : `Số tiền cộng/trừ (tối đa ${RULE_MAX_AMOUNT.toLocaleString("vi-VN")}, số âm là giảm)`
            }
            type="number"
            step={mode === "Multiplier" ? 0.05 : 1000}
          />
        )}

        <FormInputField
          name="priority"
          control={form.control}
          label={`Độ ưu tiên (0 - ${RULE_MAX_PRIORITY}, số lớn được xét trước)`}
          type="number"
          min={0}
          max={RULE_MAX_PRIORITY}
          step={1}
        />

        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <FormDatePickerField
            name="validFrom"
            control={form.control}
            label="Hiệu lực từ ngày"
            placeholder="Không giới hạn"
            clearable
          />
          <FormDatePickerField
            name="validTo"
            control={form.control}
            label="Hiệu lực đến ngày"
            placeholder="Không giới hạn"
            clearable
          />
        </div>

        <Field>
          <FieldLabel>Phạm vi áp dụng</FieldLabel>
          <FieldDescription>Để trống nghĩa là áp dụng cho tất cả.</FieldDescription>
        </Field>

        <FormMultiSelectField
          name="roomIds"
          control={form.control}
          label="Phòng chiếu"
          options={roomOptions}
          selectedOptions={roomOptions}
          getOptionValue={(o) => o.id}
          getOptionLabel={(o) => o.label}
          searchValue={roomSearch}
          onSearchChange={setRoomSearch}
          isLoading={rooms.isLoading}
          placeholder="Tất cả phòng"
        />

        <FormMultiSelectField
          name="screenTypeIds"
          control={form.control}
          label="Định dạng phòng"
          options={screenTypeOptions}
          selectedOptions={screenTypeOptions}
          getOptionValue={(o) => o.id}
          getOptionLabel={(o) => o.label}
          searchValue={screenTypeSearch}
          onSearchChange={setScreenTypeSearch}
          isLoading={screenTypes.isLoading}
          placeholder="Tất cả định dạng"
        />

        <FormMultiSelectField
          name="movieIds"
          control={form.control}
          label="Phim"
          options={movieOptions}
          selectedOptions={movieOptions}
          getOptionValue={(o) => o.id}
          getOptionLabel={(o) => o.label}
          searchValue={movieSearch}
          onSearchChange={setMovieSearch}
          isLoading={movies.isLoading}
          placeholder="Tất cả phim"
        />

        <FormSwitchField name="isActive" control={form.control} label="Đang hoạt động" />
      </FieldGroup>

      <FormActions
        isPending={isPending}
        isDirty={form.formState.isDirty}
        submitLabel={isEdit ? FORM_SAVE_CHANGES_LABEL : "Thêm quy tắc"}
        onCancel={onSuccess}
      />
    </form>
  );
};
