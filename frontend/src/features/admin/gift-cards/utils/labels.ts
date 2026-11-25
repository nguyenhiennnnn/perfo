import { giftCardStatusMap } from "@/components/common/status-badge.config";
import type { EnumOption } from "@/lib/enum-options";
import { GiftCardStatus } from "@/types/enum";
import { GiftCardCancelBalanceAction, type AdminGiftCard } from "../types";

export const GIFT_CARD_TYPE_LABELS: Record<string, string> = {
  Gift: "Quà tặng",
  ExchangeCredit: "Tín dụng đổi suất",
};

export const getGiftCardTypeLabel = (type: string) => GIFT_CARD_TYPE_LABELS[type] ?? type;

export const giftCardTypeOptions: EnumOption[] = Object.entries(GIFT_CARD_TYPE_LABELS).map(
  ([value, label]) => ({ value, label }),
);

export const giftCardStatusOptions: EnumOption[] = Object.values(GiftCardStatus).map((value) => ({
  value,
  label: giftCardStatusMap[value].label,
}));

export const GIFT_CARD_LEDGER_TYPE_LABELS: Record<string, string> = {
  Issue: "Phát hành",
  Reserve: "Giữ chỗ số dư",
  Release: "Nhả giữ chỗ",
  Redeem: "Sử dụng",
  Refund: "Hoàn lại",
  Expire: "Hết hạn",
  Cancel: "Hủy thẻ",
};

export const getGiftCardLedgerTypeLabel = (type: string) =>
  GIFT_CARD_LEDGER_TYPE_LABELS[type] ?? type;

const CANCELLABLE_STATUSES: string[] = [
  GiftCardStatus.PendingSend,
  GiftCardStatus.Sent,
  GiftCardStatus.InUse,
];

const RESENDABLE_STATUSES: string[] = [GiftCardStatus.Sent, GiftCardStatus.InUse];

export const isGiftCardCancellable = (card: Pick<AdminGiftCard, "status">) =>
  CANCELLABLE_STATUSES.includes(card.status);

export const isGiftCardResendable = (card: Pick<AdminGiftCard, "type" | "status">) =>
  card.type === "Gift" && RESENDABLE_STATUSES.includes(card.status);

export const isGiftCardPartiallyUsed = (card: Pick<AdminGiftCard, "balance" | "faceValue">) =>
  card.balance < card.faceValue;

export const GIFT_CARD_BALANCE_ACTIONS: {
  value: GiftCardCancelBalanceAction;
  label: string;
  description: string;
}[] = [
  {
    value: GiftCardCancelBalanceAction.WriteOff,
    label: "Xóa số dư",
    description: "Số dư còn lại về 0 và không hoàn lại cho người mua.",
  },
  {
    value: GiftCardCancelBalanceAction.ConvertToCredit,
    label: "Chuyển thành tín dụng đổi suất",
    description: "Phát hành thẻ tín dụng đổi suất bằng số dư còn lại cho người mua.",
  },
];
