import type { RoomRentalPaymentStage } from "@/types/enum";
import { ROOM_RENTAL_PAYMENT_RETURN_STORAGE_KEY } from "../constants";

export interface RoomRentalPaymentReturn {
  orderId: number;
  token: string;
  stage: RoomRentalPaymentStage;
}

export const saveRoomRentalPaymentReturn = (value: RoomRentalPaymentReturn): void => {
  try {
    window.sessionStorage.setItem(ROOM_RENTAL_PAYMENT_RETURN_STORAGE_KEY, JSON.stringify(value));
  } catch {
    return;
  }
};

export const readRoomRentalPaymentReturn = (orderId: number): RoomRentalPaymentReturn | null => {
  try {
    const raw = window.sessionStorage.getItem(ROOM_RENTAL_PAYMENT_RETURN_STORAGE_KEY);
    if (!raw) return null;
    const parsed = JSON.parse(raw) as Partial<RoomRentalPaymentReturn>;
    if (parsed.orderId !== orderId || typeof parsed.token !== "string" || !parsed.token) {
      return null;
    }
    if (typeof parsed.stage !== "string") return null;
    return parsed as RoomRentalPaymentReturn;
  } catch {
    return null;
  }
};

export const clearRoomRentalPaymentReturn = (): void => {
  try {
    window.sessionStorage.removeItem(ROOM_RENTAL_PAYMENT_RETURN_STORAGE_KEY);
  } catch {
    return;
  }
};
