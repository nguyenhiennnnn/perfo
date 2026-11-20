import type { APIRequestContext, APIResponse } from "@playwright/test";
import { API_BASE_URL } from "./auth";

/**
 * Tạo suất chiếu theo giờ tương đối (so với "bây giờ") qua API admin, dùng cho E2E.
 * Dùng POST /admin/showtimes (chỉ role Admin; truyền access token của admin, xem getAccessToken ở ./auth).
 * Seed chỉ có các suất ở mốc cố định lúc khởi tạo DB nên không đủ cho test cần "còn 3 giờ / 1 giờ / 20 phút".
 */

/** Mốc tương đối (phút) so với lúc gọi hàm. */
export const SHOWTIME_OFFSETS_MINUTES = {
  /** Còn >2 giờ: còn được hủy/hoàn vé. */
  in3h: 180,
  /** Còn 1 giờ: quá hạn hoàn (CancelDeadlineHours mặc định 2h), chưa mở check-in (mặc định 30 phút). */
  in1h: 60,
  /** Còn 20 phút: cửa sổ check-in đang mở. */
  in20m: 20,
} as const;

export type RelativeSlot = keyof typeof SHOWTIME_OFFSETS_MINUTES;

export interface CreatedShowtime {
  showtimeId: number;
  movieId: number;
  movieTitle: string;
  roomId: number;
  roomName: string;
  /** ISO 8601 UTC. */
  startTime: string;
  endTime: string;
  basePrice: number;
  status: string;
  seatsGenerated: number;
}

export interface CreateShowtimeOptions {
  /** Mặc định: phim NowShowing đầu tiên. */
  movieId?: number;
  /** Chỉ dùng đúng phòng này (không thử phòng khác). */
  roomId?: number;
  /** Bỏ qua các phòng này (dùng khi tạo nhiều suất chồng giờ nhau). */
  excludeRoomIds?: number[];
  /** Giá vé nền (VND, ghế thường). Mặc định 90.000. */
  basePrice?: number;
  /**
   * Khi phòng bị trùng giờ, hủy các suất đang chặn nếu chưa ai giữ/đặt ghế (DB E2E riêng!).
   * Seed lấp đầy phòng ban ngày nên thường cần bật. Suất đã có ghế bị giữ/đặt thì không bao giờ bị hủy.
   * Mặc định true.
   */
  clearConflicts?: boolean;
}

interface ApiEnvelope<T> {
  success: boolean;
  data: T;
  message?: string;
}

interface PagedItems<T> {
  items: T[];
}

interface RoomItem {
  roomId: number;
  roomName: string;
  totalSeats: number;
  isActive: boolean;
}

interface MovieItem {
  movieId: number;
  title: string;
  status: string;
}

interface CalendarItem {
  showtimeId: number;
  roomId: number;
  startTime: string;
  endTime: string;
  availableSeats: number;
  totalSeats: number;
  status: string;
}

interface CalendarData {
  days: { date: string; showtimes: CalendarItem[] }[];
}

const MAX_CLEAR_PER_ROOM = 3;
const DEFAULT_BASE_PRICE = 90_000;
const DAY_MS = 24 * 60 * 60 * 1000;

function authHeaders(accessToken: string) {
  return { Authorization: `Bearer ${accessToken}` };
}

async function describeFailure(action: string, response: APIResponse): Promise<Error> {
  return new Error(`${action} thất bại: HTTP ${response.status()} ${await response.text()}`);
}

async function getData<T>(
  request: APIRequestContext,
  accessToken: string,
  path: string,
  params?: Record<string, string | number>,
): Promise<T> {
  const response = await request.get(`${API_BASE_URL}${path}`, {
    headers: authHeaders(accessToken),
    params,
    failOnStatusCode: false,
  });
  if (!response.ok()) throw await describeFailure(`GET ${path}`, response);
  return ((await response.json()) as ApiEnvelope<T>).data;
}

/** Thời điểm now + offset phút, làm tròn xuống phút. */
export function minutesFromNow(offsetMinutes: number): Date {
  const ms = Date.now() + offsetMinutes * 60_000;
  return new Date(Math.floor(ms / 60_000) * 60_000);
}

function dateOnly(date: Date): string {
  return date.toISOString().slice(0, 10);
}

async function loadRooms(request: APIRequestContext, accessToken: string) {
  const data = await getData<PagedItems<RoomItem>>(request, accessToken, "/admin/rooms", {
    page: 1,
    pageSize: 100,
  });
  return data.items.filter((room) => room.isActive && room.totalSeats > 0);
}

async function resolveMovieId(request: APIRequestContext, accessToken: string, movieId?: number) {
  if (movieId) return movieId;
  const data = await getData<PagedItems<MovieItem>>(request, accessToken, "/admin/movies", {
    status: "NowShowing",
    page: 1,
    pageSize: 100,
  });
  const movie = data.items[0];
  if (!movie) throw new Error("Không có phim NowShowing nào để tạo suất chiếu E2E.");
  return movie.movieId;
}

/**
 * Hủy một suất đang Scheduled của phòng có End > start và chưa ai giữ/đặt ghế (sớm nhất trước).
 * Trả về true nếu đã hủy được một suất.
 */
