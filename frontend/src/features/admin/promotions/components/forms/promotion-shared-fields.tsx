import { useMemo, useState } from "react";
import type { UseFormReturn } from "react-hook-form";

import { Button } from "@/components/ui/button";
import { Field, FieldDescription, FieldError, FieldLabel } from "@/components/ui/field";
import { FormInputField } from "@/components/common/form/form-input-field";
import { FormSelectField } from "@/components/common/form/form-select-field";
import { FormSwitchField } from "@/components/common/form/form-switch-field";
import { FormTextareaField } from "@/components/common/form/form-textarea-field";
import { FormDateTimePickerField } from "@/components/common/form/form-datetime-picker-field";
import { FormMultiSelectField } from "@/components/common/form/form-search-select-field";
import { useDebouncedValue } from "@/hooks/use-debounced-value";
import { formatDateTime } from "@/lib/formatters";
import { LOOKUP_PAGE_SIZE, SEARCH_LOOKUP_PAGE_SIZE } from "@/config/default-constants";
import { PromotionDiscountType, ShowtimeStatus } from "@/types/enum";
import { useAdminMovies } from "@/features/admin/movies/hooks/use-admin-movies";
import { useAdminShowtimes } from "@/features/admin/showtimes/hooks/use-admin-showtimes";
import { useAdminScreenTypes } from "@/features/admin/catalog/hooks/use-admin-screen-type";
import { useAdminMembershipTiers } from "@/features/admin/catalog/hooks/use-admin-membership-tier";
import { DAY_OF_WEEK_OPTIONS } from "@/features/admin/pricing/utils/pricing-labels";
import { buildOptions } from "@/features/admin/pricing/components/forms/pricing-rule-form";

import {
  CHANNEL_OPTIONS,
  DISCOUNT_TYPE_OPTIONS,
  PROMOTION_MAX_PRIORITY,
  SCOPE_OPTIONS,
} from "../../utils/promotion-admin-labels";
import type { PromotionSharedValues } from "./promotion-schema";

interface PromotionSharedFieldsProps<T extends PromotionSharedValues> {
  form: UseFormReturn<T>;
}

