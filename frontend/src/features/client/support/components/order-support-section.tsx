import { LifeBuoy } from "lucide-react";
import { Button } from "@/components/ui/button";
import { ButtonLink } from "@/components/common/button-link";
import { ROUTES } from "@/config/routes";
import type { OrderDetailDto } from "@/features/client/orders/types";
import { useDialogOpen } from "@/stores/dialog-store";
import { SupportRequestDialogContent } from "./support-request-dialog-content";

interface OrderSupportSectionProps {
  order: OrderDetailDto;
}

export const OrderSupportSection = ({ order }: OrderSupportSectionProps) => {
  const openDialog = useDialogOpen();

  const openCreate = () =>
    openDialog({
      title: "Gửi yêu cầu hỗ trợ",
      description: `Yêu cầu sẽ gắn với đơn ${order.orderCode}.`,
      content: <SupportRequestDialogContent orderId={order.orderId} orderStatus={order.status} />,
    });

  return (
    <section aria-labelledby="order-support-title" className="space-y-3" data-testid="order-support-section">
      <div className="flex items-center gap-2">
        <LifeBuoy className="size-5 text-muted-foreground" />
        <h2 id="order-support-title" className="t-h2 text-foreground">
          Hỗ trợ
        </h2>
      </div>
      <div className="space-y-3 cut-md bg-card p-4">
        <p className="text-sm text-muted-foreground">
          Không nhận được email vé, bị trừ tiền nhưng đơn chưa xác nhận hoặc cần rạp xem xét thêm? Gửi yêu cầu
          và theo dõi phản hồi ngay tại đây.
        </p>
        <div className="flex flex-wrap gap-2">
          <Button onClick={openCreate}>
            <LifeBuoy className="size-4" />
            Gửi yêu cầu hỗ trợ
          </Button>
          <ButtonLink to={ROUTES.account.supportRequests} variant="outline">
            Xem yêu cầu của tôi
          </ButtonLink>
        </div>
      </div>
    </section>
  );
};
