import { ImportExportJobType } from "@/types/enum";
import type { ImportExportJobDto } from "@/types/import-export";

const MODULE_LABELS: Record<string, string> = {
  movies: "Phim",
  showtimes: "Suất chiếu",
  bookings: "Đơn hàng",
  payments: "Giao dịch thanh toán",
  users: "Người dùng",
  "seat-layout": "Sơ đồ ghế",
  revenue: "Báo cáo doanh thu",
  "top-movies": "Báo cáo top phim",
  "employee-performance": "Báo cáo hiệu suất nhân viên",
  "sales-stats": "Số liệu tổng hợp bán hàng",
};

export const getJobModuleLabel = (module: string) =>
  MODULE_LABELS[module] ?? module;

export const getJobTypeLabel = (type: string) => {
  if (type === ImportExportJobType.Import) return "Nhập";
  if (type === ImportExportJobType.Rebuild) return "Dựng lại";
  return "Xuất";
};

export const getJobTitle = (job: Pick<ImportExportJobDto, "type" | "module">) =>
  `${getJobTypeLabel(job.type)} ${getJobModuleLabel(job.module).toLowerCase()}`;

export const getJobFormatLabel = (format: string) => format.toUpperCase();

export const getJobCreatorLabel = (
  createdByUserId: number,
  currentUserId?: number | null,
) =>
  createdByUserId === currentUserId
    ? "Bạn"
    : `Người dùng #${createdByUserId}`;
