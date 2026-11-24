import { TriangleAlert } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { QueryState } from "@/components/common/query-state";
import { LineSkeleton } from "@/components/common/skeleton-shapes";

import { useAdminStockAlerts } from "../hooks/use-admin-inventory";
import { formatQuantity } from "../utils/inventory-labels";

export const LowStockAlerts = () => {
  const alerts = useAdminStockAlerts();

  return (
    <QueryState
      query={alerts}
      skeleton={<LineSkeleton className="mb-6 h-16 w-full" />}
      isEmpty={(data) => data.count === 0}
      empty={null}
      errorTitle="Không tải được cảnh báo sắp hết"
      errorClassName="mb-6"
    >
      {(data) => (
        <section aria-label="Cảnh báo sắp hết" className="mb-6 space-y-3 bg-warning/10 p-4 cut-md">
          <div className="flex items-center gap-2 text-warning-text">
            <TriangleAlert className="size-4" />
            <h2 className="t-h3">{data.count} sản phẩm kho sắp hết hoặc đã hết</h2>
          </div>
          <ul className="grid grid-cols-1 gap-3 lg:grid-cols-2">
            {data.items.map((item) => (
              <li key={item.inventoryItemId} className="space-y-1">
                <div className="flex flex-wrap items-center gap-2">
                  <span className="font-medium text-foreground">{item.name}</span>
                  <Badge variant={item.isOutOfStock ? "destructive" : "warning"}>
                    {item.isOutOfStock ? "Hết hàng" : "Sắp hết"}
                  </Badge>
                </div>
                <p className="text-sm tabular-nums text-muted-foreground">
                  Khả dụng {formatQuantity(item.availableQuantity, item.unit)}, ngưỡng{" "}
                  {formatQuantity(item.lowStockThreshold, item.unit)}
                </p>
                {item.affectedCombos.length > 0 && (
                  <p className="text-sm text-muted-foreground">
                    Combo bị ảnh hưởng: {item.affectedCombos.join(", ")}
                  </p>
                )}
              </li>
            ))}
          </ul>
        </section>
      )}
    </QueryState>
  );
};
