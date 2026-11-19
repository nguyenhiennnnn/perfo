import { HTTP_STATUS } from "./http-status";
export interface StatusErrorRule {
  message: string;
  preferServerMessage?: boolean;
}

export const STATUS_ERROR_RULES: Readonly<Record<number, StatusErrorRule>> = {
  [HTTP_STATUS.badRequest]: { message: "Dữ liệu không hợp lệ.", preferServerMessage: true },
  [HTTP_STATUS.unauthorized]: { message: "Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại." },
  [HTTP_STATUS.forbidden]: { message: "Bạn không có quyền thực hiện thao tác này." },
  [HTTP_STATUS.notFound]: { message: "Không tìm thấy dữ liệu yêu cầu." },
  [HTTP_STATUS.conflict]: {
    message: "Dữ liệu bị xung đột. Vui lòng thử lại.",
    preferServerMessage: true,
  },
  [HTTP_STATUS.unprocessable]: { message: "Dữ liệu không hợp lệ.", preferServerMessage: true },
  [HTTP_STATUS.tooManyRequests]: { message: "Bạn thao tác quá nhanh. Vui lòng thử lại sau." },
};

export const SERVER_ERROR_RULE: StatusErrorRule = {
  message: "Máy chủ đang gặp sự cố. Vui lòng thử lại sau.",
};

export const ERROR_MESSAGES = {
  offline: "Không có kết nối mạng. Vui lòng kiểm tra lại.",
  network: "Không kết nối được máy chủ. Kiểm tra mạng rồi thử lại.",
  timeout: "Máy chủ phản hồi quá lâu. Vui lòng thử lại.",
  fallback: "Đã có lỗi xảy ra. Vui lòng thử lại.",
  unknown: "Đã có lỗi xảy ra không xác định",
  toastTitle: "Đã có lỗi xảy ra",
} as const;

export interface LoadErrorCopy {
  cause: string;
  fix: string;
}

export const LOAD_ERROR_COPY = {
  network: {
    cause: "Không kết nối được tới máy chủ.",
    fix: "Kiểm tra mạng rồi bấm Thử lại.",
  },
  timeout: {
    cause: "Máy chủ phản hồi quá lâu.",
    fix: "Bấm Thử lại sau vài giây.",
  },
  forbidden: {
    cause: "Tài khoản hiện tại không đủ quyền xem dữ liệu này.",
    fix: "Đăng nhập bằng tài khoản phù hợp rồi tải lại trang.",
  },
  rateLimited: {
    cause: "Bạn gửi quá nhiều yêu cầu trong thời gian ngắn.",
    fix: "Đợi một lát rồi bấm Thử lại.",
  },
  server: {
    cause: "Máy chủ đang gặp sự cố.",
    fix: "Đợi vài phút rồi bấm Thử lại.",
  },
  fallback: {
    cause: "Dữ liệu trả về không đọc được.",
    fix: "Bấm Thử lại, nếu vẫn lỗi hãy tải lại trang.",
  },
} as const satisfies Record<string, LoadErrorCopy>;

export const STATE_MESSAGES = {
  loadingLabel: "Đang tải",
  retry: "Thử lại",
  clearFilters: "Xóa bộ lọc",
  loadError: { title: "Không tải được dữ liệu" },
  empty: { title: "Chưa có dữ liệu" },
  filtered: {
    title: "Không có kết quả phù hợp",
    description: "Thử đổi từ khóa hoặc bỏ bớt bộ lọc.",
  },
  notFound: {
    entityFallback: "dữ liệu",
    title: (entity: string) => `Không tìm thấy ${entity}`,
    description: (entity: string) =>
      `Có thể ${entity} đã bị xóa hoặc đường dẫn không đúng.`,
  },
  page: {
    notFound: {
      title: "Không tìm thấy trang",
      description: "Đường dẫn không tồn tại hoặc trang đã bị gỡ. Kiểm tra lại địa chỉ hoặc quay về trang chủ.",
    },
    forbidden: {
      title: "Không có quyền truy cập",
      description: "Tài khoản hiện tại không đủ quyền xem trang này. Đăng nhập bằng tài khoản phù hợp.",
    },
    error: {
      title: "Trang không hiển thị được",
      description: "Trang gặp lỗi khi hiển thị. Tải lại trang, nếu vẫn lỗi thì quay về trang chủ.",
    },
    reload: "Tải lại trang",
    home: "Về trang chủ",
  },
} as const;

export const VALIDATION_MESSAGES = {
  emailInvalid: "Email không hợp lệ.",
  emailRequired: "Vui lòng nhập email.",
  fullNameRequired: "Vui lòng nhập họ tên.",
  phoneInvalid: "Số điện thoại không hợp lệ.",
  passwordMismatch: "Mật khẩu xác nhận không khớp.",
  movieRequired: "Vui lòng chọn phim.",
  userRequired: "Vui lòng chọn người dùng.",
  basePriceInvalid: "Giá vé không hợp lệ.",
  imageUrlInvalid: "Đường dẫn ảnh không hợp lệ.",
  imageUrlTooLong: (max: number) => `Đường dẫn ảnh tối đa ${max} ký tự.`,
  fullNameTooLong: (max: number) => `Họ tên tối đa ${max} ký tự.`,
  confirmPasswordRequired: "Vui lòng xác nhận mật khẩu.",
  min: (value: number) => `Tối thiểu ${value}.`,
  max: (value: number) => `Tối đa ${value}.`,
} as const;

export const SILENT_TOAST_STATUSES: readonly number[] = [HTTP_STATUS.unauthorized];

export const ERROR_TOAST_DEDUPE_MS = 3000;
