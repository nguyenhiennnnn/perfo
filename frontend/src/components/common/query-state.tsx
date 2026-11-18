import type { ReactNode } from "react";
import { ErrorState } from "@/components/common/error-state";
import { NotFoundState } from "@/components/common/not-found-state";
import { STATE_MESSAGES } from "@/config/error-messages";
import { isNotFoundError } from "@/lib/error";

interface QueryLike<TData> {
  isLoading: boolean;
  isError: boolean;
  data: TData | undefined;
  error?: unknown;
  refetch: () => unknown;
}

interface QueryStateProps<TData> {
  query: QueryLike<TData>;
  /** Skeleton đúng hình khối nội dung sẽ hiển thị. */
  skeleton: ReactNode;
  isEmpty?: boolean | ((data: TData) => boolean);
  /** Trạng thái trống: EmptyState "chưa có dữ liệu" hoặc "không có kết quả lọc". */
  empty?: ReactNode;
  /**
   * Dùng cho truy vấn một thực thể (chi tiết). Hiện khi API trả 404 hoặc không có dữ liệu.
   * Không truyền thì 404 hiện NotFoundState chung.
   */
  notFound?: ReactNode;
  errorTitle?: string;
  errorClassName?: string;
  children: ReactNode | ((data: TData) => ReactNode);
}

export const QueryState = <TData,>({
  query,
  skeleton,
  isEmpty,
  empty,
  notFound,
  errorTitle,
  errorClassName,
  children,
}: QueryStateProps<TData>) => {
  const { data } = query;

  if (query.isLoading) return <>{skeleton}</>;

  if (query.isError && data === undefined) {
    if (isNotFoundError(query.error)) {
      return <>{notFound ?? <NotFoundState entity={STATE_MESSAGES.notFound.entityFallback} className={errorClassName} />}</>;
    }

    return (
      <ErrorState
        title={errorTitle}
        error={query.error}
        onRetry={() => void query.refetch()}
        className={errorClassName}
      />
    );
  }

  if ((data === undefined || data === null) && notFound !== undefined) return <>{notFound}</>;

  const showEmpty =
    data === undefined || (typeof isEmpty === "function" ? isEmpty(data) : isEmpty === true);

  if (showEmpty && empty !== undefined) return <>{empty}</>;

  if (typeof children === "function") {
    return data === undefined ? null : <>{children(data)}</>;
  }

  return <>{children}</>;
};
