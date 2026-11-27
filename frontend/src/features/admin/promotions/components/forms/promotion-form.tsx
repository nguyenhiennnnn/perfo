import { useMemo, useState } from "react";
import { Controller, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";

import { Field, FieldDescription, FieldError, FieldGroup, FieldLabel } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { FormInputField } from "@/components/common/form/form-input-field";
import { FormSelectField } from "@/components/common/form/form-select-field";
import { FormSwitchField } from "@/components/common/form/form-switch-field";
import { FormSearchSelectField } from "@/components/common/form/form-search-select-field";
import { FormActions } from "@/components/common/form/form-actions";
import { FormErrorAlert } from "@/components/common/form/form-error-alert";
import { useServerFieldErrors } from "@/hooks/use-server-field-errors";
import { useDebouncedValue } from "@/hooks/use-debounced-value";
import { toBusinessIso } from "@/lib/business-time";
import { toDateTimeInputValue } from "@/lib/formatters";
import { FORM_SAVE_CHANGES_LABEL, SEARCH_LOOKUP_PAGE_SIZE } from "@/config/default-constants";
import { OrderChannel, PromotionDiscountType, PromotionScope, PromotionType } from "@/types/enum";
import { useAdminUsers } from "@/features/admin/users/hooks/use-admin-users";

import {
  useAdminCreatePromotion,
  useAdminUpdatePromotion,
} from "../../hooks/use-admin-promotions";
import { useAdminPromotionReport } from "../../hooks/use-admin-promotion-reports";
import type { PromotionDto, UpsertPromotionRequest } from "../../types";
import {
  ALL_CHANNELS_VALUE,
  PROMOTION_TYPE_OPTIONS,
} from "../../utils/promotion-admin-labels";
import { PromotionSharedFields } from "./promotion-shared-fields";
import { promotionFormSchema, type PromotionFormValues } from "./promotion-schema";

interface PromotionFormProps {
  promotion?: PromotionDto;
  onSuccess: () => void;
}

const CUSTOMER_ROLE_ID = 1;

const toText = (value?: number | null) => (value === null || value === undefined ? "" : String(value));

const toNumberOrNull = (value: string) => (value.trim() === "" ? null : Number(value));

const CODE_HINTS: Record<string, string> = {
  [PromotionType.ManualCode]: "Khách nhập mã này ở bước thanh toán.",
  [PromotionType.AutoApply]: "Mã định danh chiến dịch, khách không cần nhập.",
  [PromotionType.PersonalCode]: "Mã khách dùng một lần ở bước thanh toán.",
};

export const PromotionForm = ({ promotion, onSuccess }: PromotionFormProps) => {
  const isEdit = Boolean(promotion);
  const createPromotion = useAdminCreatePromotion();
  const updatePromotion = useAdminUpdatePromotion();
  const mutation = isEdit ? updatePromotion : createPromotion;
  const isPending = mutation.isPending;

  const [userSearch, setUserSearch] = useState("");
  const users = useAdminUsers({
    roleId: CUSTOMER_ROLE_ID,
    search: useDebouncedValue(userSearch),
    pageSize: SEARCH_LOOKUP_PAGE_SIZE,
  });

  const report = useAdminPromotionReport(promotion?.promotionId ?? 0, undefined, isEdit);
  const hasUsage = report.data
    ? report.data.usage.redeemedCount +
        report.data.usage.activeHoldCount +
        report.data.usage.releasedCount >
        0 || report.data.limits.consumedCount > 0
    : false;

  const form = useForm<PromotionFormValues>({
    resolver: zodResolver(promotionFormSchema),
    defaultValues: {
      code: promotion?.code ?? "",
      name: promotion?.name ?? "",
      description: promotion?.description ?? "",
      type: promotion?.type ?? PromotionType.ManualCode,
      discountType: promotion?.discountType ?? PromotionDiscountType.Percent,
      discountValue: toText(promotion?.discountValue),
      maxDiscountAmount: toText(promotion?.maxDiscountAmount),
      scope: promotion?.scope ?? PromotionScope.WholeOrder,
      minOrderAmount: toText(promotion?.minOrderAmount),
      startsAt: toDateTimeInputValue(promotion?.startsAt) ?? "",
      endsAt: toDateTimeInputValue(promotion?.endsAt) ?? "",
      channel: promotion?.channel ?? ALL_CHANNELS_VALUE,
      movieIds: promotion?.movieIds ?? [],
      showtimeIds: promotion?.showtimeIds ?? [],
      screenTypeIds: promotion?.screenTypeIds ?? [],
      daysOfWeek: promotion?.daysOfWeek ?? [],
      membershipTierIds: promotion?.membershipTierIds ?? [],
      newCustomersOnly: promotion?.newCustomersOnly ?? false,
      totalUsageLimit: toText(promotion?.totalUsageLimit),
      perCustomerLimit: toText(promotion?.perCustomerLimit),
      budgetAmount: toText(promotion?.budgetAmount),
      priority: String(promotion?.priority ?? 0),
      isExclusive: promotion?.isExclusive ?? false,
      excludeEventShowtimes: promotion?.excludeEventShowtimes ?? false,
      assignedUserId: promotion?.assignedUserId ?? null,
      isActive: promotion?.isActive ?? true,
    },
  });

  const applyServerErrors = useServerFieldErrors(form);
  const type = form.watch("type");
  const isPersonal = type === PromotionType.PersonalCode;
  const assignedUserId = form.watch("assignedUserId");

  const userOptions = useMemo(() => {
    const items = (users.data?.items ?? []).map((user) => ({
      id: user.userId,
      label: `${user.fullName} · ${user.email}`,
    }));
    if (
      promotion?.assignedUserId &&
      !items.some((item) => item.id === promotion.assignedUserId)
    ) {
      items.push({
        id: promotion.assignedUserId,
        label: `${promotion.assignedUserName ?? `Khách #${promotion.assignedUserId}`} · ${promotion.assignedUserEmail ?? ""}`,
      });
    }
    return items;
  }, [users.data, promotion]);

  const onSubmit = (values: PromotionFormValues) => {
    const isPercent = values.discountType === PromotionDiscountType.Percent;
    const personal = values.type === PromotionType.PersonalCode;

    const body: UpsertPromotionRequest = {
      code: values.code.trim().toUpperCase(),
      name: values.name,
      description: values.description === "" ? null : values.description,
      type: values.type,
      discountType: values.discountType,
      discountValue: Number(values.discountValue),
      maxDiscountAmount: isPercent ? toNumberOrNull(values.maxDiscountAmount) : null,
      scope: values.scope,
      minOrderAmount: toNumberOrNull(values.minOrderAmount),
      startsAt: values.startsAt ? toBusinessIso(values.startsAt) : null,
      endsAt: values.endsAt ? toBusinessIso(values.endsAt) : null,
      channel: values.channel === ALL_CHANNELS_VALUE ? null : (values.channel as OrderChannel),
      movieIds: values.movieIds,
      showtimeIds: values.showtimeIds,
      screenTypeIds: values.screenTypeIds,
      daysOfWeek: values.daysOfWeek,
      membershipTierIds: values.membershipTierIds,
      newCustomersOnly: values.newCustomersOnly,
      totalUsageLimit: personal ? null : toNumberOrNull(values.totalUsageLimit),
      perCustomerLimit: personal ? null : toNumberOrNull(values.perCustomerLimit),
      budgetAmount: toNumberOrNull(values.budgetAmount),
      priority: Number(values.priority),
      isExclusive: values.isExclusive,
      excludeEventShowtimes: values.excludeEventShowtimes,
      assignedUserId: personal ? values.assignedUserId : null,
      isActive: values.isActive,
    };
    const options = { onSuccess: () => onSuccess(), onError: applyServerErrors };

    if (promotion) {
      updatePromotion.mutate({ id: promotion.promotionId, body }, options);
    } else {
      createPromotion.mutate(body, options);
    }
  };

  return (
    <form
      id="promotion-form"
      onSubmit={form.handleSubmit(onSubmit)}
      className="max-h-[70vh] space-y-4 overflow-y-auto px-0.5"
    >
      <FieldGroup disabled={isPending}>
        {hasUsage && (
          <p className="cut-sm bg-warning/10 px-3 py-2 text-sm text-foreground">
            Khuyến mãi này đã có lượt dùng hoặc đang được giữ chỗ nên không thể đổi mã, loại
            khuyến mãi và khách được cấp. Các thông tin còn lại vẫn chỉnh sửa được.
          </p>
        )}

        <Controller
          name="code"
          control={form.control}
          render={({ field, fieldState }) => (
            <Field data-invalid={fieldState.invalid}>
              <FieldLabel htmlFor="field-code">Mã khuyến mãi</FieldLabel>
              <Input
                {...field}
                id="field-code"
                value={field.value}
                onChange={(event) => field.onChange(event.target.value.toUpperCase())}
                placeholder="WEEKEND20"
                autoComplete="off"
                autoFocus={!isEdit}
                maxLength={30}
                disabled={hasUsage}
                aria-invalid={fieldState.invalid}
              />
              <FieldDescription>{CODE_HINTS[type]}</FieldDescription>
              {fieldState.invalid && <FieldError errors={[fieldState.error]} />}
            </Field>
          )}
        />

        <FormSelectField
          name="type"
          control={form.control}
          label="Loại khuyến mãi"
          options={[...PROMOTION_TYPE_OPTIONS]}
          getOptionValue={(o) => o.value}
          getOptionLabel={(o) => o.label}
          disabled={hasUsage}
        />
        <p className="-mt-2 text-xs text-muted-foreground">
          {PROMOTION_TYPE_OPTIONS.find((option) => option.value === type)?.hint}
        </p>

        {isPersonal && (
          <FormSearchSelectField
            name="assignedUserId"
            control={form.control}
            label="Khách hàng được cấp mã"
            options={userOptions}
            getOptionValue={(o) => o.id}
            getOptionLabel={(o) => o.label}
            selectedLabel={userOptions.find((o) => o.id === assignedUserId)?.label}
            searchValue={userSearch}
            onSearchChange={setUserSearch}
            isLoading={users.isLoading}
            placeholder="Chọn khách hàng"
            clearable={false}
            disabled={hasUsage}
          />
        )}

        <PromotionSharedFields form={form} />

        {!isPersonal && (
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <FormInputField
              name="totalUsageLimit"
              control={form.control}
              label="Tổng số lượt dùng"
              type="number"
              min={1}
              step={1}
              placeholder="Không giới hạn"
            />
            <FormInputField
              name="perCustomerLimit"
              control={form.control}
              label="Số lượt tối đa mỗi khách"
              type="number"
              min={1}
              step={1}
              placeholder="Không giới hạn"
            />
          </div>
        )}

        <FormSwitchField name="isActive" control={form.control} label="Đang hoạt động" />
      </FieldGroup>

      <FormErrorAlert error={mutation.error} preferServerMessage />

      <FormActions
        isPending={isPending}
        isDirty={form.formState.isDirty}
        submitLabel={isEdit ? FORM_SAVE_CHANGES_LABEL : "Tạo khuyến mãi"}
        onCancel={onSuccess}
      />
    </form>
  );
};
