import { Skeleton } from "@/components/ui/skeleton";

export const MovieCardSkeleton = () => {
  return (
    <div className="flex flex-col overflow-hidden cut-md bg-card">
      <Skeleton className="aspect-2/3 w-full" />
    </div>
  );
}