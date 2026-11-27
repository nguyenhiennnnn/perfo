import { Users } from "lucide-react";

import { LineSkeleton } from "@/components/common/skeleton-shapes";
import { formatNumber } from "@/lib/formatters";

import { useAdminCampaign } from "../hooks/use-admin-campaigns";

interface CampaignAudienceSummaryProps {
  campaignId: number;
}

export const CampaignAudienceSummary = ({ campaignId }: CampaignAudienceSummaryProps) => {
  const campaign = useAdminCampaign(campaignId);
  const audience = campaign.data?.audience;

  if (campaign.isLoading) {
    return (
      <div aria-busy="true" className="space-y-2 border border-border bg-muted/40 p-3">
        <LineSkeleton className="h-4 w-40" />
        <LineSkeleton className="h-4 w-full" />
      </div>
    );
  }

  if (!audience) return null;

  const deductions = [
    { label: "Chưa đồng ý nhận thông tin", value: audience.withoutConsent },
    { label: "Thiếu thông tin liên hệ", value: audience.withoutContact },
    { label: "Đã nhận chiến dịch khác gần đây", value: audience.recentlyContacted },
  ];

  return (
    <div className="space-y-3 border border-border bg-muted/40 p-3">
      <div className="flex items-center gap-2">
        <Users className="size-4 text-muted-foreground" />
        <p className="text-sm font-medium text-foreground">Đối tượng nhận theo bản đã lưu</p>
      </div>
      <dl className="grid grid-cols-2 gap-x-4 gap-y-2 text-sm sm:grid-cols-4">
        <div>
          <dt className="text-xs text-muted-foreground">Khách trong phân khúc</dt>
          <dd className="font-semibold tabular-nums text-foreground">
            {formatNumber(audience.segmentCustomers)}
          </dd>
        </div>
        {deductions.map((item) => (
          <div key={item.label}>
            <dt className="text-xs text-muted-foreground">{item.label}</dt>
            <dd className="font-semibold tabular-nums text-foreground">
              {formatNumber(item.value)}
            </dd>
          </div>
        ))}
      </dl>
      <p className="text-sm text-foreground">
        Sẽ nhận chiến dịch:{" "}
        <span className="font-semibold tabular-nums">{formatNumber(audience.eligible)}</span> khách
      </p>
    </div>
  );
};
