import { ArrowLeft } from "lucide-react";
import { Link, useParams } from "react-router";
import { PageHeader } from "@/components/common/page-header";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { QueryState } from "@/components/common/query-state";
import { ROUTES } from "@/config/routes";
import { cn } from "@/lib/utils";
import { formatCurrency, formatDate, formatDateTime, formatNumber } from "@/lib/formatters";
import { ReconciliationItemsTable } from "../components/reconciliation-items-table";
import { useReconciliationSession } from "../hooks/use-reconciliation";
import { RESULT_STATS, SESSION_STATUS, getGatewayLabel } from "../utils/reconciliation-labels";

const Metric = ({ value, label, className }: { value: string; label: string; className?: string }) => (
  <div>
    <p className={cn("t-numeral text-4xl text-foreground", className)}>{value}</p>
    <p className="mt-1 text-xs text-muted-foreground">{label}</p>
  </div>
);

const AdminReconciliationDetailPage = () => {
  const { id } = useParams();
  const sessionId = Number(id) || 0;
  const query = useReconciliationSession(sessionId);
  const session = query.data;
  const status = session ? SESSION_STATUS[session.status] : undefined;

  return (
    <div className="space-y-4">
      <PageHeader
        title={session ? `Phiên đối soát ${getGatewayLabel(session.gateway)} #${session.sessionId}` : "Phiên đối soát"}
        description={
          session
            ? session.periodFrom && session.periodTo
              ? `${formatDate(session.periodFrom)} – ${formatDate(session.periodTo)}`
              : (session.fileName ?? undefined)
            : undefined
        }
        action={
          <Button variant="outline" render={<Link to={ROUTES.admin.reconciliation} />}>
            <ArrowLeft className="size-4" />
            <span>Danh sách phiên</span>
          </Button>
        }
      />

      <QueryState
        query={query}
        skeleton={<CardSkeleton className="h-64" />}
        errorTitle="Không tải được phiên đối soát"
      >
        {(s) => (
          <div className="space-y-4">
            <Card>
              <CardHeader>
                <CardTitle className="flex flex-wrap items-center gap-2">
                  Tổng quan
                  {status && <Badge variant={status.variant}>{status.label}</Badge>}
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                {s.errorMessage && (
                  <p className="bg-destructive/10 p-3 text-sm text-destructive">{s.errorMessage}</p>
                )}
                <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
                  <Metric value={formatNumber(s.totalRows)} label="Tổng số dòng" />
                  <Metric value={formatCurrency(s.gatewayTotalAmount)} label="Tổng tiền theo cổng" />
                  <Metric value={formatCurrency(s.systemTotalAmount)} label="Tổng tiền theo hệ thống" />
                  <Metric
                    value={formatCurrency(s.gatewayTotalAmount - s.systemTotalAmount)}
                    label="Chênh lệch (cổng − hệ thống)"
                  />
                </div>
                <p className="text-xs text-muted-foreground">
                  Tạo bởi {s.createdByName ?? "—"} lúc {formatDateTime(s.createdAt)}
                  {s.completedAt && `, hoàn tất ${formatDateTime(s.completedAt)}`}.
                </p>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>Phân loại kết quả</CardTitle>
              </CardHeader>
              <CardContent className="grid grid-cols-2 gap-4 lg:grid-cols-3">
                {RESULT_STATS.map((stat) => {
                  const value = s[stat.key];
                  return (
                    <Metric
                      key={stat.key}
                      value={formatNumber(value)}
                      label={stat.label}
                      className={stat.tone === "ok" ? "text-success" : value > 0 ? "text-warning-text" : undefined}
                    />
                  );
                })}
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>Tiến độ xử lý chênh lệch</CardTitle>
              </CardHeader>
              <CardContent className="grid grid-cols-3 gap-4">
                <Metric value={formatNumber(s.openItemCount)} label="Đang mở" />
                <Metric value={formatNumber(s.resolvedItemCount)} label="Đã xử lý" />
                <Metric value={formatNumber(s.ignoredItemCount)} label="Đã bỏ qua" />
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>Dòng bất thường</CardTitle>
              </CardHeader>
              <CardContent>
                <ReconciliationItemsTable fixedSessionId={s.sessionId} />
              </CardContent>
            </Card>
          </div>
        )}
      </QueryState>
    </div>
  );
};

export default AdminReconciliationDetailPage;
