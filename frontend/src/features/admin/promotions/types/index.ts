import type {
  OrderChannel,
  PromotionDiscountType,
  PromotionRedemptionStatus,
  PromotionScope,
  PromotionType,
} from "@/types/enum";

export interface PromotionDto {
  promotionId: number;
  code: string;
  name: string;
  description?: string | null;
  type: PromotionType;
  discountType: PromotionDiscountType;
  discountValue: number;
  maxDiscountAmount?: number | null;
  scope: PromotionScope;
  minOrderAmount?: number | null;
  startsAt?: string | null;
  endsAt?: string | null;
  channel?: OrderChannel | null;
  movieIds: number[];
  showtimeIds: number[];
  screenTypeIds: number[];
  daysOfWeek: number[];
  membershipTierIds: number[];
  newCustomersOnly: boolean;
  totalUsageLimit?: number | null;
  perCustomerLimit?: number | null;
  budgetAmount?: number | null;
  priority: number;
  isExclusive: boolean;
  assignedUserId?: number | null;
  assignedUserName?: string | null;
  assignedUserEmail?: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

export interface PromotionListParams {
  search?: string;
  type?: string;
  isActive?: boolean;
  page?: number;
  pageSize?: number;
}

export interface UpsertPromotionRequest {
  code: string;
  name: string;
  description: string | null;
  type: PromotionType;
  discountType: PromotionDiscountType;
  discountValue: number;
  maxDiscountAmount: number | null;
  scope: PromotionScope;
  minOrderAmount: number | null;
  startsAt: string | null;
  endsAt: string | null;
  channel: OrderChannel | null;
  movieIds: number[];
  showtimeIds: number[];
  screenTypeIds: number[];
  daysOfWeek: number[];
  membershipTierIds: number[];
  newCustomersOnly: boolean;
  totalUsageLimit: number | null;
  perCustomerLimit: number | null;
  budgetAmount: number | null;
  priority: number;
  isExclusive: boolean;
  assignedUserId: number | null;
  isActive: boolean;
}

export interface DeletePromotionResponse {
  id: number;
  deleted: boolean;
}

export interface BulkPersonalCodesRequest {
  codePrefix: string;
  userIds: number[];
  validDays: number | null;
  name: string;
  description: string | null;
  discountType: PromotionDiscountType;
  discountValue: number;
  maxDiscountAmount: number | null;
  scope: PromotionScope;
  minOrderAmount: number | null;
  startsAt: string | null;
  endsAt: string | null;
  channel: OrderChannel | null;
  movieIds: number[];
  showtimeIds: number[];
  screenTypeIds: number[];
  daysOfWeek: number[];
  membershipTierIds: number[];
  newCustomersOnly: boolean;
  budgetAmount: number | null;
  priority: number;
  isExclusive: boolean;
}

export interface BulkPersonalCodeItemDto {
  promotionId: number;
  code: string;
  userId: number;
  userName: string;
  userEmail: string;
  endsAt?: string | null;
}

export interface BulkPersonalCodesResponse {
  created: number;
  items: BulkPersonalCodeItemDto[];
}

export interface PromotionRedemptionDto {
  promotionRedemptionId: number;
  orderId: number;
  orderCode: string;
  channel: OrderChannel;
  userId?: number | null;
  customerName?: string | null;
  status: PromotionRedemptionStatus;
  discountAmount: number;
  orderFinalAmount: number;
  reservedAt: string;
  redeemedAt?: string | null;
  releasedAt?: string | null;
}

export interface PromotionRedemptionListParams {
  status?: string;
  channel?: string;
  search?: string;
  fromDate?: string;
  toDate?: string;
  page?: number;
  pageSize?: number;
}

export interface PromotionReportListParams {
  fromDate?: string;
  toDate?: string;
  search?: string;
  type?: string;
  isActive?: boolean;
  page?: number;
  pageSize?: number;
}

export interface PromotionReportSummaryDto {
  promotionCount: number;
  redeemedCount: number;
  orderCount: number;
  totalDiscount: number;
  orderRevenue: number;
}

export interface PromotionReportListItemDto {
  promotionId: number;
  code: string;
  name: string;
  type: PromotionType;
  isActive: boolean;
  startsAt?: string | null;
  endsAt?: string | null;
  redeemedCount: number;
  totalDiscount: number;
  orderRevenue: number;
  totalUsageLimit?: number | null;
  consumedCount: number;
  usageRatio?: number | null;
  budgetAmount?: number | null;
  consumedBudget: number;
  budgetRatio?: number | null;
}

export interface PromotionReportListDto {
  summary: PromotionReportSummaryDto;
  items: {
    items: PromotionReportListItemDto[];
    page: number;
    pageSize: number;
    totalItems: number;
    totalPages: number;
  };
}

export interface PromotionReportUsageDto {
  redeemedCount: number;
  activeHoldCount: number;
  releasedCount: number;
  orderCount: number;
  customerCount: number;
  totalDiscount: number;
  orderRevenue: number;
  averageDiscountPerOrder: number;
}

export interface PromotionReportLimitsDto {
  totalUsageLimit?: number | null;
  consumedCount: number;
  remainingUsage?: number | null;
  usageRatio?: number | null;
  budgetAmount?: number | null;
  consumedBudget: number;
  remainingBudget?: number | null;
  budgetRatio?: number | null;
}

export interface PromotionReportBreakdownItemDto {
  key: string;
  redeemedCount: number;
  totalDiscount: number;
  orderRevenue: number;
}

export interface PromotionReportDto {
  promotion: PromotionDto;
  usage: PromotionReportUsageDto;
  limits: PromotionReportLimitsDto;
  byChannel: PromotionReportBreakdownItemDto[];
  byTimeSlot: PromotionReportBreakdownItemDto[];
}

export interface PromotionReportRangeParams {
  fromDate?: string;
  toDate?: string;
}
