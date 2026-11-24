import { getBusinessMinutesOfDay, toWallDate } from "@/lib/business-time";
import { getLocale } from "@/lib/regional-settings";
import { ShowtimeStatus } from "@/types/enum";
import type {
  ShowtimeCalendarClosureDto,
  ShowtimeCalendarItemDto,
} from "../types";
import { groupByRoom } from "./group-by-room";
import { MINUTES_PER_DAY, CALENDAR_EMPTY_DAY_START_MINUTES, CALENDAR_EMPTY_DAY_END_MINUTES, CALENDAR_MIN_WINDOW_MINUTES } from "@/config/default-constants";

export const MIN_GAP_MINUTES = 30;

export interface PlacedShowtime {
  showtime: ShowtimeCalendarItemDto;
  startMin: number;
  endMin: number;
  lane: number;
  conflict: boolean;
}

export interface FreeGap {
  startMin: number;
  endMin: number;
}

export interface PlacedClosure {
  closure: ShowtimeCalendarClosureDto;
  startMin: number;
  endMin: number;
  continuesBefore: boolean;
  continuesAfter: boolean;
}

export interface RoomLayout {
  roomId: number;
  roomName: string;
  screenType?: string | null;
  items: PlacedShowtime[];
  closures: PlacedClosure[];
  laneCount: number;
  gaps: FreeGap[];
  conflictCount: number;
}

export interface DayLayout {
  rooms: RoomLayout[];
  windowStart: number;
  windowEnd: number;
  totalShowtimes: number;
  showtimeRoomCount: number;
  closureCount: number;
  activeShowtimes: number;
  cancelledShowtimes: number;
  conflictCount: number;
  soldSeats: number;
  totalSeats: number;
}

const isCancelled = (s: ShowtimeCalendarItemDto) =>
  s.status === ShowtimeStatus.Cancelled;

function toRange(s: ShowtimeCalendarItemDto) {
  const startMin = getBusinessMinutesOfDay(s.startTime);
  const rawEnd = getBusinessMinutesOfDay(s.endTime);
  return { startMin, endMin: rawEnd > startMin ? rawEnd : MINUTES_PER_DAY };
}

const wallToMs = (wall: Date) =>
  Date.UTC(
    wall.getFullYear(),
    wall.getMonth(),
    wall.getDate(),
    wall.getHours(),
    wall.getMinutes(),
  );

const MS_PER_MINUTE = 60_000;

export function formatCalendarDay(date: Date) {
  const dd = String(date.getDate()).padStart(2, "0");
  const mm = String(date.getMonth() + 1).padStart(2, "0");
  return `${dd}/${mm}/${date.getFullYear()}`;
}

export function placeClosuresForDay(
  closures: ShowtimeCalendarClosureDto[] | undefined,
  day: string,
): PlacedClosure[] {
  const parsed = parseCalendarDay(day);
  if (!closures || closures.length === 0 || !parsed) return [];

  const dayStartMs = Date.UTC(
    parsed.getFullYear(),
    parsed.getMonth(),
    parsed.getDate(),
  );
  const dayEndMs = dayStartMs + MINUTES_PER_DAY * MS_PER_MINUTE;

  const placed: PlacedClosure[] = [];
  for (const closure of closures) {
    const start = toWallDate(closure.startTime);
    const end = toWallDate(closure.endTime);
    if (!start || !end) continue;
    const startMs = wallToMs(start);
    const endMs = wallToMs(end);
    if (endMs <= dayStartMs || startMs >= dayEndMs) continue;

    const clippedStart = Math.max(startMs, dayStartMs);
    const clippedEnd = Math.min(endMs, dayEndMs);
    placed.push({
      closure,
      startMin: Math.round((clippedStart - dayStartMs) / MS_PER_MINUTE),
      endMin: Math.round((clippedEnd - dayStartMs) / MS_PER_MINUTE),
      continuesBefore: startMs < dayStartMs,
      continuesAfter: endMs > dayEndMs,
    });
  }
  return placed.sort(
    (a, b) =>
      a.startMin - b.startMin || a.closure.roomClosureId - b.closure.roomClosureId,
  );
}

