import { useState } from "react";
import { ChevronDown } from "lucide-react";
import { Button } from "@/components/ui/button";
import { formatDateTime } from "@/lib/formatters";
import { cn } from "@/lib/utils";
import type { RoomRentalTrackingDto } from "../types";
import { ROOM_RENTAL_AUDIENCE_LABELS } from "../utils/room-rental-labels";

interface RoomRentalRequestSummaryProps {
  tracking: RoomRentalTrackingDto;
}

interface RowProps {
  label: string;
  value: string | null;
}

const Row = ({ label, value }: RowProps) =>
  value ? (
    <div className="flex flex-col gap-0.5">
      <dt className="text-xs text-muted-foreground">{label}</dt>
      <dd className="whitespace-pre-line text-sm text-foreground">{value}</dd>
    </div>
  ) : null;

export const RoomRentalRequestSummary = ({ tracking }: RoomRentalRequestSummaryProps) => {
  const [isOpen, setIsOpen] = useState(false);
  const flexible =
    tracking.flexibleFromAt && tracking.flexibleToAt
      ? `${formatDateTime(tracking.flexibleFromAt)} đến ${formatDateTime(tracking.flexibleToAt)}`
      : null;
  const content = tracking.bringsOwnContent
    ? `Tự mang nội dung${tracking.contentNote ? `: ${tracking.contentNote}` : ""}`
    : tracking.movieTitle;

  return (
    <section className="cut-md flex flex-col gap-3 bg-card p-4" aria-label="Thông tin yêu cầu">
      <div className="flex items-center justify-between gap-3">
        <h2 className="t-h3 text-foreground">Thông tin yêu cầu</h2>
        <Button
          type="button"
          variant="ghost"
          size="sm"
          aria-expanded={isOpen}
          aria-label={isOpen ? "Thu gọn thông tin yêu cầu" : "Xem thông tin yêu cầu"}
          onClick={() => setIsOpen((value) => !value)}
        >
          <ChevronDown className={cn("transition-transform", isOpen && "rotate-180")} />
        </Button>
      </div>
      {isOpen && (
        <dl className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <Row label="Người liên hệ" value={tracking.fullName} />
          <Row label="Đơn vị" value={tracking.organizationName} />
          <Row label="Loại đối tượng" value={ROOM_RENTAL_AUDIENCE_LABELS[tracking.audienceType]} />
          <Row label="Số người" value={`${tracking.attendeeCount} người`} />
          <Row label="Ngày giờ mong muốn" value={formatDateTime(tracking.desiredStartAt)} />
          <Row label="Khoảng thời gian dự phòng" value={flexible} />
          <Row label="Nội dung chiếu" value={content} />
          <Row label="Định dạng phòng" value={tracking.screenTypeName} />
          <Row label="Combo" value={tracking.comboRequest} />
          <Row label="Yêu cầu khác" value={tracking.notes} />
          <Row label="Gửi lúc" value={formatDateTime(tracking.createdAt)} />
        </dl>
      )}
    </section>
  );
};
