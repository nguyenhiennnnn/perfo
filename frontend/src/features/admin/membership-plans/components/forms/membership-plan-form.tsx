import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";

import { FieldGroup } from "@/components/ui/field";
import { FormInputField } from "@/components/common/form/form-input-field";
import { FormSelectField } from "@/components/common/form/form-select-field";
import { FormSwitchField } from "@/components/common/form/form-switch-field";
import { FormTextareaField } from "@/components/common/form/form-textarea-field";
import { FormActions } from "@/components/common/form/form-actions";
import { FormErrorAlert } from "@/components/common/form/form-error-alert";
import { useServerFieldErrors } from "@/hooks/use-server-field-errors";
import { FORM_SAVE_CHANGES_LABEL, PRICE_INPUT_STEP } from "@/config/default-constants";
import { formatCurrency } from "@/lib/formatters";

import {
  useAdminCreateMembershipPlan,
  useAdminUpdateMembershipPlan,
} from "../../hooks/use-admin-membership-plans";
import type { AdminMembershipPlanDto, UpsertMembershipPlanRequest } from "../../types";
import {
  MEMBERSHIP_PLAN_MAX_FREE_TICKETS,
  PLAN_PERIOD_MONTHS,
  PLAN_PERIOD_OPTIONS,
} from "../../utils/membership-plan-admin-labels";
import { MembershipRefundPolicyNote } from "../membership-refund-policy-note";
import { membershipPlanFormSchema, type MembershipPlanFormValues } from "./membership-plan-schema";

interface MembershipPlanFormProps {
  plan?: AdminMembershipPlanDto;
  onSuccess: () => void;
}

const toText = (value?: number) => (value === undefined ? "" : String(value));

const parseNumber = (value: string) => {
  const number = Number(value);
  return value.trim() !== "" && Number.isFinite(number) ? number : 0;
};

