import { useEffect, useMemo } from "react";
import { useFieldArray, useForm, useWatch } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Plus, Send, Trash2 } from "lucide-react";
import { FormDateTimePickerField } from "@/components/common/form/form-datetime-picker-field";
import { FormInputField } from "@/components/common/form/form-input-field";
import { FormSelectField } from "@/components/common/form/form-select-field";
import { FormSwitchField } from "@/components/common/form/form-switch-field";
import { FormTextareaField } from "@/components/common/form/form-textarea-field";
import { Spinner } from "@/components/common/spinner";
import { Button } from "@/components/ui/button";
import { Field, FieldDescription, FieldError, FieldGroup, FieldLabel } from "@/components/ui/field";
import { LOOKUP_PAGE_SIZE } from "@/config/default-constants";
import { useAdminCombos } from "@/features/admin/combos/hooks/use-admin-combo";
import { useAdminRooms } from "@/features/admin/rooms/hooks/use-admin-rooms";
import { useDebouncedValue } from "@/hooks/use-debounced-value";
import { useServerFieldErrors } from "@/hooks/use-server-field-errors";
import { useSheetSetDirty } from "@/stores/sheet-store";
import {
  useCreateRoomRentalQuote,
  useRoomRentalQuotePreview,
  useSendRoomRentalQuote,
} from "../../hooks/use-admin-room-rentals";
import {
  ROOM_RENTAL_CALCULATE_DEBOUNCE_MS,
  ROOM_RENTAL_QUOTE_MAX_COMBOS,
  ROOM_RENTAL_QUOTE_MAX_EXTRAS,
  ROOM_RENTAL_QUOTE_MAX_TIERS,
} from "../../utils/room-rental-admin-labels";
import type { RoomRentalAdminDto, RoomRentalQuoteVersionDto } from "../../types";
import {
  getRoomRentalQuoteDefaults,
  roomRentalQuoteFormSchema,
  toPricingRequest,
  toQuoteVersionBody,
  type RoomRentalQuoteFormValues,
} from "./room-rental-quote-schema";
import { RoomRentalQuotePreview } from "./room-rental-quote-preview";

const DISCOUNT_MODE_OPTIONS = [
  { value: "none", label: "Không chiết khấu" },
  { value: "amount", label: "Giảm số tiền" },
  { value: "percent", label: "Giảm phần trăm" },
];

interface RoomRentalQuoteFormProps {
  request: RoomRentalAdminDto;
  source?: RoomRentalQuoteVersionDto | null;
  onDone: () => void;
}

