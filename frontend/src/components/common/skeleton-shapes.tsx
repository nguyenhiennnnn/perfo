import { Skeleton } from "@/components/ui/skeleton";
import { TableCell, TableRow } from "@/components/ui/table";
import { cn } from "@/lib/utils";

interface ShapeProps {
  className?: string;
}

/** Dòng danh sách: khối dẫn đầu, hai dòng chữ, khối cuối. */
export const RowSkeleton = ({ className }: ShapeProps) => (
  <div aria-hidden className={cn("flex items-center gap-4 bg-card p-4", className)}>
    <Skeleton className="size-12 shrink-0 cut-sm" />
    <div className="flex-1 space-y-2">
      <Skeleton className="h-4 w-2/3 cut-xs" />
      <Skeleton className="h-3 w-1/3 cut-xs" />
    </div>
    <Skeleton className="h-6 w-20 shrink-0 cut-xs" />
  </div>
);

/** Thẻ vé và đơn đặt vé: ô biểu tượng, tên phim, giờ, mã; trạng thái và giá bên phải. */
export const TicketSkeleton = ({ className }: ShapeProps) => (
  <div aria-hidden className={cn("flex flex-col gap-4 bg-card p-4 sm:flex-row sm:items-center sm:justify-between", className)}>
    <div className="flex items-center gap-3">
      <Skeleton className="size-12 shrink-0 cut-sm" />
      <div className="space-y-2">
        <Skeleton className="h-4 w-40 cut-xs" />
        <Skeleton className="h-3 w-32 cut-xs" />
        <Skeleton className="h-3 w-24 cut-xs" />
      </div>
    </div>
    <div className="flex flex-col gap-2 sm:items-end">
      <Skeleton className="h-6 w-24 cut-xs" />
      <Skeleton className="h-4 w-20 cut-xs" />
    </div>
  </div>
);

/** Một khối nội dung có tiêu đề và vài dòng: thẻ đánh giá, panel thông tin. */
export const CardSkeleton = ({ className }: ShapeProps) => (
  <div aria-hidden className={cn("space-y-3 bg-card p-4", className)}>
    <Skeleton className="h-5 w-1/3 cut-xs" />
    <Skeleton className="h-4 w-full cut-xs" />
    <Skeleton className="h-4 w-4/5 cut-xs" />
  </div>
);

interface TableRowsSkeletonProps {
  rows: number;
  columns: number;
}

/** Các hàng bảng giả, dùng trong TableBody khi đang tải. */
export const TableRowsSkeleton = ({ rows, columns }: TableRowsSkeletonProps) => (
  <>
    {Array.from({ length: rows }, (_, rowIndex) => (
      <TableRow key={rowIndex} aria-hidden>
        {Array.from({ length: columns }, (_, columnIndex) => (
          <TableCell key={columnIndex}>
            <Skeleton className="h-5 w-full cut-xs" />
          </TableCell>
        ))}
      </TableRow>
    ))}
  </>
);

interface FormSkeletonProps extends ShapeProps {
  fields?: number;
}

/** Form trong dialog hoặc sheet khi đang tải dữ liệu: nhãn, ô nhập, hàng nút. */
export const FormSkeleton = ({ fields = 3, className }: FormSkeletonProps) => (
  <div aria-hidden aria-busy="true" className={cn("space-y-5", className)}>
    {Array.from({ length: fields }, (_, index) => (
      <div key={index} className="space-y-2">
        <Skeleton className="h-3 w-24 cut-xs" />
        <Skeleton className="h-10 w-full" />
      </div>
    ))}
    <div className="flex justify-end gap-3 pt-2">
      <Skeleton className="h-10 w-20 cut-sm" />
      <Skeleton className="h-10 w-24 cut-sm" />
    </div>
  </div>
);
