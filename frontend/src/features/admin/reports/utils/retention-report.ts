export const RETENTION_MONTH_OFFSETS = [1, 2, 3] as const;
export const RETENTION_DEFAULT_MONTHS = 12;
export const RETENTION_MAX_RATE_SHADE = 45;

export const RETENTION_RANGE_OPTIONS = [
  { value: "6", label: "6 tháng gần nhất" },
  { value: "12", label: "12 tháng gần nhất" },
  { value: "24", label: "24 tháng gần nhất" },
  { value: "36", label: "36 tháng gần nhất" },
];

export const normalizeRetentionMonths = (months: number) =>
  RETENTION_RANGE_OPTIONS.some((option) => Number(option.value) === months)
    ? months
    : RETENTION_DEFAULT_MONTHS;

export const formatCohortMonth = (isoDate: string) =>
  `${isoDate.slice(5, 7)}/${isoDate.slice(0, 4)}`;

export const getMonthOffsetLabel = (offset: number) => `Tháng thứ ${offset}`;

export const getRateShade = (ratePercent: number) =>
  Math.round(Math.min(Math.max(ratePercent, 0), 100) * (RETENTION_MAX_RATE_SHADE / 100));
