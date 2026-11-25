import { ExceptionalRefundStatus } from "@/types/enum";

export type RefundProposalStatusFilter = ExceptionalRefundStatus | "All";

export const REFUND_PROPOSAL_STATUS_TABS: { value: RefundProposalStatusFilter; label: string }[] = [
  { value: ExceptionalRefundStatus.Pending, label: "Chờ duyệt" },
  { value: ExceptionalRefundStatus.Approved, label: "Đã duyệt" },
  { value: ExceptionalRefundStatus.Rejected, label: "Từ chối" },
  { value: "All", label: "Tất cả" },
];
