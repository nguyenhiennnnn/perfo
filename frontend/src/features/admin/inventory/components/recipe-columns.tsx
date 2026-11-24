import { createColumnHelper } from "@tanstack/react-table";
import { Pencil, Trash2 } from "lucide-react";

import { DataTableColumnHeader } from "@/components/common/data-table/data-table-column-header";
import type { DataTableFeatures } from "@/components/common/data-table/data-table-features";
import { formatDateTime } from "@/lib/formatters";

import type { ComboRecipeListItemDto } from "../types";
import { RowActionsMenu } from "./row-actions-menu";

interface RecipeColumnsOptions {
  onEdit: (recipe: ComboRecipeListItemDto) => void;
  onDelete: (recipe: ComboRecipeListItemDto) => void;
}

const columnHelper = createColumnHelper<DataTableFeatures, ComboRecipeListItemDto>();

export const createRecipeColumns = ({ onEdit, onDelete }: RecipeColumnsOptions) =>
  columnHelper.columns([
    columnHelper.accessor("comboName", {
      header: ({ column }) => <DataTableColumnHeader column={column} title="Combo" />,
      cell: ({ row }) => <span className="font-medium text-foreground">{row.original.comboName}</span>,
      enableSorting: false,
    }),
    columnHelper.accessor("lineCount", {
      header: "Số nguyên liệu",
      cell: ({ row }) => <span className="tabular-nums">{row.original.lineCount}</span>,
      enableSorting: false,
    }),
    columnHelper.accessor("updatedAt", {
      header: "Cập nhật",
      cell: ({ row }) => <span className="tabular-nums">{formatDateTime(row.original.updatedAt)}</span>,
      enableSorting: false,
    }),
    columnHelper.display({
      id: "actions",
      header: () => <div className="text-right">Hành động</div>,
      cell: ({ row }) => (
        <RowActionsMenu
          actions={[
            { label: "Sửa công thức", icon: <Pencil />, onClick: () => onEdit(row.original) },
            {
              label: "Xóa công thức",
              icon: <Trash2 />,
              onClick: () => onDelete(row.original),
              destructive: true,
            },
          ]}
        />
      ),
      enableHiding: false,
    }),
  ]);
