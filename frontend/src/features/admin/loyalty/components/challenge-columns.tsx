import { createColumnHelper } from "@tanstack/react-table";
import { MoreHorizontal, Pencil, Trash2 } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { DataTableColumnHeader } from "@/components/common/data-table/data-table-column-header";
import type { DataTableFeatures } from "@/components/common/data-table/data-table-features";
import { STATUS_LABEL } from "@/config/default-constants";
import { formatDate } from "@/lib/formatters";

import type { AdminLoyaltyChallengeDto } from "../types";
import {
  CHALLENGE_PHASE_BADGES,
  deriveChallengePhase,
  describeAdminChallengeCondition,
  describeAdminChallengeReward,
} from "../utils/loyalty-admin-labels";

interface ChallengeColumnsOptions {
  today: string;
  onEdit: (challenge: AdminLoyaltyChallengeDto) => void;
  onDelete: (challenge: AdminLoyaltyChallengeDto) => void;
}

const columnHelper = createColumnHelper<DataTableFeatures, AdminLoyaltyChallengeDto>();

export const createChallengeColumns = ({ today, onEdit, onDelete }: ChallengeColumnsOptions) =>
  columnHelper.columns([
    columnHelper.accessor("name", {
      header: ({ column }) => <DataTableColumnHeader column={column} title="Thử thách" />,
      cell: ({ row }) => (
        <div className="flex max-w-64 flex-col gap-0.5">
          <span className="truncate font-medium text-foreground">{row.original.name}</span>
          {row.original.description && (
            <span className="truncate text-xs text-muted-foreground">
              {row.original.description}
            </span>
          )}
        </div>
      ),
      enableSorting: false,
    }),
    columnHelper.display({
      id: "period",
      header: "Kỳ áp dụng",
      cell: ({ row }) => (
        <span className="text-sm tabular-nums text-muted-foreground">
          {formatDate(row.original.startsOn)} - {formatDate(row.original.endsOn)}
        </span>
      ),
      enableSorting: false,
    }),
    columnHelper.display({
      id: "condition",
      header: "Điều kiện",
      cell: ({ row }) => (
        <span className="text-sm">{describeAdminChallengeCondition(row.original)}</span>
      ),
      enableSorting: false,
    }),
    columnHelper.display({
      id: "reward",
      header: "Phần thưởng",
      cell: ({ row }) => (
        <span className="text-sm font-semibold text-primary-text">
          {describeAdminChallengeReward(row.original)}
        </span>
      ),
      enableSorting: false,
    }),
    columnHelper.display({
      id: "participants",
      header: "Tham gia / hoàn thành",
      cell: ({ row }) => (
        <span className="text-sm tabular-nums">
          {row.original.participantCount} / {row.original.completedCount}
        </span>
      ),
      enableSorting: false,
    }),
    columnHelper.display({
      id: "status",
      header: () => STATUS_LABEL,
      cell: ({ row }) => {
        const item = CHALLENGE_PHASE_BADGES[deriveChallengePhase(row.original, today)];
        return <Badge variant={item.variant}>{item.label}</Badge>;
      },
      enableSorting: false,
    }),
    columnHelper.display({
      id: "actions",
      header: () => <div className="text-right">Hành động</div>,
      cell: ({ row }) => (
        <div className="flex justify-end">
          <DropdownMenu>
            <DropdownMenuTrigger>
              <Button variant="ghost" size="icon" className="size-8">
                <span className="sr-only">Mở menu</span>
                <MoreHorizontal className="size-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem onClick={() => onEdit(row.original)}>
                <Pencil /> Chỉnh sửa
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuItem variant="destructive" onClick={() => onDelete(row.original)}>
                <Trash2 /> Xóa
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      ),
      enableHiding: false,
    }),
  ]);
