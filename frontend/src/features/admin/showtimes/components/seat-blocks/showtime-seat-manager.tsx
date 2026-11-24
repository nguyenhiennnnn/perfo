import { useCallback, useMemo, useState } from "react";
import { CalendarX, LockOpen, X } from "lucide-react";

import { NotFoundState } from "@/components/common/not-found-state";
import { QueryState } from "@/components/common/query-state";
import { SelectableSeatMap } from "@/components/common/seat-map/selectable-seat-map";
import { formatSeatCodes } from "@/components/common/seat-map/seat-map-utils";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { Button } from "@/components/ui/button";
import { toast } from "@/components/ui/toast";
import { useRealtimeSeatMap } from "@/hooks/use-realtime-seat-map";
import { showApiErrorToast } from "@/lib/error";
import { formatDateTime } from "@/lib/formatters";
import { useAlertDialogOpen } from "@/stores/alert-dialog-store";
import { ShowtimeSeatStatus } from "@/types/enum";

import { SEAT_BLOCK_MAX_SEATS } from "../../config/seat-block-config";
import {
  useManagedShowtimeSeats,
  useSeatBlocks,
  useUnblockSeats,
} from "../../hooks/use-seat-blocks";
import {
  describeSeatBlockConflict,
  describeSeatManageNote,
  resolveSeatManageKind,
  toggleManageSelection,
  type SeatManageKind,
} from "../../utils/seat-blocks";
import { SeatBlockForm } from "./seat-block-form";
import { SeatBlockList } from "./seat-block-list";

interface ShowtimeSeatManagerProps {
  showtimeId: number;
}

const EMPTY_IDS: Set<number> = new Set();

