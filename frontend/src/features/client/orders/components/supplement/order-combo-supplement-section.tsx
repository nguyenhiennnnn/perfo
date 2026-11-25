import { useState } from "react";
import { useNavigate } from "react-router";
import { PackageOpen, Popcorn } from "lucide-react";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { ComboQuantityStepper } from "@/components/common/combo-picker/combo-quantity-stepper";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { ROUTES } from "@/config/routes";
import { formatCurrency, formatDateTime } from "@/lib/formatters";
import { OrderStatus, OrderType } from "@/types/enum";
import { useCreateComboSupplement, useOrderComboSuggestions } from "../../hooks/use-orders";
import type { OrderComboSuggestionDto, OrderDetailDto } from "../../types";
import { getSupplementTotal, toSupplementRequest } from "../../utils/order-supplement";

interface SupplementComboRowProps {
  item: OrderComboSuggestionDto;
  quantity: number;
  onChange: (quantity: number) => void;
}

const SupplementComboRow = ({ item, quantity, onChange }: SupplementComboRowProps) => (
  <li className="flex items-center gap-3 py-3">
    <div className="size-14 shrink-0 overflow-hidden bg-muted">
      {item.imageUrl ? (
        <img src={item.imageUrl} alt={item.name} className="h-full w-full object-cover" />
      ) : (
        <div className="flex h-full w-full items-center justify-center text-muted-foreground">
          <PackageOpen className="size-5" aria-hidden />
        </div>
      )}
    </div>
    <div className="min-w-0 flex-1">
      <p className="line-clamp-1 text-sm font-medium text-foreground">{item.name}</p>
      <p className="flex flex-wrap items-baseline gap-x-2 text-xs tabular-nums">
        <span className="font-semibold text-primary-text">{formatCurrency(item.preorderPrice)}</span>
        {item.preorderPrice < item.listPrice && (
          <span className="text-muted-foreground line-through">{formatCurrency(item.listPrice)}</span>
        )}
      </p>
      {(item.matchesTicketCount || item.purchasedBefore) && (
        <div className="mt-1 flex flex-wrap gap-1">
          {item.matchesTicketCount && <Badge variant="success">{item.label}</Badge>}
          {item.purchasedBefore && <Badge variant="secondary">Bạn đã mua trước đây</Badge>}
        </div>
      )}
    </div>
    <ComboQuantityStepper value={quantity} onChange={onChange} />
  </li>
);

interface OrderComboSupplementSectionProps {
  order: OrderDetailDto;
}

export const OrderComboSupplementSection = ({ order }: OrderComboSupplementSectionProps) => {
  const navigate = useNavigate();
  const [quantities, setQuantities] = useState<Record<number, number>>({});

  const isEligible = order.type === OrderType.Ticket && order.status === OrderStatus.Confirmed;
  const suggestions = useOrderComboSuggestions(order.orderId, isEligible);
  const supplement = useCreateComboSupplement(order.orderId);

  if (!isEligible) return null;
  if (suggestions.isLoading) return <CardSkeleton className="h-48" />;

  const data = suggestions.data;
  if (!data || !data.canSupplement || data.items.length === 0) return null;

  const total = getSupplementTotal(data.items, quantities);
  const request = toSupplementRequest(quantities);

  const setQuantity = (comboId: number, quantity: number) =>
    setQuantities((prev) => ({ ...prev, [comboId]: quantity }));

  const handleSubmit = () => {
    if (request.combos.length === 0) return;
    supplement.mutate(request, {
      onSuccess: (res) => navigate(ROUTES.booking.supplement(res.data.orderId)),
    });
  };

  return (
    <section aria-labelledby="order-supplement-title" className="cut-md bg-card p-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h2 id="order-supplement-title" className="t-h2 flex items-center gap-2 text-foreground">
          <Popcorn className="size-5 text-primary-text" aria-hidden />
          Đặt trước combo, nhận tại quầy
        </h2>
        {data.discountPercent > 0 && <Badge variant="success">Giảm {data.discountPercent}%</Badge>}
      </div>
      <p className="mt-1 text-sm text-muted-foreground">
        Combo được thanh toán riêng, vé và ghế của đơn hiện tại không thay đổi. Đưa mã QR của đơn combo tại quầy để
        nhận hàng.
        {data.validUntil && data.discountPercent > 0 && ` Giá ưu đãi đến ${formatDateTime(data.validUntil)}.`}
      </p>

      <ul className="mt-2 divide-y divide-border">
        {data.items.map((item) => (
          <SupplementComboRow
            key={item.comboId}
            item={item}
            quantity={quantities[item.comboId] ?? 0}
            onChange={(quantity) => setQuantity(item.comboId, quantity)}
          />
        ))}
      </ul>

      <div className="mt-3 flex flex-wrap items-center justify-between gap-3">
        <p className="text-sm text-muted-foreground">
          Tạm tính{" "}
          <span data-testid="supplement-total" className="text-lg font-bold tabular-nums text-primary-text">
            {formatCurrency(total)}
          </span>
        </p>
        <Button disabled={request.combos.length === 0 || supplement.isPending} onClick={handleSubmit}>
          {supplement.isPending ? "Đang tạo đơn..." : "Mua thêm combo"}
        </Button>
      </div>
    </section>
  );
};
