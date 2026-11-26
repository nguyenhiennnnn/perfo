import { Check } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { ComboQuantityStepper } from "@/components/common/combo-picker/combo-quantity-stepper";
import { ErrorState } from "@/components/common/error-state";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { formatCurrency, formatPercent } from "@/lib/formatters";
import { cn } from "@/lib/utils";
import type { ShowtimeSeatItemDto } from "@/features/client/showtimes/types";
import type { BundleSelection } from "../hooks/use-bundles";
import type { ApplicableBundleDto } from "../types";
import {
  describeBundleItems,
  getBundleMaxQuantity,
  getBundleSavings,
  getBundleSeatTypeNames,
  getBundleTotal,
} from "../utils/bundle-selection";

const LOW_STOCK_THRESHOLD = 5;

interface BundleOptionProps {
  bundle: ApplicableBundleDto;
  selected: boolean;
  disabled?: boolean;
  onSelect: () => void;
}

const BundleOption = ({ bundle, selected, disabled, onSelect }: BundleOptionProps) => (
  <button
    type="button"
    aria-pressed={selected}
    disabled={disabled}
    onClick={onSelect}
    className={cn(
      "group flex w-full cursor-pointer flex-col gap-2 cut-md p-4 text-left transition-colors disabled:pointer-events-none disabled:opacity-50",
      selected ? "bg-secondary" : "bg-background hover:bg-secondary/60",
    )}
  >
    <span className="flex items-start justify-between gap-3">
      <span className="min-w-0">
        <span className="t-h3 block text-foreground group-hover:underline">{bundle.name}</span>
        {bundle.description && (
          <span className="mt-0.5 line-clamp-2 block text-xs text-muted-foreground">{bundle.description}</span>
        )}
      </span>
      {selected && (
        <span className="flex shrink-0 items-center gap-1 text-xs font-medium text-foreground">
          <Check className="size-4" />
          Đã chọn
        </span>
      )}
    </span>

    <span className="text-sm text-foreground">{describeBundleItems(bundle)}</span>

    <span className="flex flex-wrap items-baseline gap-x-3 gap-y-1">
      <span className="text-lg font-bold tabular-nums text-primary-text">{formatCurrency(bundle.price)}</span>
      <span className="text-xs tabular-nums text-muted-foreground line-through">
        {formatCurrency(bundle.retailTotal)}
      </span>
      <Badge variant="success">
        Tiết kiệm {formatCurrency(bundle.savingsAmount)} ({formatPercent(bundle.savingsPercent)})
      </Badge>
    </span>

    {(bundle.excludesPromotions ||
      (bundle.remainingToday != null && bundle.remainingToday <= LOW_STOCK_THRESHOLD)) && (
      <span className="flex flex-col gap-0.5 text-xs">
        {bundle.remainingToday != null && bundle.remainingToday <= LOW_STOCK_THRESHOLD && (
          <span className="text-warning-text">Hôm nay còn {bundle.remainingToday} gói</span>
        )}
        {bundle.excludesPromotions && (
          <span className="text-muted-foreground">Không dùng chung với mã khuyến mãi</span>
        )}
      </span>
    )}
  </button>
);

interface BundlePickerProps {
  selection: BundleSelection;
  onSelect: (bundleId: number) => void;
  onQuantityChange: (quantity: number) => void;
  seats: ShowtimeSeatItemDto[] | undefined;
  selectedSeatCount: number;
  disabled?: boolean;
  id?: string;
  className?: string;
}

export const BundlePicker = ({
  selection,
  onSelect,
  onQuantityChange,
  seats,
  selectedSeatCount,
  disabled,
  id,
  className,
}: BundlePickerProps) => {
  const { bundles, selectedBundle, quantity, requiredSeats } = selection;

  if (bundles.isLoading) {
    return (
      <section id={id} aria-busy="true" className={cn("flex flex-col gap-3 bg-card p-4", className)}>
        <h3 className="t-h3 text-foreground">Gói sản phẩm</h3>
        <CardSkeleton className="h-28" />
      </section>
    );
  }

  if (bundles.isError && !bundles.data) {
    return (
      <section id={id} className={cn("bg-card p-4", className)}>
        <ErrorState
          title="Không tải được gói sản phẩm"
          error={bundles.error}
          onRetry={() => void bundles.refetch()}
          className="py-6"
        />
      </section>
    );
  }

  if (!bundles.data || bundles.data.length === 0) return null;

  const seatTypeNames = selectedBundle ? getBundleSeatTypeNames(selectedBundle, seats) : [];
  const missingSeats = Math.max(0, requiredSeats - selectedSeatCount);

  return (
    <section id={id} aria-labelledby="bundle-picker-title" className={cn("flex flex-col gap-3 bg-card p-4", className)}>
      <div>
        <h3 id="bundle-picker-title" className="t-h3 text-foreground">
          Gói sản phẩm
        </h3>
        <p className="mt-0.5 text-xs text-muted-foreground">
          Chọn gói trước, sau đó chọn ghế theo số vé của gói. Bỏ qua nếu bạn muốn mua lẻ.
        </p>
      </div>

      <div className="@container">
        <div className="grid grid-cols-1 gap-3 @2xl:grid-cols-2">
          {bundles.data.map((bundle) => (
            <BundleOption
              key={bundle.bundleId}
              bundle={bundle}
              selected={bundle.bundleId === selectedBundle?.bundleId}
              disabled={disabled}
              onSelect={() => onSelect(bundle.bundleId)}
            />
          ))}
        </div>
      </div>

      {selectedBundle && (
        <div className="flex flex-col gap-3 bg-secondary p-4 cut-md">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div>
              <p className="text-sm font-medium text-foreground">Số lượng gói</p>
              <p className="text-xs tabular-nums text-muted-foreground">
                Tổng {formatCurrency(getBundleTotal(selectedBundle, quantity))}, tiết kiệm{" "}
                {formatCurrency(getBundleSavings(selectedBundle, quantity))}
              </p>
            </div>
            <ComboQuantityStepper
              value={quantity}
              min={1}
              max={getBundleMaxQuantity(selectedBundle)}
              onChange={onQuantityChange}
            />
          </div>

          <p className="text-sm text-foreground" role="status">
            {missingSeats > 0
              ? `Chọn thêm ${missingSeats} ghế trên sơ đồ (đã chọn ${selectedSeatCount}/${requiredSeats} ghế).`
              : `Đã chọn đủ ${requiredSeats} ghế cho gói.`}
          </p>

          {seatTypeNames.length > 0 && (
            <p className="text-xs text-muted-foreground">Gói chỉ áp dụng cho ghế {seatTypeNames.join(", ")}.</p>
          )}

          <div>
            <Button variant="outline" size="sm" disabled={disabled} onClick={selection.clear}>
              Bỏ chọn gói
            </Button>
          </div>
        </div>
      )}
    </section>
  );
};
