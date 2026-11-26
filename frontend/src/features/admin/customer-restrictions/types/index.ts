export interface CustomerRestrictionDto {
  customerRestrictionId: number;
  userId: number;
  customerName: string;
  customerEmail: string;
  reason: string;
  status: string;
  expiredOrderCount: number;
  startedAt: string;
  expiresAt: string;
  liftedAt?: string | null;
  liftedByUserId?: number | null;
  liftedByName?: string | null;
}

export interface CustomerRestrictionListParams {
  status?: string;
  search?: string;
  page?: number;
  pageSize?: number;
}

export const CustomerRestrictionStatusFilter = {
  Active: "Active",
  Lifted: "Lifted",
  Expired: "Expired",
  All: "All",
} as const;
export type CustomerRestrictionStatusFilter =
  (typeof CustomerRestrictionStatusFilter)[keyof typeof CustomerRestrictionStatusFilter];
