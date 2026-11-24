import { Badge } from "@/components/ui/badge";

interface StockStatusBadgeProps {
  availableQuantity: number;
  isLowStock: boolean;
}

export const StockStatusBadge = ({ availableQuantity, isLowStock }: StockStatusBadgeProps) => {
  if (availableQuantity <= 0) return <Badge variant="destructive">Hết hàng</Badge>;
  if (isLowStock) return <Badge variant="warning">Sắp hết</Badge>;
  return <Badge variant="success">Đủ hàng</Badge>;
};
