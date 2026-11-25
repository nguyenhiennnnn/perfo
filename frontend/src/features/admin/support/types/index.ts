import type {
  ExceptionalRefundStatus,
  PromotionDiscountType,
  SupportMessageAuthorType,
  SupportRequestStatus,
  SupportRequestType,
} from "@/types/enum";

export type SupportAssigneeFilter = "all" | "mine" | "unassigned";

export interface StaffSupportRequestListParams {
  status?: SupportRequestStatus;
  assigneeId?: number;
  unassigned?: boolean;
  search?: string;
  page?: number;
  pageSize?: number;
}

export interface StaffSupportRequestListItemDto {
  supportRequestId: number;
  orderId: number;
  orderCode: string;
  customerId?: number | null;
  customerName?: string | null;
  type: SupportRequestType;
  subject: string;
  status: SupportRequestStatus;
  assigneeId?: number | null;
  assigneeName?: string | null;
  dueAt: string;
  isOverdue: boolean;
  createdAt: string;
  lastMessageAt: string;
  resolvedAt?: string | null;
}

export interface StaffSupportMessageDto {
  supportMessageId: number;
  authorType: SupportMessageAuthorType;
  authorUserId?: number | null;
  authorName?: string | null;
  content: string;
  createdAt: string;
}

export interface SupportApologyCodeDto {
  promotionId: number;
  code: string;
  discountType: PromotionDiscountType;
  discountValue: number;
  maxDiscountAmount?: number | null;
  endsAt?: string | null;
  isUsed: boolean;
}

export interface StaffSupportRequestDto {
  supportRequestId: number;
  orderId: number;
  orderCode: string;
  customerId?: number | null;
  customerName?: string | null;
  customerEmail?: string | null;
  type: SupportRequestType;
  subject: string;
  status: SupportRequestStatus;
  assigneeId?: number | null;
  assigneeName?: string | null;
  dueAt: string;
  isOverdue: boolean;
  createdAt: string;
  updatedAt: string;
  lastMessageAt: string;
  resolvedAt?: string | null;
  apologyCode?: SupportApologyCodeDto | null;
  messages: StaffSupportMessageDto[];
}

export interface AssignSupportRequestBody {
  assigneeId: number | null;
}

export interface ChangeSupportRequestStatusBody {
  status: SupportRequestStatus;
}

export interface StaffReplySupportRequestBody {
  content: string;
  status?: SupportRequestStatus | null;
}

export interface IssueSupportApologyCodeBody {
  discountType: PromotionDiscountType;
  discountValue: number;
  maxDiscountAmount?: number | null;
  validDays?: number | null;
}

export interface RefundProposalListParams {
  status?: ExceptionalRefundStatus;
  orderId?: number;
  proposedByUserId?: number;
  page?: number;
  pageSize?: number;
}

export interface ExceptionalRefundProposalDto {
  exceptionalRefundProposalId: number;
  orderId: number;
  orderCode: string;
  orderLineId?: number | null;
  orderLineDescription?: string | null;
  amount: number;
  reason: string;
  status: ExceptionalRefundStatus;
  requiresAdminApproval: boolean;
  approvalThreshold: number;
  proposedByUserId: number;
  proposedByName: string;
  proposedAt: string;
  reviewedByUserId?: number | null;
  reviewedByName?: string | null;
  reviewedAt?: string | null;
  reviewNote?: string | null;
}

export interface CreateRefundProposalBody {
  orderId: number;
  orderLineId?: number | null;
  amount: number;
  reason: string;
}

export interface ApproveRefundProposalBody {
  note?: string | null;
  cancelTicketLineIds?: number[];
}

export interface RejectRefundProposalBody {
  reason: string;
}
