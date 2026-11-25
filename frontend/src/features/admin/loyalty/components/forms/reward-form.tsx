import { useMemo } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";

import { FieldGroup } from "@/components/ui/field";
import { FormInputField } from "@/components/common/form/form-input-field";
import { FormSelectField } from "@/components/common/form/form-select-field";
import { FormSwitchField } from "@/components/common/form/form-switch-field";
import { FormDateTimePickerField } from "@/components/common/form/form-datetime-picker-field";
import { FormActions } from "@/components/common/form/form-actions";
import { FormErrorAlert } from "@/components/common/form/form-error-alert";
import { useServerFieldErrors } from "@/hooks/use-server-field-errors";
import { toBusinessIso } from "@/lib/business-time";
import { toDateTimeInputValue } from "@/lib/formatters";
import { FORM_SAVE_CHANGES_LABEL, LOOKUP_PAGE_SIZE } from "@/config/default-constants";
import { LoyaltyRewardType, PromotionDiscountType } from "@/types/enum";
import { useAdminCombos } from "@/features/admin/combos/hooks/use-admin-combo";

import {
  useAdminCreateLoyaltyReward,
  useAdminUpdateLoyaltyReward,
} from "../../hooks/use-admin-loyalty-rewards";
import type { AdminLoyaltyRewardDto, UpsertLoyaltyRewardRequest } from "../../types";
import { DISCOUNT_TYPE_OPTIONS, REWARD_TYPE_OPTIONS } from "../../utils/loyalty-admin-labels";
import { rewardFormSchema, type RewardFormValues } from "./reward-schema";

interface RewardFormProps {
  reward?: AdminLoyaltyRewardDto;
  onSuccess: () => void;
}

const DEFAULT_CODE_VALIDITY_DAYS = 30;

const toText = (value?: number | null) => (value === null || value === undefined ? "" : String(value));

const toNumberOrNull = (value: string) => (value.trim() === "" ? null : Number(value));

