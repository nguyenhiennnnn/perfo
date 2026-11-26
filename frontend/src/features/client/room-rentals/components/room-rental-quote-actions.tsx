import { useState } from "react";
import { Check, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useAlertDialogStore } from "@/stores/alert-dialog-store";
import { formatCurrency } from "@/lib/formatters";
import {
  useAcceptRoomRentalQuote,
  useDeclineRoomRentalQuote,
} from "../hooks/use-room-rental-tracking";
import type { RoomRentalTrackingQuoteDto } from "../types";
import { RoomRentalDeclineDialog } from "./room-rental-decline-dialog";

interface RoomRentalQuoteActionsProps {
  quote: RoomRentalTrackingQuoteDto;
  token: string;
}

export const RoomRentalQuoteActions = ({ quote, token }: RoomRentalQuoteActionsProps) => {
  const [declineOpen, setDeclineOpen] = useState(false);
  const openAlert = useAlertDialogStore((state) => state.open);
  const accept = useAcceptRoomRentalQuote(token);
  const decline = useDeclineRoomRentalQuote(token);

  const handleAccept = () =>
    openAlert({
      title: "Chấp nhận báo giá?",
      description: `Bạn chấp nhận báo giá phiên bản ${quote.versionNumber} với tổng ${formatCurrency(quote.total)}. Sau đó bạn cần đặt cọc ${formatCurrency(quote.depositAmount)} để rạp giữ phòng.`,
      confirmText: "Chấp nhận báo giá",
      onConfirm: async () => {
        await accept.mutateAsync(quote.versionNumber);
      },
    });

  const handleDecline = (reason: string | null) =>
    decline.mutate(
      { version: quote.versionNumber, reason },
      { onSuccess: () => setDeclineOpen(false) },
    );

  return (
    <>
      <div className="fixed inset-x-0 bottom-0 z-30 border-t border-border bg-background px-4 pt-3 pb-[max(0.75rem,env(safe-area-inset-bottom))] sm:static sm:z-auto sm:border-0 sm:bg-transparent sm:p-0">
        <div className="mx-auto flex max-w-2xl flex-col gap-2 sm:max-w-none sm:flex-row-reverse">
          <Button type="button" className="h-11 flex-1 gap-2 sm:h-9" onClick={handleAccept}>
            <Check />
            Chấp nhận báo giá
          </Button>
          <Button
            type="button"
            variant="outline"
            className="h-11 flex-1 gap-2 sm:h-9"
            onClick={() => setDeclineOpen(true)}
          >
            <X />
            Từ chối
          </Button>
        </div>
      </div>

      <RoomRentalDeclineDialog
        key={declineOpen ? "open" : "closed"}
        open={declineOpen}
        versionNumber={quote.versionNumber}
        isPending={decline.isPending}
        onOpenChange={setDeclineOpen}
        onSubmit={handleDecline}
      />
    </>
  );
};
