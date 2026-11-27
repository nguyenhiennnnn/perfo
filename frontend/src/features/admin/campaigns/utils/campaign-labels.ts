import type { BadgeVariant } from "@/components/common/status-badge.config";
import type { EnumOption } from "@/lib/enum-options";
import { formatCurrency, formatNumber } from "@/lib/formatters";
import { CampaignChannel, CampaignStatus, PromotionDiscountType } from "@/types/enum";
import type { CampaignDto, CampaignPromoDto, UpsertCampaignRequest } from "../types";

export const CAMPAIGN_NAME_MAX_LENGTH = 150;
export const CAMPAIGN_SUBJECT_MAX_LENGTH = 200;
export const CAMPAIGN_BODY_MAX_LENGTH = 5000;
export const CAMPAIGN_PROMO_MAX_VALIDITY_DAYS = 365;
export const CAMPAIGN_PROMO_MAX_AMOUNT = 1_000_000_000;
export const CAMPAIGN_SENDING_REFETCH_MS = 10_000;

export const CAMPAIGN_STATUS_BADGES: Record<
  CampaignStatus,
  { label: string; variant: BadgeVariant }
> = {
  Draft: { label: "Bản nháp", variant: "secondary" },
  Scheduled: { label: "Đã hẹn giờ", variant: "outline" },
  Sending: { label: "Đang gửi", variant: "warning" },
  Completed: { label: "Đã gửi xong", variant: "success" },
};

export const CAMPAIGN_STATUS_OPTIONS: EnumOption<CampaignStatus>[] = [
  { value: CampaignStatus.Draft, label: CAMPAIGN_STATUS_BADGES.Draft.label },
  { value: CampaignStatus.Scheduled, label: CAMPAIGN_STATUS_BADGES.Scheduled.label },
  { value: CampaignStatus.Sending, label: CAMPAIGN_STATUS_BADGES.Sending.label },
  { value: CampaignStatus.Completed, label: CAMPAIGN_STATUS_BADGES.Completed.label },
];

export const CAMPAIGN_CHANNEL_LABELS: Record<CampaignChannel, string> = {
  Email: "Email",
  InApp: "Thông báo trong ứng dụng",
};

export const CAMPAIGN_CHANNEL_OPTIONS: EnumOption<CampaignChannel>[] = [
  { value: CampaignChannel.Email, label: CAMPAIGN_CHANNEL_LABELS.Email },
  { value: CampaignChannel.InApp, label: CAMPAIGN_CHANNEL_LABELS.InApp },
];

export const getCampaignChannelLabel = (channel: string) =>
  CAMPAIGN_CHANNEL_LABELS[channel as CampaignChannel] ?? channel;

export const isCampaignEditable = (status: CampaignStatus) =>
  status === CampaignStatus.Draft || status === CampaignStatus.Scheduled;

export const isCampaignDeletable = (status: CampaignStatus) => status !== CampaignStatus.Sending;

export const hasCampaignResults = (status: CampaignStatus) =>
  status === CampaignStatus.Sending || status === CampaignStatus.Completed;

export const describeCampaignPromo = (promo: CampaignPromoDto | null) => {
  if (!promo) return "Không kèm mã";
  const validity = `hiệu lực ${formatNumber(promo.validityDays)} ngày`;
  if (promo.discountType === PromotionDiscountType.Percent) {
    const cap =
      promo.maxDiscountAmount != null
        ? `, tối đa ${formatCurrency(promo.maxDiscountAmount)}`
        : "";
    return `Giảm ${formatNumber(promo.discountValue)}%${cap}, ${validity}`;
  }
  return `Giảm ${formatCurrency(promo.discountValue)}, ${validity}`;
};

export const toUpsertRequest = (
  campaign: CampaignDto,
  overrides: Partial<UpsertCampaignRequest> = {},
): UpsertCampaignRequest => ({
  name: campaign.name,
  segmentId: campaign.segmentId,
  channel: campaign.channel,
  subject: campaign.subject,
  body: campaign.body,
  promo: campaign.promo,
  scheduledAt: campaign.scheduledAt,
  ...overrides,
});
