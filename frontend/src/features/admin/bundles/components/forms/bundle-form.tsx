import { useEffect, useMemo, useState } from "react";
import { useFieldArray, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Plus, Trash2 } from "lucide-react";

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
import { FormTextareaField } from "@/components/common/form/form-textarea-field";
import { FormTimePickerField } from "@/components/common/form/form-time-picker-field";
import { FormDateTimePickerField } from "@/components/common/form/form-datetime-picker-field";
import { FormMultiSelectField } from "@/components/common/form/form-search-select-field";
import { FormActions } from "@/components/common/form/form-actions";
import { FormErrorAlert } from "@/components/common/form/form-error-alert";
import { useServerFieldErrors } from "@/hooks/use-server-field-errors";
import { useDebouncedValue } from "@/hooks/use-debounced-value";
import { toBusinessIso } from "@/lib/business-time";
import { formatDateTime, toDateTimeInputValue } from "@/lib/formatters";
import {
  FORM_SAVE_CHANGES_LABEL,
  LOOKUP_PAGE_SIZE,
  PRICE_INPUT_STEP,
  SEARCH_LOOKUP_PAGE_SIZE,
} from "@/config/default-constants";
import { ShowtimeStatus } from "@/types/enum";
import { useAdminMovies } from "@/features/admin/movies/hooks/use-admin-movies";
import { useAdminShowtimes } from "@/features/admin/showtimes/hooks/use-admin-showtimes";
import { useAdminSeatTypes } from "@/features/admin/catalog/hooks/use-admin-seat-type";
import { useAdminCombos } from "@/features/admin/combos/hooks/use-admin-combo";
import { useAdminTicketTypes } from "@/features/admin/pricing/hooks/use-admin-ticket-types";
import { DAY_OF_WEEK_OPTIONS } from "@/features/admin/pricing/utils/pricing-labels";
import { buildOptions } from "@/features/admin/pricing/components/forms/pricing-rule-form";

import { useAdminCreateBundle, useAdminUpdateBundle } from "../../hooks/use-admin-bundles";
import type { AdminBundleDto, UpsertBundleRequest } from "../../types";
import {
  BUNDLE_ITEM_KIND_OPTIONS,
  BUNDLE_MAX_ITEMS,
  BUNDLE_MAX_ITEM_QUANTITY,
} from "../../utils/bundle-admin-labels";
import {
  bundleFormSchema,
  type BundleFormValues,
  type BundleItemFormValue,
} from "./bundle-schema";

interface BundleFormProps {
  bundle?: AdminBundleDto;
  onSuccess: () => void;
}

interface RefOption {
  id: number;
  label: string;
}

const toText = (value?: number | null) => (value === null || value === undefined ? "" : String(value));

const toNumberOrNull = (value: string) => (value.trim() === "" ? null : Number(value));

const EMPTY_ITEM: BundleItemFormValue = { kind: "ticket", refId: null, quantity: "1" };

const withSelected = (options: RefOption[], selected: RefOption[]) => {
  const known = new Set(options.map((option) => option.id));
  return [...options, ...selected.filter((option) => !known.has(option.id))];
};