export const RewardForm = ({ reward, onSuccess }: RewardFormProps) => {
  const isEdit = Boolean(reward);
  const createReward = useAdminCreateLoyaltyReward();
  const updateReward = useAdminUpdateLoyaltyReward();
  const mutation = isEdit ? updateReward : createReward;
  const isPending = mutation.isPending;

  const combos = useAdminCombos({ page: 1, pageSize: LOOKUP_PAGE_SIZE });

  const comboOptions = useMemo(() => {
    const items = (combos.data?.items ?? []).map((combo) => ({
      id: combo.comboId,
      label: combo.name,
    }));
    if (reward?.comboId && !items.some((item) => item.id === reward.comboId)) {
      items.push({ id: reward.comboId, label: reward.comboName ?? `Combo #${reward.comboId}` });
    }
    return items;
  }, [combos.data, reward]);

  const form = useForm<RewardFormValues>({
    resolver: zodResolver(rewardFormSchema),
    defaultValues: {
      name: reward?.name ?? "",
      type: reward?.type ?? LoyaltyRewardType.Discount,
      pointsCost: toText(reward?.pointsCost),
      remainingQuantity: toText(reward?.remainingQuantity),
      codeValidityDays: String(reward?.codeValidityDays ?? DEFAULT_CODE_VALIDITY_DAYS),
      validFrom: toDateTimeInputValue(reward?.validFrom) ?? "",
      validTo: toDateTimeInputValue(reward?.validTo) ?? "",
      discountType: reward?.discountType ?? PromotionDiscountType.Percent,
      discountValue: toText(reward?.discountValue),
      maxDiscountAmount: toText(reward?.maxDiscountAmount),
      comboId: reward?.comboId ? String(reward.comboId) : null,
      isActive: reward?.isActive ?? true,
    },
  });

  const applyServerErrors = useServerFieldErrors(form);
  const type = form.watch("type");
  const discountType = form.watch("discountType");
  const isDiscount = type === LoyaltyRewardType.Discount;
  const isPercent = isDiscount && discountType === PromotionDiscountType.Percent;
  const showMaxAmount = type === LoyaltyRewardType.FreeTicket || isPercent;

  const onSubmit = (values: RewardFormValues) => {
    const discount = values.type === LoyaltyRewardType.Discount;
    const percent = discount && values.discountType === PromotionDiscountType.Percent;
    const maxAmountApplies = values.type === LoyaltyRewardType.FreeTicket || percent;

    const body: UpsertLoyaltyRewardRequest = {
      name: values.name,
      type: values.type,
      pointsCost: Number(values.pointsCost),
      remainingQuantity: Number(values.remainingQuantity),
      validFrom: values.validFrom ? toBusinessIso(values.validFrom) : null,
      validTo: values.validTo ? toBusinessIso(values.validTo) : null,
      codeValidityDays: Number(values.codeValidityDays),
      discountType: discount ? values.discountType : null,
      discountValue: discount ? Number(values.discountValue) : null,
      maxDiscountAmount: maxAmountApplies ? toNumberOrNull(values.maxDiscountAmount) : null,
      comboId: values.type === LoyaltyRewardType.FreeCombo ? Number(values.comboId) : null,
      isActive: values.isActive,
    };
    const options = { onSuccess: () => onSuccess(), onError: applyServerErrors };

    if (reward) {
      updateReward.mutate({ id: reward.rewardId, body }, options);
    } else {
      createReward.mutate(body, options);
    }
  };

  return (
    <form
      id="loyalty-reward-form"
      onSubmit={form.handleSubmit(onSubmit)}
      className="max-h-[70vh] space-y-4 overflow-y-auto px-0.5"
    >
      <FieldGroup disabled={isPending}>
        <FormInputField
          name="name"
          control={form.control}
          label="Tên quà"
          placeholder="Giảm 20% cho đơn tiếp theo"
          autoFocus={!isEdit}
        />

        <FormSelectField
          name="type"
          control={form.control}
          label="Loại quà"
          options={[...REWARD_TYPE_OPTIONS]}
          getOptionValue={(o) => o.value}
          getOptionLabel={(o) => o.label}
        />
        <p className="-mt-2 text-xs text-muted-foreground">
          {REWARD_TYPE_OPTIONS.find((option) => option.value === type)?.hint}
        </p>

        {type === LoyaltyRewardType.FreeCombo && (
          <FormSelectField
            name="comboId"
            control={form.control}
            label="Combo tặng khách"
            placeholder="Chọn combo"
            options={comboOptions}
            getOptionValue={(o) => o.id}
            getOptionLabel={(o) => o.label}
          />
        )}

        {isDiscount && (
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <FormSelectField
              name="discountType"
              control={form.control}
              label="Hình thức giảm"
              options={[...DISCOUNT_TYPE_OPTIONS]}
              getOptionValue={(o) => o.value}
              getOptionLabel={(o) => o.label}
            />
            <FormInputField
              name="discountValue"
              control={form.control}
              label={isPercent ? "Phần trăm giảm (%)" : "Số tiền giảm (₫)"}
              type="number"
              min={isPercent ? 0.01 : 1}
              step={isPercent ? 0.01 : 1}
              placeholder={isPercent ? "20" : "50000"}
            />
          </div>
        )}

        {showMaxAmount && (
          <FormInputField
            name="maxDiscountAmount"
            control={form.control}
            label={type === LoyaltyRewardType.FreeTicket ? "Mức giảm tối đa (₫)" : "Giảm tối đa (₫)"}
            type="number"
            min={1}
            step={1}
            placeholder={type === LoyaltyRewardType.FreeTicket ? "100000" : "Không giới hạn"}
          />
        )}

        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <FormInputField
            name="pointsCost"
            control={form.control}
            label="Số điểm cần để đổi"
            type="number"
            min={1}
            step={1}
            placeholder="500"
          />
          <FormInputField
            name="remainingQuantity"
            control={form.control}
            label="Số lượng còn"
            type="number"
            min={0}
            step={1}
            placeholder="100"
          />
        </div>

        <FormInputField
          name="codeValidityDays"
          control={form.control}
          label="Hạn dùng của mã (ngày)"
          type="number"
          min={1}
          step={1}
          placeholder="30"
        />
        <p className="-mt-2 text-xs text-muted-foreground">
          Mã hết hạn mà chưa dùng sẽ không được hoàn lại điểm cho khách.
        </p>

        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <FormDateTimePickerField
            name="validFrom"
            control={form.control}
            label="Mở đổi từ"
            placeholder="Không giới hạn"
            clearable
          />
          <FormDateTimePickerField
            name="validTo"
            control={form.control}
            label="Đóng đổi lúc"
            placeholder="Không giới hạn"
            clearable
          />
        </div>

        <FormSwitchField name="isActive" control={form.control} label="Đang hoạt động" />
      </FieldGroup>

      <FormErrorAlert error={mutation.error} preferServerMessage />

      <FormActions
        isPending={isPending}
        isDirty={form.formState.isDirty}
        submitLabel={isEdit ? FORM_SAVE_CHANGES_LABEL : "Tạo quà"}
        onCancel={onSuccess}
      />
    </form>
  );
};
