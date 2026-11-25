import { SearchX, UserSearch } from "lucide-react";

import { EmptyState } from "@/components/common/empty-state";
import { ErrorState } from "@/components/common/error-state";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { FilterSearch } from "@/components/common/filter/filter-bar";
import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";
import { formatPoints } from "@/lib/formatters";

import type { LoyaltyCustomerLookupItemDto } from "../types";

interface LoyaltyCustomerSearchProps {
  value: string;
  onChange: (value: string) => void;
  keyword: string;
  selectedUserId: number | null;
  onSelect: (customer: LoyaltyCustomerLookupItemDto) => void;
  results: LoyaltyCustomerLookupItemDto[] | undefined;
  isLoading: boolean;
  isError: boolean;
  error: unknown;
  onRetry: () => void;
}

export const LoyaltyCustomerSearch = ({
  value,
  onChange,
  keyword,
  selectedUserId,
  onSelect,
  results,
  isLoading,
  isError,
  error,
  onRetry,
}: LoyaltyCustomerSearchProps) => (
  <div className="space-y-3">
    <FilterSearch
      value={value}
      onChange={onChange}
      placeholder="Tìm khách theo tên, email hoặc số điện thoại..."
      className="w-full sm:max-w-md"
      autoFocus
    />

    {keyword.trim() === "" ? (
      <EmptyState
        icon={UserSearch}
        title="Tra cứu khách hàng"
        description="Nhập tên, email hoặc số điện thoại để xem điểm và điều chỉnh."
      />
    ) : isLoading ? (
      <div className="grid grid-cols-1 gap-3 md:grid-cols-2 xl:grid-cols-3">
        <CardSkeleton className="h-20" />
        <CardSkeleton className="h-20" />
        <CardSkeleton className="h-20" />
      </div>
    ) : isError && results === undefined ? (
      <ErrorState title="Không tìm được khách hàng" error={error} onRetry={onRetry} />
    ) : !results || results.length === 0 ? (
      <EmptyState
        icon={SearchX}
        title="Không tìm thấy khách hàng"
        description="Thử lại với tên, email hoặc số điện thoại khác."
      />
    ) : (
      <ul className="grid grid-cols-1 gap-3 md:grid-cols-2 xl:grid-cols-3">
        {results.map((customer) => {
          const selected = customer.userId === selectedUserId;
          return (
            <li key={customer.userId}>
              <button
                type="button"
                onClick={() => onSelect(customer)}
                aria-pressed={selected}
                className={cn(
                  "flex w-full flex-col gap-1 bg-card p-3 text-left transition-colors hover:bg-muted/60",
                  "focus-visible:outline-2 focus-visible:outline-ring",
                  selected && "ring-2 ring-primary",
                )}
              >
                <span className="flex items-center justify-between gap-2">
                  <span className="truncate font-medium text-foreground">{customer.fullName}</span>
                  {customer.tierName && <Badge variant="outline">{customer.tierName}</Badge>}
                </span>
                <span className="truncate text-xs text-muted-foreground">
                  {[customer.email, customer.phone].filter(Boolean).join(" · ")}
                </span>
                <span className="text-xs text-muted-foreground">
                  Số dư:{" "}
                  <span className="font-medium tabular-nums text-foreground">
                    {formatPoints(customer.balance)}
                  </span>{" "}
                  điểm
                </span>
              </button>
            </li>
          );
        })}
      </ul>
    )}
  </div>
);
