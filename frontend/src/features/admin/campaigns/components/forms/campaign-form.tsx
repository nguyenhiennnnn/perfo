import { useMemo } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Loader2, Send } from "lucide-react";

import { Button } from "@/components/ui/button";
import { FieldGroup } from "@/components/ui/field";
import { FormActions } from "@/components/common/form/form-actions";
import { FormDateTimePickerField } from "@/components/common/form/form-datetime-picker-field";
import { FormErrorAlert } from "@/components/common/form/form-error-alert";
import { FormInputField } from "@/components/common/form/form-input-field";
import { FormSelectField } from "@/components/common/form/form-select-field";
import { FormSwitchField } from "@/components/common/form/form-switch-field";
import { FormTextareaField } from "@/components/common/form/form-textarea-field";
import { FORM_SAVE_CHANGES_LABEL, LOOKUP_PAGE_SIZE } from "@/config/default-constants";
import { useAdminCustomerSegments } from "@/features/admin/customer-segments/hooks/use-admin-customer-segments";
import { DISCOUNT_TYPE_OPTIONS } from "@/features/admin/promotions/utils/promotion-admin-labels";
import { useServerFieldErrors } from "@/hooks/use-server-field-errors";
import { getBusinessTodayDate } from "@/lib/business-time";
import { formatNumber } from "@/lib/formatters";
import { CampaignChannel, PromotionDiscountType } from "@/types/enum";
import type { ApiResponse } from "@/types/api";

import {
  useAdminCreateCampaign,
  useAdminTestSendCampaign,
  useAdminUpdateCampaign,
} from "../../hooks/use-admin-campaigns";
import type { CampaignDto } from "../../types";
import { CAMPAIGN_CHANNEL_OPTIONS } from "../../utils/campaign-labels";
import { CampaignAudienceSummary } from "../campaign-audience-summary";
import {
  campaignFormSchema,
  getCampaignFormDefaults,
  toCampaignRequest,
  type CampaignFormValues,
} from "./campaign-schema";

interface CampaignFormProps {
  campaign?: CampaignDto;
  onSuccess: () => void;
}

const SectionTitle = ({ children }: { children: string }) => (
  <h3 className="text-sm font-semibold text-foreground">{children}</h3>
);

const Hint = ({ children }: { children: string }) => (
  <p className="-mt-2 text-xs text-muted-foreground">{children}</p>
);

