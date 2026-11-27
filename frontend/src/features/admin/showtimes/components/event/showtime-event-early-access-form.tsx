import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";

import { FormDateTimePickerField } from "@/components/common/form/form-datetime-picker-field";
import { FormSwitchField } from "@/components/common/form/form-switch-field";
import { SubmitButton } from "@/components/common/form/submit-button";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { FieldGroup } from "@/components/ui/field";
import { LOOKUP_PAGE_SIZE } from "@/config/default-constants";
import { useAdminMembershipTiers } from "@/features/admin/catalog/hooks/use-admin-membership-tier";
import { useServerFieldErrors } from "@/hooks/use-server-field-errors";
import { toBusinessIso } from "@/lib/business-time";
import { toDateTimeInputValue } from "@/lib/formatters";

import {
  useRemoveShowtimeEventEarlyAccess,
  useUpsertShowtimeEventEarlyAccess,
} from "../../hooks/use-showtime-event";
import type { ShowtimeEarlyAccessSettingDto } from "../../types";

const schema = z.object({
  generalSaleStartTime: z.string().min(1, "Vui lòng chọn giờ mở bán chung."),
  membershipTierIds: z.array(z.number()),
  paidMembers: z.boolean(),
  movieInterest: z.boolean(),
});

type FormValues = z.infer<typeof schema>;

interface ShowtimeEventEarlyAccessFormProps {
  showtimeId: number;
  setting: ShowtimeEarlyAccessSettingDto;
}

export const ShowtimeEventEarlyAccessForm = ({
  showtimeId,
  setting,
}: ShowtimeEventEarlyAccessFormProps) => {
  const upsert = useUpsertShowtimeEventEarlyAccess();
  const remove = useRemoveShowtimeEventEarlyAccess();
  const tiers = useAdminMembershipTiers({ pageSize: LOOKUP_PAGE_SIZE });
  const isConfigured = setting.generalSaleStartTime !== null;

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      generalSaleStartTime: toDateTimeInputValue(setting.generalSaleStartTime) ?? "",
      membershipTierIds: setting.membershipTierIds,
      paidMembers: setting.paidMembers,
      movieInterest: setting.movieInterest,
    },
  });
  const applyServerErrors = useServerFieldErrors(form);
  const selectedTierIds = form.watch("membershipTierIds");

  const toggleTier = (tierId: number, checked: boolean) =>
    form.setValue(
      "membershipTierIds",
      checked ? [...selectedTierIds, tierId] : selectedTierIds.filter((id) => id !== tierId),
      { shouldDirty: true },
    );

  const onSubmit = (values: FormValues) =>
    upsert.mutate(
      {
        showtimeId,
        body: { ...values, generalSaleStartTime: toBusinessIso(values.generalSaleStartTime) },
      },
      { onError: applyServerErrors },
    );

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
      <p className="text-sm text-muted-foreground">
        Chỉ nhóm được chọn đặt được vé trước giờ mở bán chung; đến giờ chung mới mở cho mọi khách.
      </p>
      <FieldGroup disabled={upsert.isPending || remove.isPending}>
        <FormDateTimePickerField
          name="generalSaleStartTime"
          control={form.control}
          label="Giờ mở bán chung"
          minuteStep={5}
        />
        <div className="space-y-2">
          <p className="text-sm font-medium">Hạng thành viên được mở bán sớm</p>
          {(tiers.data?.items ?? []).length === 0 && (
            <p className="text-sm text-muted-foreground">Chưa có hạng thành viên.</p>
          )}
          {(tiers.data?.items ?? []).map((tier) => (
            <label key={tier.tierId} className="flex items-center gap-2 text-sm">
              <Checkbox
                checked={selectedTierIds.includes(tier.tierId)}
                onCheckedChange={(checked) => toggleTier(tier.tierId, checked === true)}
              />
              {tier.tierName}
            </label>
          ))}
        </div>
        <FormSwitchField
          name="paidMembers"
          control={form.control}
          label="Hội viên gói trả phí"
        />
        <FormSwitchField
          name="movieInterest"
          control={form.control}
          label="Khách đã bấm quan tâm phim"
        />
      </FieldGroup>
      <div className="flex justify-end gap-2">
        {isConfigured && (
          <Button
            type="button"
            variant="outline"
            disabled={remove.isPending || upsert.isPending}
            onClick={() => remove.mutate(showtimeId)}
          >
            Gỡ mở bán sớm
          </Button>
        )}
        <SubmitButton
          isPending={upsert.isPending}
          disabled={remove.isPending}
          label="Lưu mở bán sớm"
          pendingLabel="Đang lưu..."
        />
      </div>
    </form>
  );
};
