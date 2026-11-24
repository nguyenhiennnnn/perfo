import {
  ImportExportJobStatus,
  ImportExportJobType,
  ImportExportModule,
  MovieStatus,
  OrderChannel,
  OrderStatus,
  OrderType,
  ShowtimeStatus,
} from "@/types/enum";
import { FAILED_LABEL } from "@/config/default-constants";
import { getOrderTypeLabel } from "@/features/client/orders/utils/order-labels";

export interface EnumOption<T extends string = string> {
  value: T;
  label: string;
}

function toOptions<T extends Record<string, string>>(
  enumObj: T,
  labels: Record<T[keyof T], string>,
): EnumOption<T[keyof T]>[] {
  return Object.values(enumObj).map((value) => ({
    value: value as T[keyof T],
    label: labels[value as T[keyof T]],
  }));
}

export const roleOptions: EnumOption[] = [
  { value: "1", label: "Khách hàng" },
  { value: "2", label: "Quản trị viên" },
  { value: "3", label: "Nhân viên" },
];

export const orderStatusOptions = toOptions(OrderStatus, {
  Pending: "Chờ xử lý",
  Confirmed: "Đã xác nhận",
  Cancelled: "Đã hủy",
  Expired: "Hết hạn",
});

export const orderChannelOptions = toOptions(OrderChannel, {
  Online: "Online",
  Counter: "Tại quầy",
});

export const orderTypeOptions: EnumOption<OrderType>[] = Object.values(OrderType).map((value) => ({
  value,
  label: getOrderTypeLabel(value),
}));

export const movieStatusOptions = toOptions(MovieStatus, {
  ComingSoon: "Sắp chiếu",
  NowShowing: "Đang chiếu",
  Ended: "Đã ngừng chiếu",
});

export const showtimeStatusOptions = toOptions(ShowtimeStatus, {
  Scheduled: "Đã lên lịch",
  Cancelled: "Đã hủy",
  Completed: "Đã hoàn thành",
});

export const importExportJobTypeOptions = toOptions(ImportExportJobType, {
  Import: "Nhập dữ liệu",
  Export: "Xuất dữ liệu",
  Rebuild: "Dựng lại",
});

export const importExportJobStatusOptions = toOptions(ImportExportJobStatus, {
  Queued: "Đang chờ",
  Running: "Đang xử lý",
  Completed: "Hoàn thành",
  Failed: FAILED_LABEL,
});

export const importExportModuleOptions = toOptions(ImportExportModule, {
  movies: "Phim",
  showtimes: "Suất chiếu",
  bookings: "Đơn hàng",
  payments: "Giao dịch thanh toán",
  "promotion-redemptions": "Lịch sử dùng khuyến mãi",
});
