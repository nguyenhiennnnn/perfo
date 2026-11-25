import { PromotionDiscountType } from "@/types/enum";
import { Badge } from "@/components/ui/badge";
import { formatCurrency, formatDateTime } from "@/lib/formatters";
import type { SupportApologyCodeDto } from "../types";

const describeDiscount = (code: SupportApologyCodeDto) => {
  if (code.discountType === PromotionDiscountType.Percent) {
    const cap = code.maxDiscountAmount ? `, tối đa ${formatCurrency(code.maxDiscountAmount)}` : "";
    return `Giảm ${code.discountValue}%${cap}`;
  }
  return `Giảm ${formatCurrency(code.discountValue)}`;
};

export const SupportApologyCodeCard = ({ code }: { code: SupportApologyCodeDto }) => (
  <div className="space-y-1 cut-sm bg-card p-3" data-testid="support-apology-code">
    <div className="flex flex-wrap items-center justify-between gap-2">
      <span className="t-code text-foreground">{code.code}</span>
      <Badge variant={code.isUsed ? "outline" : "success"}>{code.isUsed ? "Đã dùng" : "Chưa dùng"}</Badge>
    </div>
    <p className="text-sm text-foreground">{describeDiscount(code)}</p>
    {code.endsAt && <p className="text-xs text-muted-foreground">Hết hạn {formatDateTime(code.endsAt)}</p>}
  </div>
);
