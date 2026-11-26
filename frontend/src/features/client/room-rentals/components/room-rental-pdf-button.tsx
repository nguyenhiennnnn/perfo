import { FileDown } from "lucide-react";
import { Spinner } from "@/components/common/spinner";
import { Button } from "@/components/ui/button";
import { useDownloadRoomRentalQuotePdf } from "../hooks/use-room-rental-tracking";

interface RoomRentalPdfButtonProps {
  token: string;
  version: number;
  label?: string;
}

export const RoomRentalPdfButton = ({
  token,
  version,
  label = "Tải PDF",
}: RoomRentalPdfButtonProps) => {
  const { mutate, isPending } = useDownloadRoomRentalQuotePdf(token);

  return (
    <Button
      type="button"
      variant="outline"
      size="sm"
      className="gap-2"
      disabled={isPending}
      onClick={() => mutate(version)}
    >
      {isPending ? <Spinner size="sm" decorative /> : <FileDown />}
      <span>{isPending ? "Đang tải" : label}</span>
    </Button>
  );
};
