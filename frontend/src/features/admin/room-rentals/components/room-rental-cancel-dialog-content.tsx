import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Field, FieldDescription, FieldLabel } from "@/components/ui/field";
import { Skeleton } from "@/components/ui/skeleton";
import { Textarea } from "@/components/ui/textarea";
import { formatCurrency, formatDateTime } from "@/lib/formatters";
import { useCancelRoomRental, useRoomRentalCancelPreview } from "../hooks/use-admin-room-rentals";
import { ROOM_RENTAL_CANCEL_NOTE_MAX_LENGTH } from "../utils/room-rental-admin-labels";
import type { RoomRentalCancellationResultDto } from "../types";

interface RoomRentalCancelDialogContentProps {
  requestId: number;
  onDone: () => void;
}

const Row = ({ label, value }: { label: string; value: string }) => (
  <div className="flex items-baseline justify-between gap-3 py-1.5 text-sm">
    <dt className="text-muted-foreground">{label}</dt>
    <dd className="text-right font-medium tabular-nums text-foreground">{value}</dd>
  </div>
);

export const RoomRentalCancelDialogContent = ({
  requestId,
  onDone,
}: RoomRentalCancelDialogContentProps) => {
  const [venueIncident, setVenueIncident] = useState(false);
  const [note, setNote] = useState("");
  const [result, setResult] = useState<RoomRentalCancellationResultDto | null>(null);
  const preview = useRoomRentalCancelPreview(requestId, venueIncident);
  const cancel = useCancelRoomRental(requestId);

  if (result) {
    return (
      <div className="space-y-4">
        <p className="text-sm text-foreground">
          Đã hủy yêu cầu <strong>{result.requestCode}</strong>.
          {result.showtimeReleased && " Suất riêng đã được giải phóng khỏi lịch phòng."}
        </p>
        <dl className="divide-y divide-border">
          <Row label="Tỷ lệ hoàn cọc" value={`${result.depositRefundPercent}%`} />
          <Row label="Đã hoàn cho khách" value={formatCurrency(result.refundedAmount)} />
          <Row label="Rạp giữ lại" value={formatCurrency(result.retainedAmount)} />
        </dl>
        <div className="flex justify-end">
          <Button onClick={onDone}>Đóng</Button>
        </div>
      </div>
    );
  }

  const data = preview.data;
  const canSubmit = note.trim().length > 0 && data?.canCancel === true && !cancel.isPending;

  return (
    <div className="space-y-4">
      <Field orientation="horizontal">
        <Checkbox
          id="room-rental-venue-incident"
          checked={venueIncident}
          disabled={cancel.isPending}
          onCheckedChange={(checked) => setVenueIncident(checked === true)}
        />
        <FieldLabel htmlFor="room-rental-venue-incident">Rạp hủy vì sự cố (hoàn đủ toàn bộ)</FieldLabel>
      </Field>

      <section className="cut-md bg-muted/40 p-3" aria-live="polite">
        {preview.isLoading || !data ? (
          <div className="space-y-2">
            <Skeleton className="h-4 w-2/3" />
            <Skeleton className="h-4 w-full" />
            <Skeleton className="h-4 w-full" />
          </div>
        ) : !data.canCancel ? (
          <p className="text-sm text-destructive">
            Không thể hủy theo chính sách: yêu cầu không còn ở giai đoạn thanh toán hoặc suất đã bắt đầu.
          </p>
        ) : (
          <>
            <p className="text-sm text-muted-foreground">
              {data.startAt && <>Suất bắt đầu {formatDateTime(data.startAt)}, còn </>}
              <strong className="tabular-nums text-foreground">{data.hoursBeforeStart} giờ</strong>. Ước tính theo chính sách hủy của báo giá:
            </p>
            <dl className="divide-y divide-border">
              <Row label="Tỷ lệ hoàn cọc" value={`${data.depositRefundPercent}%`} />
              <Row label="Cọc đã thu" value={formatCurrency(data.depositPaid)} />
              <Row label="Thanh toán nốt đã thu" value={formatCurrency(data.balancePaid)} />
              <Row label="Hoàn cho khách" value={formatCurrency(data.estimatedRefund)} />
              <Row label="Rạp giữ lại" value={formatCurrency(data.estimatedRetained)} />
            </dl>
          </>
        )}
      </section>

      <Field>
        <FieldLabel htmlFor="room-rental-cancel-note">Lý do hủy</FieldLabel>
        <Textarea
          id="room-rental-cancel-note"
          value={note}
          rows={3}
          maxLength={ROOM_RENTAL_CANCEL_NOTE_MAX_LENGTH}
          className="min-h-20 resize-none"
          disabled={cancel.isPending}
          onChange={(event) => setNote(event.target.value)}
        />
        <FieldDescription>
          Lý do được lưu vào nhật ký. Tiền hoàn đi qua sổ giao dịch; hoàn qua cổng thanh toán được xử lý nền.
        </FieldDescription>
      </Field>

      <div className="flex justify-end gap-2">
        <Button variant="outline" onClick={onDone} disabled={cancel.isPending}>
          Đóng
        </Button>
        <Button
          variant="destructive"
          disabled={!canSubmit}
          onClick={() =>
            cancel.mutate({ venueIncident, note: note.trim() }, { onSuccess: (res) => setResult(res.data) })
          }
        >
          {cancel.isPending ? "Đang hủy..." : "Hủy yêu cầu"}
        </Button>
      </div>
    </div>
  );
};
