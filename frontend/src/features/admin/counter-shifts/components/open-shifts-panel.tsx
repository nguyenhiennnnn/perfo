import { TriangleAlert } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { formatCurrency, formatDateTime } from "@/lib/formatters";
import { useAdminOpenCounterShifts } from "../hooks/use-admin-counter-shifts";
import type { CounterShift } from "../types";

interface OpenShiftsPanelProps {
  onCloseByAdmin: (shift: CounterShift) => void;
}

export const OpenShiftsPanel = ({ onCloseByAdmin }: OpenShiftsPanelProps) => {
  const { data } = useAdminOpenCounterShifts();
  if (!data || data.length === 0) return null;

  return (
    <section className="mb-4 space-y-3 bg-warning/10 p-4" aria-label="Ca quầy đang mở">
      <div className="flex items-center gap-2 text-sm font-medium text-foreground">
        <TriangleAlert className="size-4 text-warning" />
        <span>{data.length} ca quầy đang mở</span>
      </div>
      <ul className="space-y-2">
        {data.map(({ shift, isOverdue }) => (
          <li
            key={shift.counterShiftId}
            className="flex flex-col gap-2 bg-card p-3 sm:flex-row sm:items-center sm:justify-between"
          >
            <div className="space-y-0.5 text-sm">
              <p className="flex flex-wrap items-center gap-2 font-medium text-foreground">
                {shift.employeeName}
                {isOverdue ? <Badge variant="destructive">Quá ngày kinh doanh</Badge> : null}
              </p>
              <p className="text-muted-foreground">
                Mở lúc {formatDateTime(shift.openedAt)}, tiền đầu ca{" "}
                <span className="tabular-nums">{formatCurrency(shift.openingCash)}</span>
              </p>
            </div>
            <Button variant="outline" size="sm" onClick={() => onCloseByAdmin(shift)}>
              Đóng thay ca
            </Button>
          </li>
        ))}
      </ul>
    </section>
  );
};
