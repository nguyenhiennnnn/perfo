import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { Skeleton } from "@/components/ui/skeleton";

export const BookingPaymentSkeleton = () => (
  <div className="flex flex-col gap-4" aria-busy="true">
    <CardSkeleton className="h-56" />
    <CardSkeleton className="h-28" />
    <Skeleton className="h-11 w-full cut-sm" />
  </div>
);
