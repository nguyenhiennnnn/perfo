import { ArrowLeft, BarChart3 } from "lucide-react";
import { Link, useParams } from "react-router";

import { EmptyState } from "@/components/common/empty-state";
import { NotFoundState } from "@/components/common/not-found-state";
import { PageHeader } from "@/components/common/page-header";
import { QueryState } from "@/components/common/query-state";
import { SkeletonList } from "@/components/common/skeleton-list";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { ROUTES } from "@/config/routes";
import { cn } from "@/lib/utils";
import { formatCurrency, formatDateTime, formatNumber, formatPercent } from "@/lib/formatters";
import { CampaignChannel, CampaignStatus } from "@/types/enum";

import { CampaignStatusBadge } from "../components/campaign-status-badge";
import { useAdminCampaign, useAdminCampaignReport } from "../hooks/use-admin-campaigns";
import type { CampaignDto, CampaignReportDto } from "../types";
import {
  CAMPAIGN_SENDING_REFETCH_MS,
  describeCampaignPromo,
  getCampaignChannelLabel,
  hasCampaignResults,
} from "../utils/campaign-labels";

interface Metric {
  label: string;
  value: string;
  hint?: string;
}

const MetricGrid = ({ metrics }: { metrics: Metric[] }) => (
  <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
    {metrics.map((metric) => (
      <Card key={metric.label} className="gap-2 py-4">
        <CardContent className="px-4">
          <p className="text-xs font-medium text-muted-foreground">{metric.label}</p>
          <p className="mt-1.5 truncate text-xl font-semibold tabular-nums text-foreground">
            {metric.value}
          </p>
          {metric.hint && <p className="mt-0.5 text-xs text-muted-foreground">{metric.hint}</p>}
        </CardContent>
      </Card>
    ))}
  </div>
);

const SectionHeading = ({ title, description }: { title: string; description?: string }) => (
  <div className="space-y-0.5">
    <h2 className="text-sm font-semibold text-foreground">{title}</h2>
    {description && <p className="text-xs text-muted-foreground">{description}</p>}
  </div>
);

const ReportSkeleton = () => (
  <div aria-busy="true" className="space-y-4">
    <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
      <SkeletonList count={8} className="h-24" />
    </div>
    <CardSkeleton className="h-32" />
  </div>
);

const DeliveryBar = ({ report }: { report: CampaignReportDto }) => {
  const segments = [
    { key: "sent", label: "Đã gửi", value: report.sent, swatch: "bg-success" },
    { key: "failed", label: "Thất bại", value: report.failed, swatch: "bg-destructive" },
    { key: "skipped", label: "Bỏ qua", value: report.skipped, swatch: "bg-chart-5" },
    { key: "pending", label: "Đang chờ", value: report.pending, swatch: "bg-warning" },
  ];
  const total = segments.reduce((sum, segment) => sum + segment.value, 0);
  if (total <= 0) return null;

  const summary = segments
    .map((segment) => `${segment.label} ${formatNumber(segment.value)}`)
    .join(", ");

  return (
    <Card>
      <CardHeader>
        <CardTitle>Phân bổ theo trạng thái gửi</CardTitle>
        <CardDescription>
          Tỷ lệ từng trạng thái trên tổng {formatNumber(total)} người nhận.
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        <div role="img" aria-label={summary} className="flex h-3 w-full overflow-hidden bg-muted">
          {segments.map(
            (segment) =>
              segment.value > 0 && (
                <div
                  key={segment.key}
                  className={segment.swatch}
                  style={{ width: `${(segment.value / total) * 100}%` }}
                />
              ),
          )}
        </div>
        <ul className="grid grid-cols-2 gap-x-4 gap-y-2 text-sm lg:grid-cols-4">
          {segments.map((segment) => (
            <li key={segment.key} className="flex items-center gap-2">
              <span aria-hidden="true" className={cn("size-2.5 shrink-0", segment.swatch)} />
              <span className="text-muted-foreground">{segment.label}</span>
              <span className="ml-auto font-medium tabular-nums text-foreground">
                {formatNumber(segment.value)}
                <span className="ml-1 text-xs font-normal text-muted-foreground">
                  ({formatPercent((segment.value / total) * 100)})
                </span>
              </span>
            </li>
          ))}
        </ul>
      </CardContent>
    </Card>
  );
};

interface ReportContentProps {
  report: CampaignReportDto;
  campaign: CampaignDto | undefined;
}

