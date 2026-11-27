import { Gift } from "lucide-react";
import {
  StandaloneOrderPaymentStep,
  type StandaloneOrderPaymentStepProps,
} from "@/features/client/orders/components/booking/standalone-order-payment-step";

export const GiftCardPaymentStep = (props: StandaloneOrderPaymentStepProps) => (
  <StandaloneOrderPaymentStep {...props} icon={Gift} cancelLabel="Hủy đơn, nhập lại thông tin" />
);
