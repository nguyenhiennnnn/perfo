import type { DailyClosingSnapshot } from "../types";

export interface ClosingBlocker {
  key: "dayNotEnded" | "openShifts" | "problemOrders";
  message: string;
  overridable: boolean;
}

export const getClosingBlockers = (
  snapshot: DailyClosingSnapshot,
  dayNotEnded: boolean,
): ClosingBlocker[] => {
  const blockers: ClosingBlocker[] = [];
  if (dayNotEnded) {
    blockers.push({
      key: "dayNotEnded",
      message: "Ngày kinh doanh này chưa kết thúc nên chưa thể chốt.",
      overridable: false,
    });
  }
  if (snapshot.cash.openShiftCount > 0) {
    blockers.push({
      key: "openShifts",
      message: `Còn ${snapshot.cash.openShiftCount} ca quầy chưa đóng. Hãy đóng hoặc đóng thay ca trước khi chốt.`,
      overridable: true,
    });
  }
  if (snapshot.orders.problemOrders > 0) {
    blockers.push({
      key: "problemOrders",
      message: `Còn ${snapshot.orders.problemOrders} đơn lỗi cần xử lý.`,
      overridable: true,
    });
  }
  return blockers;
};