export const MembershipPlanForm = ({ plan, onSuccess }: MembershipPlanFormProps) => {
  const isEdit = Boolean(plan);
  const createPlan = useAdminCreateMembershipPlan();
  const updatePlan = useAdminUpdateMembershipPlan();
  const mutation = isEdit ? updatePlan : createPlan;
  const isPending = mutation.isPending;

  const form = useForm<MembershipPlanFormValues>({
    resolver: zodResolver(membershipPlanFormSchema),
    defaultValues: {
      name: plan?.name ?? "",
      description: plan?.description ?? "",
      period: plan?.period ?? "Monthly",
      price: toText(plan?.price),
      ticketDiscountPercent: toText(plan?.ticketDiscountPercent ?? 0),
      freeTicketsPerMonth: toText(plan?.freeTicketsPerMonth ?? 0),
      freeTicketConversionValue: toText(plan?.freeTicketConversionValue ?? 0),
      comboDiscountPercent: toText(plan?.comboDiscountPercent ?? 0),
      earlyAccess: plan?.earlyAccess ?? false,
      stackableWithPromotions: plan?.stackableWithPromotions ?? false,
      isActive: plan?.isActive ?? true,
    },
  });
  const applyServerErrors = useServerFieldErrors(form);

  const [period, price, freeTickets, conversion] = form.watch([
    "period",
    "price",
    "freeTicketsPerMonth",
    "freeTicketConversionValue",
  ]);
  const totalConversion =
    parseNumber(conversion) * parseNumber(freeTickets) * PLAN_PERIOD_MONTHS[period];

  const onSubmit = (values: MembershipPlanFormValues) => {
    const body: UpsertMembershipPlanRequest = {
      name: values.name,
      description: values.description === "" ? null : values.description,
      period: values.period,
      price: Number(values.price),
      ticketDiscountPercent: Number(values.ticketDiscountPercent),
      freeTicketsPerMonth: Number(values.freeTicketsPerMonth),
      comboDiscountPercent: Number(values.comboDiscountPercent),
      earlyAccess: values.earlyAccess,
      stackableWithPromotions: values.stackableWithPromotions,
      isActive: values.isActive,
      freeTicketConversionValue: Number(values.freeTicketConversionValue),
    };
    const options = { onSuccess: () => onSuccess(), onError: applyServerErrors };

    if (plan) {
      updatePlan.mutate({ id: plan.membershipPlanId, body }, options);
    } else {
      createPlan.mutate(body, options);
    }
  };

  return (
    <form
      id="membership-plan-form"
      onSubmit={form.handleSubmit(onSubmit)}
      className="max-h-[70vh] space-y-4 overflow-y-auto px-0.5"
    >
      <FieldGroup disabled={isPending}>
        <FormInputField
          name="name"
          control={form.control}
          label="Tên gói"
          placeholder="Perfo Plus, Perfo Premium..."
          autoComplete="off"
          autoFocus={!isEdit}
        />
        <FormTextareaField
          name="description"
          control={form.control}
          label="Mô tả hiển thị cho khách (không bắt buộc)"
          placeholder="Giảm giá vé, vé miễn phí mỗi tháng và đặt sớm suất đặc biệt"
        />

        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <FormSelectField
            name="period"
            control={form.control}
            label="Thời hạn"
            options={PLAN_PERIOD_OPTIONS}
            getOptionValue={(o) => o.value}
            getOptionLabel={(o) => o.label}
            className="w-full"
          />
          <FormInputField
            name="price"
            control={form.control}
            label="Giá gói (đồng)"
            type="number"
            min={1}
            step={PRICE_INPUT_STEP}
            placeholder="199000"
          />
        </div>

        <p className="text-sm font-medium text-foreground">Quyền lợi</p>
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <FormInputField
            name="ticketDiscountPercent"
            control={form.control}
            label="Giảm giá vé (%)"
            type="number"
            min={0}
            max={100}
            step={0.5}
          />
          <FormInputField
            name="comboDiscountPercent"
            control={form.control}
            label="Giảm giá combo (%)"
            type="number"
            min={0}
            max={100}
            step={0.5}
          />
          <FormInputField
            name="freeTicketsPerMonth"
            control={form.control}
            label="Vé miễn phí mỗi tháng"
            type="number"
            min={0}
            max={MEMBERSHIP_PLAN_MAX_FREE_TICKETS}
            step={1}
          />
          <FormInputField
            name="freeTicketConversionValue"
            control={form.control}
            label="Giá trị quy đổi mỗi vé miễn phí (đồng)"
            type="number"
            min={0}
            step={PRICE_INPUT_STEP}
          />
        </div>
        <p className="-mt-2 text-xs text-muted-foreground">
          Khi khách dùng vé miễn phí, phần giá trị quy đổi được chuyển từ doanh thu gói sang doanh
          thu vé của suất chiếu (và tính vào số chia cho nhà phát hành). Vé miễn phí không cộng dồn
          sang tháng sau.
          {parseNumber(freeTickets) > 0 && parseNumber(price) > 0 ? (
            <>
              {" "}
              Tổng giá trị quy đổi tối đa cả kỳ:{" "}
              <span className="font-medium tabular-nums text-foreground">
                {formatCurrency(totalConversion)}
              </span>{" "}
              / giá gói{" "}
              <span className="font-medium tabular-nums text-foreground">
                {formatCurrency(parseNumber(price))}
              </span>
              .
            </>
          ) : null}
        </p>

        <FormSwitchField
          name="earlyAccess"
          control={form.control}
          label="Được đặt sớm các suất chiếu đặc biệt"
        />
        <FormSwitchField
          name="stackableWithPromotions"
          control={form.control}
          label="Dùng chung được với mã khuyến mãi"
        />
        <FormSwitchField name="isActive" control={form.control} label="Đang bán" />
      </FieldGroup>

      <MembershipRefundPolicyNote />

      <FormErrorAlert error={mutation.error} preferServerMessage />

      <FormActions
        isPending={isPending}
        isDirty={form.formState.isDirty}
        submitLabel={isEdit ? FORM_SAVE_CHANGES_LABEL : "Tạo gói"}
        onCancel={onSuccess}
      />
    </form>
  );
};
