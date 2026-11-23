import { useMemo, useState } from "react";
import { Controller, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Download } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Field, FieldDescription, FieldError, FieldGroup, FieldLabel } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { FormInputField } from "@/components/common/form/form-input-field";
import { FormActions } from "@/components/common/form/form-actions";
import { FormErrorAlert } from "@/components/common/form/form-error-alert";
import { MultiSelectField } from "@/components/common/form/multi-select-field";
import { useServerFieldErrors } from "@/hooks/use-server-field-errors";
import { useDebouncedValue } from "@/hooks/use-debounced-value";
import { toBusinessIso } from "@/lib/business-time";
import { formatDateTime } from "@/lib/formatters";
import { SEARCH_LOOKUP_PAGE_SIZE } from "@/config/default-constants";
import { OrderChannel, PromotionDiscountType, PromotionScope } from "@/types/enum";
import { useAdminUsers } from "@/features/admin/users/hooks/use-admin-users";

import { useAdminCreatePersonalCodes } from "../../hooks/use-admin-promotions";
import type { BulkPersonalCodesRequest, BulkPersonalCodesResponse } from "../../types";
import {
  ALL_CHANNELS_VALUE,
  PROMOTION_MAX_BULK_USERS,
  PROMOTION_MAX_VALID_DAYS,
  downloadPersonalCodesCsv,
} from "../../utils/promotion-admin-labels";
import { PromotionSharedFields } from "./promotion-shared-fields";
import {
  bulkPersonalCodeFormSchema,
  type BulkPersonalCodeFormValues,
} from "./promotion-schema";

interface PersonalCodeBulkFormProps {
  onClose: () => void;
}

interface UserOption {
  id: number;
  label: string;
}

const CUSTOMER_ROLE_ID = 1;
const CSV_FILENAME = "personal-codes.csv";

const toNumberOrNull = (value: string) => (value.trim() === "" ? null : Number(value));

