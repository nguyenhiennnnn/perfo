import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";

import { FormInputField } from "@/components/common/form/form-input-field";
import { FormSelectField } from "@/components/common/form/form-select-field";
import { FormSwitchField } from "@/components/common/form/form-switch-field";
import { SubmitButton } from "@/components/common/form/submit-button";
import { FieldGroup } from "@/components/ui/field";
import { useServerFieldErrors } from "@/hooks/use-server-field-errors";

import { useUpsertShowtimeEvent } from "../../hooks/use-showtime-event";
import type { ShowtimeEventDto } from "../../types";

const MODE_OPTIONS = [
  { value: "Amount", label: "Phụ thu cố định (VND)" },
  { value: "Multiplier", label: "Hệ số giá (nhân với giá vé)" },
];

const schema = z.object({
  adjustmentMode: z.enum(["Amount", "Multiplier"]),
  adjustmentValue: z.coerce.number({ message: "Vui lòng nhập giá trị hợp lệ." }),
  allowMembershipFreeTickets: z.boolean(),
});

type FormValues = z.infer<typeof schema>;

interface ShowtimeEventPricingFormProps {
  showtimeId: number;
  event: ShowtimeEventDto | undefined;
  onSaved: () => void;
}

export const ShowtimeEventPricingForm = ({
  showtimeId,
  event,
  onSaved,
}: ShowtimeEventPricingFormProps) => {
  const upsert = useUpsertShowtimeEvent();

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      adjustmentMode: event?.adjustmentMode ?? "Amount",
      adjustmentValue: event?.adjustmentValue ?? 0,
      allowMembershipFreeTickets: event?.allowMembershipFreeTickets ?? false,
    },
  });
  const applyServerErrors = useServerFieldErrors(form);
  const mode = form.watch("adjustmentMode");

  const onSubmit = (values: FormValues) =>
    upsert.mutate(
      { showtimeId, body: values },
      { onSuccess: onSaved, onError: applyServerErrors },
    );

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
      <p className="text-sm text-muted-foreground">
        Suất sự kiện chỉ áp dụng quy tắc giá riêng này, không áp quy tắc giá thường.
      </p>
      <FieldGroup disabled={upsert.isPending}>
        <FormSelectField
          name="adjustmentMode"
          control={form.control}
          label="Cách điều chỉnh giá"
          options={MODE_OPTIONS}
          getOptionValue={(o) => o.value}
          getOptionLabel={(o) => o.label}
        />
        <FormInputField
          name="adjustmentValue"
          control={form.control}
          label={mode === "Amount" ? "Phụ thu mỗi vé (VND)" : "Hệ số giá"}
          type="number"
          step={mode === "Amount" ? 1000 : 0.01}
        />
        <FormSwitchField
          name="allowMembershipFreeTickets"
          control={form.control}
          label="Cho phép dùng vé miễn phí của gói thành viên"
        />
      </FieldGroup>
      <div className="flex justify-end">
        <SubmitButton
          isPending={upsert.isPending}
          label={event ? "Lưu thay đổi" : "Tạo suất sự kiện"}
          pendingLabel="Đang lưu..."
        />
      </div>
    </form>
  );
};
