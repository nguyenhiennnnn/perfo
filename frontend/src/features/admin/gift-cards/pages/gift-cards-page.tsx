import { Gift } from "lucide-react";

import { PageHeader } from "@/components/common/page-header";
import { DataTable } from "@/components/common/data-table/data-table";
import { EnumSelectFilter } from "@/components/common/filter/enum-select-filter";
import { DEFAULT_PAGE, STATUS_LABEL } from "@/config/default-constants";
import { useAdminGiftCardParams } from "@/hooks/use-query-params";
import { useDebouncedValue } from "@/hooks/use-debounced-value";
import { useTablePagination } from "@/hooks/use-table-pagination";
import { useAlertDialogOpen } from "@/stores/alert-dialog-store";
import { useDialogOpen } from "@/stores/dialog-store";

import { createGiftCardColumns } from "../components/gift-card-columns";
import { GiftCardCancelDialogContent } from "../components/gift-card-cancel-dialog";
import { GiftCardHistoryContent } from "../components/gift-card-history-content";
import { useAdminGiftCards, useAdminResendGiftCard } from "../hooks/use-admin-gift-cards";
import type { AdminGiftCard } from "../types";
import { giftCardStatusOptions, giftCardTypeOptions } from "../utils/labels";

const AdminGiftCardsPage = () => {
  const [params, setParams] = useAdminGiftCardParams();
  const { pagination, onPaginationChange } = useTablePagination(params, setParams);

  const giftCards = useAdminGiftCards({
    search: useDebouncedValue(params.search) || undefined,
    status: params.status ?? undefined,
    type: params.type ?? undefined,
    page: pagination.pageIndex + 1,
    pageSize: pagination.pageSize,
  });
  const resend = useAdminResendGiftCard();

  const openDialog = useDialogOpen();
  const openAlertDialog = useAlertDialogOpen();

  const handleViewHistory = (giftCard: AdminGiftCard) =>
    openDialog({
      title: "Lịch sử sử dụng thẻ",
      description: "Mọi lần phát hành, giữ chỗ, sử dụng, hoàn lại, hết hạn và hủy của thẻ.",
      content: <GiftCardHistoryContent giftCard={giftCard} />,
      size: "xl",
    });

  const handleResend = (giftCard: AdminGiftCard) =>
    openAlertDialog({
      title: "Gửi lại email thẻ quà tặng?",
      description: `Thẻ ${giftCard.maskedCode} sẽ được gửi lại tới ${giftCard.recipientEmail ?? "người nhận"}.`,
      confirmText: "Gửi lại",
      onConfirm: async () => {
        await resend.mutateAsync(giftCard.giftCardId);
      },
    });

  const handleCancel = (giftCard: AdminGiftCard) =>
    openDialog({
      title: "Hủy thẻ quà tặng",
      description: "Thao tác được ghi vào nhật ký và sổ thẻ.",
      content: <GiftCardCancelDialogContent giftCard={giftCard} />,
      size: "md",
    });

  const columns = createGiftCardColumns({
    onViewHistory: handleViewHistory,
    onResend: handleResend,
    onCancel: handleCancel,
  });

  return (
    <div>
      <PageHeader
        title="Thẻ quà tặng"
        description="Tra cứu thẻ theo mã hoặc email, xem lịch sử sử dụng, hủy thẻ và gửi lại email."
      />

      <DataTable
        columns={columns}
        data={giftCards.data?.items ?? []}
        isLoading={giftCards.isLoading}
        isError={giftCards.isError}
        error={giftCards.error}
        onRetry={() => void giftCards.refetch()}
        isRefreshing={giftCards.isPlaceholderData}
        totalItems={giftCards.data?.totalItems ?? 0}
        pagination={pagination}
        onPaginationChange={onPaginationChange}
        searchValue={params.search}
        onSearchChange={(value) => setParams({ search: value, page: DEFAULT_PAGE })}
        searchPlaceholder="Tìm theo mã thẻ hoặc email..."
        hasActiveFilters={Boolean(params.search) || params.status !== null || params.type !== null}
        onClearFilters={() =>
          setParams({ search: "", status: null, type: null, page: DEFAULT_PAGE })
        }
        toolbarFilters={
          <>
            <EnumSelectFilter
              value={params.status}
              onChange={(status) => setParams({ status, page: DEFAULT_PAGE })}
              options={giftCardStatusOptions}
              placeholder={STATUS_LABEL}
              allLabel="Tất cả trạng thái"
            />
            <EnumSelectFilter
              value={params.type}
              onChange={(type) => setParams({ type, page: DEFAULT_PAGE })}
              options={giftCardTypeOptions}
              placeholder="Loại thẻ"
              allLabel="Tất cả loại"
            />
          </>
        }
        empty={{
          icon: Gift,
          title: "Chưa có thẻ quà tặng",
          description:
            "Thẻ được tạo khi khách mua thẻ quà hoặc khi hủy thẻ đã dùng một phần và chuyển số dư thành tín dụng đổi suất.",
        }}
      />
    </div>
  );
};

export default AdminGiftCardsPage;
