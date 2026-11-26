export interface AdminBundleItemDto {
  bundleItemId: number;
  ticketTypeId: number | null;
  ticketTypeName: string | null;
  comboId: number | null;
  comboName: string | null;
  quantity: number;
}

export interface AdminBundleDto {
  bundleId: number;
  name: string;
  description: string | null;
  price: number;
  items: AdminBundleItemDto[];
  validFrom: string | null;
  validTo: string | null;
  movieIds: number[];
  showtimeIds: number[];
  daysOfWeek: number[];
  seatTypeIds: number[];
  allowedFromTime: string | null;
  allowedToTime: string | null;
  dailyLimit: number | null;
  excludesPromotions: boolean;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface UpsertBundleItemRequest {
  ticketTypeId: number | null;
  comboId: number | null;
  quantity: number;
}

export interface UpsertBundleRequest {
  name: string;
  description: string | null;
  price: number;
  items: UpsertBundleItemRequest[];
  validFrom: string | null;
  validTo: string | null;
  movieIds: number[];
  showtimeIds: number[];
  daysOfWeek: number[];
  seatTypeIds: number[];
  allowedFromTime: string | null;
  allowedToTime: string | null;
  dailyLimit: number | null;
  excludesPromotions: boolean;
  isActive: boolean;
}

export interface DeleteBundleResponse {
  id: number;
  deleted: boolean;
}

export interface BundleListParams {
  page?: number;
  pageSize?: number;
  search?: string;
  isActive?: boolean;
}
