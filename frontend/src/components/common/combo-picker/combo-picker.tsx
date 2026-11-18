import { PackageOpen } from "lucide-react";
import { formatCurrency } from "@/lib/formatters";
import { Badge } from "@/components/ui/badge";
import { EmptyState } from "@/components/common/empty-state";
import { ErrorState } from "@/components/common/error-state";
import { SkeletonList } from "@/components/common/skeleton-list";
import { RowSkeleton } from "@/components/common/skeleton-shapes";
import { ComboQuantityStepper } from "./combo-quantity-stepper";

export interface ComboOption {
  comboId: number;
  name: string;
  description?: string | null;
  price: number;
  imageUrl?: string | null;
}

interface ComboPickerProps {
  combos: ComboOption[] | undefined;
  quantities: Record<number, number>;
  onChange: (comboId: number, quantity: number) => void;
  isLoading?: boolean;
  isError?: boolean;
  error?: unknown;
  onRetry?: () => void;
  labels?: Record<number, string>;
}

export const ComboPicker = ({ combos, quantities, onChange, isLoading, isError, error, onRetry, labels }: ComboPickerProps) => {
  if (isLoading) {
    return (
      <div className="@container grid grid-cols-1 gap-3 @sm:grid-cols-2">
        <SkeletonList count={4} shape={RowSkeleton} />
      </div>
    );
  }

  if (isError && !combos) {
    return (
      <ErrorState
        title="Không tải được danh sách combo"
        error={error}
        onRetry={onRetry ?? (() => window.location.reload())}
        className="py-8"
      />
    );
  }

  if (!combos || combos.length === 0) {
    return (
      <EmptyState
        icon={PackageOpen}
        title="Chưa có combo đang bán"
        description="Combo bắp nước sẽ hiện ở đây khi rạp mở bán."
        className="py-8"
      />
    );
  }

  return (
    <div className="@container">
      <div className="grid grid-cols-1 gap-3 @sm:grid-cols-2">
        {combos.map((combo) => (
          <div
            key={combo.comboId}
            className="flex items-center gap-3 bg-card p-3"
          >
            <div className="size-14 shrink-0 overflow-hidden bg-muted">
              {combo.imageUrl ? (
                <img src={combo.imageUrl} alt={combo.name} className="h-full w-full object-cover" />
              ) : (
                <div className="flex h-full w-full items-center justify-center text-muted-foreground">
                  <PackageOpen className="size-5" />
                </div>
              )}
            </div>
            <div className="min-w-0 flex-1">
              <p className="line-clamp-1 text-sm font-medium text-foreground">{combo.name}</p>
              <p className="text-xs font-semibold text-primary-text">{formatCurrency(combo.price)}</p>
              {labels?.[combo.comboId] && (
                <Badge variant="success" className="mt-1">
                  {labels[combo.comboId]}
                </Badge>
              )}
            </div>
            <div className="shrink-0">
              <ComboQuantityStepper
                value={quantities[combo.comboId] ?? 0}
                onChange={(qty) => onChange(combo.comboId, qty)}
              />
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};