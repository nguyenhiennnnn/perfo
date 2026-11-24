const CHANNEL_LABELS: Record<string, string> = {
  Online: "Online",
  Counter: "Tại quầy",
};

const CANCEL_REASON_LABELS: Record<string, string> = {
  CustomerChangedMind: "Khách đổi ý",
  DuplicateOrder: "Đơn trùng",
  PaymentError: "Lỗi thanh toán",
  ShowtimeCancelled: "Rạp hủy suất chiếu",
  RoomClosed: "Đóng phòng chiếu",
  Other: "Lý do khác",
  Unknown: "Không rõ",
};

const CANCELLED_BY_LABELS: Record<string, string> = {
  Customer: "Khách hàng",
  Staff: "Nhân viên",
  Admin: "Quản trị viên",
  System: "Hệ thống",
  Unknown: "Không rõ",
};

const TIME_TO_SHOWTIME_LABELS: Record<string, string> = {
  Under3Hours: "Dưới 3 giờ",
  Hours3To24: "3 – 24 giờ",
  Days1To3: "1 – 3 ngày",
  Over3Days: "Trên 3 ngày",
  NoShowtime: "Không có suất",
};

export const getChannelLabel = (key: string) => CHANNEL_LABELS[key] ?? key;
export const getCancelReasonLabel = (key: string) => CANCEL_REASON_LABELS[key] ?? key;
export const getCancelledByLabel = (key: string) => CANCELLED_BY_LABELS[key] ?? key;
export const getTimeToShowtimeLabel = (key: string) => TIME_TO_SHOWTIME_LABELS[key] ?? key;

export const formatOptionalPercent = (
  value: number | null | undefined,
  format: (v: number) => string,
) => (value == null ? "—" : format(value));
