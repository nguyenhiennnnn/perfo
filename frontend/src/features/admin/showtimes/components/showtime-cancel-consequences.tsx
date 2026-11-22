import { Banknote, CreditCard, ReceiptText } from "lucide-react";
import type { ReactNode } from "react";

interface Consequence {
  icon: ReactNode;
  text: string;
}

const CONSEQUENCES: Consequence[] = [
  {
    icon: <ReceiptText className="mt-0.5 size-4 shrink-0 text-muted-foreground" />,
    text: "Mọi đơn của suất chiếu (đang chờ thanh toán hoặc đã xác nhận) đều bị hủy và được hoàn đầy đủ, không tính phí hủy.",
  },
  {
    icon: <CreditCard className="mt-0.5 size-4 shrink-0 text-success" />,
    text: "Đơn thanh toán qua cổng được hoàn tự động về đúng phương thức khách đã dùng.",
  },
  {
    icon: <Banknote className="mt-0.5 size-4 shrink-0 text-warning-text" />,
    text: "Đơn thanh toán tiền mặt cần hoàn tiền cho khách tại quầy, sau đó xác nhận đã hoàn trong danh sách đơn tiền mặt cần hoàn.",
  },
];

export const ShowtimeCancelConsequences = () => (
  <ul className="flex flex-col gap-3 cut-sm bg-secondary p-4">
    {CONSEQUENCES.map((item) => (
      <li key={item.text} className="flex items-start gap-3 text-sm text-foreground">
        {item.icon}
        <span>{item.text}</span>
      </li>
    ))}
  </ul>
);
