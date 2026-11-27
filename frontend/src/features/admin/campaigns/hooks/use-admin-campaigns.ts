import { keepPreviousData, useMutation, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import { CampaignStatus } from "@/types/enum";
import { campaignsAdminApi } from "../api/campaigns-api";
import type { CampaignListParams, UpsertCampaignRequest } from "../types";
import { CAMPAIGN_SENDING_REFETCH_MS } from "../utils/campaign-labels";

export function useAdminCampaigns(params: CampaignListParams) {
  return useQuery({
    queryKey: queryKeys.adminCampaigns.list(params),
    queryFn: () => campaignsAdminApi.getAll(params),
    placeholderData: keepPreviousData,
    refetchInterval: (query) =>
      query.state.data?.data.items.some((item) => item.status === CampaignStatus.Sending)
        ? CAMPAIGN_SENDING_REFETCH_MS
        : false,
    select: (res) => res.data,
  });
}

export function useAdminCampaign(id: number, enabled = true) {
  return useQuery({
    queryKey: queryKeys.adminCampaigns.detail(id),
    queryFn: () => campaignsAdminApi.getById(id),
    enabled: enabled && id > 0,
    select: (res) => res.data,
  });
}

export function useAdminCampaignReport(id: number) {
  return useQuery({
    queryKey: queryKeys.adminCampaigns.report(id),
    queryFn: () => campaignsAdminApi.getReport(id),
    enabled: id > 0,
    placeholderData: keepPreviousData,
    refetchInterval: (query) =>
      query.state.data?.data.status === CampaignStatus.Sending
        ? CAMPAIGN_SENDING_REFETCH_MS
        : false,
    select: (res) => res.data,
  });
}

export function useAdminCreateCampaign() {
  return useMutation({
    meta: { invalidates: "campaignChanged", skipErrorToast: true },
    mutationFn: (body: UpsertCampaignRequest) => campaignsAdminApi.create(body),
  });
}

export function useAdminUpdateCampaign() {
  return useMutation({
    meta: { invalidates: "campaignChanged", skipErrorToast: true },
    mutationFn: ({ id, body }: { id: number; body: UpsertCampaignRequest }) =>
      campaignsAdminApi.update(id, body),
  });
}

export function useAdminCancelCampaignSchedule() {
  return useMutation({
    meta: { invalidates: "campaignChanged" },
    mutationFn: ({ id, body }: { id: number; body: UpsertCampaignRequest }) =>
      campaignsAdminApi.update(id, body),
  });
}

export function useAdminDeleteCampaign() {
  return useMutation({
    meta: { invalidates: "campaignChanged" },
    mutationFn: (id: number) => campaignsAdminApi.remove(id),
  });
}

export function useAdminTestSendCampaign() {
  return useMutation({
    meta: { invalidates: "campaignChanged" },
    mutationFn: (id: number) => campaignsAdminApi.testSend(id),
  });
}
