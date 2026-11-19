import { Skeleton } from "@/components/ui/skeleton";
import { TableCell, TableRow } from "@/components/ui/table";
import { cn } from "@/lib/utils";

const CUT_CLASS = {
  xs: "cut-xs",
  sm: "cut-sm",
  md: "cut-md",
  lg: "cut-lg",
} as const;

interface ShapeProps {
  className?: string;
}

export type SkeletonCut = keyof typeof CUT_CLASS;

export interface CutShapeProps extends ShapeProps {
  cut?: SkeletonCut;
}

export const LineSkeleton = ({ className }: ShapeProps) => (
  <Skeleton className={cn("h-4 w-full", CUT_CLASS.xs, className)} />
);

export const PosterSkeleton = ({ className, cut = "md" }: CutShapeProps) => (
  <Skeleton className={cn("aspect-2/3 w-full", CUT_CLASS[cut], className)} />
);

export const AvatarSkeleton = ({ className }: ShapeProps) => (
  <Skeleton className={cn("size-9 shrink-0 rounded-full", className)} />
);

export const CardSkeleton = ({ className, cut = "md" }: CutShapeProps) => (
  <Skeleton className={cn("h-32 w-full", CUT_CLASS[cut], className)} />
);

export const TicketSkeleton = ({ className }: ShapeProps) => (
  <div className={cn("w-full pr-4 pb-3", className)}>
    <Skeleton className={cn("h-120 w-full sm:h-72", CUT_CLASS.lg)} />
  </div>
);

interface TableRowsSkeletonProps {
  rows: number;
  columns: number;
}

export const TableRowsSkeleton = ({ rows, columns }: TableRowsSkeletonProps) => (
  <>
    {Array.from({ length: rows }, (_, rowIndex) => (
      <TableRow key={rowIndex}>
        {Array.from({ length: columns }, (_, columnIndex) => (
          <TableCell key={columnIndex}>
            <LineSkeleton className="h-5" />
          </TableCell>
        ))}
      </TableRow>
    ))}
  </>
);

interface FormSkeletonProps extends ShapeProps {
  fields?: number;
}

export const FormSkeleton = ({ fields = 3, className }: FormSkeletonProps) => (
  <div aria-busy="true" className={cn("space-y-5", className)}>
    {Array.from({ length: fields }, (_, index) => (
      <div key={index} className="space-y-2">
        <LineSkeleton className="h-3.5 w-24" />
        <Skeleton className="h-10 w-full" />
      </div>
    ))}
    <div className="flex justify-end gap-3 pt-2">
      <Skeleton className={cn("h-10 w-20", CUT_CLASS.sm)} />
      <Skeleton className={cn("h-10 w-24", CUT_CLASS.sm)} />
    </div>
  </div>
);
