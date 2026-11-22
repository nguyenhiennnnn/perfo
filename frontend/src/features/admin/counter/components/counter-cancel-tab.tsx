import { useState } from "react";
import { ArrowLeft, ClipboardList, Search, X } from "lucide-react";
import { EmptyState } from "@/components/common/empty-state";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { useDebouncedValue } from "@/hooks/use-debounced-value";
import { cn } from "@/lib/utils";
import { CounterCancelOrderDetail } from "./counter-cancel-order-detail";
import { CounterCancelOrderList } from "./counter-cancel-order-list";

export const CounterCancelTab = () => {
  const [search, setSearch] = useState("");
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const debouncedSearch = useDebouncedValue(search);

  return (
    <div className="grid gap-6 lg:grid-cols-[minmax(0,2fr)_minmax(0,3fr)]">
      <div
        className={cn(
          "flex min-w-0 flex-col gap-4",
          selectedId !== null && "hidden lg:flex",
        )}
      >
        <div className="relative">
          <Search className="absolute top-1/2 left-2.5 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            type="text"
            autoComplete="off"
            aria-label="Tìm đơn hàng theo mã đơn hoặc số điện thoại"
            placeholder="Mã đơn hoặc số điện thoại khách"
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            className="pr-9 pl-9"
          />
          {search !== "" && (
            <button
              type="button"
              aria-label="Xóa nội dung tìm kiếm"
              onClick={() => setSearch("")}
              className="absolute top-1/2 right-2.5 -translate-y-1/2 text-muted-foreground hover:text-foreground"
            >
              <X className="size-4" />
            </button>
          )}
        </div>

        <CounterCancelOrderList
          search={debouncedSearch}
          selectedId={selectedId}
          onSelect={setSelectedId}
        />
      </div>

      <div className={cn("min-w-0", selectedId === null && "hidden lg:block")}>
        {selectedId === null ? (
          <EmptyState
            icon={ClipboardList}
            title="Chọn đơn hàng cần hủy"
            description="Chọn một đơn trong danh sách để xem chi tiết và hủy đơn."
          />
        ) : (
          <div className="flex flex-col gap-3">
            <Button
              variant="ghost"
              size="sm"
              className="self-start lg:hidden"
              onClick={() => setSelectedId(null)}
            >
              <ArrowLeft className="size-4" />
              <span>Quay lại danh sách</span>
            </Button>
            <CounterCancelOrderDetail key={selectedId} orderId={selectedId} />
          </div>
        )}
      </div>
    </div>
  );
};
