import { ApproveRefundDialogContent } from "../components/approve-refund-dialog-content";
import { RejectRefundDialogContent } from "../components/reject-refund-dialog-content";
import { useDialogOpen } from "@/stores/dialog-store";
import { formatCurrency } from "@/lib/formatters";
import type { ExceptionalRefundProposalDto } from "../types";

export function useRefundProposalActions() {
  const openDialog = useDialogOpen();

  return {
    approve: (proposal: ExceptionalRefundProposalDto) =>
      openDialog({
        title: "Duyệt hoàn tiền ngoại lệ",
        description: `Đơn ${proposal.orderCode} · ${formatCurrency(proposal.amount)}`,
        content: <ApproveRefundDialogContent proposal={proposal} />,
      }),
    reject: (proposal: ExceptionalRefundProposalDto) =>
      openDialog({
        title: "Từ chối đề xuất hoàn tiền",
        description: `Đơn ${proposal.orderCode} · ${formatCurrency(proposal.amount)}`,
        content: <RejectRefundDialogContent proposalId={proposal.exceptionalRefundProposalId} />,
      }),
  };
}
