import type { ReactNode } from "react";
import { ErrorState } from "@/components/common/error-state";

const DEFAULT_ERROR_TITLE = "Không tải được dữ liệu";
const DEFAULT_ERROR_DESCRIPTION = "Kết nối tới máy chủ bị gián đoạn. Kiểm tra mạng rồi bấm Thử lại.";

interface QueryLike<TData> {
  isLoading: boolean;
  isError: boolean;
  data: TData | undefined;
  refetch: () => unknown;
}

interface QueryStateProps<TData> {
  query: QueryLike<TData>;
  skeleton: ReactNode;
  isEmpty?: boolean | ((data: TData) => boolean);
  empty?: ReactNode;
  errorTitle?: string;
  errorDescription?: string;
  errorClassName?: string;
  children: ReactNode | ((data: TData) => ReactNode);
}

export const QueryState = <TData,>({
  query,
  skeleton,
  isEmpty,
  empty,
  errorTitle = DEFAULT_ERROR_TITLE,
  errorDescription = DEFAULT_ERROR_DESCRIPTION,
  errorClassName,
  children,
}: QueryStateProps<TData>) => {
  const { data } = query;

  if (query.isLoading) return <>{skeleton}</>;

  if (query.isError && data === undefined) {
    return (
      <ErrorState
        title={errorTitle}
        description={errorDescription}
        onRetry={() => void query.refetch()}
        className={errorClassName}
      />
    );
  }

  const showEmpty =
    data === undefined || (typeof isEmpty === "function" ? isEmpty(data) : isEmpty === true);

  if (showEmpty && empty !== undefined) return <>{empty}</>;

  if (typeof children === "function") {
    return data === undefined ? null : <>{children(data)}</>;
  }

  return <>{children}</>;
};
