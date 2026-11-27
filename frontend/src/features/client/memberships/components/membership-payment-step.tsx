import { Crown } from "lucide-react";
import {
  StandaloneOrderPaymentStep,
  type StandaloneOrderPaymentStepProps,
} from "@/features/client/orders/components/booking/standalone-order-payment-step";

export const MembershipPaymentStep = (props: StandaloneOrderPaymentStepProps) => (
  <StandaloneOrderPaymentStep {...props} icon={Crown} cancelLabel="Hủy đơn, chọn lại gói" />
);
