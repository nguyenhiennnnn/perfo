import type {
  SupportMessageAuthorType,
  SupportRequestStatus,
  SupportRequestType,
} from "@/types/enum";

export interface CreateSupportRequestRequest {
  orderId: number;
  type: SupportRequestType;
  subject: string;
  content: string;
}

export interface ReplySupportRequestRequest {
  content: string;
}

export interface SupportRequestListParams {
  status?: SupportRequestStatus;
  page?: number;
  pageSize?: number;
}

export interface SupportRequestListItemDto {
  supportRequestId: number;
  orderId: number;
  orderCode: string;
  type: SupportRequestType;
  subject: string;
  status: SupportRequestStatus;
  dueAt: string;
  createdAt: string;
  lastMessageAt: string;
  resolvedAt?: string | null;
}

export interface SupportMessageDto {
  supportMessageId: number;
  authorType: SupportMessageAuthorType;
  authorName?: string | null;
  content: string;
  createdAt: string;
}

export interface SupportRequestDto {
  supportRequestId: number;
  orderId: number;
  orderCode: string;
  type: SupportRequestType;
  subject: string;
  status: SupportRequestStatus;
  assigneeName?: string | null;
  dueAt: string;
  createdAt: string;
  updatedAt: string;
  lastMessageAt: string;
  resolvedAt?: string | null;
  messages: SupportMessageDto[];
}
