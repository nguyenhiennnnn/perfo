import { NotFoundState } from "@/components/common/not-found-state";
import { QueryState } from "@/components/common/query-state";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { Badge } from "@/components/ui/badge";
import { formatDateTime } from "@/lib/formatters";

import { useAdminAuditLogDetail } from "../hooks/use-admin-audit-logs";
import {
  SYSTEM_ACTOR_LABEL,
  getAuditActionLabel,
  getAuditActionVariant,
  getAuditEntityTypeLabel,
} from "../utils/audit-log-labels";
import { AuditLogDiff } from "./audit-log-diff";

interface AuditLogDetailViewProps {
  auditLogId: number;
}

export const AuditLogDetailView = ({ auditLogId }: AuditLogDetailViewProps) => {
  const detail = useAdminAuditLogDetail(auditLogId);

  if (!detail.data) {
    return (
      <QueryState
        query={detail}
        skeleton={
          <div aria-busy="true" className="space-y-4">
            <CardSkeleton cut="sm" className="h-24" />
            <CardSkeleton className="h-56" />
          </div>
        }
        notFound={<NotFoundState entity="dòng nhật ký" />}
        errorTitle="Không tải được chi tiết nhật ký"
        errorClassName="py-8"
      >
        {null}
      </QueryState>
    );
  }

  const log = detail.data;

  const rows: Array<[string, string]> = [
    ["Thời điểm", formatDateTime(log.occurredAt)],
    [
      "Người thực hiện",
      log.actorName
        ? `${log.actorName}${log.actorEmail ? ` (${log.actorEmail})` : ""}`
        : SYSTEM_ACTOR_LABEL,
    ],
    ["Đối tượng", `${getAuditEntityTypeLabel(log.entityType)} #${log.entityId}`],
  ];

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between bg-muted/30 p-3">
        <span className="text-sm text-muted-foreground">Thao tác</span>
        <Badge variant={getAuditActionVariant(log.action)}>{getAuditActionLabel(log.action)}</Badge>
      </div>

      <dl className="grid grid-cols-1 gap-x-4 gap-y-2 sm:grid-cols-[auto_1fr]">
        {rows.map(([label, value]) => (
          <div key={label} className="contents">
            <dt className="text-sm text-muted-foreground">{label}</dt>
            <dd className="break-words text-sm text-foreground">{value}</dd>
          </div>
        ))}
      </dl>

      <AuditLogDiff beforeJson={log.beforeJson} afterJson={log.afterJson} />
    </div>
  );
};
