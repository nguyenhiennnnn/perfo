import { useMemo } from "react";
import { Megaphone, Plus } from "lucide-react";
import { useNavigate } from "react-router";

import { PageHeader } from "@/components/common/page-header";
import { Button } from "@/components/ui/button";
import { DataTable } from "@/components/common/data-table/data-table";
import { EnumSelectFilter } from "@/components/common/filter/enum-select-filter";
import { useDialogClose, useDialogOpen } from "@/stores/dialog-store";
import { useAlertDialogOpen } from "@/stores/alert-dialog-store";
import { useAdminCampaignParams } from "@/hooks/use-query-params";
import { useDebouncedValue } from "@/hooks/use-debounced-value";
import { useTablePagination } from "@/hooks/use-table-pagination";
import {
  DEFAULT_PAGE,
  IRREVERSIBLE_ACTION_MESSAGE,
  LOOKUP_PAGE_SIZE,
  STATUS_LABEL,
} from "@/config/default-constants";
import { ROUTES } from "@/config/routes";
import { useAdminCustomerSegments } from "@/features/admin/customer-segments/hooks/use-admin-customer-segments";
import { formatDateTime } from "@/lib/formatters";

import {
  useAdminCampaigns,
  useAdminCancelCampaignSchedule,
  useAdminDeleteCampaign,
  useAdminTestSendCampaign,
} from "../hooks/use-admin-campaigns";
import { createCampaignColumns } from "../components/campaign-columns";
import { CampaignForm } from "../components/forms/campaign-form";
import type { CampaignDto } from "../types";
import { CAMPAIGN_STATUS_OPTIONS, toUpsertRequest } from "../utils/campaign-labels";

const AdminCampaignsPage = () => {
  const navigate = useNavigate();
  const [params, setParams] = useAdminCampaignParams();
  const { pagination, onPaginationChange } = useTablePagination(params, setParams);

  const campaigns = useAdminCampaigns({
    search: useDebouncedValue(params.search) || undefined,
    status: params.status ?? undefined,
    segmentId: params.segmentId ? Number(params.segmentId) : undefined,
    page: pagination.pageIndex + 1,
    pageSize: pagination.pageSize,
  });
  const segments = useAdminCustomerSegments({ pageSize: LOOKUP_PAGE_SIZE });
  const deleteCampaign = useAdminDeleteCampaign();
  const cancelSchedule = useAdminCancelCampaignSchedule();
  const testSend = useAdminTestSendCampaign();

  const openDialog = useDialogOpen();
  const closeDialog = useDialogClose();
  const openAlertDialog = useAlertDialogOpen();

  const segmentOptions = useMemo(
    () =>
      (segments.data?.items ?? []).map((s) => ({ value: String(s.segmentId), label: s.name })),
    [segments.data],
  );

  const handleCreate = () => {
    openDialog({
      title: "Soạn chiến dịch",
      description:
        "Chọn phân khúc, soạn nội dung, kèm mã khuyến mãi cá nhân và hẹn giờ gửi.",
      content: <CampaignForm onSuccess={closeDialog} />,
      size: "xl",
    });
  };

  const handleEdit = (campaign: CampaignDto) => {
    openDialog({
      title: "Cập nhật chiến dịch",
      description: "Chỉ sửa được khi chiến dịch còn là bản nháp hoặc đang hẹn giờ.",
      content: <CampaignForm campaign={campaign} onSuccess={closeDialog} />,
      size: "xl",
    });
  };

  const handleTestSend = (campaign: CampaignDto) => {
    testSend.mutate(campaign.campaignId);
  };

  const handleCancelSchedule = (campaign: CampaignDto) => {
    openAlertDialog({
      title: `Hủy hẹn giờ chiến dịch "${campaign.name}"?`,
      description: `Chiến dịch đang hẹn gửi lúc ${formatDateTime(campaign.scheduledAt)} sẽ quay về bản nháp và không tự gửi.`,
      confirmText: "Hủy hẹn giờ",
      onConfirm: async () => {
        await cancelSchedule.mutateAsync({
          id: campaign.campaignId,
          body: toUpsertRequest(campaign, { scheduledAt: null }),
        });
      },
    });
  };

  const handleDelete = (campaign: CampaignDto) => {
    openAlertDialog({
      title: `Xóa chiến dịch "${campaign.name}"?`,
      description: IRREVERSIBLE_ACTION_MESSAGE,
      confirmText: "Xóa",
      variant: "destructive",
      onConfirm: async () => {
        await deleteCampaign.mutateAsync(campaign.campaignId);
      },
    });
  };

  const columns = createCampaignColumns({
    onEdit: handleEdit,
    onTestSend: handleTestSend,
    onCancelSchedule: handleCancelSchedule,
    onViewReport: (campaign) => void navigate(ROUTES.admin.campaignReport(campaign.campaignId)),
    onDelete: handleDelete,
  });

  const hasActiveFilters = Boolean(params.search) || params.status !== null || params.segmentId !== null;

  return (
    <div>
      <PageHeader
        title="Chiến dịch"
        description="Gửi email hoặc thông báo kèm mã khuyến mãi cá nhân đến từng phân khúc khách hàng, hẹn giờ và theo dõi kết quả."
        action={
          <Button onClick={handleCreate}>
            <Plus className="size-4" />
            <span>Soạn chiến dịch</span>
          </Button>
        }
      />

      <DataTable
        columns={columns}
        data={campaigns.data?.items ?? []}
        isLoading={campaigns.isLoading}
        isError={campaigns.isError}
        error={campaigns.error}
        onRetry={() => void campaigns.refetch()}
        isRefreshing={campaigns.isPlaceholderData}
        totalItems={campaigns.data?.totalItems ?? 0}
        pagination={pagination}
        onPaginationChange={onPaginationChange}
        searchValue={params.search}
        onSearchChange={(value) => setParams({ search: value, page: DEFAULT_PAGE })}
        searchPlaceholder="Tìm theo tên hoặc tiêu đề..."
        hasActiveFilters={hasActiveFilters}
        onClearFilters={() =>
          setParams({ search: "", status: null, segmentId: null, page: DEFAULT_PAGE })
        }
        toolbarFilters={
          <>
            <EnumSelectFilter
              value={params.status}
              onChange={(status) => setParams({ status, page: DEFAULT_PAGE })}
              options={CAMPAIGN_STATUS_OPTIONS}
              placeholder={STATUS_LABEL}
              allLabel="Tất cả trạng thái"
            />
            <EnumSelectFilter
              value={params.segmentId}
              onChange={(segmentId) => setParams({ segmentId, page: DEFAULT_PAGE })}
              options={segmentOptions}
              placeholder="Phân khúc"
              allLabel="Tất cả phân khúc"
            />
          </>
        }
        empty={{
          icon: Megaphone,
          title: "Chưa có chiến dịch",
          description:
            "Soạn chiến dịch đầu tiên để chăm sóc khách theo phân khúc và đo hiệu quả bằng mã khuyến mãi cá nhân.",
          actionLabel: "Soạn chiến dịch",
          onAction: handleCreate,
        }}
      />
    </div>
  );
};

export default AdminCampaignsPage;
