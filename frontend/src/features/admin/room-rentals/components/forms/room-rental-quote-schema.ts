import { z } from "zod";
import type {
  RoomRentalAdminDto,
  RoomRentalQuotePricingRequest,
  RoomRentalQuoteVersionDto,
  UpsertRoomRentalQuoteVersionBody,
} from "../../types";
import { RoomRentalQuoteLineKind } from "@/types/enum";
import {
  ROOM_RENTAL_NOTE_MAX_LENGTH,
  ROOM_RENTAL_QUOTE_MAX_COMBOS,
  ROOM_RENTAL_QUOTE_MAX_EXTRAS,
  ROOM_RENTAL_QUOTE_MAX_TIERS,
} from "../../utils/room-rental-admin-labels";
import { toDateTimeInputValue } from "@/lib/formatters";

export const MAX_DURATION_MINUTES = 1440;
export const MIN_ATTENDEES = 1;
export const MAX_ATTENDEES = 500;
export const MAX_AMOUNT = 1_000_000_000;
export const MAX_QUANTITY = 1000;
export const DISCOUNT_REASON_MAX_LENGTH = 150;
export const EXTRA_LABEL_MAX_LENGTH = 150;
export const DEFAULT_DURATION_MINUTES = 120;

const integer = (label: string, min: number, max: number) =>
  z.coerce
    .number({ message: `${label} phải là số.` })
    .int(`${label} phải là số nguyên.`)
    .min(min, `${label} tối thiểu là ${min}.`)
    .max(max, `${label} tối đa là ${max}.`);

export const roomRentalQuoteFormSchema = z
  .object({
    roomId: z.string().min(1, "Vui lòng chọn phòng."),
    startAt: z.string().min(1, "Vui lòng chọn ngày giờ chiếu."),
    durationMinutes: integer("Thời lượng", 1, MAX_DURATION_MINUTES),
    attendeeCount: integer("Số người", MIN_ATTENDEES, MAX_ATTENDEES),
    combos: z
      .array(
        z.object({
          comboId: z.string().min(1, "Chọn combo."),
          quantity: integer("Số lượng", 1, MAX_QUANTITY),
        }),
      )
      .max(ROOM_RENTAL_QUOTE_MAX_COMBOS),
    extras: z
      .array(
        z.object({
          label: z
            .string()
            .trim()
            .min(1, "Nhập tên khoản phụ.")
            .max(EXTRA_LABEL_MAX_LENGTH, `Tối đa ${EXTRA_LABEL_MAX_LENGTH} ký tự.`),
          amount: integer("Đơn giá", 1, MAX_AMOUNT),
          quantity: integer("Số lượng", 1, MAX_QUANTITY),
        }),
      )
      .max(ROOM_RENTAL_QUOTE_MAX_EXTRAS),
    discountMode: z.enum(["none", "amount", "percent"]),
    discountValue: z.coerce.number({ message: "Chiết khấu phải là số." }).min(0, "Không được âm."),
    discountReason: z.string().trim().max(DISCOUNT_REASON_MAX_LENGTH, "Tối đa 150 ký tự."),
    validUntil: z.string(),
    depositPercent: z.string(),
    balanceDueAt: z.string(),
    useDefaultTiers: z.boolean(),
    tiers: z
      .array(
        z.object({
          minHoursBeforeStart: integer("Số giờ", 0, 24 * 365),
          refundPercent: integer("Phần trăm hoàn", 0, 100),
        }),
      )
      .max(ROOM_RENTAL_QUOTE_MAX_TIERS),
    note: z.string().trim().max(ROOM_RENTAL_NOTE_MAX_LENGTH, `Tối đa ${ROOM_RENTAL_NOTE_MAX_LENGTH} ký tự.`),
  })
  .superRefine((values, ctx) => {
    const comboIds = values.combos.map((c) => c.comboId);
    if (new Set(comboIds).size !== comboIds.length) {
      ctx.addIssue({ code: "custom", path: ["combos"], message: "Mỗi combo chỉ được chọn một lần." });
    }
    if (values.discountMode === "amount" && values.discountValue > MAX_AMOUNT) {
      ctx.addIssue({ code: "custom", path: ["discountValue"], message: "Số tiền chiết khấu quá lớn." });
    }
    if (values.discountMode === "amount" && !Number.isInteger(values.discountValue)) {
      ctx.addIssue({ code: "custom", path: ["discountValue"], message: "Số tiền phải là số nguyên đồng." });
    }
    if (values.discountMode === "percent" && values.discountValue > 100) {
      ctx.addIssue({ code: "custom", path: ["discountValue"], message: "Tối đa 100%." });
    }
    if (values.depositPercent !== "") {
      const percent = Number(values.depositPercent);
      if (!Number.isFinite(percent) || percent <= 0 || percent > 100) {
        ctx.addIssue({ code: "custom", path: ["depositPercent"], message: "Phần trăm cọc từ 1 đến 100." });
      }
    }
    if (!values.useDefaultTiers) {
      const hours = values.tiers.map((t) => t.minHoursBeforeStart);
      if (new Set(hours).size !== hours.length) {
        ctx.addIssue({ code: "custom", path: ["tiers"], message: "Các mốc hủy không được trùng số giờ." });
      }
    }
  });

