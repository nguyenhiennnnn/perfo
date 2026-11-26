import { FileDown, FilePlus2, Pencil, Send } from "lucide-react";
import { EmptyState } from "@/components/common/empty-state";
import { QueryState } from "@/components/common/query-state";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { Spinner } from "@/components/common/spinner";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { formatCurrency, formatDateTime } from "@/lib/formatters";
import { useAlertDialogOpen } from "@/stores/alert-dialog-store";
import {
  useAdminRoomRentalQuotes,
  useDownloadAdminRoomRentalQuotePdf,
  useSendRoomRentalQuote,
} from "../hooks/use-admin-room-rentals";
import type { RoomRentalQuoteVersionListItemDto } from "../types";

interface RoomRentalQuoteVersionsProps {
  requestId: number;
  canQuote: boolean;
  contactEmail: string;
  onCompose: () => void;
  onReviseFrom: (version: number) => void;
  onView: (version: number) => void;
}

const VersionRow = ({
  item,
  requestId,
  canQuote,
  contactEmail,
  onReviseFrom,
  onView,
}: {
  item: RoomRentalQuoteVersionListItemDto;
  requestId: number;
  canQuote: boolean;
  contactEmail: string;
  onReviseFrom: (version: number) => void;
  onView: (version: number) => void;
}) => {
  const openAlertDialog = useAlertDialogOpen();
  const sendQuote = useSendRoomRentalQuote(requestId);
  const downloadPdf = useDownloadAdminRoomRentalQuotePdf(requestId);
  const isExpired = new Date(item.validUntil).getTime() <= Date.now();

  const handleSend = () =>
    openAlertDialog({
      title: item.sentAt ? "Gửi lại báo giá?" : "Gửi báo giá cho khách?",
      description: `Phiên bản ${item.versionNumber} sẽ được gửi tới ${contactEmail} kèm liên kết theo dõi yêu cầu.`,
      confirmText: "Gửi",
      onConfirm: async () => {
        await sendQuote.mutateAsync(item.versionNumber);
      },
    });

  return (
    <li className="space-y-2 cut-md bg-card p-3" data-testid={`quote-version-${item.versionNumber}`}>
      <div className="flex flex-wrap items-center gap-2">
        <span className="font-semibold text-foreground">Phiên bản {item.versionNumber}</span>
        {item.isCurrent && <Badge variant="default">Mới nhất</Badge>}
        {item.sentAt ? (
          <Badge variant="success">Đã gửi {formatDateTime(item.sentAt)}</Badge>
        ) : (
          <Badge variant="secondary">Chưa gửi</Badge>
        )}
        {isExpired && <Badge variant="destructive">Hết hạn</Badge>}
      </div>
      <dl className="grid grid-cols-2 gap-x-4 gap-y-1 text-sm sm:grid-cols-3">
        <div>
          <dt className="text-xs text-muted-foreground">Tổng cộng</dt>
          <dd className="font-medium tabular-nums">{formatCurrency(item.total)}</dd>
        </div>
        <div>
          <dt className="text-xs text-muted-foreground">Tiền đặt cọc</dt>
          <dd className="tabular-nums">{formatCurrency(item.depositAmount)}</dd>
        </div>
        <div>
          <dt className="text-xs text-muted-foreground">Hiệu lực đến</dt>
          <dd className="tabular-nums">{formatDateTime(item.validUntil)}</dd>
        </div>
      </dl>
      <div className="flex flex-wrap gap-2">
        <Button size="sm" variant="outline" onClick={() => onView(item.versionNumber)}>
          Xem chi tiết
        </Button>
        <Button
          size="sm"
          variant="outline"
          disabled={downloadPdf.isPending}
          onClick={() => downloadPdf.mutate(item.versionNumber)}
        >
          {downloadPdf.isPending ? <Spinner size="sm" decorative /> : <FileDown className="size-4" />}
          <span>PDF</span>
        </Button>
        {canQuote && (
          <Button size="sm" variant="outline" onClick={() => onReviseFrom(item.versionNumber)}>
            <Pencil className="size-4" />
            <span>Sửa thành bản mới</span>
          </Button>
        )}
        {canQuote && item.isCurrent && !isExpired && (
          <Button size="sm" disabled={sendQuote.isPending} onClick={handleSend}>
            <Send className="size-4" />
            <span>{item.sentAt ? "Gửi lại" : "Gửi cho khách"}</span>
          </Button>
        )}
      </div>
    </li>
  );
};

export const RoomRentalQuoteVersions = ({
  requestId,
  canQuote,
  contactEmail,
  onCompose,
  onReviseFrom,
  onView,
}: RoomRentalQuoteVersionsProps) => {
  const quotes = useAdminRoomRentalQuotes(requestId);

  return (
    <section className="space-y-3">
      <div className="flex items-center justify-between gap-2">
        <h2 className="t-h3 text-foreground">Báo giá</h2>
        {canQuote && (
          <Button size="sm" onClick={onCompose}>
            <FilePlus2 className="size-4" />
            <span>Soạn báo giá</span>
          </Button>
        )}
      </div>

      <QueryState
        query={quotes}
        skeleton={<CardSkeleton className="h-32" />}
        isEmpty={(data) => data.length === 0}
        empty={
          <EmptyState
            icon={FilePlus2}
            title="Chưa có báo giá"
            description={
              canQuote
                ? "Soạn báo giá đầu tiên để gửi cho khách."
                : "Yêu cầu này không còn ở trạng thái có thể báo giá."
            }
            actionLabel={canQuote ? "Soạn báo giá" : undefined}
            onAction={canQuote ? onCompose : undefined}
          />
        }
      >
        {(items) => (
          <ul className="space-y-3">
            {items.map((item) => (
              <VersionRow
                key={item.roomRentalQuoteId}
                item={item}
                requestId={requestId}
                canQuote={canQuote}
                contactEmail={contactEmail}
                onReviseFrom={onReviseFrom}
                onView={onView}
              />
            ))}
          </ul>
        )}
      </QueryState>
    </section>
  );
};
