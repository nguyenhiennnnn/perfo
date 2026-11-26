import { z } from "zod";
import { differenceInCalendarDays } from "date-fns";
import {
  EMAIL_MAX_LENGTH,
  VN_PHONE_REGEX,
} from "@/config/default-constants";
import { VALIDATION_MESSAGES } from "@/config/error-messages";
import { getBusinessNow } from "@/lib/business-time";
import { RoomRentalAudienceType } from "@/types/enum";
import {
  ROOM_RENTAL_FULL_NAME_MAX_LENGTH,
  ROOM_RENTAL_MAX_ATTENDEES,
  ROOM_RENTAL_MAX_FLEXIBLE_SPAN_DAYS,
  ROOM_RENTAL_MIN_ATTENDEES,
  ROOM_RENTAL_NOTE_MAX_LENGTH,
  ROOM_RENTAL_OPTION_NONE,
  ROOM_RENTAL_ORGANIZATION_MAX_LENGTH,
} from "../constants";
import type { CreateRoomRentalRequestRequest } from "../types";

const AUDIENCE_VALUES = new Set<string>(Object.values(RoomRentalAudienceType));

const noteSchema = z
  .string()
  .trim()
  .max(ROOM_RENTAL_NOTE_MAX_LENGTH, `Tối đa ${ROOM_RENTAL_NOTE_MAX_LENGTH} ký tự.`);

export const roomRentalRequestSchema = z
  .object({
    fullName: z
      .string()
      .trim()
      .min(1, VALIDATION_MESSAGES.fullNameRequired)
      .max(
        ROOM_RENTAL_FULL_NAME_MAX_LENGTH,
        VALIDATION_MESSAGES.fullNameTooLong(ROOM_RENTAL_FULL_NAME_MAX_LENGTH),
      ),
    audienceType: z
      .string()
      .refine((value) => AUDIENCE_VALUES.has(value), "Vui lòng chọn loại đối tượng."),
    organizationName: z
      .string()
      .trim()
      .max(
        ROOM_RENTAL_ORGANIZATION_MAX_LENGTH,
        `Tối đa ${ROOM_RENTAL_ORGANIZATION_MAX_LENGTH} ký tự.`,
      ),
    contactPhone: z
      .string()
      .trim()
      .min(1, "Vui lòng nhập số điện thoại.")
      .regex(VN_PHONE_REGEX, VALIDATION_MESSAGES.phoneInvalid),
    contactEmail: z
      .string()
      .trim()
      .min(1, VALIDATION_MESSAGES.emailRequired)
      .email(VALIDATION_MESSAGES.emailInvalid)
      .max(EMAIL_MAX_LENGTH, `Email tối đa ${EMAIL_MAX_LENGTH} ký tự.`),
    desiredStartAt: z.string().min(1, "Vui lòng chọn ngày giờ mong muốn."),
    hasFlexibleRange: z.boolean(),
    flexibleFromAt: z.string(),
    flexibleToAt: z.string(),
    attendeeCount: z
      .string()
      .trim()
      .min(1, "Vui lòng nhập số người tham dự.")
      .refine(
        (value) =>
          Number.isInteger(Number(value)) &&
          Number(value) >= ROOM_RENTAL_MIN_ATTENDEES &&
          Number(value) <= ROOM_RENTAL_MAX_ATTENDEES,
        `Số người từ ${ROOM_RENTAL_MIN_ATTENDEES} đến ${ROOM_RENTAL_MAX_ATTENDEES}.`,
      ),
    bringsOwnContent: z.boolean(),
    movieId: z.string(),
    contentNote: noteSchema,
    screenTypeId: z.string(),
    comboRequest: noteSchema,
    notes: noteSchema,
  })
  .superRefine((values, ctx) => {
    if (values.desiredStartAt && new Date(values.desiredStartAt) <= getBusinessNow()) {
      ctx.addIssue({
        code: "custom",
        path: ["desiredStartAt"],
        message: "Thời gian mong muốn phải ở tương lai.",
      });
    }

    if (!values.hasFlexibleRange) return;

    if (!values.flexibleFromAt) {
      ctx.addIssue({
        code: "custom",
        path: ["flexibleFromAt"],
        message: "Vui lòng chọn thời điểm bắt đầu khoảng dự phòng.",
      });
    }
    if (!values.flexibleToAt) {
      ctx.addIssue({
        code: "custom",
        path: ["flexibleToAt"],
        message: "Vui lòng chọn thời điểm kết thúc khoảng dự phòng.",
      });
    }
    if (!values.flexibleFromAt || !values.flexibleToAt) return;

    const from = new Date(values.flexibleFromAt);
    const to = new Date(values.flexibleToAt);
    if (from > to) {
      ctx.addIssue({
        code: "custom",
        path: ["flexibleToAt"],
        message: "Thời điểm kết thúc phải sau thời điểm bắt đầu.",
      });
    } else if (differenceInCalendarDays(to, from) > ROOM_RENTAL_MAX_FLEXIBLE_SPAN_DAYS) {
      ctx.addIssue({
        code: "custom",
        path: ["flexibleToAt"],
        message: `Khoảng dự phòng tối đa ${ROOM_RENTAL_MAX_FLEXIBLE_SPAN_DAYS} ngày.`,
      });
    }
  });

export type RoomRentalRequestFormValues = z.infer<typeof roomRentalRequestSchema>;

export const EMPTY_ROOM_RENTAL_REQUEST: RoomRentalRequestFormValues = {
  fullName: "",
  audienceType: RoomRentalAudienceType.Business,
  organizationName: "",
  contactPhone: "",
  contactEmail: "",
  desiredStartAt: "",
  hasFlexibleRange: false,
  flexibleFromAt: "",
  flexibleToAt: "",
  attendeeCount: "",
  bringsOwnContent: false,
  movieId: ROOM_RENTAL_OPTION_NONE,
  contentNote: "",
  screenTypeId: ROOM_RENTAL_OPTION_NONE,
  comboRequest: "",
  notes: "",
};

const toNullableText = (value: string) => value.trim() || null;

const toOptionalId = (value: string) =>
  value === ROOM_RENTAL_OPTION_NONE || value === "" ? null : Number(value);

export const toCreateRoomRentalRequest = (
  values: RoomRentalRequestFormValues,
): CreateRoomRentalRequestRequest => ({
  fullName: values.fullName.trim(),
  organizationName: toNullableText(values.organizationName),
  audienceType: values.audienceType as RoomRentalAudienceType,
  contactPhone: values.contactPhone.trim(),
  contactEmail: values.contactEmail.trim(),
  desiredStartAt: values.desiredStartAt,
  flexibleFromAt: values.hasFlexibleRange ? values.flexibleFromAt : null,
  flexibleToAt: values.hasFlexibleRange ? values.flexibleToAt : null,
  attendeeCount: Number(values.attendeeCount),
  movieId: values.bringsOwnContent ? null : toOptionalId(values.movieId),
  bringsOwnContent: values.bringsOwnContent,
  contentNote: values.bringsOwnContent ? toNullableText(values.contentNote) : null,
  screenTypeId: toOptionalId(values.screenTypeId),
  comboRequest: toNullableText(values.comboRequest),
  notes: toNullableText(values.notes),
});
