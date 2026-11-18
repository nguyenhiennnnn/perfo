import type { ComponentType } from "react";
import { Skeleton } from "@/components/ui/skeleton";

interface SkeletonListProps {
  count: number;
  className?: string;
  /** Hình khối của từng mục: RowSkeleton, TicketSkeleton, CardSkeleton... Mặc định là một khối phẳng. */
  shape?: ComponentType<{ className?: string }>;
}

export const SkeletonList = ({ count, className, shape: Shape = Skeleton }: SkeletonListProps) => (
  <>
    {Array.from({ length: count }, (_, index) => (
      <Shape key={index} className={className} />
    ))}
  </>
);
