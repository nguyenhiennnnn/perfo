import {
  addDays,
  addMonths,
  addWeeks,
  endOfMonth,
  endOfWeek,
  startOfMonth,
  startOfWeek,
} from "date-fns";
import { getBusinessTodayDate, toWallDate } from "@/lib/business-time";
import { toDateInputValue } from "@/lib/formatters";
import type { FinanceReportPeriod } from "@/hooks/use-query-params";

const WEEK_OPTIONS = { weekStartsOn: 1 } as const;

export const FINANCE_PERIOD_OPTIONS: { value: FinanceReportPeriod; label: string }[] = [
  { value: "day", label: "Ngày" },
  { value: "week", label: "Tuần" },
  { value: "month", label: "Tháng" },
  { value: "custom", label: "Tùy chọn" },
];

export const resolveFinanceRange = (
  period: FinanceReportPeriod,
  date: string | null,
  from: string | null,
  to: string | null,
) => {
  const today = getBusinessTodayDate();
  const anchor = toWallDate(date) ?? today;
  const fmt = (d: Date) => toDateInputValue(d) ?? "";

  switch (period) {
    case "day":
      return { anchor: fmt(anchor), from: fmt(anchor), to: fmt(anchor) };
    case "week":
      return {
        anchor: fmt(anchor),
        from: fmt(startOfWeek(anchor, WEEK_OPTIONS)),
        to: fmt(endOfWeek(anchor, WEEK_OPTIONS)),
      };
    case "month":
      return {
        anchor: fmt(anchor),
        from: fmt(startOfMonth(anchor)),
        to: fmt(endOfMonth(anchor)),
      };
    default: {
      const defaultFrom = fmt(startOfMonth(today));
      return { anchor: fmt(anchor), from: from ?? defaultFrom, to: to ?? fmt(today) };
    }
  }
};

export const shiftFinanceAnchor = (
  period: FinanceReportPeriod,
  anchor: string,
  direction: 1 | -1,
) => {
  const base = toWallDate(anchor) ?? getBusinessTodayDate();
  const next =
    period === "day"
      ? addDays(base, direction)
      : period === "week"
        ? addWeeks(base, direction)
        : addMonths(base, direction);
  return toDateInputValue(next) ?? anchor;
};