export const BundleForm = ({ bundle, onSuccess }: BundleFormProps) => {
  const isEdit = Boolean(bundle);
  const createBundle = useAdminCreateBundle();
  const updateBundle = useAdminUpdateBundle();
  const mutation = isEdit ? updateBundle : createBundle;
  const isPending = mutation.isPending;

  const [movieSearch, setMovieSearch] = useState("");
  const [showtimeSearch, setShowtimeSearch] = useState("");
  const [seatTypeSearch, setSeatTypeSearch] = useState("");

  const movies = useAdminMovies({
    search: useDebouncedValue(movieSearch),
    pageSize: SEARCH_LOOKUP_PAGE_SIZE,
  });
  const showtimes = useAdminShowtimes({
    status: ShowtimeStatus.Scheduled,
    pageSize: LOOKUP_PAGE_SIZE,
  });
  const seatTypes = useAdminSeatTypes({ pageSize: LOOKUP_PAGE_SIZE });
  const ticketTypes = useAdminTicketTypes({ pageSize: LOOKUP_PAGE_SIZE });
  const combos = useAdminCombos({ page: 1, pageSize: LOOKUP_PAGE_SIZE });

  const form = useForm<BundleFormValues>({
    resolver: zodResolver(bundleFormSchema),
    defaultValues: {
      name: bundle?.name ?? "",
      description: bundle?.description ?? "",
      price: toText(bundle?.price),
      items: bundle
        ? bundle.items.map((item) => ({
            kind: item.ticketTypeId ? "ticket" : "combo",
            refId: String(item.ticketTypeId ?? item.comboId),
            quantity: String(item.quantity),
          }))
        : [{ ...EMPTY_ITEM }],
      validFrom: toDateTimeInputValue(bundle?.validFrom) ?? "",
      validTo: toDateTimeInputValue(bundle?.validTo) ?? "",
      movieIds: bundle?.movieIds ?? [],
      showtimeIds: bundle?.showtimeIds ?? [],
      seatTypeIds: bundle?.seatTypeIds ?? [],
      daysOfWeek: bundle?.daysOfWeek ?? [],
      allowedFromTime: bundle?.allowedFromTime ?? "",
      allowedToTime: bundle?.allowedToTime ?? "",
      dailyLimit: toText(bundle?.dailyLimit),
      excludesPromotions: bundle?.excludesPromotions ?? false,
      isActive: bundle?.isActive ?? true,
    },
  });

  const { fields, append, remove } = useFieldArray({ control: form.control, name: "items" });
  const applyServerErrors = useServerFieldErrors(form);

  useEffect(() => {
    const subscription = form.watch((_, { name, type }) => {
      const match = name?.match(/^items\.(\d+)\.kind$/);
      if (!match || type !== "change") return;
      const index = Number(match[1]);
      form.setValue(`items.${index}.refId`, null, { shouldDirty: true });
      form.clearErrors(`items.${index}.refId`);
    });
    return () => subscription.unsubscribe();
  }, [form]);

  const watchedItems = form.watch("items");
  const selectedDays = form.watch("daysOfWeek");
  const movieIds = form.watch("movieIds");
  const showtimeIds = form.watch("showtimeIds");
  const seatTypeIds = form.watch("seatTypeIds");

  const ticketTypeOptions = useMemo(() => {
    const current = (bundle?.items ?? [])
      .filter((item) => item.ticketTypeId !== null)
      .map((item) => ({
        id: item.ticketTypeId as number,
        label: item.ticketTypeName ?? `Loại vé #${item.ticketTypeId}`,
      }));
    const active = (ticketTypes.data?.items ?? [])
      .filter((type) => type.isActive)
      .map((type) => ({ id: type.ticketTypeId, label: type.name }));
    return withSelected(active, current);
  }, [ticketTypes.data, bundle]);

  const comboOptions = useMemo(() => {
    const current = (bundle?.items ?? [])
      .filter((item) => item.comboId !== null)
      .map((item) => ({
        id: item.comboId as number,
        label: item.comboName ?? `Combo #${item.comboId}`,
      }));
    const active = (combos.data?.items ?? [])
      .filter((combo) => combo.isActive)
      .map((combo) => ({ id: combo.comboId, label: combo.name }));
    return withSelected(active, current);
  }, [combos.data, bundle]);

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
  const seatTypeOptions = useMemo(
    () =>
      buildOptions(
        seatTypeIds,
        seatTypes.data?.items ?? [],
        (s) => s.seatTypeId,
        (s) => s.name,
        "Loại ghế",
      ),
    [seatTypeIds, seatTypes.data],
  );

  const toggleDay = (day: number) => {
    const next = selectedDays.includes(day)
      ? selectedDays.filter((d) => d !== day)
      : [...selectedDays, day];
    form.setValue("daysOfWeek", next, { shouldDirty: true });
  };

  const onSubmit = (values: BundleFormValues) => {
    const body: UpsertBundleRequest = {
      name: values.name,
      description: values.description === "" ? null : values.description,
      price: Number(values.price),
      items: values.items.map((item) => ({
        ticketTypeId: item.kind === "ticket" ? Number(item.refId) : null,
        comboId: item.kind === "combo" ? Number(item.refId) : null,
        quantity: Number(item.quantity),
      })),
      validFrom: values.validFrom ? toBusinessIso(values.validFrom) : null,
      validTo: values.validTo ? toBusinessIso(values.validTo) : null,
      movieIds: values.movieIds,
      showtimeIds: values.showtimeIds,
      daysOfWeek: values.daysOfWeek,
      seatTypeIds: values.seatTypeIds,
      allowedFromTime: values.allowedFromTime || null,
      allowedToTime: values.allowedToTime || null,
      dailyLimit: toNumberOrNull(values.dailyLimit),
      excludesPromotions: values.excludesPromotions,
      isActive: values.isActive,
    };
    const options = { onSuccess: () => onSuccess(), onError: applyServerErrors };

    if (bundle) {
      updateBundle.mutate({ id: bundle.bundleId, body }, options);
    } else {
      createBundle.mutate(body, options);
    }
  };

  const itemsError = form.formState.errors.items;
  const itemsMessage = itemsError?.root?.message ?? itemsError?.message;

  return (
    <form
      id="bundle-form"
      onSubmit={form.handleSubmit(onSubmit)}
      className="max-h-[70vh] space-y-4 overflow-y-auto px-0.5"
    >
      <FieldGroup disabled={isPending}>
        <FormInputField
          name="name"
          control={form.control}
          label="Tên gói"
          placeholder="Gói đôi, Gói gia đình..."
          autoComplete="off"
          autoFocus={!isEdit}
        />
        <FormTextareaField
          name="description"
          control={form.control}
          label="Mô tả hiển thị cho khách (không bắt buộc)"
          placeholder="2 vé + 1 combo đôi, tiết kiệm hơn mua lẻ"
        />

        <Field data-invalid={Boolean(itemsMessage)}>
          <FieldLabel>Thành phần của gói</FieldLabel>
          <FieldDescription>
            Chọn số vé theo từng loại vé và các combo đi kèm. Gói cần có ít nhất một loại vé.
          </FieldDescription>
          <div className="space-y-2">
            {fields.map((field, index) => {
              const kind = watchedItems[index]?.kind ?? "ticket";
              return (
                <div
                  key={field.id}
                  className="grid grid-cols-[1fr_auto] gap-2 border border-border p-2 sm:grid-cols-[7rem_1fr_6rem_auto]"
                >
                  <div className="col-span-2 sm:col-span-1">
                    <FormSelectField
                      name={`items.${index}.kind`}
                      control={form.control}
                      label="Loại"
                      options={BUNDLE_ITEM_KIND_OPTIONS}
                      getOptionValue={(o) => o.value}
                      getOptionLabel={(o) => o.label}
                      className="w-full"
                    />
                  </div>
                  <div className="col-span-2 sm:col-span-1">
                    <FormSelectField
                      key={kind}
                      name={`items.${index}.refId`}
                      control={form.control}
                      label={kind === "ticket" ? "Loại vé" : "Combo"}
                      placeholder={kind === "ticket" ? "Chọn loại vé" : "Chọn combo"}
                      options={kind === "ticket" ? ticketTypeOptions : comboOptions}
                      getOptionValue={(o) => o.id}
                      getOptionLabel={(o) => o.label}
                      className="w-full"
                    />
                  </div>
                  <FormInputField
                    name={`items.${index}.quantity`}
                    control={form.control}
                    label="Số lượng"
                    type="number"
                    min={1}
                    max={BUNDLE_MAX_ITEM_QUANTITY}
                    step={1}
                  />
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    className="size-9 self-end"
                    disabled={fields.length === 1}
                    onClick={() => remove(index)}
                  >
                    <span className="sr-only">Xóa thành phần</span>
                    <Trash2 className="size-4" />
                  </Button>
                </div>
              );
            })}
            {itemsMessage && <FieldError errors={[{ message: itemsMessage }]} />}
            <div className="flex flex-wrap gap-2">
              <Button
                type="button"
                variant="outline"
                size="sm"
                disabled={fields.length >= BUNDLE_MAX_ITEMS}
                onClick={() => append({ ...EMPTY_ITEM })}
              >
                <Plus className="size-4" />
                <span>Thêm thành phần</span>
              </Button>
            </div>
          </div>
        </Field>

        <FormInputField
          name="price"
          control={form.control}
          label="Giá gói (đồng)"
          type="number"
          min={1}
          step={PRICE_INPUT_STEP}
          placeholder="150000"
        />
        <p className="-mt-2 text-xs text-muted-foreground">
          Giá gói phải thấp hơn tổng giá lẻ của các thành phần; hệ thống kiểm tra khi lưu.
        </p>

        <Field>
          <FieldLabel>Điều kiện áp dụng</FieldLabel>
          <FieldDescription>Để trống nghĩa là áp dụng cho tất cả.</FieldDescription>
        </Field>

        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <FormDateTimePickerField
            name="validFrom"
            control={form.control}
            label="Hiệu lực từ"
            placeholder="Không giới hạn"
            clearable
          />
          <FormDateTimePickerField
            name="validTo"
            control={form.control}
            label="Hiệu lực đến"
            placeholder="Không giới hạn"
            clearable
          />
        </div>

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
          name="seatTypeIds"
          control={form.control}
          label="Loại ghế được phép"
          options={seatTypeOptions}
          selectedOptions={seatTypeOptions}
          getOptionValue={(o) => o.id}
          getOptionLabel={(o) => o.label}
          searchValue={seatTypeSearch}
          onSearchChange={setSeatTypeSearch}
          isLoading={seatTypes.isLoading}
          placeholder="Mọi loại ghế"
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
          <FieldDescription>Ngày tính theo ngày chiếu của suất.</FieldDescription>
        </Field>

        <div className="grid grid-cols-2 gap-3">
          <FormTimePickerField name="allowedFromTime" control={form.control} label="Từ giờ" />
          <FormTimePickerField name="allowedToTime" control={form.control} label="Đến giờ" />
        </div>
        <p className="-mt-2 text-xs text-muted-foreground">
          Khung giờ tính theo giờ chiếu của suất.
        </p>

        <FormInputField
          name="dailyLimit"
          control={form.control}
          label="Hạn mức số gói mỗi ngày (không bắt buộc)"
          type="number"
          min={1}
          step={1}
          placeholder="Không giới hạn"
        />

        <FormSwitchField
          name="excludesPromotions"
          control={form.control}
          label="Không dùng chung với mã khuyến mãi"
        />
        <FormSwitchField name="isActive" control={form.control} label="Đang bán" />
      </FieldGroup>

      <FormErrorAlert error={mutation.error} preferServerMessage />

      <FormActions
        isPending={isPending}
        isDirty={form.formState.isDirty}
        submitLabel={isEdit ? FORM_SAVE_CHANGES_LABEL : "Tạo gói"}
        onCancel={onSuccess}
      />
    </form>
  );
};
