import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Wrench } from "lucide-react";
import { z } from "zod";

import { FieldGroup } from "@/components/ui/field";
import { FormDateTimePickerField } from "@/components/common/form/form-datetime-picker-field";
import { FormEnumSelectField } from "@/components/common/form/form-enum-select-field";
import { FormTextareaField } from "@/components/common/form/form-textarea-field";
import { SubmitButton } from "@/components/common/form/submit-button";
import { useServerFieldErrors } from "@/hooks/use-server-field-errors";
import { getBusinessTodayDate, toBusinessIso } from "@/lib/business-time";
import { showApiErrorToast } from "@/lib/error";
import { getServerNow } from "@/lib/server-clock";

import {
  SEAT_BLOCK_REASON_MAX_LENGTH,
  seatBlockTypeOptions,
} from "../../config/seat-block-config";
import { useBlockSeats } from "../../hooks/use-seat-blocks";
import { SeatBlockType } from "../../types";
import { getConflictSeatIds } from "../../utils/seat-blocks";

const formSchema = z
  .object({
    type: z.enum([SeatBlockType.Maintenance, SeatBlockType.Broken], {
      message: "Vui lòng chọn loại khóa.",
    }),
    reason: z
      .string()
      .trim()
      .min(1, "Vui lòng nhập lý do khóa ghế.")
      .max(
        SEAT_BLOCK_REASON_MAX_LENGTH,
        `Lý do tối đa ${SEAT_BLOCK_REASON_MAX_LENGTH} ký tự.`,
      ),
    expiresAt: z.string(),
  })
  .superRefine((values, ctx) => {
    if (!values.expiresAt) return;
    const expiresAt = Date.parse(toBusinessIso(values.expiresAt));
    if (Number.isNaN(expiresAt) || expiresAt <= getServerNow()) {
      ctx.addIssue({
        code: "custom",
        path: ["expiresAt"],
        message: "Thời hạn khóa phải ở tương lai.",
      });
    }
  });

type FormValues = z.infer<typeof formSchema>;

interface SeatBlockFormProps {
  showtimeId: number;
  seatIds: number[];
  seatCount: number;
  onBlocked: () => void;
  onConflict: (error: unknown, conflictSeatIds: number[]) => void;
}

export const SeatBlockForm = ({
  showtimeId,
  seatIds,
  seatCount,
  onBlocked,
  onConflict,
}: SeatBlockFormProps) => {
  const blockSeats = useBlockSeats();

  const form = useForm<FormValues>({
    resolver: zodResolver(formSchema),
    defaultValues: { type: SeatBlockType.Broken, reason: "", expiresAt: "" },
  });
  const applyServerErrors = useServerFieldErrors(form);

  const onSubmit = (values: FormValues) => {
    blockSeats.mutate(
      {
        showtimeId,
        body: {
          showtimeSeatIds: seatIds,
          type: values.type,
          reason: values.reason,
          expiresAt: values.expiresAt ? toBusinessIso(values.expiresAt) : null,
        },
      },
      {
        onSuccess: () => {
          form.reset({ type: values.type, reason: "", expiresAt: "" });
          onBlocked();
        },
        onError: (error) => {
          const conflictSeatIds = getConflictSeatIds(error);
          if (conflictSeatIds.length > 0) {
            onConflict(error, conflictSeatIds);
            return;
          }
          if (!applyServerErrors(error)) {
            showApiErrorToast(error, { title: "Không khóa được ghế" });
          }
        },
      },
    );
  };

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
      <FieldGroup disabled={blockSeats.isPending}>
        <FormEnumSelectField
          name="type"
          control={form.control}
          label="Loại khóa"
          options={seatBlockTypeOptions}
          placeholder="Chọn loại khóa"
        />
        <FormTextareaField
          name="reason"
          control={form.control}
          label="Lý do"
          placeholder="Ví dụ: ghế gãy tựa lưng, cần thay"
          rows={3}
        />
        <FormDateTimePickerField
          name="expiresAt"
          control={form.control}
          label="Thời hạn khóa (không bắt buộc)"
          placeholder="Khóa đến khi mở thủ công"
          clearable
          calendarDisabled={{ before: getBusinessTodayDate() }}
        />
      </FieldGroup>
      <SubmitButton
        isPending={blockSeats.isPending}
        disabled={seatCount === 0}
        label={`Khóa ${seatCount} ghế`}
        pendingLabel="Đang khóa ghế..."
        icon={<Wrench className="size-4" />}
        className="w-full"
      />
    </form>
  );
};
