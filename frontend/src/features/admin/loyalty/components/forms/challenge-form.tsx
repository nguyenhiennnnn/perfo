import { useMemo } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";

import { FieldGroup } from "@/components/ui/field";
import { FormInputField } from "@/components/common/form/form-input-field";
import { FormSelectField } from "@/components/common/form/form-select-field";
import { FormSwitchField } from "@/components/common/form/form-switch-field";
import { FormTextareaField } from "@/components/common/form/form-textarea-field";
import { FormDatePickerField } from "@/components/common/form/form-date-picker-field";
import { FormActions } from "@/components/common/form/form-actions";
import { FormErrorAlert } from "@/components/common/form/form-error-alert";
import { useServerFieldErrors } from "@/hooks/use-server-field-errors";
import { toDateInputValue } from "@/lib/formatters";
import { FORM_SAVE_CHANGES_LABEL, LOOKUP_PAGE_SIZE } from "@/config/default-constants";
import { LoyaltyChallengeConditionType, LoyaltyChallengeRewardType } from "@/types/enum";

import {
  useAdminCreateLoyaltyChallenge,
  useAdminUpdateLoyaltyChallenge,
} from "../../hooks/use-admin-loyalty-challenges";
import { useAdminLoyaltyRewards } from "../../hooks/use-admin-loyalty-rewards";
import type { AdminLoyaltyChallengeDto, UpsertLoyaltyChallengeRequest } from "../../types";
import {
  CHALLENGE_CONDITION_OPTIONS,
  CHALLENGE_REWARD_TYPE_OPTIONS,
} from "../../utils/loyalty-admin-labels";
import { challengeFormSchema, type ChallengeFormValues } from "./challenge-schema";

interface ChallengeFormProps {
  challenge?: AdminLoyaltyChallengeDto;
  onSuccess: () => void;
}

const toText = (value?: number | null) => (value === null || value === undefined ? "" : String(value));

