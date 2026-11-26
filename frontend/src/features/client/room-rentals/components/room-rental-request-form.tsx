import { useState, type ReactNode } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm, useWatch } from "react-hook-form";
import { useNavigate } from "react-router";
import { Send } from "lucide-react";
import { FormDateTimePickerField } from "@/components/common/form/form-datetime-picker-field";
import { FormEnumSelectField } from "@/components/common/form/form-enum-select-field";
import { FormErrorAlert } from "@/components/common/form/form-error-alert";
import { FormInputField } from "@/components/common/form/form-input-field";
import { FormSelectField } from "@/components/common/form/form-select-field";
import { FormSwitchField } from "@/components/common/form/form-switch-field";
import { SubmitButton } from "@/components/common/form/submit-button";
import { FormTextareaField } from "@/components/common/form/form-textarea-field";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { FieldGroup } from "@/components/ui/field";
import { MS_PER_SECOND } from "@/config/default-constants";
import { ROUTES } from "@/config/routes";
import { useCountdown } from "@/hooks/use-countdown";
import { useServerFieldErrors } from "@/hooks/use-server-field-errors";
import { getBusinessTodayDate } from "@/lib/business-time";
import { getPublicFormRetrySeconds } from "@/lib/error";
import { getServerNow } from "@/lib/server-clock";
import {
  ROOM_RENTAL_MAX_ATTENDEES,
  ROOM_RENTAL_MIN_ATTENDEES,
  ROOM_RENTAL_OPTION_NONE,
  ROOM_RENTAL_RATE_LIMIT_FALLBACK_SECONDS,
} from "../constants";
import {
  useCreateRoomRentalRequest,
  useRentalMovieOptions,
  useRentalScreenTypes,
} from "../hooks/use-room-rental-request";
import type { RoomRentalSelectOption } from "../types";
import {
  EMPTY_ROOM_RENTAL_REQUEST,
  roomRentalRequestSchema,
  toCreateRoomRentalRequest,
  type RoomRentalRequestFormValues,
} from "../utils/room-rental-form";
import {
  getRoomRentalOrganizationLabel,
  roomRentalAudienceOptions,
} from "../utils/room-rental-labels";
import { RoomRentalRateLimitAlert } from "./room-rental-rate-limit-alert";

interface FormSectionProps {
  title: string;
  description: string;
  children: ReactNode;
}

const FormSection = ({ title, description, children }: FormSectionProps) => (
  <Card>
    <CardHeader>
      <CardTitle className="t-h3">{title}</CardTitle>
      <CardDescription>{description}</CardDescription>
    </CardHeader>
    <CardContent>
      <FieldGroup className="gap-4">{children}</FieldGroup>
    </CardContent>
  </Card>
);

const withNoneOption = (
  options: RoomRentalSelectOption[] | undefined,
  noneLabel: string,
): RoomRentalSelectOption[] => [
  { value: ROOM_RENTAL_OPTION_NONE, label: noneLabel },
  ...(options ?? []),
];

