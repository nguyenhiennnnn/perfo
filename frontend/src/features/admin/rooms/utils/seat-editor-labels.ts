import { seatTypeLabel } from "@/components/common/seat-map/seat-kind";
import type { SeatTypeDto } from "@/features/admin/catalog/types";
import type { SeatEditorBrush } from "../types/seat-editor";

export const describeBrush = (
  brush: SeatEditorBrush,
  seatTypes: SeatTypeDto[] | undefined,
): { label: string; hint: string } => {
  switch (brush.type) {
    case "seatType": {
      const seatType = seatTypes?.find((t) => t.seatTypeId === brush.seatTypeId);
      const name = seatType?.name ?? "";
      const isCouple = seatType?.isCouple ?? false;
      const label = seatType ? seatTypeLabel(name, isCouple) : "Loại ghế";
      return {
        label: `Gán ${label}`,
        hint: isCouple
          ? "Bấm vào ô ghế để tự ghép với ô liền kề thành ghế đôi loại này. Bấm vào ghế đôi có sẵn để đổi loại cả cặp."
          : "Bấm vào ô ghế để đổi sang loại này. Nếu ô thuộc ghế đôi, cặp ghế sẽ được tách ra.",
      };
    }
    case "pairCouple":
      return {
        label: "Ghế đôi",
        hint: "Bấm một ô để tự ghép với ô liền kề bên phải (hoặc bên trái nếu không được) thành ghế đôi. Bấm lại lên ghế đôi để tách về 2 ghế thường.",
      };
    case "toggleAisle":
      return {
        label: "Đặt/bỏ lối đi",
        hint: "Bấm vào ô để biến thành lối đi, bấm lần nữa để trả lại thành ghế. Với ghế đôi, cả cặp cùng chuyển thành lối đi.",
      };
    case "toggleActive":
      return {
        label: "Bật/tắt ghế",
        hint: "Bấm vào ghế để tắt (không bán), bấm lần nữa để bật lại. Với ghế đôi, cả cặp cùng bật/tắt.",
      };
  }
};
