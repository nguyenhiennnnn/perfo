import { LineSkeleton, PosterSkeleton } from "@/components/common/skeleton-shapes";

export const ComboCardSkeleton = () => (
  <div className="w-48 shrink-0">
    <PosterSkeleton className="aspect-square" />
    <div className="space-y-2 p-3">
      <LineSkeleton className="w-3/4" />
      <LineSkeleton className="h-3.5 w-full" />
      <LineSkeleton className="w-1/3" />
    </div>
  </div>
);
