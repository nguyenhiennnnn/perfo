import { CardSkeleton } from "@/components/common/skeleton-shapes";

export const OrderDetailSkeleton = () => (
  <div aria-busy="true" className="grid gap-4 lg:grid-cols-3">
    <div className="space-y-4 lg:col-span-2">
      <CardSkeleton className="h-64" />
      <CardSkeleton className="h-48" />
    </div>
    <div className="space-y-4">
      <CardSkeleton className="h-56" />
      <CardSkeleton className="h-48" />
    </div>
  </div>
);
