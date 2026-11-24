import { cn } from "@/lib/utils";
import { formatNumber } from "@/lib/formatters";

import type { AffectedShowtimesResponse } from "../types";

interface StatProps {
  label: string;
  value: number;
  testId: string;
}

const Stat = ({ label, value, testId }: StatProps) => (
  <div className="flex flex-col gap-1 cut-md bg-secondary p-4">
    <p data-testid={testId} className="t-numeral text-3xl text-foreground tabular-nums">
      {formatNumber(value)}
    </p>
    <p className="text-sm text-muted-foreground">{label}</p>
  </div>
);

interface AffectedShowtimesStatsProps {
  data: Pick<
    AffectedShowtimesResponse,
    "totalShowtimes" | "totalOrders" | "totalCustomers" | "totalTickets"
  >;
  className?: string;
}

export const AffectedShowtimesStats = ({
  data,
  className,
}: AffectedShowtimesStatsProps) => (
  <div className={cn("grid grid-cols-2 gap-3 md:grid-cols-4", className)}>
    <Stat label="Suất chiếu" value={data.totalShowtimes} testId="closure-total-showtimes" />
    <Stat label="Đơn hàng" value={data.totalOrders} testId="closure-total-orders" />
    <Stat label="Khách" value={data.totalCustomers} testId="closure-total-customers" />
    <Stat label="Vé" value={data.totalTickets} testId="closure-total-tickets" />
  </div>
);
