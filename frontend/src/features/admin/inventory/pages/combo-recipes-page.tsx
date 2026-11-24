import { ClipboardList, Plus } from "lucide-react";

import { PageHeader } from "@/components/common/page-header";
import { Button } from "@/components/ui/button";
import { DataTable } from "@/components/common/data-table/data-table";
import { DEFAULT_PAGE } from "@/config/default-constants";
import { useAdminListParams } from "@/hooks/use-query-params";
import { useTablePagination } from "@/hooks/use-table-pagination";
import { useAlertDialogOpen } from "@/stores/alert-dialog-store";
import { useDialogClose, useDialogOpen } from "@/stores/dialog-store";

import { ComboRecipeEditContent, ComboRecipeForm } from "../components/forms/combo-recipe-form";
import { createRecipeColumns } from "../components/recipe-columns";
import { useAdminComboRecipes, useAdminDeleteComboRecipe } from "../hooks/use-admin-inventory";
import type { ComboRecipeListItemDto } from "../types";

const AdminComboRecipesPage = () => {
  const [params, setParams] = useAdminListParams();
  const { pagination, onPaginationChange } = useTablePagination(params, setParams);

  const recipes = useAdminComboRecipes({
    page: pagination.pageIndex + 1,
    pageSize: pagination.pageSize,
    search: params.search || undefined,
  });
  const deleteRecipe = useAdminDeleteComboRecipe();

  const openDialog = useDialogOpen();
  const closeDialog = useDialogClose();
  const openAlertDialog = useAlertDialogOpen();

  const handleCreate = () => {
    openDialog({
      title: "Thêm công thức combo",
      description: "Khai báo sản phẩm kho tiêu hao khi bán một combo. Combo chưa có công thức không theo dõi kho.",
      size: "xl",
      content: <ComboRecipeForm onSuccess={closeDialog} />,
    });
  };

  const handleEdit = (recipe: ComboRecipeListItemDto) => {
    openDialog({
      title: `Công thức "${recipe.comboName}"`,
      description: "Chỉnh sửa nguyên liệu và số lượng tiêu hao cho một combo.",
      size: "xl",
      content: <ComboRecipeEditContent comboId={recipe.comboId} onSuccess={closeDialog} />,
    });
  };

  const handleDelete = (recipe: ComboRecipeListItemDto) => {
    openAlertDialog({
      title: `Xóa công thức "${recipe.comboName}"?`,
      description: "Combo sẽ không còn theo dõi kho và luôn được bán.",
      confirmText: "Xóa công thức",
      variant: "destructive",
      onConfirm: async () => {
        await deleteRecipe.mutateAsync(recipe.comboId);
      },
    });
  };

  const columns = createRecipeColumns({ onEdit: handleEdit, onDelete: handleDelete });

  return (
    <div>
      <PageHeader
        title="Công thức combo"
        description="Sản phẩm kho và số lượng tiêu hao cho một combo; dùng để trừ kho và tính giá vốn."
        action={
          <Button onClick={handleCreate}>
            <Plus className="size-4" />
            <span>Thêm công thức</span>
          </Button>
        }
      />

      <DataTable
        columns={columns}
        data={recipes.data?.items ?? []}
        isLoading={recipes.isLoading}
        isError={recipes.isError}
        error={recipes.error}
        onRetry={() => void recipes.refetch()}
        isRefreshing={recipes.isPlaceholderData}
        totalItems={recipes.data?.totalItems ?? 0}
        pagination={pagination}
        onPaginationChange={onPaginationChange}
        searchValue={params.search}
        onSearchChange={(value) => setParams({ search: value, page: DEFAULT_PAGE })}
        searchPlaceholder="Tìm theo tên combo..."
        empty={{
          icon: ClipboardList,
          title: "Chưa có công thức combo",
          description: "Thêm công thức để combo tự động trừ kho khi bán.",
          actionLabel: "Thêm công thức",
          onAction: handleCreate,
        }}
      />
    </div>
  );
};

export default AdminComboRecipesPage;
