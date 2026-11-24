import { Skeleton } from "@/components/ui/skeleton";
import { getDisplayMessage } from "@/lib/error";

import { useAffectedShowtimesPreview } from "../hooks/use-room-closures";
import type { AffectedShowtimesParams } from "../types";
import { AffectedShowtimesStats } from "./affected-showtimes-stats";

interface AffectedShowtimesPreviewProps {
  params: AffectedShowtimesParams | null;
}

export const AffectedShowtimesPreview = ({ params }: AffectedShowtimesPreviewProps) => {
  const query = useAffectedShowtimesPreview(params);

  if (!params) {
    return (
      <p className="text-sm text-muted-foreground">
        Chọn phòng và khoảng thời gian đóng để xem các suất chiếu bị ảnh hưởng.
      </p>
    );
  }

  if (query.isLoading) return <Skeleton className="h-24 w-full cut-md" />;

  if (query.isError || !query.data) {
    return (
      <p className="text-sm text-destructive">
        {query.isError
          ? getDisplayMessage(query.error, { preferServerMessage: true })
          : "Không tải được các suất chiếu bị ảnh hưởng. Thử lại sau."}
      </p>
    );
  }

  return (
    <div className="flex flex-col gap-2">
      <p className="text-sm font-medium text-foreground">Suất chiếu bị ảnh hưởng</p>
      <AffectedShowtimesStats data={query.data} />
      <p className="text-xs text-muted-foreground">
        {query.data.totalShowtimes === 0
          ? "Không có suất chiếu nào nằm trong khoảng đóng phòng."
          : "Sau khi lưu, bạn xử lý từng suất bằng cách chuyển sang phòng khác hoặc hủy suất."}
      </p>
    </div>
  );
};
