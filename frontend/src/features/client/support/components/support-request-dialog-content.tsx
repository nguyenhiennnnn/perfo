import { useNavigate } from "react-router";
import { ROUTES } from "@/config/routes";
import { useDialogClose } from "@/stores/dialog-store";
import { OrderStatus, SupportRequestType } from "@/types/enum";
import { useCreateSupportRequest } from "../hooks/use-support-requests";
import { toCreateSupportRequest } from "../utils/support-form";
import { SupportRequestForm } from "./support-request-form";

interface SupportRequestDialogContentProps {
  orderId: number;
  orderStatus: string;
}

export const SupportRequestDialogContent = ({
  orderId,
  orderStatus,
}: SupportRequestDialogContentProps) => {
  const close = useDialogClose();
  const navigate = useNavigate();
  const create = useCreateSupportRequest();

  return (
    <SupportRequestForm
      defaultType={
        orderStatus === OrderStatus.Pending ? SupportRequestType.ChargedOrderNotConfirmed : ""
      }
      isPending={create.isPending}
      onCancel={close}
      onSubmit={(values, onServerError) =>
        create.mutate(toCreateSupportRequest(orderId, values), {
          onSuccess: (res) => {
            close();
            void navigate(ROUTES.account.supportRequestDetail(res.data.supportRequestId));
          },
          onError: onServerError,
        })
      }
    />
  );
};