export const ShowtimeSeatManager = ({ showtimeId }: ShowtimeSeatManagerProps) => {
  const seatsQuery = useManagedShowtimeSeats(showtimeId);
  const blocksQuery = useSeatBlocks(showtimeId);
  const unblockSeats = useUnblockSeats();
  const openAlert = useAlertDialogOpen();

  const [selectedIds, setSelectedIds] = useState<Set<number>>(EMPTY_IDS);
  const [issueIds, setIssueIds] = useState<Set<number>>(EMPTY_IDS);

  const { refetch: refetchBlocks } = blocksQuery;
  const refreshBlocks = useCallback(() => void refetchBlocks(), [refetchBlocks]);

  const seats = useMemo(() => seatsQuery.data?.seats ?? [], [seatsQuery.data]);
  const statusMap = useRealtimeSeatMap({
    showtimeId,
    initialSeats: seatsQuery.data?.seats,
    onSeatsBlocked: refreshBlocks,
    onSeatsReleased: refreshBlocks,
  });

  const blockBySeatId = useMemo(
    () => new Map((blocksQuery.data ?? []).map((block) => [block.showtimeSeatId, block])),
    [blocksQuery.data],
  );

  const kindOf = useCallback(
    (showtimeSeatId: number): SeatManageKind | null => {
      const status = statusMap.get(showtimeSeatId);
      return status ? resolveSeatManageKind(status, blockBySeatId.get(showtimeSeatId)) : null;
    },
    [statusMap, blockBySeatId],
  );

  const selectableIds = useMemo(
    () => new Set(seats.map((s) => s.showtimeSeatId).filter((id) => kindOf(id) === "unblock")),
    [seats, kindOf],
  );

  const { effectiveIds, selectionKind } = useMemo(() => {
    const valid = [...selectedIds].filter((id) => kindOf(id) !== null);
    const kind = valid.length > 0 ? kindOf(valid[0]) : null;
    return {
      effectiveIds: new Set(valid.filter((id) => kindOf(id) === kind)),
      selectionKind: kind,
    };
  }, [selectedIds, kindOf]);

  const codesOf = (ids: Set<number>) =>
    formatSeatCodes(seats.filter((seat) => ids.has(seat.showtimeSeatId)));

  const handleToggle = (seat: { showtimeSeatId: number }) => {
    const { next, result } = toggleManageSelection(
      effectiveIds,
      seats,
      kindOf,
      seat.showtimeSeatId,
      SEAT_BLOCK_MAX_SEATS,
    );
    if (result === "limit") {
      toast.add({
        title: `Mỗi lần xử lý tối đa ${SEAT_BLOCK_MAX_SEATS} ghế.`,
        type: "warning",
      });
      return;
    }
    setSelectedIds(next);
  };

  const refreshAll = () => {
    void seatsQuery.refetch();
    void blocksQuery.refetch();
  };

  const handleBlocked = () => {
    setSelectedIds(EMPTY_IDS);
    setIssueIds(EMPTY_IDS);
  };

  const handleConflict = (error: unknown, conflictSeatIds: number[]) => {
    const hasOrder = conflictSeatIds.some(
      (id) => statusMap.get(id) !== ShowtimeSeatStatus.Blocked,
    );
    setIssueIds(new Set(conflictSeatIds));
    toast.add({
      title: "Không khóa được ghế",
      description: describeSeatBlockConflict(error, hasOrder),
      type: "error",
    });
    refreshAll();
  };

  const handleUnblock = () => {
    const ids = [...effectiveIds];
    openAlert({
      title: "Mở khóa ghế",
      description: `Mở khóa ${codesOf(effectiveIds).join(", ")}. Ghế trở về trạng thái trống và khách có thể đặt ngay.`,
      confirmText: `Mở khóa ${ids.length} ghế`,
      onConfirm: async () => {
        try {
          await unblockSeats.mutateAsync({ showtimeId, body: { showtimeSeatIds: ids } });
          setSelectedIds(EMPTY_IDS);
        } catch (error) {
          showApiErrorToast(error, { title: "Không mở khóa được ghế" });
          refreshAll();
        }
      },
    });
  };

  const selectedCodes = codesOf(effectiveIds);

  return (
    <div className="flex flex-col gap-6">
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
        <div className="lg:col-span-2">
          <QueryState
            query={seatsQuery}
            skeleton={<CardSkeleton cut="lg" className="h-96" />}
            notFound={<NotFoundState entity="suất chiếu" icon={CalendarX} />}
            errorTitle="Không tải được sơ đồ ghế"
          >
            {(data) => (
              <div className="flex flex-col gap-2">
                <p className="text-sm text-muted-foreground">
                  {data.movieTitle}, Phòng {data.roomName}, {formatDateTime(data.startTime)}
                </p>
                <SelectableSeatMap
                  rows={data.rows}
                  cols={data.cols}
                  aisles={data.aisles}
                  seats={data.seats}
                  statusMap={statusMap}
                  selectedIds={effectiveIds}
                  onToggle={handleToggle}
                  issueIds={issueIds}
                  selectableIds={selectableIds}
                  getSeatNote={(id, status) =>
                    describeSeatManageNote(status, blockBySeatId.get(id), blocksQuery.isSuccess)
                  }
                  fitWidth
                />
              </div>
            )}
          </QueryState>
        </div>

        <div className="flex flex-col gap-4 bg-card p-4 cut-md lg:self-start">
          <div>
            <h3 className="t-h3 text-foreground">
              {selectionKind === "unblock" ? "Mở khóa ghế" : "Khóa ghế"}
            </h3>
            <p className="mt-1 text-sm text-muted-foreground">
              Chọn ghế trống để khóa, hoặc ghế đang khóa thủ công để mở khóa. Ghế đang giữ, đã
              bán và ghế khóa do hệ thống không chọn được.
            </p>
          </div>

          {issueIds.size > 0 && (
            <div className="flex flex-col items-start gap-2 bg-destructive/10 p-3 text-sm text-destructive cut-sm">
              <p>
                Ghế vướng: <span className="t-code font-medium">{codesOf(issueIds).join(", ")}</span>.
                Ghế đã có đơn, xử lý đơn trước rồi khóa lại.
              </p>
              <Button type="button" variant="ghost" size="sm" onClick={() => setIssueIds(EMPTY_IDS)}>
                Bỏ đánh dấu
              </Button>
            </div>
          )}

          {selectionKind === null ? (
            <p className="text-sm text-muted-foreground">Chưa chọn ghế nào.</p>
          ) : (
            <>
              <div className="flex items-start justify-between gap-3">
                <div className="min-w-0">
                  <p className="text-sm font-medium text-foreground">
                    Đã chọn {effectiveIds.size} ghế
                  </p>
                  <p className="t-code wrap-break-word text-sm text-muted-foreground">
                    {selectedCodes.join(", ")}
                  </p>
                </div>
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  onClick={() => setSelectedIds(EMPTY_IDS)}
                >
                  <X className="size-4" />
                  Bỏ chọn
                </Button>
              </div>

              {selectionKind === "block" ? (
                <SeatBlockForm
                  showtimeId={showtimeId}
                  seatIds={[...effectiveIds]}
                  seatCount={effectiveIds.size}
                  onBlocked={handleBlocked}
                  onConflict={handleConflict}
                />
              ) : (
                <Button
                  type="button"
                  className="w-full"
                  disabled={unblockSeats.isPending}
                  onClick={handleUnblock}
                >
                  <LockOpen className="size-4" />
                  Mở khóa {effectiveIds.size} ghế
                </Button>
              )}
            </>
          )}
        </div>
      </div>

      <section className="flex flex-col gap-3">
        <h3 className="t-h3 text-foreground">Ghế đang khóa của suất này</h3>
        <SeatBlockList query={blocksQuery} selectedIds={effectiveIds} />
      </section>
    </div>
  );
};