export type RoomRentalQuoteFormValues = z.output<typeof roomRentalQuoteFormSchema>;

export const getRoomRentalQuoteDefaults = (
  request: RoomRentalAdminDto,
  source?: RoomRentalQuoteVersionDto | null,
): RoomRentalQuoteFormValues => {
  if (!source) {
    return {
      roomId: "",
      startAt: toDateTimeInputValue(request.desiredStartAt) ?? "",
      durationMinutes: DEFAULT_DURATION_MINUTES,
      attendeeCount: request.attendeeCount,
      combos: [],
      extras: [],
      discountMode: "none",
      discountValue: 0,
      discountReason: "",
      validUntil: "",
      depositPercent: "",
      balanceDueAt: "",
      useDefaultTiers: true,
      tiers: [],
      note: "",
    };
  }

  const discountLine = source.lines.find((l) => l.kind === RoomRentalQuoteLineKind.Discount);
  return {
    roomId: String(source.roomId),
    startAt: toDateTimeInputValue(source.startAt) ?? "",
    durationMinutes: source.durationMinutes,
    attendeeCount: source.attendeeCount,
    combos: source.lines
      .filter((l) => l.kind === RoomRentalQuoteLineKind.Combo && l.referenceId !== null)
      .map((l) => ({ comboId: String(l.referenceId), quantity: l.quantity })),
    extras: source.lines
      .filter((l) => l.kind === RoomRentalQuoteLineKind.Extra)
      .map((l) => ({ label: l.label, amount: l.unitPrice, quantity: l.quantity })),
    discountMode: discountLine ? "amount" : "none",
    discountValue: discountLine ? Math.abs(discountLine.amount) : 0,
    discountReason: "",
    validUntil: "",
    depositPercent: String(source.depositPercent),
    balanceDueAt: "",
    useDefaultTiers: false,
    tiers: source.cancellationTiers.map((t) => ({
      minHoursBeforeStart: t.minHoursBeforeStart,
      refundPercent: t.refundPercent,
    })),
    note: source.note ?? "",
  };
};

export const toPricingRequest = (
  values: RoomRentalQuoteFormValues,
): RoomRentalQuotePricingRequest => ({
  roomId: Number(values.roomId),
  startAt: values.startAt,
  durationMinutes: values.durationMinutes,
  attendeeCount: values.attendeeCount,
  combos: values.combos.map((c) => ({ comboId: Number(c.comboId), quantity: c.quantity })),
  extras: values.extras.map((e) => ({
    label: e.label,
    amount: e.amount,
    quantity: e.quantity,
  })),
  discountAmount: values.discountMode === "amount" && values.discountValue > 0 ? values.discountValue : null,
  discountPercent:
    values.discountMode === "percent" && values.discountValue > 0 ? values.discountValue : null,
  discountReason:
    values.discountMode !== "none" && values.discountReason ? values.discountReason : null,
});

export const toQuoteVersionBody = (
  values: RoomRentalQuoteFormValues,
): UpsertRoomRentalQuoteVersionBody => ({
  pricing: toPricingRequest(values),
  validUntil: values.validUntil || null,
  depositPercent: values.depositPercent === "" ? null : Number(values.depositPercent),
  balanceDueAt: values.balanceDueAt || null,
  cancellationTiers: values.useDefaultTiers ? null : values.tiers,
  note: values.note || null,
});
