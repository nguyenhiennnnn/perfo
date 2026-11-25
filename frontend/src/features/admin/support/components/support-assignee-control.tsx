import { useMemo } from "react";
import { UserCheck, UserMinus } from "lucide-react";
import { SelectField } from "@/components/common/select-field";
import { Button } from "@/components/ui/button";
import { useAuthRole, useAuthUser } from "@/stores/auth-store";
import { Role } from "@/types/enum";
import { useAssignSupportRequest, useSupportAssigneeCandidates } from "../hooks/use-admin-support";
import type { StaffSupportRequestDto } from "../types";

const UNASSIGNED = "none";

interface AssigneeOption {
  value: string;
  label: string;
}

interface SupportAssigneeControlProps {
  request: StaffSupportRequestDto;
  disabled?: boolean;
}

export const SupportAssigneeControl = ({ request, disabled }: SupportAssigneeControlProps) => {
  const user = useAuthUser();
  const isAdmin = useAuthRole() === Role.Admin;
  const assign = useAssignSupportRequest(request.supportRequestId);
  const candidates = useSupportAssigneeCandidates(isAdmin);
  const isMine = user != null && request.assigneeId === user.userId;
  const isBusy = assign.isPending || disabled;

  const options = useMemo<AssigneeOption[]>(() => {
    const list: AssigneeOption[] = [{ value: UNASSIGNED, label: "Chưa có người phụ trách" }];
    if (user) list.push({ value: String(user.userId), label: `${user.fullName} (tôi)` });
    (candidates.data ?? [])
      .filter((e) => e.isActive && !e.isPendingActivation && e.userId !== user?.userId)
      .forEach((e) => list.push({ value: String(e.userId), label: e.fullName }));
    if (request.assigneeId != null && !list.some((o) => o.value === String(request.assigneeId))) {
      list.push({ value: String(request.assigneeId), label: request.assigneeName ?? "Nhân viên" });
    }
    return list;
  }, [candidates.data, request.assigneeId, request.assigneeName, user]);

  const setAssignee = (assigneeId: number | null) => assign.mutate({ assigneeId });

  if (isAdmin) {
    return (
      <SelectField
        value={request.assigneeId != null ? String(request.assigneeId) : UNASSIGNED}
        onChange={(value) => setAssignee(value === UNASSIGNED ? null : Number(value))}
        options={options}
        getOptionValue={(option) => option.value}
        getOptionLabel={(option) => option.label}
        disabled={isBusy}
      />
    );
  }

  return (
    <div className="flex flex-wrap items-center gap-2">
      <span className="text-sm text-foreground">{request.assigneeName ?? "Chưa có người phụ trách"}</span>
      {user && (
        <Button
          size="sm"
          variant="outline"
          disabled={isBusy}
          onClick={() => setAssignee(isMine ? null : user.userId)}
        >
          {isMine ? <UserMinus className="size-4" /> : <UserCheck className="size-4" />}
          {isMine ? "Bỏ nhận" : "Nhận xử lý"}
        </Button>
      )}
    </div>
  );
};
