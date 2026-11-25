import { keepPreviousData, useMutation, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import { SupportRequestStatus } from "@/types/enum";
import { employeesAdminApi } from "@/features/admin/employees/api/employee-api";
import { ASSIGNEE_PICKER_PAGE_SIZE, SUPPORT_DETAIL_POLL_INTERVAL_MS } from "../constants";
import { supportStaffApi } from "../api/support-staff-api";
import type {
  AssignSupportRequestBody,
  ChangeSupportRequestStatusBody,
  IssueSupportApologyCodeBody,
  StaffReplySupportRequestBody,
  StaffSupportRequestListParams,
} from "../types";

export function useAdminSupportRequests(params: StaffSupportRequestListParams) {
  return useQuery({
    queryKey: queryKeys.adminSupportRequests.list(params),
    queryFn: () => supportStaffApi.getList(params),
    placeholderData: keepPreviousData,
    select: (res) => res.data,
  });
}

export function useAdminSupportRequestDetail(requestId: number) {
  return useQuery({
    queryKey: queryKeys.adminSupportRequests.detail(requestId),
    queryFn: () => supportStaffApi.getById(requestId),
    enabled: requestId > 0,
    refetchInterval: (query) =>
      query.state.data?.data.status === SupportRequestStatus.Resolved
        ? false
        : SUPPORT_DETAIL_POLL_INTERVAL_MS,
    select: (res) => res.data,
  });
}

export function useAssignSupportRequest(requestId: number) {
  return useMutation({
    meta: { invalidates: "supportStaffChanged" },
    mutationFn: (body: AssignSupportRequestBody) => supportStaffApi.assign(requestId, body),
  });
}

export function useChangeSupportRequestStatus(requestId: number) {
  return useMutation({
    meta: { invalidates: "supportStaffChanged" },
    mutationFn: (body: ChangeSupportRequestStatusBody) =>
      supportStaffApi.changeStatus(requestId, body),
  });
}

export function useStaffReplySupportRequest(requestId: number) {
  return useMutation({
    meta: { invalidates: "supportStaffChanged" },
    mutationFn: (body: StaffReplySupportRequestBody) => supportStaffApi.reply(requestId, body),
  });
}

export function useIssueSupportApologyCode(requestId: number) {
  return useMutation({
    meta: { invalidates: "supportStaffChanged" },
    mutationFn: (body: IssueSupportApologyCodeBody) =>
      supportStaffApi.issueApologyCode(requestId, body),
  });
}

export function useResendSupportTicketEmail(requestId: number) {
  return useMutation({
    meta: { invalidates: "supportStaffChanged" },
    mutationFn: () => supportStaffApi.resendTicketEmail(requestId),
  });
}

export function useSupportAssigneeCandidates(enabled: boolean) {
  return useQuery({
    queryKey: queryKeys.adminEmployees.list({ pageSize: ASSIGNEE_PICKER_PAGE_SIZE }),
    queryFn: () => employeesAdminApi.getEmployees({ pageSize: ASSIGNEE_PICKER_PAGE_SIZE }),
    enabled,
    select: (res) => res.data.items,
  });
}
