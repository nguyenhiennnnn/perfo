import { Skeleton } from "@/components/ui/skeleton";

export const BookingResultSkeleton = () => (
  <div className="flex w-full flex-col gap-6" aria-busy="true">
    <div className="flex items-center gap-3">
      <Skeleton className="size-10 cut-sm" />
      <div className="flex-1 space-y-2">
        <Skeleton className="h-7 w-2/3 cut-xs" />
        <Skeleton className="h-4 w-1/2 cut-xs" />
      </div>
    </div>
    <Skeleton className="h-[30rem] w-full cut-lg sm:h-72" />
    <div className="flex gap-3">
      <Skeleton className="h-10 w-36 cut-sm" />
      <Skeleton className="h-10 w-32 cut-sm" />
    </div>
  </div>
);