async function cancelEarliestBlocker(
  request: APIRequestContext,
  accessToken: string,
  roomId: number,
  start: Date,
): Promise<boolean> {
  const calendar = await getData<CalendarData>(request, accessToken, "/admin/showtimes", {
    from: dateOnly(new Date(start.getTime() - DAY_MS)),
    to: dateOnly(new Date(start.getTime() + 2 * DAY_MS)),
    roomId,
    status: "Scheduled",
  });

  const blocker = calendar.days
    .flatMap((day) => day.showtimes)
    .filter((s) => s.status === "Scheduled" && new Date(s.endTime).getTime() > start.getTime())
    .sort((a, b) => new Date(a.startTime).getTime() - new Date(b.startTime).getTime())[0];

  // Suất chặn đã có ghế bị giữ/đặt: không đụng vào.
  if (!blocker || blocker.availableSeats < blocker.totalSeats) return false;

  await cancelShowtime(request, accessToken, blocker.showtimeId);
  return true;
}

/** Hủy suất chiếu (DELETE /admin/showtimes/{id}); dùng để dọn sau test. Suất đã bắt đầu thì backend từ chối. */
export async function cancelShowtime(
  request: APIRequestContext,
  accessToken: string,
  showtimeId: number,
): Promise<void> {
  const response = await request.delete(`${API_BASE_URL}/admin/showtimes/${showtimeId}`, {
    headers: authHeaders(accessToken),
    failOnStatusCode: false,
  });
  if (!response.ok()) throw await describeFailure(`Hủy suất ${showtimeId}`, response);
}

/**
 * Tạo suất chiếu bắt đầu sau `offsetMinutes` phút kể từ bây giờ.
 * Thử lần lượt các phòng active có ghế; phòng trùng giờ (HTTP 409) thì dọn suất chặn (nếu clearConflicts)
 * hoặc chuyển phòng khác. Gửi allowAfterHours=true nên giờ KẾT THÚC được phép vượt giờ đóng cửa, nhưng
 * giờ BẮT ĐẦU vẫn phải nằm trong giờ hoạt động của rạp (Cài đặt), nên chạy E2E ngoài giờ đó sẽ bị backend từ chối.
 */
export async function createShowtimeIn(
  request: APIRequestContext,
  accessToken: string,
  offsetMinutes: number,
  options: CreateShowtimeOptions = {},
): Promise<CreatedShowtime> {
  const { clearConflicts = true, basePrice = DEFAULT_BASE_PRICE } = options;
  const movieId = await resolveMovieId(request, accessToken, options.movieId);
  const start = minutesFromNow(offsetMinutes);

  const rooms = options.roomId
    ? [{ roomId: options.roomId }]
    : (await loadRooms(request, accessToken)).filter(
        (room) => !options.excludeRoomIds?.includes(room.roomId),
      );
  if (rooms.length === 0) throw new Error("Không có phòng active có ghế để tạo suất chiếu E2E.");

  const conflicts: number[] = [];
  for (const { roomId } of rooms) {
    for (let cleared = 0; cleared <= MAX_CLEAR_PER_ROOM; cleared++) {
      const response = await request.post(`${API_BASE_URL}/admin/showtimes`, {
        headers: authHeaders(accessToken),
        data: {
          movieId,
          roomId,
          startTime: start.toISOString(),
          basePrice,
          allowAfterHours: true,
        },
        failOnStatusCode: false,
      });

      if (response.status() === 201) {
        const data = ((await response.json()) as ApiEnvelope<CreatedShowtime>).data;
        return { ...data, startTime: start.toISOString() };
      }
      if (response.status() !== 409) throw await describeFailure("Tạo suất chiếu", response);

      const canClear =
        clearConflicts &&
        cleared < MAX_CLEAR_PER_ROOM &&
        (await cancelEarliestBlocker(request, accessToken, roomId, start));
      if (!canClear) {
        conflicts.push(roomId);
        break;
      }
    }
  }

  throw new Error(
    `Không tạo được suất chiếu +${offsetMinutes} phút: phòng ${conflicts.join(", ")} đều trùng giờ` +
      " với suất đã có ghế giữ/đặt (hoặc clearConflicts=false).",
  );
}

/** Tạo ba suất ở +3 giờ, +1 giờ, +20 phút, mỗi suất một phòng riêng (tránh tự trùng nhau). */
export async function createRelativeShowtimes(
  request: APIRequestContext,
  accessToken: string,
  options: Omit<CreateShowtimeOptions, "roomId" | "excludeRoomIds"> = {},
): Promise<Record<RelativeSlot, CreatedShowtime>> {
  const result = {} as Record<RelativeSlot, CreatedShowtime>;
  const usedRoomIds: number[] = [];

  for (const slot of Object.keys(SHOWTIME_OFFSETS_MINUTES) as RelativeSlot[]) {
    const created = await createShowtimeIn(request, accessToken, SHOWTIME_OFFSETS_MINUTES[slot], {
      ...options,
      excludeRoomIds: usedRoomIds,
    });
    usedRoomIds.push(created.roomId);
    result[slot] = created;
  }

  return result;
}