export function listClosureDays(
  closures: ShowtimeCalendarClosureDto[] | undefined,
  from: string,
  to: string,
): string[] {
  if (!closures || closures.length === 0) return [];
  const rangeStart = parseCalendarDay(from);
  const rangeEnd = parseCalendarDay(to);
  if (!rangeStart || !rangeEnd) return [];

  const days = new Set<string>();
  for (const closure of closures) {
    const start = toWallDate(closure.startTime);
    const end = toWallDate(closure.endTime);
    if (!start || !end) continue;
    const cursor = new Date(start.getFullYear(), start.getMonth(), start.getDate());
    const lastMs = wallToMs(end);
    for (
      ;
      Date.UTC(cursor.getFullYear(), cursor.getMonth(), cursor.getDate()) < lastMs;
      cursor.setDate(cursor.getDate() + 1)
    ) {
      if (cursor < rangeStart || cursor > rangeEnd) continue;
      days.add(formatCalendarDay(cursor));
    }
  }
  return [...days];
}

function subtractClosures(gaps: FreeGap[], closures: PlacedClosure[]): FreeGap[] {
  let result = gaps;
  for (const closure of closures) {
    const next: FreeGap[] = [];
    for (const gap of result) {
      if (closure.endMin <= gap.startMin || closure.startMin >= gap.endMin) {
        next.push(gap);
        continue;
      }
      if (closure.startMin - gap.startMin >= MIN_GAP_MINUTES) {
        next.push({ startMin: gap.startMin, endMin: closure.startMin });
      }
      if (gap.endMin - closure.endMin >= MIN_GAP_MINUTES) {
        next.push({ startMin: closure.endMin, endMin: gap.endMin });
      }
    }
    result = next;
  }
  return result;
}

function layoutRoom(
  room: ReturnType<typeof groupByRoom>[number],
  closures: PlacedClosure[],
): RoomLayout {
  const items: PlacedShowtime[] = room.showtimes
    .map((showtime) => ({
      showtime,
      ...toRange(showtime),
      lane: 0,
      conflict: false,
    }))
    .sort((a, b) => a.startMin - b.startMin || a.endMin - b.endMin);

  const lanes: PlacedShowtime[][] = [];
  const byPriority = [
    ...items.filter((i) => !isCancelled(i.showtime)),
    ...items.filter((i) => isCancelled(i.showtime)),
  ];
  for (const item of byPriority) {
    let lane = lanes.findIndex((placed) =>
      placed.every((p) => p.endMin <= item.startMin || p.startMin >= item.endMin),
    );
    if (lane === -1) {
      lane = lanes.length;
      lanes.push([]);
    }
    lanes[lane].push(item);
    item.lane = lane;
  }

  const live = items.filter((i) => !isCancelled(i.showtime));
  let reach = -1;
  let reachItem: PlacedShowtime | undefined;
  for (const item of live) {
    if (reachItem && item.startMin < reach) {
      item.conflict = true;
      reachItem.conflict = true;
    }
    if (item.endMin > reach) {
      reach = item.endMin;
      reachItem = item;
    }
  }

  const gaps: FreeGap[] = [];
  let cursor: number | null = null;
  for (const item of live) {
    if (cursor !== null && item.startMin - cursor >= MIN_GAP_MINUTES) {
      gaps.push({ startMin: cursor, endMin: item.startMin });
    }
    cursor = cursor === null ? item.endMin : Math.max(cursor, item.endMin);
  }

  return {
    roomId: room.roomId,
    roomName: room.roomName,
    screenType: room.screenType,
    items,
    closures,
    laneCount: Math.max(lanes.length, 1),
    gaps: subtractClosures(gaps, closures),
    conflictCount: live.filter((i) => i.conflict).length,
  };
}

