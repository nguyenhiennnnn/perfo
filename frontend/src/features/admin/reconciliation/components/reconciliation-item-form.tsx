import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { FieldGroup } from "@/components/ui/field";
import { FormActions } from "@/components/common/form/form-actions";
import { FormEnumSelectField } from "@/components/common/form/form-enum-select-field";
import { FormTextareaField } from "@/components/common/form/form-textarea-field";
import { useAdminUsersSelect } from "@/features/admin/users/hooks/use-admin-users";
import { useServerFieldErrors } from "@/hooks/use-server-field-errors";
import { formatCurrency } from "@/lib/formatters";
import { useUpdateReconciliationItem } from "../hooks/use-reconciliation";
import type { ReconciliationItemDto, ReconciliationResolution } from "../types";
import { RESOLUTION_OPTIONS, getItemResultLabel } from "../utils/reconciliation-labels";

const NOTE_MAX_LENGTH = 500;

const schema = z.object({
  note: z.string().max(NOTE_MAX_LENGTH, `Ghi chú tối đa ${NOTE_MAX_LENGTH} ký tự`),
  assignee: z.string(),
  resolution: z.enum(["Open", "Resolved", "Ignored"]),
});

type FormValues = z.infer<typeof schema>;

interface ReconciliationItemFormProps {
  item: ReconciliationItemDto;
  onSuccess: () => void;
}

export const ReconciliationItemForm = ({ item, onSuccess }: ReconciliationItemFormProps) => {
  const update = useUpdateReconciliationItem();
  const staff = useAdminUsersSelect({ availableOnly: false });

  const assigneeOptions = (staff.data ?? []).map((u) => ({
    value: String(u.userId),
    label: `${u.fullName} (${u.email})`,
  }));
  if (
    item.assignedToUserId !== null &&
    !assigneeOptions.some((o) => o.value === String(item.assignedToUserId))
  ) {
    assigneeOptions.unshift({
      value: String(item.assignedToUserId),
      label: item.assignedToName ?? `#${item.assignedToUserId}`,
    });
  }

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      note: item.note ?? "",
      assignee: item.assignedToUserId !== null ? String(item.assignedToUserId) : "",
      resolution: item.resolution,
    },
  });
  const applyServerErrors = useServerFieldErrors(form);

  const onSubmit = (values: FormValues) => {
    const body: {
      note?: string;
      assignedToUserId?: number;
      clearAssignee?: boolean;
      resolution?: ReconciliationResolution;
    } = {};
    const note = values.note.trim();
    if (note !== (item.note ?? "")) body.note = note;
    const currentAssignee = item.assignedToUserId !== null ? String(item.assignedToUserId) : "";
    if (values.assignee !== currentAssignee) {
      if (values.assignee) body.assignedToUserId = Number(values.assignee);
      else body.clearAssignee = true;
    }
    if (values.resolution !== item.resolution) body.resolution = values.resolution;

    if (Object.keys(body).length === 0) {
      onSuccess();
      return;
    }
    update.mutate(
      { id: item.itemId, body },
      { onSuccess: () => onSuccess(), onError: applyServerErrors },
    );
  };

  return (
    <form
      id="reconciliation-item-form"
      onSubmit={form.handleSubmit(onSubmit)}
      className="space-y-4"
    >
      <div className="space-y-1 bg-muted/50 p-3 text-sm">
        <p className="font-medium">{item.gatewayTransactionCode}</p>
        <p className="text-muted-foreground">{getItemResultLabel(item.result)}</p>
        <p className="tabular-nums text-muted-foreground">
          Cổng: {item.gatewayAmount !== null ? formatCurrency(item.gatewayAmount) : "—"} · Hệ thống:{" "}
          {item.systemAmount !== null ? formatCurrency(item.systemAmount) : "—"}
        </p>
      </div>

      <FieldGroup disabled={update.isPending}>
        <FormEnumSelectField
          name="resolution"
          control={form.control}
          label="Trạng thái xử lý"
          options={RESOLUTION_OPTIONS}
          placeholder="Chọn trạng thái"
        />
        <FormEnumSelectField
          name="assignee"
          control={form.control}
          label="Người xử lý"
          options={assigneeOptions}
          placeholder="Chưa gán"
        />
        <FormTextareaField
          name="note"
          control={form.control}
          label="Ghi chú"
          placeholder="Nguyên nhân, cách xử lý..."
          rows={4}
        />
      </FieldGroup>

      <FormActions
        isPending={update.isPending}
        isDirty={form.formState.isDirty}
        submitLabel="Lưu"
        onCancel={onSuccess}
      />
    </form>
  );
};
