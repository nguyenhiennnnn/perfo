import { useState } from "react";
import { CheckCircle2, FileText } from "lucide-react";
import { InvoiceInfoForm } from "@/features/client/invoices/components/invoice-info-form";
import { useCreateCounterInvoiceRequest } from "@/features/client/invoices/hooks/use-invoices";
import { InvoiceRequestCard } from "@/features/client/invoices/components/invoice-request-card";
import { Button } from "@/components/ui/button";
import { useDialogClose, useDialogOpen } from "@/stores/dialog-store";
import type { InvoiceRequestDto } from "@/features/client/invoices/types";

interface CounterInvoiceDialogContentProps {
  orderId: number;
  onCreated: (request: InvoiceRequestDto) => void;
}

const CounterInvoiceDialogContent = ({ orderId, onCreated }: CounterInvoiceDialogContentProps) => {
  const close = useDialogClose();
  const create = useCreateCounterInvoiceRequest(orderId);

  return (
    <InvoiceInfoForm
      isPending={create.isPending}
      submitLabel="Gửi yêu cầu"
      onCancel={close}
      onSubmit={(body, onServerError) =>
        create.mutate(body, {
          onSuccess: (res) => {
            close();
            onCreated(res.data);
          },
          onError: onServerError,
        })
      }
    />
  );
};

interface CounterInvoiceSectionProps {
  orderId: number;
  orderCode: string;
}

export const CounterInvoiceSection = ({ orderId, orderCode }: CounterInvoiceSectionProps) => {
  const openDialog = useDialogOpen();
  const [created, setCreated] = useState<InvoiceRequestDto | null>(null);

  if (created) {
    return (
      <div className="flex flex-col gap-3" data-testid="counter-invoice-created">
        <p className="flex items-start gap-2 text-sm text-success">
          <CheckCircle2 className="mt-0.5 size-4 shrink-0" />
          <span>Đã ghi nhận yêu cầu xuất hóa đơn. Hóa đơn sẽ gửi tới email của đơn vị sau khi rạp xử lý.</span>
        </p>
        <InvoiceRequestCard request={created} />
      </div>
    );
  }

  const handleOpen = () =>
    openDialog({
      title: "Yêu cầu xuất hóa đơn VAT",
      description: `Đơn ${orderCode} - khách vãng lai`,
      content: <CounterInvoiceDialogContent orderId={orderId} onCreated={setCreated} />,
    });

  return (
    <Button variant="outline" className="w-full sm:w-auto sm:self-start" onClick={handleOpen}>
      <FileText className="size-4" />
      <span>Yêu cầu hóa đơn VAT</span>
    </Button>
  );
};
