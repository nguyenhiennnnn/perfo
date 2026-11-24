import { useMemo, useState } from "react";
import { useForm, useWatch } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";

import { FieldGroup } from "@/components/ui/field";
import { FormActions } from "@/components/common/form/form-actions";
import { FormDateTimePickerField } from "@/components/common/form/form-datetime-picker-field";
import { FormSearchSelectField } from "@/components/common/form/form-search-select-field";
import { FormTextareaField } from "@/components/common/form/form-textarea-field";
import { LOOKUP_PAGE_SIZE, MS_PER_DAY } from "@/config/default-constants";
import { useAdminRooms } from "@/features/admin/rooms/hooks/use-admin-rooms";
import { useServerFieldErrors } from "@/hooks/use-server-field-errors";
import { getBusinessTodayDate, toBusinessIso } from "@/lib/business-time";

import {
  ROOM_CLOSURE_MAX_SPAN_DAYS,
  ROOM_CLOSURE_REASON_MAX_LENGTH,
} from "../../config/closure-limits";
import { useCreateRoomClosure } from "../../hooks/use-room-closures";
import type { AffectedShowtimesParams, RoomClosureDto } from "../../types";
import { AffectedShowtimesPreview } from "../affected-showtimes-preview";

const toTime = (value: string) => Date.parse(toBusinessIso(value));

const closureFormSchema = z
  .object({
    roomId: z.coerce.number({ message: "Vui lòng chọn phòng chiếu." }),
    startTime: z.string().min(1, "Vui lòng chọn thời gian bắt đầu đóng."),
    endTime: z.string().min(1, "Vui lòng chọn thời gian kết thúc đóng."),
    reason: z
      .string()
      .trim()
      .min(1, "Vui lòng nhập lý do đóng phòng.")
      .max(
        ROOM_CLOSURE_REASON_MAX_LENGTH,
        `Lý do tối đa ${ROOM_CLOSURE_REASON_MAX_LENGTH} ký tự.`,
      ),
  })
  .superRefine((values, ctx) => {
    if (!values.startTime || !values.endTime) return;
    const span = toTime(values.endTime) - toTime(values.startTime);
    if (span <= 0) {
      ctx.addIssue({
        code: "custom",
        path: ["endTime"],
        message: "Thời gian kết thúc phải sau thời gian bắt đầu.",
      });
      return;
    }
    if (span > ROOM_CLOSURE_MAX_SPAN_DAYS * MS_PER_DAY) {
      ctx.addIssue({
        code: "custom",
        path: ["endTime"],
        message: `Khoảng đóng phòng tối đa ${ROOM_CLOSURE_MAX_SPAN_DAYS} ngày.`,
      });
    }
  });

type ClosureFormValues = z.infer<typeof closureFormSchema>;

interface RoomClosureFormProps {
  onCreated: (closure: RoomClosureDto | undefined) => void;
  onCancel: () => void;
}

export const RoomClosureForm = ({ onCreated, onCancel }: RoomClosureFormProps) => {
  const createClosure = useCreateRoomClosure();
  const [roomSearch, setRoomSearch] = useState("");
  const rooms = useAdminRooms({ pageSize: LOOKUP_PAGE_SIZE });

  const form = useForm<ClosureFormValues>({
    resolver: zodResolver(closureFormSchema),
    defaultValues: { startTime: "", endTime: "", reason: "" },
  });
  const applyServerErrors = useServerFieldErrors(form);

  const [roomId, startTime, endTime] = useWatch({
    control: form.control,
    name: ["roomId", "startTime", "endTime"],
  });

  const previewParams = useMemo<AffectedShowtimesParams | null>(() => {
    if (!roomId || !startTime || !endTime) return null;
    const start = toBusinessIso(startTime);
    const end = toBusinessIso(endTime);
    if (Date.parse(end) <= Date.parse(start)) return null;
    return { roomId: Number(roomId), startTime: start, endTime: end };
  }, [roomId, startTime, endTime]);

  const onSubmit = (values: ClosureFormValues) => {
    createClosure.mutate(
      {
        roomId: values.roomId,
        startTime: toBusinessIso(values.startTime),
        endTime: toBusinessIso(values.endTime),
        reason: values.reason,
      },
      {
        onSuccess: (res) => onCreated(res.data ?? undefined),
        onError: applyServerErrors,
      },
    );
  };

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
      <FieldGroup disabled={createClosure.isPending}>
        <FormSearchSelectField
          name="roomId"
          control={form.control}
          label="Phòng chiếu"
          options={(rooms.data?.items ?? []).filter((r) => r.isActive)}
          getOptionValue={(o) => o.roomId}
          getOptionLabel={(o) => o.roomName}
          searchValue={roomSearch}
          onSearchChange={setRoomSearch}
          isLoading={rooms.isLoading}
          placeholder="Chọn phòng chiếu"
        />

        <FormDateTimePickerField
          name="startTime"
          control={form.control}
          label="Bắt đầu đóng"
          placeholder="Chọn ngày và giờ bắt đầu"
          clearable={false}
          calendarDisabled={{ before: getBusinessTodayDate() }}
        />

        <FormDateTimePickerField
          name="endTime"
          control={form.control}
          label="Kết thúc đóng"
          placeholder="Chọn ngày và giờ kết thúc"
          clearable={false}
          calendarDisabled={{ before: getBusinessTodayDate() }}
        />

        <FormTextareaField
          name="reason"
          control={form.control}
          label="Lý do đóng phòng"
          placeholder="Ví dụ: Thay máy chiếu, sửa điều hòa"
          rows={3}
        />
      </FieldGroup>

      <AffectedShowtimesPreview params={previewParams} />

      <FormActions
        isPending={createClosure.isPending}
        isDirty={form.formState.isDirty}
        submitLabel="Đóng phòng"
        onCancel={onCancel}
      />
    </form>
  );
};
