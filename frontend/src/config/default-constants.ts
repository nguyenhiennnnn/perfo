export const DEFAULT_PAGE = 1;
export const DEFAULT_PAGE_SIZE = 10;

export const SEARCH_DEBOUNCE_MS = 300;
export const MS_PER_SECOND = 1000;
export const MS_PER_MINUTE = 60 * MS_PER_SECOND;
export const MS_PER_HOUR = 60 * MS_PER_MINUTE;
export const MINUTES_PER_HOUR = 60;
export const MINUTES_PER_DAY = 24 * MINUTES_PER_HOUR;

export const BYTES_PER_KB = 1024;
export const BYTES_PER_MB = BYTES_PER_KB * BYTES_PER_KB;

export const MAX_IMAGE_SIZE_BYTES = 5 * BYTES_PER_MB;
export const MAX_VIDEO_SIZE_BYTES = 100 * BYTES_PER_MB;
export const ACCEPTED_IMAGE_TYPES = [
  "image/png",
  "image/jpeg",
  "image/webp",
] as const;
export const ACCEPTED_VIDEO_TYPES = [
  "video/mp4",
  "video/quicktime",
  "video/webm",
] as const;

export const IMPORT_MAX_FILE_BYTES = 5 * BYTES_PER_MB;
export const IMPORT_ACCEPTED_EXTENSIONS = [".xlsx", ".csv"] as const;
export const IMPORT_ACCEPT = IMPORT_ACCEPTED_EXTENSIONS.join(",");
export const IMPORT_PREVIEW_PAGE_SIZE = 20;

export const ACCEPTED_IMAGE_ACCEPT = ACCEPTED_IMAGE_TYPES.join(",");
export const ACCEPTED_VIDEO_ACCEPT = ACCEPTED_VIDEO_TYPES.join(",");

export const PAGE_SIZE_OPTIONS = [10, 20, 30, 50];
export const LOOKUP_PAGE_SIZE = 100;
export const SEARCH_LOOKUP_PAGE_SIZE = 20;
export const COUNT_ONLY_PAGE_SIZE = 1;
export const ACCOUNT_LIST_PAGE_LIMIT = 10;
export const HOME_MOVIE_SECTION_LIMIT = 10;
export const HERO_CAROUSEL_MOVIES_LIMIT = 5;
export const DEFAULT_MOVIES_PAGE_LIMIT = 12;
export const TOP_MOVIES_REPORT_LIMIT = 10;

export const API_TIMEOUT_MS = 20_000;
export const EXPORT_TIMEOUT_MS = 60_000;
export const IMPORT_TIMEOUT_MS = 120_000;
export const QUERY_STALE_TIME_MS = 30 * 1000;
export const QUERY_GC_TIME_MS = 5 * 60 * 1000;
export const QUERY_MAX_RETRIES = 2;
export const MUTATION_MAX_RETRIES = 0;
export const NO_RETRY_STATUS_MIN = 400;
export const NO_RETRY_STATUS_MAX = 500;
export const DASHBOARD_REFETCH_INTERVAL_MS = 60_000;
export const BOOKING_RESULT_POLL_INTERVAL_MS = 3_000;
export const BOOKING_HOLD_POLL_INTERVAL_MS = 10_000;
export const BOOKING_HOLD_POLL_MAX_FAILURES = 3;
export const SEAT_SUGGESTIONS_STALE_TIME_MS = 0;
export const SEAT_SUGGESTIONS_GC_TIME_MS = 0;
export const SETTINGS_STALE_TIME_MS = 5 * 60 * 1000;
export const NOW_TICK_INTERVAL_MS = 60_000;
export const COPY_FEEDBACK_MS = 2000;
export const DEFAULT_SCREEN_TYPE = "2D";
export const BUSINESS_TIME_ZONE_BOOT_TIMEOUT_MS = 3_000;
export const FALLBACK_OPENING_MINUTES = 8 * MINUTES_PER_HOUR;
export const FALLBACK_CLOSING_MINUTES = 23 * MINUTES_PER_HOUR;
export const DEFAULT_PICKER_OFFSET_FROM_OPENING_MINUTES = 2 * MINUTES_PER_HOUR;
export const CALENDAR_EMPTY_DAY_START_MINUTES = 8 * MINUTES_PER_HOUR;
export const CALENDAR_EMPTY_DAY_END_MINUTES = 22 * MINUTES_PER_HOUR;
export const CALENDAR_MIN_WINDOW_MINUTES = 4 * MINUTES_PER_HOUR;
export const DEFAULT_SHOWTIME_BASE_PRICE = 75_000;
export const PRICE_INPUT_STEP = 1000;
export const DEFAULT_MOVIE_DURATION_MINUTES = 120;
export const QR_SCAN_REPEAT_GUARD_MS = 2500;
export const COMBOBOX_BLUR_CLOSE_DELAY_MS = 150;
export const SIGNALR_RECONNECT_DELAYS_MS = [0, 1000, 3000, 5000, 10000];
export const IMPORT_EXPORT_RESYNC_INTERVAL_MS = 15_000;
export const IMPORT_EXPORT_PANEL_MAX_ITEMS = 20;
export const HERO_INTERVAL_MS = 7000;
export const HERO_TRANSITION_MS = 480;
export const HERO_LEAVE_GRACE_MS = 40;
export const ROUTE_FALLBACK_DELAY_MS = 300;
export const CONFETTI_DURATION_MS = 3200;
export const GOOGLE_SIGN_IN_BUTTON_WIDTH_PX = 336;

