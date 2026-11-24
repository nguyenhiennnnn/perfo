import type { ReactNode } from "react";
import { ArrowLeft, CalendarX2, CircleCheck, Undo2 } from "lucide-react";
import { useNavigate, useParams } from "react-router";

import { EmptyState } from "@/components/common/empty-state";
import { NotFoundState } from "@/components/common/not-found-state";
import { PageHeader } from "@/components/common/page-header";
import { QueryState } from "@/components/common/query-state";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { Button } from "@/components/ui/button";
import { ROUTES } from "@/config/routes";
import { useShowtimeCancelFlow } from "@/features/admin/showtimes/hooks/use-showtime-cancel-flow";
import { useShowtimeRoomChangeFlow } from "@/features/admin/showtimes/hooks/use-showtime-room-change-flow";
import { formatDateTime } from "@/lib/formatters";

import { AffectedShowtimesStats } from "../components/affected-showtimes-stats";
import { AffectedShowtimesTable } from "../components/affected-showtimes-table";
import { RoomClosureStatusBadge } from "../components/room-closure-status-badge";
import { useRoomClosureEndFlow } from "../hooks/use-room-closure-end-flow";
import {
  useClosureAffectedShowtimes,
  useRoomClosure,
} from "../hooks/use-room-closures";
import type { RoomClosureDto } from "../types";
import { getClosureStatus } from "../utils/closure-status";

const DetailSkeleton = () => (
  <div aria-busy="true" className="space-y-4">
    <CardSkeleton className="h-28" />
    <CardSkeleton className="h-24" />
    <CardSkeleton cut="lg" className="h-64" />
  </div>
);

const InfoRow = ({ label, children }: { label: string; children: ReactNode }) => (
  <div className="flex flex-col gap-0.5">
    <dt className="text-xs text-muted-foreground">{label}</dt>
    <dd className="text-sm text-foreground">{children}</dd>
  </div>
);

const ClosureDetail = ({ closure }: { closure: RoomClosureDto }) => {
  const navigate = useNavigate();
  const status = getClosureStatus(closure);
  const isEnded = status === "ended";

  const affected = useClosureAffectedShowtimes(closure.roomClosureId);
  const roomChangeFlow = useShowtimeRoomChangeFlow();
  const cancelFlow = useShowtimeCancelFlow(undefined, { reason: "RoomClosed" });
  const endFlow = useRoomClosureEndFlow((_, result) => {
    if (result?.deleted) navigate(ROUTES.admin.roomClosures);
  });

  return (
    <div className="flex flex-col gap-6">
      <section className="flex flex-col gap-4 cut-md border border-border bg-card p-4">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
          <dl className="grid flex-1 grid-cols-1 gap-4 sm:grid-cols-3">
            <InfoRow label="Phòng chiếu">
              <span className="font-medium">{closure.roomName}</span>
            </InfoRow>
            <InfoRow label="Thời gian đóng">
              <span className="tabular-nums">
                {formatDateTime(closure.startTime)} đến {formatDateTime(closure.endTime)}
              </span>
            </InfoRow>
            <InfoRow label="Trạng thái">
              <RoomClosureStatusBadge status={status} />
            </InfoRow>
            <div className="sm:col-span-3">
              <InfoRow label="Lý do">{closure.reason}</InfoRow>
            </div>
          </dl>

          {!isEnded && (
            <Button
              variant="outline"
              disabled={endFlow.isPending}
              onClick={() => endFlow.start(closure)}
            >
              {status === "upcoming" ? <CalendarX2 className="size-4" /> : <Undo2 className="size-4" />}
              <span>{status === "upcoming" ? "Hủy lịch đóng" : "Mở lại phòng sớm"}</span>
            </Button>
          )}
        </div>
      </section>

      <section className="flex flex-col gap-4">
        <h2 className="t-h3 text-foreground">Suất chiếu bị ảnh hưởng</h2>

        <QueryState
          query={affected}
          skeleton={<CardSkeleton cut="lg" className="h-48" />}
          errorTitle="Không tải được danh sách suất bị ảnh hưởng"
        >
          {(data) => (
            <>
              <AffectedShowtimesStats data={data} />
              {data.showtimes.length === 0 ? (
                <EmptyState
                  icon={CircleCheck}
                  title="Không còn suất chiếu nào cần xử lý"
                  description={
                    isEnded
                      ? "Đợt đóng phòng này không có suất chiếu nào bị ảnh hưởng."
                      : "Mọi suất chiếu trong khoảng đóng phòng đã được chuyển phòng hoặc hủy."
                  }
                />
              ) : (
                <>
                  {!isEnded && (
                    <p className="text-sm text-muted-foreground">
                      Với mỗi suất, chuyển sang phòng khác cùng giờ hoặc hủy suất với lý do đóng phòng.
                    </p>
                  )}
                  <AffectedShowtimesTable
                    showtimes={data.showtimes}
                    readOnly={isEnded}
                    onChangeRoom={(showtime) => roomChangeFlow.start(showtime)}
                    onCancel={(showtime) => cancelFlow.start(showtime)}
                  />
                </>
              )}
            </>
          )}
        </QueryState>
      </section>

      <Button
        variant="outline"
        className="w-fit"
        onClick={() => navigate(ROUTES.admin.roomClosures)}
      >
        <ArrowLeft className="size-4" />
        <span>Quay lại danh sách đóng phòng</span>
      </Button>
    </div>
  );
};

const AdminRoomClosureDetailPage = () => {
  const { id } = useParams<{ id: string }>();
  const closureId = Number(id);
  const closure = useRoomClosure(Number.isFinite(closureId) ? closureId : 0);

  return (
    <div>
      <PageHeader
        title={closure.data ? `Đóng phòng — ${closure.data.roomName}` : "Đóng phòng"}
        description="Xử lý từng suất chiếu bị ảnh hưởng: chuyển sang phòng khác hoặc hủy suất."
      />

      <QueryState
        query={closure}
        skeleton={<DetailSkeleton />}
        errorTitle="Không tải được thông tin đóng phòng"
        notFound={
          <NotFoundState
            entity="lịch đóng phòng"
            backLabel="Về danh sách đóng phòng"
            backTo={ROUTES.admin.roomClosures}
          />
        }
      >
        {(data) => <ClosureDetail closure={data} />}
      </QueryState>
    </div>
  );
};

export default AdminRoomClosureDetailPage;
