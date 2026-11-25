import { SelectField } from "@/components/common/select-field";
import { SupportRequestStatus } from "@/types/enum";
import { useChangeSupportRequestStatus } from "../hooks/use-admin-support";
import { SUPPORT_SETTABLE_STATUS_OPTIONS } from "../utils/support-labels";

interface SupportStatusControlProps {
  requestId: number;
  status: SupportRequestStatus;
}

export const SupportStatusControl = ({ requestId, status }: SupportStatusControlProps) => {
  const changeStatus = useChangeSupportRequestStatus(requestId);

  return (
    <SelectField
      value={status === SupportRequestStatus.New ? null : status}
      onChange={(value) => {
        if (value !== status) changeStatus.mutate({ status: value as SupportRequestStatus });
      }}
      options={SUPPORT_SETTABLE_STATUS_OPTIONS}
      getOptionValue={(option) => option.value}
      getOptionLabel={(option) => option.label}
      placeholder="Mới"
      disabled={changeStatus.isPending}
    />
  );
};
