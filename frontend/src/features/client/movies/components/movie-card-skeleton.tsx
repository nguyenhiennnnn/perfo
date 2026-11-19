import { LineSkeleton, PosterSkeleton } from "@/components/common/skeleton-shapes";

export const MovieCardSkeleton = () => (
  <div>
    <PosterSkeleton />
    <div className="space-y-2 p-3.5">
      <LineSkeleton className="w-3/4" />
      <LineSkeleton className="h-3.5 w-1/2" />
    </div>
  </div>
);
