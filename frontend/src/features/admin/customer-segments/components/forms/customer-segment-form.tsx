import { useState } from "react";
import { Controller, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";

import { FieldGroup } from "@/components/ui/field";
import { FormInputField } from "@/components/common/form/form-input-field";
import { FormSwitchField } from "@/components/common/form/form-switch-field";
import { FormTextareaField } from "@/components/common/form/form-textarea-field";
import { FormActions } from "@/components/common/form/form-actions";
import { FormErrorAlert } from "@/components/common/form/form-error-alert";
import { useServerFieldErrors } from "@/hooks/use-server-field-errors";
import { FORM_SAVE_CHANGES_LABEL } from "@/config/default-constants";

import {
  useAdminCreateCustomerSegment,
  useAdminUpdateCustomerSegment,
  useSegmentLookups,
} from "../../hooks/use-admin-customer-segments";
import type { CustomerSegmentDto, UpsertCustomerSegmentRequest } from "../../types";
import { toDraftRoot, toRequestNode } from "../../utils/segment-condition-fields";
import {
  customerSegmentFormSchema,
  type CustomerSegmentFormValues,
} from "./customer-segment-schema";
import { SegmentConditionBuilder } from "./segment-condition-builder";

interface CustomerSegmentFormProps {
  segment?: CustomerSegmentDto;
  onSuccess: () => void;
}

export const CustomerSegmentForm = ({ segment, onSuccess }: CustomerSegmentFormProps) => {
  const isEdit = Boolean(segment);
  const createSegment = useAdminCreateCustomerSegment();
  const updateSegment = useAdminUpdateCustomerSegment();
  const mutation = isEdit ? updateSegment : createSegment;
  const isPending = mutation.isPending;
  const lookups = useSegmentLookups();
  const [initialConditions] = useState(() => toDraftRoot(segment?.conditions));

  const form = useForm<CustomerSegmentFormValues>({
    resolver: zodResolver(customerSegmentFormSchema),
    defaultValues: {
      name: segment?.name ?? "",
      description: segment?.description ?? "",
      isActive: segment?.isActive ?? true,
      conditions: initialConditions,
    },
  });
  const applyServerErrors = useServerFieldErrors(form);

  const onSubmit = (values: CustomerSegmentFormValues) => {
    const body: UpsertCustomerSegmentRequest = {
      name: values.name,
      description: values.description === "" ? null : values.description,
      conditions: toRequestNode(values.conditions),
      isActive: values.isActive,
    };
    const options = { onSuccess: () => onSuccess(), onError: applyServerErrors };

    if (segment) {
      updateSegment.mutate({ id: segment.segmentId, body }, options);
    } else {
      createSegment.mutate(body, options);
    }
  };

  return (
    <form
      id="customer-segment-form"
      onSubmit={form.handleSubmit(onSubmit)}
      className="max-h-[70vh] space-y-4 overflow-y-auto px-0.5"
    >
      <FieldGroup disabled={isPending}>
        <FormInputField
          name="name"
          control={form.control}
          label="Tên phân khúc"
          placeholder="Khách chi tiêu cao thích phim hành động"
          autoComplete="off"
          autoFocus={!isEdit}
        />
        <FormTextareaField
          name="description"
          control={form.control}
          label="Mô tả (không bắt buộc)"
          placeholder="Mục đích sử dụng phân khúc này"
        />

        <Controller
          name="conditions"
          control={form.control}
          render={({ field, fieldState }) => (
            <SegmentConditionBuilder
              value={field.value}
              onChange={field.onChange}
              lookups={lookups}
              error={fieldState.error?.message}
            />
          )}
        />
        <p className="-mt-2 text-xs text-muted-foreground">
          Số khách được tính từ các chỉ số cập nhật định kỳ. Chỉ khách đã bật nhận thông tin tiếp
          thị mới được tính là đủ điều kiện nhận thông tin.
        </p>

        <FormSwitchField name="isActive" control={form.control} label="Đang hoạt động" />
      </FieldGroup>

      <FormErrorAlert error={mutation.error} preferServerMessage />

      <FormActions
        isPending={isPending}
        isDirty={form.formState.isDirty}
        submitLabel={isEdit ? FORM_SAVE_CHANGES_LABEL : "Tạo phân khúc"}
        onCancel={onSuccess}
      />
    </form>
  );
};
