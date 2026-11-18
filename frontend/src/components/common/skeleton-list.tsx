import { Skeleton } from "@/components/ui/skeleton";

interface SkeletonListProps {
  count: number;
  className?: string;
}

export const SkeletonList = ({ count, className }: SkeletonListProps) => (
  <>
    {Array.from({ length: count }, (_, index) => (
      <Skeleton key={index} className={className} />
    ))}
  </>
);
