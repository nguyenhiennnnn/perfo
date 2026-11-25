import { z } from "zod";

import { LoyaltyChallengeConditionType, LoyaltyChallengeRewardType } from "@/types/enum";

import {
  CHALLENGE_DESCRIPTION_MAX_LENGTH,
  CHALLENGE_MAX_REWARD_POINTS,
  CHALLENGE_MAX_SPAN_DAYS,
  CHALLENGE_MAX_TARGET_COUNT,
  CHALLENGE_NAME_MAX_LENGTH,
} from "../../utils/loyalty-admin-labels";

const INTEGER_PATTERN = /^\d+$/;
const DAY_MS = 86_400_000;

const isInteger = (value: string, min: number, max: number) =>
  INTEGER_PATTERN.test(value.trim()) && Number(value) >= min && Number(value) <= max;

const spanDays = (from: string, to: string) =>
  Math.round((Date.parse(`${to}T00:00:00Z`) - Date.parse(`${from}T00:00:00Z`)) / DAY_MS) + 1;

export const challengeFormSchema = z
  .object({
    name: z
      .string()
      .trim()
      .min(1, "Vui lòng nhập tên thử thách.")
      .max(CHALLENGE_NAME_MAX_LENGTH, `Tên thử thách tối đa ${CHALLENGE_NAME_MAX_LENGTH} ký tự.`),
    description: z
      .string()
      .trim()
      .max(
        CHALLENGE_DESCRIPTION_MAX_LENGTH,
        `Mô tả tối đa ${CHALLENGE_DESCRIPTION_MAX_LENGTH} ký tự.`,
      ),
    startsOn: z.string().min(1, "Vui lòng chọn ngày bắt đầu."),
    endsOn: z.string().min(1, "Vui lòng chọn ngày kết thúc."),
    conditionType: z.enum([
      LoyaltyChallengeConditionType.DistinctMovies,
      LoyaltyChallengeConditionType.Screenings,
    ]),
    targetCount: z.string(),
    rewardType: z.enum([LoyaltyChallengeRewardType.Points, LoyaltyChallengeRewardType.Gift]),
    rewardPoints: z.string(),
    rewardGiftId: z.string().nullable(),
    isActive: z.boolean(),
  })
  .superRefine((values, ctx) => {
    const addIssue = (path: string, message: string) =>
      ctx.addIssue({ code: "custom", path: [path], message });

    if (values.startsOn && values.endsOn) {
      if (values.endsOn < values.startsOn) {
        addIssue("endsOn", "Ngày kết thúc phải từ ngày bắt đầu trở đi.");
      } else if (spanDays(values.startsOn, values.endsOn) > CHALLENGE_MAX_SPAN_DAYS) {
        addIssue("endsOn", `Thử thách kéo dài tối đa ${CHALLENGE_MAX_SPAN_DAYS} ngày.`);
      }
    }

    if (!isInteger(values.targetCount, 1, CHALLENGE_MAX_TARGET_COUNT)) {
      addIssue("targetCount", `Mục tiêu phải là số nguyên từ 1 đến ${CHALLENGE_MAX_TARGET_COUNT}.`);
    }

    if (values.rewardType === LoyaltyChallengeRewardType.Points) {
      if (!isInteger(values.rewardPoints, 1, CHALLENGE_MAX_REWARD_POINTS)) {
        addIssue(
          "rewardPoints",
          `Điểm thưởng phải là số nguyên từ 1 đến ${CHALLENGE_MAX_REWARD_POINTS.toLocaleString("vi-VN")}.`,
        );
      }
      return;
    }

    if (!values.rewardGiftId) addIssue("rewardGiftId", "Vui lòng chọn quà thưởng.");
  });

export type ChallengeFormValues = z.infer<typeof challengeFormSchema>;
