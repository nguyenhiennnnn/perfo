import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import { MS_PER_MINUTE } from "@/config/default-constants";
import type { AuditLogListParams } from "@/features/admin/audit-logs/types";
import { auditLogsAdminApi } from "../api/audit-logs-admin-api";

export function useAdminAuditLogs(params: AuditLogListParams) {
  return useQuery({
    queryKey: queryKeys.adminAuditLogs.list(params),
    queryFn: () => auditLogsAdminApi.getAuditLogs(params),
    placeholderData: keepPreviousData,
    select: (res) => res.data,
  });
}

export function useAdminAuditLogDetail(id: number) {
  return useQuery({
    queryKey: queryKeys.adminAuditLogs.detail(id),
    queryFn: () => auditLogsAdminApi.getAuditLogDetail(id),
    enabled: id > 0,
    staleTime: Infinity,
    select: (res) => res.data,
  });
}

export function useAdminAuditLogFilterOptions() {
  return useQuery({
    queryKey: queryKeys.adminAuditLogs.filterOptions(),
    queryFn: () => auditLogsAdminApi.getFilterOptions(),
    staleTime: MS_PER_MINUTE,
    select: (res) => res.data,
  });
}
