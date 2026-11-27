import { keepPreviousData, useMutation, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import { useAdminGenres } from "@/features/admin/catalog/hooks/use-admin-genre";
import { useAdminMembershipTiers } from "@/features/admin/catalog/hooks/use-admin-membership-tier";
import { useAdminScreenTypes } from "@/features/admin/catalog/hooks/use-admin-screen-type";
import { customerSegmentsAdminApi } from "../api/customer-segments-api";
import type { CustomerSegmentListParams, UpsertCustomerSegmentRequest } from "../types";
import type { SegmentLookups } from "../utils/segment-condition-fields";

const LOOKUP_PARAMS = { page: 1, pageSize: 100 };

export function useAdminCustomerSegments(params: CustomerSegmentListParams) {
  return useQuery({
    queryKey: queryKeys.adminCustomerSegments.list(params),
    queryFn: () => customerSegmentsAdminApi.getAll(params),
    placeholderData: keepPreviousData,
    select: (res) => res.data,
  });
}

export function useAdminCreateCustomerSegment() {
  return useMutation({
    meta: { invalidates: "customerSegmentChanged", skipErrorToast: true },
    mutationFn: (body: UpsertCustomerSegmentRequest) => customerSegmentsAdminApi.create(body),
  });
}

export function useAdminUpdateCustomerSegment() {
  return useMutation({
    meta: { invalidates: "customerSegmentChanged", skipErrorToast: true },
    mutationFn: ({ id, body }: { id: number; body: UpsertCustomerSegmentRequest }) =>
      customerSegmentsAdminApi.update(id, body),
  });
}

export function useAdminDeleteCustomerSegment() {
  return useMutation({
    meta: { invalidates: "customerSegmentChanged" },
    mutationFn: (id: number) => customerSegmentsAdminApi.remove(id),
  });
}

export function useSegmentLookups(): SegmentLookups {
  const genres = useAdminGenres(LOOKUP_PARAMS);
  const screenTypes = useAdminScreenTypes(LOOKUP_PARAMS);
  const tiers = useAdminMembershipTiers(LOOKUP_PARAMS);

  return {
    genre: (genres.data?.items ?? []).map((g) => ({ value: String(g.genreId), label: g.name })),
    screenType: (screenTypes.data?.items ?? []).map((s) => ({
      value: String(s.screenTypeId),
      label: s.name,
    })),
    tier: (tiers.data?.items ?? []).map((t) => ({ value: String(t.tierId), label: t.tierName })),
  };
}
