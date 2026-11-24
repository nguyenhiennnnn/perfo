import type { OccupancyGroupBy, ShowtimeKind } from "@/types/enum";

export interface DashboardSummaryDto {
  todayRevenue: number;
  yesterdayRevenue: number;
  revenueChangePercent: number | null;
  ticketsSoldToday: number;
  confirmedBookingsToday: number;
  activePendingBookings: number;
  todayOccupancyPercent: number | null;
}

export interface DashboardInsightsParams {
  days?: number;
}

export interface DashboardRevenueTrendItemDto {
  period: string;
  ticketRevenue: number;
  comboRevenue: number;
  totalRevenue: number;
  bookingCount: number;
}

export interface DashboardShowtimeItemDto {
  showtimeId: number;
  movieTitle: string;
  roomName: string;
  startTime: string;
  bookedSeats: number;
  totalSeats: number;
}

export interface DashboardTopMovieDto {
  movieId: number;
  title: string;
  ticketsSold: number;
  revenue: number;
}

export interface DashboardInsightsDto {
  revenueTrend: DashboardRevenueTrendItemDto[];
  todayShowtimes: DashboardShowtimeItemDto[];
  topMovies: DashboardTopMovieDto[];
}

export interface DashboardOccupancyParams {
  from: string;
  to: string;
  groupBy: OccupancyGroupBy;
  kind: ShowtimeKind[];
}

export interface DashboardOccupancyGroupDto {
  key: string;
  name: string | null;
  showtimeCount: number;
  sellableSeats: number;
  ticketsSold: number;
  occupancyPercent: number | null;
  previousShowtimeCount: number;
  previousSellableSeats: number;
  previousTicketsSold: number;
  previousOccupancyPercent: number | null;
  changePoints: number | null;
}

export type DashboardOccupancyTotalDto = Omit<DashboardOccupancyGroupDto, "key" | "name">;

export interface DashboardOccupancyDto {
  from: string;
  to: string;
  previousFrom: string;
  previousTo: string;
  total: DashboardOccupancyTotalDto;
  items: DashboardOccupancyGroupDto[];
}

export interface DashboardSeatHeatmapParams {
  roomId: number;
  from: string;
  to: string;
  movieId?: number;
  genreId?: number;
}

export interface DashboardSeatHeatmapSeatDto {
  seatId: number;
  seatRow: string;
  seatNumber: number;
  seatTypeId: number | null;
  seatTypeName: string | null;
  isActive: boolean;
  coupleId: string | null;
  isCouple: boolean;
  timesSold: number;
  timesAbandoned: number;
}

export interface DashboardSeatHeatmapDto {
  roomId: number;
  roomName: string;
  from: string;
  to: string;
  maxTimesSold: number;
  maxTimesAbandoned: number;
  seats: DashboardSeatHeatmapSeatDto[];
}
