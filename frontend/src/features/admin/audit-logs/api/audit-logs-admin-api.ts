import { apiGet } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse, OffsetPageResult } from "@/types/api";
import type {
  AuditLogDetailDto,
  AuditLogFilterOptionsDto,
  AuditLogListItemDto,
  AuditLogListParams,
} from "@/features/admin/audit-logs/types";

export const auditLogsAdminApi = {
  getAuditLogs: (params: AuditLogListParams) =>
    apiGet<ApiResponse<OffsetPageResult<AuditLogListItemDto>>>(ENDPOINTS.admin.auditLogs.root, {
      params,
    }),

  getAuditLogDetail: (id: number) =>
    apiGet<ApiResponse<AuditLogDetailDto>>(ENDPOINTS.admin.auditLogs.byId(id)),

  getFilterOptions: () =>
    apiGet<ApiResponse<AuditLogFilterOptionsDto>>(ENDPOINTS.admin.auditLogs.filterOptions),
};
