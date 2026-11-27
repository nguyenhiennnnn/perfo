import { createColumnHelper } from "@tanstack/react-table";
import { BarChart3, CalendarX, MoreHorizontal, Pencil, Send, Trash2 } from "lucide-react";

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
import { formatDateTime } from "@/lib/formatters";
import { CampaignStatus } from "@/types/enum";

import type { CampaignDto } from "../types";
import {
  describeCampaignPromo,
  getCampaignChannelLabel,
  hasCampaignResults,
  isCampaignDeletable,
  isCampaignEditable,
} from "../utils/campaign-labels";
import { CampaignStatusBadge } from "./campaign-status-badge";

interface CampaignColumnsOptions {
  onEdit: (campaign: CampaignDto) => void;
  onTestSend: (campaign: CampaignDto) => void;
  onCancelSchedule: (campaign: CampaignDto) => void;
  onViewReport: (campaign: CampaignDto) => void;
  onDelete: (campaign: CampaignDto) => void;
}

const columnHelper = createColumnHelper<DataTableFeatures, CampaignDto>();

export const createCampaignColumns = ({
  onEdit,
  onTestSend,
  onCancelSchedule,
  onViewReport,
  onDelete,
}: CampaignColumnsOptions) =>
  columnHelper.columns([
    columnHelper.accessor("name", {
      header: ({ column }) => <DataTableColumnHeader column={column} title="Chiến dịch" />,
      cell: ({ row }) => (
        <div className="flex max-w-72 flex-col gap-0.5">
          <span className="truncate font-medium text-foreground">{row.original.name}</span>
          <span className="line-clamp-1 text-xs text-muted-foreground">
            {row.original.subject}
          </span>
        </div>
      ),
      enableSorting: false,
    }),
    columnHelper.display({
      id: "segment",
      header: "Phân khúc",
      cell: ({ row }) => (
        <span className="line-clamp-1 max-w-48 text-sm text-foreground">
          {row.original.segmentName || "—"}
        </span>
      ),
      enableSorting: false,
    }),
    columnHelper.display({
      id: "channel",
      header: "Kênh",
      cell: ({ row }) => (
        <Badge variant="outline">{getCampaignChannelLabel(row.original.channel)}</Badge>
      ),
      enableSorting: false,
    }),
    columnHelper.display({
      id: "promo",
      header: "Mã cá nhân",
      cell: ({ row }) => (
        <span className="line-clamp-2 max-w-56 text-sm text-muted-foreground">
          {describeCampaignPromo(row.original.promo)}
        </span>
      ),
      enableSorting: false,
    }),
    columnHelper.display({
      id: "scheduledAt",
      header: "Giờ gửi",
      cell: ({ row }) => (
        <span className="text-sm tabular-nums text-foreground">
          {formatDateTime(row.original.scheduledAt)}
        </span>
      ),
      enableSorting: false,
    }),
    columnHelper.display({
      id: "status",
      header: () => STATUS_LABEL,
      cell: ({ row }) => <CampaignStatusBadge status={row.original.status} />,
      enableSorting: false,
    }),
    columnHelper.display({
      id: "actions",
      header: () => <div className="text-right">Hành động</div>,
      cell: ({ row }) => {
        const campaign = row.original;
        const canEdit = isCampaignEditable(campaign.status);
        return (
          <div className="flex justify-end">
            <DropdownMenu>
              <DropdownMenuTrigger>
                <Button variant="ghost" size="icon" className="size-8">
                  <span className="sr-only">Mở menu</span>
                  <MoreHorizontal className="size-4" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                {hasCampaignResults(campaign.status) && (
                  <DropdownMenuItem onClick={() => onViewReport(campaign)}>
                    <BarChart3 /> Xem kết quả
                  </DropdownMenuItem>
                )}
                {canEdit && (
                  <DropdownMenuItem onClick={() => onEdit(campaign)}>
                    <Pencil /> Chỉnh sửa
                  </DropdownMenuItem>
                )}
                <DropdownMenuItem onClick={() => onTestSend(campaign)}>
                  <Send /> Gửi thử
                </DropdownMenuItem>
                {campaign.status === CampaignStatus.Scheduled && (
                  <DropdownMenuItem onClick={() => onCancelSchedule(campaign)}>
                    <CalendarX /> Hủy hẹn giờ
                  </DropdownMenuItem>
                )}
                {isCampaignDeletable(campaign.status) && (
                  <>
                    <DropdownMenuSeparator />
                    <DropdownMenuItem variant="destructive" onClick={() => onDelete(campaign)}>
                      <Trash2 /> Xóa
                    </DropdownMenuItem>
                  </>
                )}
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        );
      },
      enableHiding: false,
    }),
  ]);
