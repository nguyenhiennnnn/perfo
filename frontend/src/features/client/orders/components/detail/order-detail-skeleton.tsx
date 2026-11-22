import { CardSkeleton, LineSkeleton } from "@/components/common/skeleton-shapes";

export const OrderDetailSkeleton = () => (
  <div className="space-y-6" aria-busy="true">
    <CardSkeleton className="h-24" />
    <CardSkeleton className="h-40" />
    <div>
      <LineSkeleton className="h-7 w-1/3" />
      <CardSkeleton className="mt-3 h-44" />
    </div>
    <CardSkeleton className="h-48" />
    <CardSkeleton className="h-40" />
  </div>
);