export function buildDayLayout(
  showtimes: ShowtimeCalendarItemDto[],
  closures: PlacedClosure[] = [],
): DayLayout {
  const closuresByRoom = new Map<number, PlacedClosure[]>();
  for (const closure of closures) {
    const list = closuresByRoom.get(closure.closure.roomId);
    if (list) list.push(closure);
    else closuresByRoom.set(closure.closure.roomId, [closure]);
  }

  const showtimeRooms = groupByRoom(showtimes);
  const showtimeRoomIds = new Set(showtimeRooms.map((r) => r.roomId));
  const closureOnlyRooms = [...closuresByRoom.entries()]
    .filter(([roomId]) => !showtimeRoomIds.has(roomId))
    .map(([roomId, list]) => ({
      roomId,
      roomName: list[0].closure.roomName,
      screenType: null,
      showtimes: [] as ShowtimeCalendarItemDto[],
    }));

  const rooms = [...showtimeRooms, ...closureOnlyRooms]
    .sort((a, b) => a.roomName.localeCompare(b.roomName, getLocale()))
    .map((room) => layoutRoom(room, closuresByRoom.get(room.roomId) ?? []));
  const placed = rooms.flatMap((r) => r.items);
  const placedClosures = rooms.flatMap((r) => r.closures);

  const starts = [
    ...placed.map((i) => i.startMin),
    ...placedClosures.map((c) => c.startMin),
  ];
  const ends = [
    ...placed.map((i) => i.endMin),
    ...placedClosures.map((c) => c.endMin),
  ];
  const minStart = starts.length
    ? Math.min(...starts)
    : CALENDAR_EMPTY_DAY_START_MINUTES;
  const maxEnd = ends.length
    ? Math.max(...ends)
    : CALENDAR_EMPTY_DAY_END_MINUTES;

  let windowStart = Math.max(Math.floor(minStart / 60) * 60, 0);
  let windowEnd = Math.min(Math.ceil(maxEnd / 60) * 60, MINUTES_PER_DAY);
  while (windowEnd - windowStart < CALENDAR_MIN_WINDOW_MINUTES) {
    if (windowEnd < MINUTES_PER_DAY) windowEnd += 60;
    else if (windowStart > 0) windowStart -= 60;
    else break;
  }

  const live = placed.filter((i) => !isCancelled(i.showtime));
  return {
    rooms,
    windowStart,
    windowEnd,
    totalShowtimes: placed.length,
    showtimeRoomCount: rooms.filter((r) => r.items.length > 0).length,
    closureCount: new Set(placedClosures.map((c) => c.closure.roomClosureId))
      .size,
    activeShowtimes: live.length,
    cancelledShowtimes: placed.length - live.length,
    conflictCount: rooms.reduce((sum, r) => sum + r.conflictCount, 0),
    soldSeats: live.reduce(
      (sum, i) => sum + (i.showtime.totalSeats - i.showtime.availableSeats),
      0,
    ),
    totalSeats: live.reduce((sum, i) => sum + i.showtime.totalSeats, 0),
  };
}

export function getSoldSeats(s: ShowtimeCalendarItemDto) {
  return Math.max(s.totalSeats - s.availableSeats, 0);
}

export function getFillPercent(s: ShowtimeCalendarItemDto) {
  return s.totalSeats > 0
    ? Math.min(Math.round((getSoldSeats(s) / s.totalSeats) * 100), 100)
    : 0;
}

export function parseCalendarDay(value: string): Date | null {
  const m = /^(\d{2})\/(\d{2})\/(\d{4})$/.exec(value);
  if (!m) return null;
  return new Date(Number(m[3]), Number(m[2]) - 1, Number(m[1]));
}

export function formatMinutes(min: number) {
  const h = Math.floor(min / 60) % 24;
  const m = min % 60;
  return `${String(h).padStart(2, "0")}:${String(m).padStart(2, "0")}`;
}

export function formatGapLength(minutes: number) {
  const h = Math.floor(minutes / 60);
  const m = minutes % 60;
  if (h === 0) return `${m} phút`;
  return m === 0 ? `${h} giờ` : `${h} giờ ${m} phút`;
}
