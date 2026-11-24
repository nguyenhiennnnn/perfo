import { History } from "lucide-react";
import { formatDateTime } from "@/lib/formatters";
import type { ComboPickupHistoryDto } from "../types";

interface ComboPickupHistoryProps {
  history: ComboPickupHistoryDto[];
}

export const ComboPickupHistory = ({ history }: ComboPickupHistoryProps) => (
  <section aria-label="Lịch sử nhận combo" className="flex flex-col gap-2">
    <div className="flex items-center gap-2 text-sm font-medium text-foreground">
      <History className="size-4 text-muted-foreground" />
      <h4>Lịch sử nhận combo</h4>
    </div>
    {history.length === 0 ? (
      <p className="text-sm text-muted-foreground">Chưa có lần nhận nào.</p>
    ) : (
      <ul className="flex flex-col divide-y divide-border">
        {history.map((entry) => (
          <li
            key={entry.comboPickupId}
            className="flex flex-wrap items-center justify-between gap-x-3 gap-y-1 py-2 text-sm"
          >
            <span className="min-w-0 text-foreground">
              <span className="wrap-break-word">{entry.comboName ?? "Combo"}</span>
              <span className="ml-1 tabular-nums text-muted-foreground">× {entry.quantity}</span>
            </span>
            <span className="text-xs text-muted-foreground">
              {formatDateTime(entry.pickedUpAt)}
              {entry.staffName ? ` · ${entry.staffName}` : ""}
            </span>
          </li>
        ))}
      </ul>
    )}
  </section>
);
