import { useMemo } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { FormActions } from "@/components/common/form/form-actions";
import { FormInputField } from "@/components/common/form/form-input-field";
import { FormSelectField } from "@/components/common/form/form-select-field";
import { FormSwitchField } from "@/components/common/form/form-switch-field";
import { FormTimePickerField } from "@/components/common/form/form-time-picker-field";
import { FieldGroup } from "@/components/ui/field";
import { FORM_SAVE_CHANGES_LABEL, LOOKUP_PAGE_SIZE, NAME_MAX_LENGTH } from "@/config/default-constants";
import { useAdminRooms } from "@/features/admin/rooms/hooks/use-admin-rooms";
import { useServerFieldErrors } from "@/hooks/use-server-field-errors";
import {
  useAdminCreateRoomRentalRate,
  useAdminUpdateRoomRentalRate,
} from "../../hooks/use-admin-room-rental-rates";
import type { RoomRentalRateDto } from "../../types";
import { MAX_AMOUNT, MAX_ATTENDEES, MAX_DURATION_MINUTES } from "./room-rental-quote-schema";

const money = (label: string) =>
  z.coerce
    .number({ message: `${label} phải là số.` })
    .int(`${label} phải là số nguyên đồng.`)
    .min(0, `${label} không được âm.`)
    .max(MAX_AMOUNT, `${label} quá lớn.`);

const duration = (label: string) =>
  z.coerce
    .number({ message: `${label} phải là số.` })
    .int(`${label} phải là số nguyên.`)
    .min(1, `${label} tối thiểu 1 phút.`)
    .max(MAX_DURATION_MINUTES, `${label} tối đa ${MAX_DURATION_MINUTES} phút.`);

const rateFormSchema = z
  .object({
    name: z
      .string()
      .trim()
      .min(1, "Vui lòng nhập tên bảng giá.")
      .max(NAME_MAX_LENGTH, `Tối đa ${NAME_MAX_LENGTH} ký tự.`),
    roomId: z.string().min(1, "Vui lòng chọn phòng."),
    startTime: z.string().min(1, "Chọn giờ bắt đầu."),
    endTime: z.string().min(1, "Chọn giờ kết thúc."),
    minDurationMinutes: duration("Thời lượng tối thiểu"),
    maxDurationMinutes: duration("Thời lượng tối đa"),
    basePrice: money("Giá thuê"),
    perPersonFee: money("Phí theo đầu người"),
    includedAttendees: z.coerce
      .number({ message: "Số người phải là số." })
      .int("Số người phải là số nguyên.")
      .min(0, "Không được âm.")
      .max(MAX_ATTENDEES, `Tối đa ${MAX_ATTENDEES}.`),
    isActive: z.boolean(),
  })
  .superRefine((values, ctx) => {
    if (values.startTime && values.endTime && values.startTime >= values.endTime) {
      ctx.addIssue({ code: "custom", path: ["endTime"], message: "Giờ kết thúc phải sau giờ bắt đầu." });
    }
    if (values.maxDurationMinutes < values.minDurationMinutes) {
      ctx.addIssue({
        code: "custom",
        path: ["maxDurationMinutes"],
        message: "Thời lượng tối đa phải không nhỏ hơn tối thiểu.",
      });
    }
  });

type RateFormValues = z.infer<typeof rateFormSchema>;

interface RoomRentalRateFormProps {
  rate?: RoomRentalRateDto;
  onSuccess: () => void;
}

export const RoomRentalRateForm = ({ rate, onSuccess }: RoomRentalRateFormProps) => {
  const isEdit = Boolean(rate);
  const rooms = useAdminRooms({ pageSize: LOOKUP_PAGE_SIZE });
  const createRate = useAdminCreateRoomRentalRate();
  const updateRate = useAdminUpdateRoomRentalRate();
  const isPending = createRate.isPending || updateRate.isPending;

  const roomOptions = useMemo(() => rooms.data?.items ?? [], [rooms.data]);

  const form = useForm<RateFormValues>({
    resolver: zodResolver(rateFormSchema),
    defaultValues: {
      name: rate?.name ?? "",
      roomId: rate ? String(rate.roomId) : "",
      startTime: rate?.startTime ?? "08:00",
      endTime: rate?.endTime ?? "23:00",
      minDurationMinutes: rate?.minDurationMinutes ?? 60,
      maxDurationMinutes: rate?.maxDurationMinutes ?? 240,
      basePrice: rate?.basePrice ?? 0,
      perPersonFee: rate?.perPersonFee ?? 0,
      includedAttendees: rate?.includedAttendees ?? 0,
      isActive: rate?.isActive ?? true,
    },
  });
  const applyServerErrors = useServerFieldErrors(form);

  const onSubmit = (values: RateFormValues) => {
    const body = { ...values, roomId: Number(values.roomId) };
    const options = { onSuccess: () => onSuccess(), onError: applyServerErrors };
    if (rate) {
      updateRate.mutate({ id: rate.roomRentalRateId, body }, options);
    } else {
      createRate.mutate(body, options);
    }
  };

  return (
    <form
      id="room-rental-rate-form"
      onSubmit={form.handleSubmit(onSubmit)}
      className="max-h-[70vh] space-y-4 overflow-y-auto px-0.5"
    >
      <FieldGroup disabled={isPending}>
        <FormInputField
          name="name"
          control={form.control}
          label="Tên bảng giá"
          placeholder="Phòng 1 - buổi tối..."
          autoComplete="off"
          autoFocus
        />
        <FormSelectField
          name="roomId"
          control={form.control}
          label="Phòng chiếu"
          placeholder="Chọn phòng"
          options={roomOptions}
          getOptionValue={(r) => r.roomId}
          getOptionLabel={(r) => r.roomName}
        />
        <div className="grid grid-cols-2 gap-3">
          <FormTimePickerField name="startTime" control={form.control} label="Từ giờ" />
          <FormTimePickerField name="endTime" control={form.control} label="Đến giờ" />
        </div>
        <div className="grid grid-cols-2 gap-3">
          <FormInputField
            name="minDurationMinutes"
            control={form.control}
            label="Thời lượng tối thiểu (phút)"
            type="number"
            min={1}
          />
          <FormInputField
            name="maxDurationMinutes"
            control={form.control}
            label="Thời lượng tối đa (phút)"
            type="number"
            min={1}
            max={MAX_DURATION_MINUTES}
          />
        </div>
        <FormInputField
          name="basePrice"
          control={form.control}
          label="Giá thuê phòng (đồng)"
          type="number"
          min={0}
          step={1000}
        />
        <div className="grid grid-cols-2 gap-3">
          <FormInputField
            name="perPersonFee"
            control={form.control}
            label="Phí mỗi người vượt mức (đồng)"
            type="number"
            min={0}
            step={1000}
          />
          <FormInputField
            name="includedAttendees"
            control={form.control}
            label="Số người đã gồm trong giá"
            type="number"
            min={0}
            max={MAX_ATTENDEES}
          />
        </div>
        <FormSwitchField name="isActive" control={form.control} label="Đang áp dụng" />
      </FieldGroup>

      <FormActions
        isPending={isPending}
        isDirty={form.formState.isDirty}
        submitLabel={isEdit ? FORM_SAVE_CHANGES_LABEL : "Thêm bảng giá"}
        onCancel={onSuccess}
      />
    </form>
  );
};