export const ChallengeForm = ({ challenge, onSuccess }: ChallengeFormProps) => {
  const isEdit = Boolean(challenge);
  const createChallenge = useAdminCreateLoyaltyChallenge();
  const updateChallenge = useAdminUpdateLoyaltyChallenge();
  const mutation = isEdit ? updateChallenge : createChallenge;
  const isPending = mutation.isPending;

  const hasProgress = (challenge?.participantCount ?? 0) > 0;

  const rewards = useAdminLoyaltyRewards({
    isActive: true,
    page: 1,
    pageSize: LOOKUP_PAGE_SIZE,
  });

  const giftOptions = useMemo(() => {
    const items = (rewards.data?.items ?? []).map((reward) => ({
      id: reward.rewardId,
      label: reward.name,
    }));
    if (challenge?.rewardGiftId && !items.some((item) => item.id === challenge.rewardGiftId)) {
      items.push({
        id: challenge.rewardGiftId,
        label: challenge.rewardGiftName ?? `Quà #${challenge.rewardGiftId}`,
      });
    }
    return items;
  }, [rewards.data, challenge]);

  const form = useForm<ChallengeFormValues>({
    resolver: zodResolver(challengeFormSchema),
    defaultValues: {
      name: challenge?.name ?? "",
      description: challenge?.description ?? "",
      startsOn: toDateInputValue(challenge?.startsOn) ?? "",
      endsOn: toDateInputValue(challenge?.endsOn) ?? "",
      conditionType: challenge?.conditionType ?? LoyaltyChallengeConditionType.DistinctMovies,
      targetCount: toText(challenge?.targetCount),
      rewardType: challenge?.rewardType ?? LoyaltyChallengeRewardType.Points,
      rewardPoints: toText(challenge?.rewardPoints),
      rewardGiftId: challenge?.rewardGiftId ? String(challenge.rewardGiftId) : null,
      isActive: challenge?.isActive ?? true,
    },
  });

  const applyServerErrors = useServerFieldErrors(form);
  const rewardType = form.watch("rewardType");
  const isPoints = rewardType === LoyaltyChallengeRewardType.Points;

  const onSubmit = (values: ChallengeFormValues) => {
    const points = values.rewardType === LoyaltyChallengeRewardType.Points;

    const body: UpsertLoyaltyChallengeRequest = {
      name: values.name,
      description: values.description === "" ? null : values.description,
      startsOn: values.startsOn,
      endsOn: values.endsOn,
      conditionType: values.conditionType,
      targetCount: Number(values.targetCount),
      rewardType: values.rewardType,
      rewardPoints: points ? Number(values.rewardPoints) : null,
      rewardGiftId: points ? null : Number(values.rewardGiftId),
      isActive: values.isActive,
    };
    const options = { onSuccess: () => onSuccess(), onError: applyServerErrors };

    if (challenge) {
      updateChallenge.mutate({ id: challenge.challengeId, body }, options);
    } else {
      createChallenge.mutate(body, options);
    }
  };

  return (
    <form
      id="loyalty-challenge-form"
      onSubmit={form.handleSubmit(onSubmit)}
      className="max-h-[70vh] space-y-4 overflow-y-auto px-0.5"
    >
      <FieldGroup disabled={isPending}>
        <FormInputField
          name="name"
          control={form.control}
          label="Tên thử thách"
          placeholder="Cuồng phim tháng 12"
          autoFocus={!isEdit}
        />

        <FormTextareaField
          name="description"
          control={form.control}
          label="Mô tả"
          placeholder="Mô tả ngắn để khách hiểu cách tham gia"
        />

        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <FormDatePickerField
            name="startsOn"
            control={form.control}
            label="Bắt đầu từ ngày"
            placeholder="Chọn ngày"
            clearable={false}
          />
          <FormDatePickerField
            name="endsOn"
            control={form.control}
            label="Kết thúc vào ngày"
            placeholder="Chọn ngày"
            clearable={false}
          />
        </div>

        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <FormSelectField
            name="conditionType"
            control={form.control}
            label="Điều kiện"
            options={[...CHALLENGE_CONDITION_OPTIONS]}
            getOptionValue={(o) => o.value}
            getOptionLabel={(o) => o.label}
            disabled={hasProgress}
          />
          <FormInputField
            name="targetCount"
            control={form.control}
            label="Mục tiêu (số lượng)"
            type="number"
            min={1}
            step={1}
            placeholder="3"
          />
        </div>
        {hasProgress ? (
          <p className="-mt-2 text-xs text-muted-foreground">
            Thử thách đã có khách tham gia nên không thể đổi điều kiện. Chỉ vé đã check-in mới được
            tính vào tiến độ.
          </p>
        ) : (
          <p className="-mt-2 text-xs text-muted-foreground">
            Chỉ vé đã check-in mới được tính vào tiến độ của khách.
          </p>
        )}

        <FormSelectField
          name="rewardType"
          control={form.control}
          label="Phần thưởng"
          options={[...CHALLENGE_REWARD_TYPE_OPTIONS]}
          getOptionValue={(o) => o.value}
          getOptionLabel={(o) => o.label}
        />

        {isPoints ? (
          <FormInputField
            name="rewardPoints"
            control={form.control}
            label="Điểm thưởng"
            type="number"
            min={1}
            step={1}
            placeholder="200"
          />
        ) : (
          <FormSelectField
            name="rewardGiftId"
            control={form.control}
            label="Quà thưởng"
            placeholder="Chọn quà từ danh mục"
            options={giftOptions}
            getOptionValue={(o) => o.id}
            getOptionLabel={(o) => o.label}
          />
        )}

        <FormSwitchField name="isActive" control={form.control} label="Đang hoạt động" />
      </FieldGroup>

      <FormErrorAlert error={mutation.error} preferServerMessage />

      <FormActions
        isPending={isPending}
        isDirty={form.formState.isDirty}
        submitLabel={isEdit ? FORM_SAVE_CHANGES_LABEL : "Tạo thử thách"}
        onCancel={onSuccess}
      />
    </form>
  );
};