export const FORM_PENDING_LABEL = "Đang lưu...";
export const FORM_CANCEL_LABEL = "Hủy";
export const FORM_SAVE_CHANGES_LABEL = "Lưu thay đổi";
export const FORM_PROCESSING_LABEL = "Đang xử lý...";
export const HOLDING_SEATS_LABEL = "Đang giữ ghế...";
export const FAILED_LABEL = "Thất bại";
export const STATUS_LABEL = "Trạng thái";
export const ALL_STATUSES_LABEL = "Tất cả trạng thái";
export const SEARCH_PLACEHOLDER = "Tìm kiếm...";
export const SEARCH_NAME_OR_EMAIL_PLACEHOLDER = "Tìm theo tên hoặc email...";
export const IRREVERSIBLE_ACTION_MESSAGE = "Hành động này không thể hoàn tác.";

export const QUICK_CREATE_NAME_MAX_LENGTH = 50;
export const QUICK_CREATE_AGE_RATING_MAX_LENGTH = 10;
export const QUICK_CREATE_PERSON_NAME_MAX_LENGTH = 150;

export const NAME_MAX_LENGTH = 100;
export const EMAIL_MAX_LENGTH = 150;
export const PHONE_MAX_LENGTH = 20;
export const PERSON_NAME_MAX_LENGTH = 255;
export const IMAGE_URL_MAX_LENGTH = 500;
export const AGE_RATING_CODE_MAX_LENGTH = 10;
export const AGE_RATING_DESCRIPTION_MAX_LENGTH = 255;
export const COMBO_NAME_MAX_LENGTH = 150;
export const COMBO_DESCRIPTION_MAX_LENGTH = 500;
export const MOVIE_TITLE_MAX_LENGTH = 200;

export const PERCENT_MAX = 100;
export const DEFAULT_CANCEL_DEADLINE_HOURS = 2;
export const TOP_MOVIES_CHART_LABEL_MAX_CHARS = 18;
export const SEO_DESCRIPTION_MAX_LENGTH = 160;
export const MOVIE_CARD_GENRES_LIMIT = 2;
export const HERO_SLIDE_GENRES_LIMIT = 3;
export const ROOM_CREATE_MAX_ROWS = 50;
export const ROOM_CREATE_MAX_COLS = 50;
export const SEAT_SUGGESTION_MAX_COUNT = 10;
export const SEAT_GRID_MAX_ROWS = 52;
export const SEAT_GRID_MAX_COLS = 60;
export const SHOWTIME_CLONE_MAX_SOURCE_DAYS = 31;
export const AUTO_SCHEDULE_MAX_SHOWS_PER_MOVIE = 20;

export const VN_PHONE_REGEX = /^(0|\+84)[0-9]{9,10}$/;

export const CHECKIN_SOUND = {
  success: { frequencyHz: 800, durationSeconds: 0.15 },
  error: { frequencyHz: 200, durationSeconds: 0.35 },
} as const;

export const URL_PLACEHOLDER = "https://...";
export const PERSON_NAME_PLACEHOLDER = "Nguyễn Văn A";
export const PASSWORD_PLACEHOLDER = "••••••••";
export const YOUTUBE_EMBED_BASE_URL = "https://www.youtube.com/embed";

export const MOVIE_LIST_STATUS_QUERY = {
  nowShowing: "now-showing",
  comingSoon: "coming-soon",
} as const;

export const SETTINGS_FIELD_MAX = {
  stopSellingMinutesBefore: 1440,
  roomBufferMinutes: 120,
  maxSeatsPerBooking: 50,
  maxComboQuantity: 50,
  loyaltyWindowMonths: 120,
  movieEndGraceDays: 365,
  showtimeLookaheadDays: 60,
  schedulerStepMinutes: 60,
  schedulerMinSameMovieGapMinutes: 1440,
  schedulerHeatLookbackDays: 365,
  schedulerHeatSalesWeight: 10,
  schedulerHeatRatingWeight: 10,
  schedulerMaxRating: 10,
} as const;

export const SKELETON_LIST_COUNT = 3;
export const SKELETON_ROW_COUNT = 5;
export const SKELETON_GRID_COUNT = 6;

export const COUNTER_WALK_IN_NAME = "Khách vãng lai";
export const COUNTER_CASH_QUICK_AMOUNTS = [50_000, 100_000, 200_000, 500_000] as const;
