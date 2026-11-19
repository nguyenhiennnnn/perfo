import type { ComponentType } from "react";
import { CardSkeleton, type CutShapeProps } from "@/components/common/skeleton-shapes";

interface SkeletonListProps extends CutShapeProps {
  count: number;
  shape?: ComponentType<CutShapeProps>;
}

export const SkeletonList = ({ count, className, cut, shape: Shape = CardSkeleton }: SkeletonListProps) => (
  <>
    {Array.from({ length: count }, (_, index) => (
      <Shape key={index} className={className} cut={cut} />
    ))}
  </>
);