const ReportContent = ({ report, campaign }: ReportContentProps) => {
  if (!hasCampaignResults(report.status) || report.recipients <= 0) {
    return (
      <EmptyState
        icon={BarChart3}
        title="Chưa có kết quả, chiến dịch chưa bắt đầu gửi"
      />
    );
  }

  const isEmail = campaign?.channel === CampaignChannel.Email;
  const hasPromo = Boolean(campaign?.promo) || report.codesIssued > 0;
  const isSending = report.status === CampaignStatus.Sending;

  const deliveryMetrics: Metric[] = [
    { label: "Người nhận", value: formatNumber(report.recipients) },
    { label: "Đã gửi", value: formatNumber(report.sent) },
    { label: "Thất bại", value: formatNumber(report.failed) },
    { label: "Bỏ qua", value: formatNumber(report.skipped) },
    { label: "Đang chờ", value: formatNumber(report.pending) },
  ];

  const engagementMetrics: Metric[] = [
    { label: "Đã mở", value: formatNumber(report.opened) },
    { label: "Tỷ lệ mở", value: formatPercent(report.openRate * 100), hint: "trên số đã gửi" },
  ];

  const promoMetrics: Metric[] = [
    { label: "Mã đã tạo", value: formatNumber(report.codesIssued) },
    { label: "Khách dùng mã", value: formatNumber(report.codeUsers) },
    {
      label: "Tỷ lệ dùng mã",
      value: formatPercent(report.codeUseRate * 100),
      hint: "trên số đã gửi",
    },
    { label: "Số đơn", value: formatNumber(report.orders) },
    { label: "Tiền giảm", value: formatCurrency(report.discountAmount) },
    { label: "Doanh thu", value: formatCurrency(report.revenue) },
  ];

  return (
    <div className="space-y-6">
      <section className="space-y-3">
        <SectionHeading
          title="Gửi"
          description={
            isSending
              ? `Chiến dịch đang gửi, số liệu tự làm mới mỗi ${CAMPAIGN_SENDING_REFETCH_MS / 1000} giây.`
              : undefined
          }
        />
        <MetricGrid metrics={deliveryMetrics} />
        <DeliveryBar report={report} />
      </section>

      {isEmail && (
        <section className="space-y-3">
          <SectionHeading title="Tương tác" />
          <MetricGrid metrics={engagementMetrics} />
          <p className="text-xs text-muted-foreground">
            Lượt mở được đo bằng điểm ảnh theo dõi trong email. Một số ứng dụng email chặn ảnh nên
            số thực tế có thể cao hơn.
          </p>
        </section>
      )}

      {hasPromo && (
        <section className="space-y-3">
          <SectionHeading title="Mã khuyến mãi" />
          <MetricGrid metrics={promoMetrics} />
          <p className="text-xs text-muted-foreground">
            Doanh thu là doanh thu thuần sau hoàn tiền của các đơn đã xác nhận có dùng mã của chiến
            dịch.
          </p>
        </section>
      )}
    </div>
  );
};

const describeCampaign = (campaign: CampaignDto | undefined) => {
  if (!campaign) return "Kết quả gửi và hiệu quả của chiến dịch.";
  return [
    campaign.segmentName,
    getCampaignChannelLabel(campaign.channel),
    campaign.scheduledAt ? `Gửi lúc ${formatDateTime(campaign.scheduledAt)}` : "Chưa hẹn giờ",
    describeCampaignPromo(campaign.promo),
  ].join(" · ");
};

const AdminCampaignReportPage = () => {
  const { id } = useParams();
  const campaignId = Number(id) || 0;

  const report = useAdminCampaignReport(campaignId);
  const campaignQuery = useAdminCampaign(campaignId);
  const campaign = campaignQuery.data;

  const name = campaign?.name ?? report.data?.name;
  const status = report.data?.status ?? campaign?.status;

  return (
    <div className="space-y-6">
      <PageHeader
        title={name ? `Kết quả · ${name}` : "Kết quả chiến dịch"}
        description={describeCampaign(campaign)}
        action={
          <div className="flex flex-wrap items-center gap-2">
            {status && <CampaignStatusBadge status={status} />}
            <Button variant="outline" render={<Link to={ROUTES.admin.campaigns} />}>
              <ArrowLeft className="size-4" />
              <span>Danh sách chiến dịch</span>
            </Button>
          </div>
        }
      />

      <QueryState
        query={report}
        skeleton={<ReportSkeleton />}
        notFound={<NotFoundState entity="chiến dịch" />}
        errorTitle="Không tải được kết quả chiến dịch"
      >
        {(data) => <ReportContent report={data} campaign={campaign} />}
      </QueryState>
    </div>
  );
};

export default AdminCampaignReportPage;
