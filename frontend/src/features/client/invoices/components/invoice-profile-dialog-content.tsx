import { useDialogClose } from "@/stores/dialog-store";
import { useCreateInvoiceProfile, useUpdateInvoiceProfile } from "../hooks/use-invoices";
import type { InvoiceProfileDto } from "../types";
import { InvoiceInfoForm } from "./invoice-info-form";

interface InvoiceProfileDialogContentProps {
  profile?: InvoiceProfileDto;
  onSaved?: (profile: InvoiceProfileDto) => void;
}

export const InvoiceProfileDialogContent = ({ profile, onSaved }: InvoiceProfileDialogContentProps) => {
  const close = useDialogClose();
  const create = useCreateInvoiceProfile();
  const update = useUpdateInvoiceProfile(profile?.invoiceProfileId ?? 0);
  const mutation = profile ? update : create;

  return (
    <InvoiceInfoForm
      defaultValues={profile}
      isPending={mutation.isPending}
      submitLabel={profile ? "Lưu thay đổi" : "Thêm hồ sơ"}
      onCancel={close}
      onSubmit={(body, onServerError) =>
        mutation.mutate(body, {
          onSuccess: (res) => {
            close();
            onSaved?.(res.data);
          },
          onError: onServerError,
        })
      }
    />
  );
};