export const CampaignForm = ({ campaign, onSuccess }: CampaignFormProps) => {
  const isEdit = Boolean(campaign);
  const createCampaign = useAdminCreateCampaign();
  const updateCampaign = useAdminUpdateCampaign();
  const testSend = useAdminTestSendCampaign();
  const mutation = isEdit ? updateCampaign : createCampaign;
  const isPending = mutation.isPending || testSend.isPending;

  const segments = useAdminCustomerSegments({ pageSize: LOOKUP_PAGE_SIZE });

  const form = useForm<CampaignFormValues>({
    resolver: zodResolver(campaignFormSchema),
    defaultValues: getCampaignFormDefaults(campaign),
  });
  const applyServerErrors = useServerFieldErrors(form);

  const channel = form.watch("channel");
  const segmentId = form.watch("segmentId");
  const hasPromo = form.watch("hasPromo");
  const discountType = form.watch("promo.discountType");
  const scheduledAt = form.watch("scheduledAt");
  const isDirty = form.formState.isDirty;
  const isEmail = channel === CampaignChannel.Email;
  const isPercent = discountType === PromotionDiscountType.Percent;

  const allSegments = useMemo(() => segments.data?.items ?? [], [segments.data]);
  const segmentOptions = useMemo(
    () => allSegments.filter((s) => s.isActive || String(s.segmentId) === segmentId),
    [allSegments, segmentId],
  );
  const selectedSegment = allSegments.find((s) => String(s.segmentId) === segmentId);

  const save = (values: CampaignFormValues, sendTest: boolean) => {
    const body = toCampaignRequest(values);
    const options = {
      onSuccess: (res: ApiResponse<CampaignDto>) => {
        if (sendTest) testSend.mutate(res.data.campaignId);
        onSuccess();
      },
      onError: applyServerErrors,
    };

    if (campaign) {
      updateCampaign.mutate({ id: campaign.campaignId, body }, options);
    } else {
      createCampaign.mutate(body, options);
    }
  };

  const handleTestSend = () => {
    if (campaign && !isDirty) {
      testSend.mutate(campaign.campaignId);
      return;
    }
    void form.handleSubmit((values) => save(values, true))();
  };

  const submitLabel = scheduledAt
    ? "Lưu và hẹn giờ"
    : isEdit
      ? FORM_SAVE_CHANGES_LABEL
      : "Lưu nháp";

  return (
    <form
      id="campaign-form"
      onSubmit={form.handleSubmit((values) => save(values, false))}
      className="max-h-[70vh] space-y-5 overflow-y-auto px-0.5"
    >
      <FieldGroup disabled={isPending}>
        <SectionTitle>Đối tượng</SectionTitle>
        <FormInputField
          name="name"
          control={form.control}
          label="Tên chiến dịch"
          placeholder="Ưu đãi quay lại cho khách lâu chưa xem phim"
          autoComplete="off"
          autoFocus={!isEdit}
        />
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <FormSelectField
            name="segmentId"
            control={form.control}
            label="Phân khúc nhận"
            placeholder="Chọn phân khúc"
            options={segmentOptions}
            getOptionValue={(s) => String(s.segmentId)}
            getOptionLabel={(s) => (s.isActive ? s.name : `${s.name} (tạm dừng)`)}
          />
          <FormSelectField
            name="channel"
            control={form.control}
            label="Kênh gửi"
            options={[...CAMPAIGN_CHANNEL_OPTIONS]}
            getOptionValue={(o) => o.value}
            getOptionLabel={(o) => o.label}
          />
        </div>
        {selectedSegment && (
          <Hint>
            {`${formatNumber(selectedSegment.eligibleCount)} trên ${formatNumber(selectedSegment.customerCount)} khách trong phân khúc đủ điều kiện nhận thông tin (đã bật tiếp thị).${
              selectedSegment.isActive ? "" : " Phân khúc đang tạm dừng nên chưa thể hẹn giờ gửi."
            }`}
          </Hint>
        )}
        {campaign && <CampaignAudienceSummary campaignId={campaign.campaignId} />}

        <SectionTitle>Nội dung</SectionTitle>
        <FormInputField
          name="subject"
          control={form.control}
          label={isEmail ? "Tiêu đề email" : "Tiêu đề thông báo"}
          placeholder="Lâu rồi chưa gặp, quay lại xem phim cùng Perfo"
          autoComplete="off"
        />
        <FormTextareaField
          name="body"
          control={form.control}
          label={isEmail ? "Nội dung email" : "Nội dung thông báo"}
          placeholder="Nội dung gửi đến khách"
          rows={8}
          className="min-h-40 resize-y"
        />
        <Hint>
          {isEmail
            ? "Email có lời chào theo tên khách, liên kết hủy đăng ký và điểm ảnh theo dõi lượt mở được thêm tự động."
            : "Kênh này không gửi email nên không có liên kết hủy đăng ký và không đo được lượt mở."}
        </Hint>

        <SectionTitle>Mã khuyến mãi cá nhân</SectionTitle>
        <FormSwitchField
          name="hasPromo"
          control={form.control}
          label="Kèm mã riêng cho từng khách"
        />
        {hasPromo && (
          <>
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
              <FormSelectField
                name="promo.discountType"
                control={form.control}
                label="Loại ưu đãi"
                options={[...DISCOUNT_TYPE_OPTIONS]}
                getOptionValue={(o) => o.value}
                getOptionLabel={(o) => o.label}
              />
              <FormInputField
                name="promo.discountValue"
                control={form.control}
                label={isPercent ? "Phần trăm giảm (%)" : "Số tiền giảm (đồng)"}
                type="number"
                min={0}
                step={isPercent ? 0.01 : 1000}
              />
              {isPercent && (
                <FormInputField
                  name="promo.maxDiscountAmount"
                  control={form.control}
                  label="Giảm tối đa (đồng, không bắt buộc)"
                  type="number"
                  min={0}
                  step={1000}
                  placeholder="Không giới hạn"
                />
              )}
              <FormInputField
                name="promo.validityDays"
                control={form.control}
                label="Hiệu lực sau khi nhận (ngày)"
                type="number"
                min={1}
                step={1}
              />
            </div>
            <Hint>
              Mỗi khách nhận một mã riêng khi chiến dịch gửi. Mã dùng một lần, chỉ dùng được với
              tài khoản nhận và áp dụng cho cả đơn.
            </Hint>
          </>
        )}

        <SectionTitle>Hẹn giờ</SectionTitle>
        <FormDateTimePickerField
          name="scheduledAt"
          control={form.control}
          label="Gửi lúc"
          placeholder="Chưa hẹn giờ, lưu nháp"
          clearable
          minuteStep={5}
          calendarDisabled={{ before: getBusinessTodayDate() }}
        />
        <Hint>
          Giờ theo múi giờ của rạp. Để trống để lưu nháp. Chiến dịch tự gửi khi đến giờ hẹn, chỉ
          đến khách đã bật nhận thông tin tiếp thị và mỗi khách nhận tối đa một chiến dịch trong
          khoảng thời gian cấu hình.
        </Hint>

        <div className="flex flex-col gap-2 border border-border p-3 sm:flex-row sm:items-center sm:justify-between">
          <p className="text-xs text-muted-foreground">
            {isEdit && !isDirty
              ? "Gửi bản đã lưu đến email quản trị của bạn, kèm mã mẫu nếu có."
              : "Lưu nội dung hiện tại rồi gửi thử đến email quản trị của bạn, kèm mã mẫu nếu có."}
          </p>
          <Button
            type="button"
            variant="outline"
            onClick={handleTestSend}
            disabled={isPending}
            className="shrink-0"
          >
            {testSend.isPending ? (
              <Loader2 className="size-4 animate-spin" />
            ) : (
              <Send className="size-4" />
            )}
            <span>{isEdit && !isDirty ? "Gửi thử" : "Lưu và gửi thử"}</span>
          </Button>
        </div>
      </FieldGroup>

      <FormErrorAlert error={mutation.error} preferServerMessage />

      <FormActions
        isPending={isPending}
        isDirty={isDirty}
        submitLabel={submitLabel}
        onCancel={onSuccess}
      />
    </form>
  );
};
