import type { UseQueryResult } from "@tanstack/react-query";
import { Armchair } from "lucide-react";

import { EmptyState } from "@/components/common/empty-state";
import { QueryState } from "@/components/common/query-state";
import { LineSkeleton } from "@/components/common/skeleton-shapes";
import { Badge } from "@/components/ui/badge";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { formatDateTime } from "@/lib/formatters";

import {
  SEAT_BLOCK_TYPE_LABEL,
} from "../../config/seat-block-config";
import { SeatBlockType, type SeatBlockDto } from "../../types";
import { isManualSeatBlock } from "../../utils/seat-blocks";

const TYPE_BADGE_VARIANT: Record<string, "warning" | "destructive" | "secondary"> = {
  [SeatBlockType.Maintenance]: "warning",
  [SeatBlockType.Broken]: "destructive",
};

interface SeatBlockListProps {
  query: UseQueryResult<SeatBlockDto[]>;
  selectedIds: Set<number>;
}

const ListSkeleton = () => (
  <div aria-busy="true" className="space-y-3">
    {Array.from({ length: 3 }, (_, index) => (
      <LineSkeleton key={index} className="h-9" />
    ))}
  </div>
);

export const SeatBlockList = ({ query, selectedIds }: SeatBlockListProps) => (
  <QueryState
    query={query}
    skeleton={<ListSkeleton />}
    errorTitle="Không tải được danh sách ghế khóa"
    isEmpty={(blocks) => blocks.length === 0}
    empty={
      <EmptyState
        icon={Armchair}
        title="Chưa có ghế nào bị khóa"
        description="Chọn ghế trống trên sơ đồ để khóa ghế hỏng hoặc đang bảo trì."
      />
    }
  >
    {(blocks) => (
      <Table containerClassName="max-h-72 overflow-y-auto">
        <TableHeader>
          <TableRow>
            <TableHead>Ghế</TableHead>
            <TableHead>Loại</TableHead>
            <TableHead>Lý do</TableHead>
            <TableHead>Thời hạn</TableHead>
            <TableHead>Người khóa</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {blocks.map((block) => (
            <TableRow
              key={block.seatBlockId}
              data-state={selectedIds.has(block.showtimeSeatId) ? "selected" : undefined}
            >
              <TableCell className="t-code font-medium">
                {block.seatRow}
                {block.seatNumber}
              </TableCell>
              <TableCell>
                <Badge variant={TYPE_BADGE_VARIANT[block.type] ?? "secondary"}>
                  {SEAT_BLOCK_TYPE_LABEL[block.type] ?? block.type}
                </Badge>
                {!isManualSeatBlock(block) && (
                  <span className="mt-1 block text-xs text-muted-foreground">
                    Hệ thống tạo
                  </span>
                )}
              </TableCell>
              <TableCell className="max-w-56 whitespace-normal wrap-break-word text-sm">
                {block.reason}
              </TableCell>
              <TableCell className="text-sm tabular-nums">
                {block.expiresAt ? formatDateTime(block.expiresAt) : "Đến khi mở khóa"}
              </TableCell>
              <TableCell className="text-sm text-muted-foreground">
                {block.createdByUserId ? `Người dùng #${block.createdByUserId}` : "Hệ thống"}
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    )}
  </QueryState>
);
