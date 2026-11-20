import { expect, type Page, type WebSocket } from "@playwright/test";

/**
 * Theo dõi kết nối SignalR (ShowtimeHub) của một trang qua WebSocket để test realtime không phải
 * dùng sleep cố định: chỉ phát sự kiện từ khách khác SAU KHI trang quan sát đã vào nhóm suất chiếu.
 *
 * Giao thức JSON của SignalR: mỗi khung gồm các thông điệp JSON nối nhau bằng ký tự 0x1E.
 *  - type 1 (Invocation): { type: 1, invocationId, target: "JoinShowtime", arguments: [id] }
 *  - type 3 (Completion): { type: 3, invocationId, error? } là phản hồi của lần gọi trên.
 */

const RECORD_SEPARATOR = "\u001e";
const SIGNALR_INVOCATION = 1;
const SIGNALR_COMPLETION = 3;
const JOIN_METHOD = "JoinShowtime";
const DEFAULT_JOIN_TIMEOUT_MS = 20_000;

interface SignalRMessage {
  type?: number;
  invocationId?: string;
  target?: string;
  error?: string;
}

interface SocketState {
  pendingJoinIds: Set<string>;
  joined: boolean;
  closed: boolean;
}

function parseFrames(payload: string | Buffer): SignalRMessage[] {
  const text = typeof payload === "string" ? payload : payload.toString("utf8");
  const messages: SignalRMessage[] = [];
  for (const part of text.split(RECORD_SEPARATOR)) {
    if (!part) continue;
    try {
      messages.push(JSON.parse(part) as SignalRMessage);
    } catch {
      // Khung không phải JSON (ví dụ khung nhị phân): bỏ qua.
    }
  }
  return messages;
}

export interface ShowtimeHubTracker {
  /** Chờ tới khi một kết nối còn mở đã gọi JoinShowtime thành công. */
  waitUntilJoined: (timeoutMs?: number) => Promise<void>;
}

/**
 * Gọi TRƯỚC page.goto(): sự kiện websocket chỉ bắt được các kết nối mở sau khi đăng ký.
 * Nếu SignalR hạ cấp xuống SSE/long-polling (không có WebSocket) thì waitUntilJoined sẽ hết giờ.
 */
export function trackShowtimeHub(page: Page): ShowtimeHubTracker {
  const sockets = new Map<WebSocket, SocketState>();

  page.on("websocket", (socket) => {
    const state: SocketState = { pendingJoinIds: new Set(), joined: false, closed: false };
    sockets.set(socket, state);

    socket.on("framesent", ({ payload }) => {
      for (const message of parseFrames(payload)) {
        if (message.type === SIGNALR_INVOCATION && message.target === JOIN_METHOD && message.invocationId) {
          state.pendingJoinIds.add(message.invocationId);
        }
      }
    });

    socket.on("framereceived", ({ payload }) => {
      for (const message of parseFrames(payload)) {
        const isJoinCompletion =
          message.type === SIGNALR_COMPLETION &&
          message.invocationId !== undefined &&
          state.pendingJoinIds.has(message.invocationId);
        if (isJoinCompletion && !message.error) state.joined = true;
      }
    });

    socket.on("close", () => {
      state.closed = true;
    });
  });

  return {
    waitUntilJoined: async (timeoutMs = DEFAULT_JOIN_TIMEOUT_MS) => {
      await expect
        .poll(() => [...sockets.values()].some((state) => state.joined && !state.closed), {
          message: "trang phải kết nối SignalR và vào nhóm suất chiếu (JoinShowtime)",
          timeout: timeoutMs,
        })
        .toBe(true);
    },
  };
}
