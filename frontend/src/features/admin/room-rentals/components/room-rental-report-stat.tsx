import type { ReactNode } from "react";

interface RoomRentalReportStatProps {
  label: string;
  value: ReactNode;
  hint?: string;
}

export const RoomRentalReportStat = ({ label, value, hint }: RoomRentalReportStatProps) => (
  <div className="cut-md bg-card p-4">
    <p className="text-sm text-muted-foreground">{label}</p>
    <p className="t-h2 mt-1 tabular-nums text-foreground">{value}</p>
    {hint && <p className="mt-1 text-xs text-muted-foreground">{hint}</p>}
  </div>
);
