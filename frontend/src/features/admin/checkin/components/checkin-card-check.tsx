import { IdCard, ShieldAlert, ShieldCheck } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { FORM_PROCESSING_LABEL } from "@/config/default-constants";
import { useDialogOpen } from "@/stores/dialog-store";
import { useRecordCardNotPresented } from "../hooks/use-checkin";
import { CardCheckStatus, type CheckinResponse } from "../types";
import { SupplementOrderDialogContent } from "./supplement-order-dialog";

interface CheckinCardCheckProps {
  result: CheckinResponse;
  onStatusChange: (status: CardCheckStatus) => void;
  onAcknowledge: () => void;
}

export const CheckinCardCheck = ({ result, onStatusChange, onAcknowledge }: CheckinCardCheckProps) => {
  const openDialog = useDialogOpen();
  const recordNotPresented = useRecordCardNotPresented();

  if (!result.requiresCardPresentation || result.cardCheckStatus === CardCheckStatus.NotRequired) {
    return null;
  }

  const seatCode = `${result.seatRow}${result.seatNumber}`;
  const typeName = result.ticketTypeName ?? "ưu đãi";

  const handleNotPresented = async () => {
    try {
      const res = await recordNotPresented.mutateAsync(result.orderLineId);
      onStatusChange(res.data.cardCheckStatus);
    } catch {
      return;
    }
  };

  const openSupplement = () =>
    openDialog({
      title: "Thu chênh lệch giá vé",
      size: "md",
      content: (
        <SupplementOrderDialogContent
          ticket={result}
          onPaid={() => onStatusChange(CardCheckStatus.Supplemented)}
        />
      ),
    });

  if (result.cardCheckStatus === CardCheckStatus.Supplemented) {
    return (
      <Card role="status" className="flex items-center gap-3 p-4 text-sm">
        <ShieldCheck className="size-5 shrink-0 text-success" />
        <p className="text-foreground">Đã thu chênh lệch cho ghế {seatCode}. Không cần kiểm tra thẻ nữa.</p>
      </Card>
    );
  }

  if (result.cardCheckStatus === CardCheckStatus.NotPresented) {
    return (
      <Card role="alert" className="flex flex-col gap-3 p-4">
        <div className="flex items-start gap-3">
          <ShieldAlert className="mt-0.5 size-5 shrink-0 text-warning" />
          <div className="text-sm">
            <p className="font-semibold text-foreground">Khách không có thẻ ({typeName})</p>
            <p className="text-muted-foreground">
              Ghế {seatCode} cần thu chênh lệch lên giá vé thường trước khi khách vào rạp.
            </p>
          </div>
        </div>
        <Button onClick={openSupplement}>Tạo đơn thu chênh lệch</Button>
      </Card>
    );
  }

  return (
    <Card role="alert" className="flex flex-col gap-3 border-warning p-4">
      <div className="flex items-start gap-3">
        <IdCard className="mt-0.5 size-6 shrink-0 text-warning" />
        <div className="text-sm">
          <p className="t-h3 text-foreground">Kiểm tra thẻ trước khi cho vào</p>
          <p className="text-muted-foreground">
            Vé ghế {seatCode} là loại <span className="font-medium text-foreground">{typeName}</span>.
            Yêu cầu khách xuất trình thẻ phù hợp.
          </p>
        </div>
      </div>
      <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
        <Button onClick={onAcknowledge} disabled={recordNotPresented.isPending}>
          <ShieldCheck className="size-4" />
          <span>Đã kiểm tra thẻ</span>
        </Button>
        <Button
          variant="outline"
          onClick={() => void handleNotPresented()}
          disabled={recordNotPresented.isPending}
        >
          <span>{recordNotPresented.isPending ? FORM_PROCESSING_LABEL : "Không có thẻ"}</span>
        </Button>
      </div>
    </Card>
  );
};