export const RoomRentalQuoteForm = ({ request, source, onDone }: RoomRentalQuoteFormProps) => {
  const rooms = useAdminRooms({ pageSize: LOOKUP_PAGE_SIZE });
  const combos = useAdminCombos({ pageSize: LOOKUP_PAGE_SIZE });
  const createQuote = useCreateRoomRentalQuote(request.roomRentalRequestId);
  const sendQuote = useSendRoomRentalQuote(request.roomRentalRequestId);
  const setSheetDirty = useSheetSetDirty();

  const form = useForm<RoomRentalQuoteFormValues>({
    resolver: zodResolver(roomRentalQuoteFormSchema),
    defaultValues: getRoomRentalQuoteDefaults(request, source),
  });
  const applyServerErrors = useServerFieldErrors(form);

  const comboFields = useFieldArray({ control: form.control, name: "combos" });
  const extraFields = useFieldArray({ control: form.control, name: "extras" });
  const tierFields = useFieldArray({ control: form.control, name: "tiers" });

  const watched = useWatch({ control: form.control });
  const useDefaultTiers = watched.useDefaultTiers ?? true;
  const discountMode = watched.discountMode ?? "none";

  useEffect(() => {
    setSheetDirty(form.formState.isDirty);
  }, [form.formState.isDirty, setSheetDirty]);

  const pricingKey = useMemo(() => {
    const parsed = roomRentalQuoteFormSchema.safeParse(watched);
    return parsed.success ? JSON.stringify(toPricingRequest(parsed.data)) : "";
  }, [watched]);
  const debouncedKey = useDebouncedValue(pricingKey, ROOM_RENTAL_CALCULATE_DEBOUNCE_MS);
  const previewRequest = debouncedKey ? JSON.parse(debouncedKey) : null;
  const preview = useRoomRentalQuotePreview(previewRequest);

  const activeCombos = useMemo(
    () => (combos.data?.items ?? []).filter((c) => c.isActive),
    [combos.data],
  );
  const roomOptions = useMemo(
    () => (rooms.data?.items ?? []).filter((r) => r.isActive),
    [rooms.data],
  );

  const isPending = createQuote.isPending || sendQuote.isPending;

  const submit = (sendAfter: boolean) =>
    form.handleSubmit(async (values) => {
      try {
        const created = await createQuote.mutateAsync(toQuoteVersionBody(values));
        if (sendAfter) await sendQuote.mutateAsync(created.data.versionNumber);
        onDone();
      } catch (error) {
        applyServerErrors(error);
      }
    })();

  return (
    <form
      id="room-rental-quote-form"
      onSubmit={(event) => {
        event.preventDefault();
        void submit(false);
      }}
      className="flex h-full min-h-0 flex-col"
    >
      <div className="min-h-0 flex-1 space-y-6 overflow-y-auto px-0.5 pb-4">
        <FieldGroup disabled={isPending}>
          <div className="grid gap-3 sm:grid-cols-2">
            <FormSelectField
              name="roomId"
              control={form.control}
              label="Phòng chiếu"
              placeholder="Chọn phòng"
              options={roomOptions}
              getOptionValue={(r) => r.roomId}
              getOptionLabel={(r) => r.roomName}
            />
            <FormDateTimePickerField name="startAt" control={form.control} label="Ngày giờ bắt đầu" />
            <FormInputField
              name="durationMinutes"
              control={form.control}
              label="Thời lượng (phút)"
              type="number"
              min={1}
              max={1440}
              step={5}
            />
            <FormInputField
              name="attendeeCount"
              control={form.control}
              label="Số người tham dự"
              type="number"
              min={1}
              max={500}
            />
          </div>
        </FieldGroup>

        <section className="space-y-3">
          <div className="flex items-center justify-between">
            <h3 className="t-h3 text-foreground">Combo</h3>
            <Button
              type="button"
              size="sm"
              variant="outline"
              disabled={isPending || comboFields.fields.length >= ROOM_RENTAL_QUOTE_MAX_COMBOS}
              onClick={() => comboFields.append({ comboId: "", quantity: 1 })}
            >
              <Plus className="size-4" />
              <span>Thêm combo</span>
            </Button>
          </div>
          {request.comboRequest && (
            <p className="text-sm text-muted-foreground">Khách yêu cầu: {request.comboRequest}</p>
          )}
          {comboFields.fields.map((field, index) => (
            <div key={field.id} className="grid grid-cols-[minmax(0,1fr)_6rem_auto] items-start gap-2">
              <FormSelectField
                name={`combos.${index}.comboId`}
                control={form.control}
                placeholder="Chọn combo"
                options={activeCombos}
                getOptionValue={(c) => c.comboId}
                getOptionLabel={(c) => c.name}
              />
              <FormInputField
                name={`combos.${index}.quantity`}
                control={form.control}
                label="SL"
                type="number"
                min={1}
                max={1000}
              />
              <Button
                type="button"
                size="icon"
                variant="ghost"
                className="mt-6"
                aria-label="Xóa combo"
                onClick={() => comboFields.remove(index)}
              >
                <Trash2 className="size-4" />
              </Button>
            </div>
          ))}
          {form.formState.errors.combos?.message && (
            <FieldError errors={[form.formState.errors.combos]} />
          )}
        </section>

        <section className="space-y-3">
          <div className="flex items-center justify-between">
            <h3 className="t-h3 text-foreground">Khoản phụ</h3>
            <Button
              type="button"
              size="sm"
              variant="outline"
              disabled={isPending || extraFields.fields.length >= ROOM_RENTAL_QUOTE_MAX_EXTRAS}
              onClick={() => extraFields.append({ label: "", amount: 0, quantity: 1 })}
            >
              <Plus className="size-4" />
              <span>Thêm khoản phụ</span>
            </Button>
          </div>
          {extraFields.fields.map((field, index) => (
            <div
              key={field.id}
              className="grid grid-cols-[minmax(0,1fr)_7rem_4.5rem_auto] items-start gap-2"
            >
              <FormInputField
                name={`extras.${index}.label`}
                control={form.control}
                label="Tên khoản"
                placeholder="Trang trí, MC, thiết bị..."
              />
              <FormInputField
                name={`extras.${index}.amount`}
                control={form.control}
                label="Đơn giá"
                type="number"
                min={1}
              />
              <FormInputField
                name={`extras.${index}.quantity`}
                control={form.control}
                label="SL"
                type="number"
                min={1}
                max={1000}
              />
              <Button
                type="button"
                size="icon"
                variant="ghost"
                className="mt-6"
                aria-label="Xóa khoản phụ"
                onClick={() => extraFields.remove(index)}
              >
                <Trash2 className="size-4" />
              </Button>
            </div>
          ))}
        </section>

        <FieldGroup disabled={isPending}>
          <h3 className="t-h3 text-foreground">Chiết khấu</h3>
          <div className="grid gap-3 sm:grid-cols-2">
            <FormSelectField
              name="discountMode"
              control={form.control}
              label="Hình thức"
              options={DISCOUNT_MODE_OPTIONS}
              getOptionValue={(o) => o.value}
              getOptionLabel={(o) => o.label}
            />
            {discountMode !== "none" && (
              <FormInputField
                name="discountValue"
                control={form.control}
                label={discountMode === "percent" ? "Phần trăm giảm" : "Số tiền giảm (đồng)"}
                type="number"
                min={0}
                max={discountMode === "percent" ? 100 : undefined}
                step={discountMode === "percent" ? 0.5 : 1000}
              />
            )}
          </div>
          {discountMode !== "none" && (
            <FormInputField
              name="discountReason"
              control={form.control}
              label="Lý do chiết khấu"
              placeholder="Khách hàng thân thiết, hợp đồng nhiều buổi..."
            />
          )}
        </FieldGroup>

        <FieldGroup disabled={isPending}>
          <h3 className="t-h3 text-foreground">Điều khoản</h3>
          <div className="grid gap-3 sm:grid-cols-2">
            <FormDateTimePickerField
              name="validUntil"
              control={form.control}
              label="Báo giá có hiệu lực đến"
              clearable
            />
            <FormDateTimePickerField
              name="balanceDueAt"
              control={form.control}
              label="Hạn thanh toán phần còn lại"
              clearable
            />
            <FormInputField
              name="depositPercent"
              control={form.control}
              label="Tiền đặt cọc (%)"
              type="number"
              min={1}
              max={100}
              step={1}
            />
          </div>
          <FieldDescription>
            Để trống các trường trên để dùng giá trị mặc định trong cài đặt rạp.
          </FieldDescription>

          <FormSwitchField
            name="useDefaultTiers"
            control={form.control}
            label="Dùng chính sách hủy mặc định của rạp"
          />
          {!useDefaultTiers && (
            <Field>
              <FieldLabel>Mốc hoàn cọc khi hủy</FieldLabel>
              <div className="space-y-2">
                {tierFields.fields.map((field, index) => (
                  <div key={field.id} className="grid grid-cols-[1fr_1fr_auto] items-start gap-2">
                    <FormInputField
                      name={`tiers.${index}.minHoursBeforeStart`}
                      control={form.control}
                      label="Hủy trước (giờ)"
                      type="number"
                      min={0}
                    />
                    <FormInputField
                      name={`tiers.${index}.refundPercent`}
                      control={form.control}
                      label="Hoàn cọc (%)"
                      type="number"
                      min={0}
                      max={100}
                    />
                    <Button
                      type="button"
                      size="icon"
                      variant="ghost"
                      className="mt-6"
                      aria-label="Xóa mốc hủy"
                      onClick={() => tierFields.remove(index)}
                    >
                      <Trash2 className="size-4" />
                    </Button>
                  </div>
                ))}
                <Button
                  type="button"
                  size="sm"
                  variant="outline"
                  disabled={tierFields.fields.length >= ROOM_RENTAL_QUOTE_MAX_TIERS}
                  onClick={() => tierFields.append({ minHoursBeforeStart: 0, refundPercent: 0 })}
                >
                  <Plus className="size-4" />
                  <span>Thêm mốc</span>
                </Button>
                {form.formState.errors.tiers?.message && (
                  <FieldError errors={[form.formState.errors.tiers]} />
                )}
              </div>
              <FieldDescription>
                Hủy muộn hơn mốc nhỏ nhất thì khách mất cọc.
              </FieldDescription>
            </Field>
          )}

          <FormTextareaField
            name="note"
            control={form.control}
            label="Ghi chú gửi khách"
            rows={3}
          />
        </FieldGroup>

        <RoomRentalQuotePreview
          result={preview.data}
          isLoading={preview.isFetching}
          error={preview.error}
          hasInput={previewRequest !== null}
        />
      </div>

      <div className="flex flex-wrap justify-end gap-2 border-t border-border bg-background py-3">
        <Button type="button" variant="outline" onClick={onDone} disabled={isPending}>
          Đóng
        </Button>
        <Button type="submit" variant="secondary" disabled={isPending}>
          {createQuote.isPending && !sendQuote.isPending ? <Spinner size="sm" decorative /> : null}
          Lưu phiên bản
        </Button>
        <Button type="button" disabled={isPending} onClick={() => void submit(true)}>
          {isPending ? <Spinner size="sm" decorative className="text-current" /> : <Send className="size-4" />}
          Lưu và gửi cho khách
        </Button>
      </div>
    </form>
  );
};
