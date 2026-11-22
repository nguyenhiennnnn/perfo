import type { OrderListItemDto } from "@/features/client/orders/types";

export type {
  CancelOrderRefundDto,
  CancelOrderRequest,
  CancelOrderResponse,
  OrderComboDto,
  OrderDetailDto,
  OrderEmployeeBriefDto,
  OrderLineDto,
  OrderPaymentSummaryDto,
  OrderPriceAdjustmentDto,
  OrderRefundStatus,
  OrderTicketDto,
  OrderTransactionDto,
} from "@/features/client/orders/types";

export type AdminOrderListItemDto = OrderListItemDto;

export interface AdminOrderListParams {
  status?: string;
  channel?: string;
  type?: string;
  showtimeId?: number;
  fromDate?: string;
  toDate?: string;
  search?: string;
  page?: number;
  pageSize?: number;
}
