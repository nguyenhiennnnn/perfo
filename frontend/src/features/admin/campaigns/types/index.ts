import type { CampaignChannel, CampaignStatus, PromotionDiscountType } from "@/types/enum";

export interface CampaignPromoDto {
  discountType: PromotionDiscountType;
  discountValue: number;
  maxDiscountAmount: number | null;
  validityDays: number;
}

export interface CampaignAudienceDto {
  segmentCustomers: number;
  withoutConsent: number;
  withoutContact: number;
  recentlyContacted: number;
  eligible: number;
}

export interface CampaignDto {
  campaignId: number;
  name: string;
  segmentId: number;
  segmentName: string;
  channel: CampaignChannel;
  subject: string;
  body: string;
  promo: CampaignPromoDto | null;
  scheduledAt: string | null;
  status: CampaignStatus;
  audience: CampaignAudienceDto | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface UpsertCampaignRequest {
  name: string;
  segmentId: number;
  channel: CampaignChannel;
  subject: string;
  body: string;
  promo: CampaignPromoDto | null;
  scheduledAt: string | null;
}

export interface CampaignListParams {
  page?: number;
  pageSize?: number;
  search?: string;
  status?: string;
  segmentId?: number;
}

export interface DeleteCampaignResponse {
  id: number;
  deleted: boolean;
}

export interface CampaignTestSendResponse {
  sentTo: string | null;
}

export interface CampaignReportDto {
  campaignId: number;
  name: string;
  status: CampaignStatus;
  recipients: number;
  sent: number;
  failed: number;
  skipped: number;
  pending: number;
  opened: number;
  openRate: number;
  codesIssued: number;
  codeUsers: number;
  codeUseRate: number;
  orders: number;
  discountAmount: number;
  revenue: number;
}
