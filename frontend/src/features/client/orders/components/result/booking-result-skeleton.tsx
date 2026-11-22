import { CardSkeleton, LineSkeleton } from "@/components/common/skeleton-shapes";
import { Skeleton } from "@/components/ui/skeleton";

export const BookingResultSkeleton = () => (
  <div className="flex w-full flex-col gap-6" aria-busy="true">
    <div className="flex items-center gap-3">
      <CardSkeleton cut="sm" className="size-10 shrink-0" />
      <div className="flex-1 space-y-2">
        <LineSkeleton className="h-7 w-2/3" />
        <LineSkeleton className="w-1/2" />
      </div>
    </div>
    <CardSkeleton className="h-36" />
    <div className="flex flex-wrap gap-3">
      <Skeleton className="h-11 w-36 cut-sm" />
      <Skeleton className="h-11 w-32 cut-sm" />
    </div>
  </div>
);
