import { zodResolver } from "@hookform/resolvers/zod";
import { useMemo } from "react";
import { useForm } from "react-hook-form";
import { FormActions } from "@/components/common/form/form-actions";
import { FormInputField } from "@/components/common/form/form-input-field";
import { FormSwitchField } from "@/components/common/form/form-switch-field";
import { FieldGroup } from "@/components/ui/field";
import { SETTINGS_FIELD_MAX } from "@/config/default-constants";
import { useCinemaSettings } from "@/features/client/settings/hooks/use-settings";
import { useServerFieldErrors } from "@/hooks/use-server-field-errors";
import { useDialogClose } from "@/stores/dialog-store";
import { useRegisterWaitlist } from "../hooks/use-waitlist";
import {
  createWaitlistRegisterSchema,
  DEFAULT_WAITLIST_VALUES,
  type WaitlistRegisterValues,
} from "../utils/waitlist-form";

export const WaitlistRegisterDialogContent = ({ showtimeId }: { showtimeId: number }) => {
  const close = useDialogClose();
  const settings = useCinemaSettings();
  const register = useRegisterWaitlist(showtimeId);
  const maxSeats = settings.data?.maxSeatsPerBooking ?? SETTINGS_FIELD_MAX.maxSeatsPerBooking;
  const schema = useMemo(() => createWaitlistRegisterSchema(maxSeats), [maxSeats]);

  const form = useForm<WaitlistRegisterValues>({
    resolver: zodResolver(schema),
    mode: "onTouched",
    defaultValues: DEFAULT_WAITLIST_VALUES,
  });
  const applyServerErrors = useServerFieldErrors(form);

  return (
    <form
      onSubmit={form.handleSubmit((values) =>
        register.mutate(values, { onSuccess: close, onError: applyServerErrors }),
      )}
      className="space-y-5 pb-6"
    >
      <FieldGroup disabled={register.isPending} className="gap-4">
        <FormInputField
          name="seatCount"
          control={form.control}
          type="number"
          min={1}
          max={maxSeats}
          label="Số ghế cần"
          autoFocus
        />
        <FormSwitchField name="requireAdjacent" control={form.control} label="Cần ghế liền kề" />
      </FieldGroup>
      <p className="text-sm text-muted-foreground">
        Khi có ghế được nhả, bạn sẽ được thông báo và ưu tiên giữ ghế trong thời gian ngắn trước khi
        mở cho người khác.
      </p>

      <FormActions
        isPending={register.isPending}
        isDirty={form.formState.isDirty}
        submitLabel="Đăng ký chờ"
        onCancel={close}
      />
    </form>
  );
};
