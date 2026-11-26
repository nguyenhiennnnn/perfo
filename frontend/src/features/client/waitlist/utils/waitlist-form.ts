import { z } from "zod";

export const createWaitlistRegisterSchema = (maxSeats: number) =>
  z.object({
    seatCount: z.coerce
      .number({ message: "Vui lòng nhập số ghế." })
      .int("Số ghế phải là số nguyên.")
      .min(1, "Cần ít nhất 1 ghế.")
      .max(maxSeats, `Tối đa ${maxSeats} ghế mỗi lần đặt.`),
    requireAdjacent: z.boolean(),
  });

export type WaitlistRegisterValues = z.infer<ReturnType<typeof createWaitlistRegisterSchema>>;

export const DEFAULT_WAITLIST_VALUES: WaitlistRegisterValues = {
  seatCount: 2,
  requireAdjacent: true,
};