const BulkResult = ({
  result,
  onClose,
}: {
  result: BulkPersonalCodesResponse;
  onClose: () => void;
}) => (
  <div className="space-y-4">
    <p className="text-sm text-foreground">
      Đã tạo <span className="font-semibold tabular-nums">{result.created}</span> mã cá nhân.
    </p>

    <div className="max-h-[50vh] overflow-auto">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Mã</TableHead>
            <TableHead>Khách hàng</TableHead>
            <TableHead>Hạn dùng</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {result.items.map((item) => (
            <TableRow key={item.promotionId}>
              <TableCell className="t-code font-medium">{item.code}</TableCell>
              <TableCell>
                <div className="flex flex-col">
                  <span className="text-sm text-foreground">{item.userName}</span>
                  <span className="text-xs text-muted-foreground">{item.userEmail}</span>
                </div>
              </TableCell>
              <TableCell className="text-sm tabular-nums text-muted-foreground">
                {item.endsAt ? formatDateTime(item.endsAt) : "Không giới hạn"}
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>

    <div className="flex justify-end gap-2 border-t border-border pt-3">
      <Button
        type="button"
        variant="outline"
        onClick={() => downloadPersonalCodesCsv(result.items, formatDateTime, CSV_FILENAME)}
      >
        <Download className="size-4" />
        <span>Tải CSV</span>
      </Button>
      <Button type="button" onClick={onClose}>
        Đóng
      </Button>
    </div>
  </div>
);

export const PersonalCodeBulkForm = ({ onClose }: PersonalCodeBulkFormProps) => {
  const createCodes = useAdminCreatePersonalCodes();
  const [result, setResult] = useState<BulkPersonalCodesResponse | null>(null);
  const [userSearch, setUserSearch] = useState("");

  const users = useAdminUsers({
    roleId: CUSTOMER_ROLE_ID,
    search: useDebouncedValue(userSearch),
    pageSize: SEARCH_LOOKUP_PAGE_SIZE,
  });

  const form = useForm<BulkPersonalCodeFormValues>({
    resolver: zodResolver(bulkPersonalCodeFormSchema),
    defaultValues: {
      codePrefix: "",
      userIds: [],
      validDays: "",
      name: "",
      description: "",
      discountType: PromotionDiscountType.Percent,
      discountValue: "",
      maxDiscountAmount: "",
      scope: PromotionScope.WholeOrder,
      minOrderAmount: "",
      startsAt: "",
      endsAt: "",
      channel: ALL_CHANNELS_VALUE,
      movieIds: [],
      showtimeIds: [],
      screenTypeIds: [],
      daysOfWeek: [],
      membershipTierIds: [],
      newCustomersOnly: false,
      budgetAmount: "",
      priority: "0",
      isExclusive: false,
    },
  });

  const applyServerErrors = useServerFieldErrors(form);
  const userOptions = useMemo<UserOption[]>(
    () =>
      (users.data?.items ?? []).map((user) => ({
        id: user.userId,
        label: `${user.fullName} · ${user.email}`,
      })),
    [users.data],
  );

  const onSubmit = (values: BulkPersonalCodeFormValues) => {
    const isPercent = values.discountType === PromotionDiscountType.Percent;
    const body: BulkPersonalCodesRequest = {
      codePrefix: values.codePrefix.trim().toUpperCase(),
      userIds: values.userIds,
      validDays: toNumberOrNull(values.validDays),
      name: values.name,
      description: values.description === "" ? null : values.description,
      discountType: values.discountType,
      discountValue: Number(values.discountValue),
      maxDiscountAmount: isPercent ? toNumberOrNull(values.maxDiscountAmount) : null,
      scope: values.scope,
      minOrderAmount: toNumberOrNull(values.minOrderAmount),
      startsAt: values.startsAt ? toBusinessIso(values.startsAt) : null,
      endsAt: values.endsAt ? toBusinessIso(values.endsAt) : null,
      channel: values.channel === ALL_CHANNELS_VALUE ? null : (values.channel as OrderChannel),
      movieIds: values.movieIds,
      showtimeIds: values.showtimeIds,
      screenTypeIds: values.screenTypeIds,
      daysOfWeek: values.daysOfWeek,
      membershipTierIds: values.membershipTierIds,
      newCustomersOnly: values.newCustomersOnly,
      budgetAmount: toNumberOrNull(values.budgetAmount),
      priority: Number(values.priority),
      isExclusive: values.isExclusive,
    };

    createCodes.mutate(body, {
      onSuccess: (res) => setResult(res.data),
      onError: applyServerErrors,
    });
  };

  if (result) return <BulkResult result={result} onClose={onClose} />;

  return (
    <form
      id="personal-code-bulk-form"
      onSubmit={form.handleSubmit(onSubmit)}
      className="max-h-[70vh] space-y-4 overflow-y-auto px-0.5"
    >
      <FieldGroup disabled={createCodes.isPending}>
        <FormInputField
          name="codePrefix"
          control={form.control}
          label="Tiền tố mã (2 - 12 ký tự chữ hoặc số)"
          placeholder="TRIAN"
          autoComplete="off"
          autoFocus
        />

        <Controller
          name="userIds"
          control={form.control}
          render={({ field, fieldState }) => (
            <Field data-invalid={fieldState.invalid}>
              <FieldLabel>Khách hàng nhận mã</FieldLabel>
              <MultiSelectField
                value={field.value}
                onChange={(values) => field.onChange(values.slice(0, PROMOTION_MAX_BULK_USERS))}
                options={userOptions}
                getOptionValue={(o) => o.id}
                getOptionLabel={(o) => o.label}
                searchValue={userSearch}
                onSearchChange={setUserSearch}
                isLoading={users.isLoading}
                placeholder="Chọn khách hàng"
                maxBadges={2}
              />
              <FieldDescription>
                Đã chọn {field.value.length}/{PROMOTION_MAX_BULK_USERS}
                {field.value.length >= PROMOTION_MAX_BULK_USERS && ". Đã đạt số lượng tối đa."}
              </FieldDescription>
              {fieldState.invalid && <FieldError errors={[fieldState.error]} />}
            </Field>
          )}
        />

        <PromotionSharedFields form={form} />

        <Controller
          name="validDays"
          control={form.control}
          render={({ field, fieldState }) => (
            <Field data-invalid={fieldState.invalid}>
              <FieldLabel htmlFor="field-validDays">
                Số ngày hiệu lực kể từ lúc tạo (không bắt buộc)
              </FieldLabel>
              <Input
                {...field}
                id="field-validDays"
                type="number"
                min={1}
                max={PROMOTION_MAX_VALID_DAYS}
                step={1}
                placeholder="Dùng cấu hình mặc định"
                aria-invalid={fieldState.invalid}
              />
              <FieldDescription>
                Nếu để trống cả số ngày hiệu lực và thời điểm kết thúc, hệ thống dùng thời hạn mặc
                định trong cấu hình.
              </FieldDescription>
              {fieldState.invalid && <FieldError errors={[fieldState.error]} />}
            </Field>
          )}
        />
      </FieldGroup>

      <FormErrorAlert error={createCodes.error} preferServerMessage />

      <FormActions
        isPending={createCodes.isPending}
        isDirty={form.formState.isDirty}
        submitLabel="Tạo mã cá nhân"
        onCancel={onClose}
      />
    </form>
  );
};
