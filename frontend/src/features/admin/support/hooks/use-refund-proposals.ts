import { keepPreviousData, useMutation, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import { refundProposalsApi } from "../api/refund-proposals-api";
import type {
  ApproveRefundProposalBody,
  CreateRefundProposalBody,
  RefundProposalListParams,
  RejectRefundProposalBody,
} from "../types";

export function useRefundProposals(params: RefundProposalListParams, enabled = true) {
  return useQuery({
    queryKey: queryKeys.adminRefundProposals.list(params),
    queryFn: () => refundProposalsApi.getList(params),
    placeholderData: keepPreviousData,
    enabled,
    select: (res) => res.data,
  });
}

export function useProposeRefund() {
  return useMutation({
    meta: { invalidates: "refundProposalChanged" },
    mutationFn: (body: CreateRefundProposalBody) => refundProposalsApi.propose(body),
  });
}

export function useApproveRefundProposal(proposalId: number) {
  return useMutation({
    meta: { invalidates: "refundProposalChanged" },
    mutationFn: (body: ApproveRefundProposalBody) => refundProposalsApi.approve(proposalId, body),
  });
}

export function useRejectRefundProposal(proposalId: number) {
  return useMutation({
    meta: { invalidates: "refundProposalChanged" },
    mutationFn: (body: RejectRefundProposalBody) => refundProposalsApi.reject(proposalId, body),
  });
}