export const PromotionSharedFields = <T extends PromotionSharedValues>({
  form: typedForm,
}: PromotionSharedFieldsProps<T>) => {
  const form = typedForm as unknown as UseFormReturn<PromotionSharedValues>;
  const [movieSearch, setMovieSearch] = useState("");
  const [showtimeSearch, setShowtimeSearch] = useState("");
  const [screenTypeSearch, setScreenTypeSearch] = useState("");
  const [tierSearch, setTierSearch] = useState("");

  const movies = useAdminMovies({
    search: useDebouncedValue(movieSearch),
    pageSize: SEARCH_LOOKUP_PAGE_SIZE,
  });
  const showtimes = useAdminShowtimes({
    status: ShowtimeStatus.Scheduled,
    pageSize: LOOKUP_PAGE_SIZE,
  });
  const screenTypes = useAdminScreenTypes({ pageSize: LOOKUP_PAGE_SIZE });
  const tiers = useAdminMembershipTiers({ pageSize: LOOKUP_PAGE_SIZE });

  const discountType = form.watch("discountType");
  const selectedDays = form.watch("daysOfWeek");
  const movieIds = form.watch("movieIds");
  const showtimeIds = form.watch("showtimeIds");
  const screenTypeIds = form.watch("screenTypeIds");
  const tierIds = form.watch("membershipTierIds");

  const isPercent = discountType === PromotionDiscountType.Percent;

  const movieOptions = useMemo(
    () => buildOptions(movieIds, movies.data?.items ?? [], (m) => m.movieId, (m) => m.title, "Phim"),
    [movieIds, movies.data],
  );
  const showtimeOptions = useMemo(
    () =>
      buildOptions(
        showtimeIds,
        showtimes.data?.items ?? [],
        (s) => s.showtimeId,
        (s) => `${s.movieTitle} · ${s.roomName} · ${formatDateTime(s.startTime)}`,
        "Suất chiếu",
      ),
    [showtimeIds, showtimes.data],
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
  const tierOptions = useMemo(
    () =>
      buildOptions(
        tierIds,
        tiers.data?.items ?? [],
        (t) => t.tierId,
        (t) => t.tierName,
        "Hạng",
      ),
    [tierIds, tiers.data],
  );

  const toggleDay = (day: number) => {
    const next = selectedDays.includes(day)
      ? selectedDays.filter((d) => d !== day)
      : [...selectedDays, day];
    form.setValue("daysOfWeek", next, { shouldDirty: true });
  };

  return (
    <>
      <FormInputField
        name="name"
        control={form.control}
        label="Tên khuyến mãi"
        placeholder="Ưu đãi cuối tuần, Mừng khai trương..."
        autoComplete="off"
      />
      <FormTextareaField
        name="description"
        control={form.control}
        label="Mô tả hiển thị cho khách"
        placeholder="Nội dung ngắn gọn mô tả ưu đãi (không bắt buộc)"
      />

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
        <FormSelectField
          name="discountType"
          control={form.control}
          label="Loại ưu đãi"
          options={[...DISCOUNT_TYPE_OPTIONS]}
          getOptionValue={(o) => o.value}
          getOptionLabel={(o) => o.label}
        />
        <FormSelectField
          name="scope"
          control={form.control}
          label="Phạm vi giảm"
          options={[...SCOPE_OPTIONS]}
          getOptionValue={(o) => o.value}
          getOptionLabel={(o) => o.label}
        />
      </div>

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
        <FormInputField
          name="discountValue"
          control={form.control}
          label={isPercent ? "Phần trăm giảm (%)" : "Số tiền giảm (đồng)"}
          type="number"
          min={0}
          step={isPercent ? 0.01 : 1000}
        />
        {isPercent && (
          <FormInputField
            name="maxDiscountAmount"
            control={form.control}
            label="Giảm tối đa (đồng, không bắt buộc)"
            type="number"
            min={0}
            step={1000}
            placeholder="Không giới hạn"
          />
        )}
      </div>

      <FormInputField
        name="minOrderAmount"
        control={form.control}
        label="Giá trị đơn tối thiểu (đồng, không bắt buộc)"
        type="number"
        min={0}
        step={1000}
        placeholder="Không yêu cầu"
      />

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
        <FormDateTimePickerField
          name="startsAt"
          control={form.control}
          label="Bắt đầu từ"
          placeholder="Không giới hạn"
          clearable
        />
        <FormDateTimePickerField
          name="endsAt"
          control={form.control}
          label="Kết thúc lúc"
          placeholder="Không giới hạn"
          clearable
        />
      </div>

      <FormSelectField
        name="channel"
        control={form.control}
        label="Kênh bán"
        options={[...CHANNEL_OPTIONS]}
        getOptionValue={(o) => o.value}
        getOptionLabel={(o) => o.label}
      />

      <Field>
        <FieldLabel>Điều kiện áp dụng</FieldLabel>
        <FieldDescription>Để trống nghĩa là áp dụng cho tất cả.</FieldDescription>
      </Field>

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
      <FormMultiSelectField
        name="showtimeIds"
        control={form.control}
        label="Suất chiếu"
        options={showtimeOptions}
        selectedOptions={showtimeOptions}
        getOptionValue={(o) => o.id}
        getOptionLabel={(o) => o.label}
        searchValue={showtimeSearch}
        onSearchChange={setShowtimeSearch}
        isLoading={showtimes.isLoading}
        placeholder="Tất cả suất chiếu"
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
        name="membershipTierIds"
        control={form.control}
        label="Hạng thành viên"
        options={tierOptions}
        selectedOptions={tierOptions}
        getOptionValue={(o) => o.id}
        getOptionLabel={(o) => o.label}
        searchValue={tierSearch}
        onSearchChange={setTierSearch}
        isLoading={tiers.isLoading}
        placeholder="Mọi hạng thành viên"
      />

      <Field data-invalid={Boolean(form.formState.errors.daysOfWeek)}>
        <FieldLabel>Thứ trong tuần</FieldLabel>
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

      <FormSwitchField name="newCustomersOnly" control={form.control} label="Chỉ áp dụng cho khách mới" />

      <FormInputField
        name="budgetAmount"
        control={form.control}
        label="Ngân sách chiến dịch (đồng, không bắt buộc)"
        type="number"
        min={0}
        step={1000}
        placeholder="Không giới hạn"
      />

      <FormInputField
        name="priority"
        control={form.control}
        label={`Độ ưu tiên (0 - ${PROMOTION_MAX_PRIORITY}, số lớn được xét trước)`}
        type="number"
        min={0}
        max={PROMOTION_MAX_PRIORITY}
        step={1}
      />

      <FormSwitchField
        name="isExclusive"
        control={form.control}
        label="Không dùng chung với ưu đãi khác"
      />

      <FormSwitchField
        name="excludeEventShowtimes"
        control={form.control}
        label="Không áp dụng cho suất chiếu sự kiện"
      />
    </>
  );
};