export const RoomRentalRequestForm = () => {
  const navigate = useNavigate();
  const form = useForm<RoomRentalRequestFormValues>({
    resolver: zodResolver(roomRentalRequestSchema),
    mode: "onTouched",
    defaultValues: EMPTY_ROOM_RENTAL_REQUEST,
  });
  const applyServerErrors = useServerFieldErrors(form);
  const createRequest = useCreateRoomRentalRequest();
  const movieOptions = useRentalMovieOptions();
  const screenTypes = useRentalScreenTypes();

  const [submitError, setSubmitError] = useState<unknown>(null);
  const [lockedUntil, setLockedUntil] = useState<string | null>(null);
  const countdown = useCountdown(lockedUntil);
  const isLocked = lockedUntil !== null && countdown.remainingMs > 0;

  const audienceType = useWatch({ control: form.control, name: "audienceType" });
  const hasFlexibleRange = useWatch({ control: form.control, name: "hasFlexibleRange" });
  const bringsOwnContent = useWatch({ control: form.control, name: "bringsOwnContent" });

  const handleSubmit = form.handleSubmit((values) => {
    setSubmitError(null);
    createRequest.mutate(toCreateRoomRentalRequest(values), {
      onSuccess: (response) => {
        navigate(ROUTES.roomRental.submitted(response.data.requestCode), { replace: true });
      },
      onError: (error) => {
        const retrySeconds = getPublicFormRetrySeconds(error);
        if (retrySeconds !== null) {
          const waitSeconds = retrySeconds || ROOM_RENTAL_RATE_LIMIT_FALLBACK_SECONDS;
          setLockedUntil(new Date(getServerNow() + waitSeconds * MS_PER_SECOND).toISOString());
          return;
        }
        if (applyServerErrors(error)) return;
        setSubmitError(error);
      },
    });
  });

  return (
    <form onSubmit={handleSubmit} noValidate className="space-y-5 pb-6">
      <FormSection
        title="Thông tin liên hệ"
        description="Rạp dùng thông tin này để gửi báo giá và liên hệ với bạn."
      >
        <FormInputField
          name="fullName"
          control={form.control}
          label="Họ và tên"
          placeholder="Nguyễn Văn A"
          autoComplete="name"
        />
        <div className="grid gap-4 sm:grid-cols-2">
          <FormEnumSelectField
            name="audienceType"
            control={form.control}
            label="Bạn thuê phòng với tư cách"
            options={roomRentalAudienceOptions}
            placeholder="Chọn loại đối tượng"
            className="w-full"
          />
          <FormInputField
            name="organizationName"
            control={form.control}
            label={getRoomRentalOrganizationLabel(audienceType)}
            autoComplete="organization"
          />
        </div>
        <div className="grid gap-4 sm:grid-cols-2">
          <FormInputField
            name="contactPhone"
            control={form.control}
            label="Số điện thoại"
            type="tel"
            placeholder="0901234567"
            autoComplete="tel"
          />
          <FormInputField
            name="contactEmail"
            control={form.control}
            label="Email nhận báo giá"
            type="email"
            placeholder="ban@example.com"
            autoComplete="email"
          />
        </div>
      </FormSection>

      <FormSection
        title="Thời gian và quy mô"
        description="Chọn thời điểm mong muốn. Nếu có thể linh hoạt, hãy cho rạp thêm một khoảng dự phòng."
      >
        <FormDateTimePickerField
          name="desiredStartAt"
          control={form.control}
          label="Ngày giờ bắt đầu mong muốn"
          clearable={false}
          calendarDisabled={{ before: getBusinessTodayDate() }}
          minuteStep={15}
        />
        <FormSwitchField
          name="hasFlexibleRange"
          control={form.control}
          label="Tôi có thể linh hoạt về thời gian"
        />
        {hasFlexibleRange && (
          <div className="grid gap-4 sm:grid-cols-2">
            <FormDateTimePickerField
              name="flexibleFromAt"
              control={form.control}
              label="Dự phòng từ"
              calendarDisabled={{ before: getBusinessTodayDate() }}
              minuteStep={15}
            />
            <FormDateTimePickerField
              name="flexibleToAt"
              control={form.control}
              label="Dự phòng đến"
              calendarDisabled={{ before: getBusinessTodayDate() }}
              minuteStep={15}
            />
          </div>
        )}
        <FormInputField
          name="attendeeCount"
          control={form.control}
          label="Số người tham dự"
          type="number"
          min={ROOM_RENTAL_MIN_ATTENDEES}
          max={ROOM_RENTAL_MAX_ATTENDEES}
          placeholder="Ví dụ: 40"
        />
      </FormSection>

      <FormSection
        title="Nội dung và dịch vụ"
        description="Chọn phim bạn muốn chiếu hoặc cho biết bạn tự mang nội dung đến."
      >
        <FormSwitchField
          name="bringsOwnContent"
          control={form.control}
          label="Tôi tự mang nội dung trình chiếu"
        />
        {bringsOwnContent ? (
          <FormTextareaField
            name="contentNote"
            control={form.control}
            label="Mô tả nội dung (không bắt buộc)"
            placeholder="Ví dụ: video sự kiện 30 phút, định dạng MP4, có phụ đề"
            rows={3}
          />
        ) : (
          <FormSelectField
            name="movieId"
            control={form.control}
            label="Phim mong muốn (không bắt buộc)"
            placeholder={movieOptions.isPending ? "Đang tải danh sách phim..." : "Chọn phim"}
            options={withNoneOption(movieOptions.data, "Chưa chọn, cần rạp tư vấn")}
            getOptionValue={(option) => option.value}
            getOptionLabel={(option) => option.label}
            className="w-full"
          />
        )}
        <FormSelectField
          name="screenTypeId"
          control={form.control}
          label="Định dạng phòng (không bắt buộc)"
          placeholder={screenTypes.isPending ? "Đang tải định dạng phòng..." : "Chọn định dạng phòng"}
          options={withNoneOption(screenTypes.data, "Chưa chọn, cần rạp tư vấn")}
          getOptionValue={(option) => option.value}
          getOptionLabel={(option) => option.label}
          className="w-full"
        />
        <FormTextareaField
          name="comboRequest"
          control={form.control}
          label="Combo bắp nước (không bắt buộc)"
          placeholder="Ví dụ: 40 combo bắp nước vừa"
          rows={2}
        />
        <FormTextareaField
          name="notes"
          control={form.control}
          label="Yêu cầu khác (không bắt buộc)"
          placeholder="Trang trí, thiết bị âm thanh, thời lượng sử dụng phòng..."
          rows={3}
        />
      </FormSection>

      <div className="space-y-3">
        <FormErrorAlert error={submitError} />
        {isLocked && <RoomRentalRateLimitAlert countdownLabel={countdown.label} />}
        <SubmitButton
          isPending={createRequest.isPending}
          disabled={isLocked}
          label="Gửi yêu cầu thuê phòng"
          pendingLabel="Đang gửi..."
          icon={<Send className="size-4" />}
          className="w-full sm:w-auto"
          size="lg"
        />
        <p className="text-xs text-muted-foreground">
          Gửi yêu cầu không ràng buộc thanh toán. Rạp sẽ phản hồi báo giá qua email của bạn.
        </p>
      </div>
    </form>
  );
};
