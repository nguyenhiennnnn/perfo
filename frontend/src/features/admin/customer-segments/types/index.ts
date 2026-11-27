export interface SegmentConditionNode {
  operator?: string | null;
  items?: SegmentConditionNode[] | null;
  field?: string | null;
  op?: string | null;
  value?: string | null;
  values?: string[] | null;
}

export interface CustomerSegmentDto {
  segmentId: number;
  name: string;
  description: string | null;
  conditions: SegmentConditionNode;
  isActive: boolean;
  customerCount: number;
  eligibleCount: number;
  createdAt: string;
  updatedAt: string | null;
}

export interface UpsertCustomerSegmentRequest {
  name: string;
  description: string | null;
  conditions: SegmentConditionNode;
  isActive: boolean;
}

export interface DeleteCustomerSegmentResponse {
  id: number;
  deleted: boolean;
}

export interface CustomerSegmentListParams {
  page?: number;
  pageSize?: number;
  search?: string;
  isActive?: boolean;
}
