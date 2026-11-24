export interface RoomClosureDto {
  roomClosureId: number;
  roomId: number;
  roomName: string;
  startTime: string;
  endTime: string;
  reason: string;
  createdByUserId?: number | null;
  createdAt: string;
}

export interface CreateRoomClosureRequest {
  roomId: number;
  startTime: string;
  endTime: string;
  reason: string;
}

export interface RoomClosureListParams {
  roomId?: number;
  from?: string;
  to?: string;
  ongoing?: boolean;
  page?: number;
  pageSize?: number;
}

export interface DeleteRoomClosureResponse {
  roomClosureId: number;
  deleted: boolean;
  endedEarly: boolean;
}

export interface AffectedShowtimeDto {
  showtimeId: number;
  movieId: number;
  movieTitle: string;
  roomId: number;
  roomName: string;
  startTime: string;
  endTime: string;
  orderCount: number;
  customerCount: number;
  ticketCount: number;
}

export interface AffectedShowtimesResponse {
  roomId: number;
  startTime: string;
  endTime: string;
  showtimes: AffectedShowtimeDto[];
  totalShowtimes: number;
  totalOrders: number;
  totalCustomers: number;
  totalTickets: number;
}

export interface AffectedShowtimesParams {
  roomId: number;
  startTime: string;
  endTime: string;
}

export type RoomClosureStatus = "upcoming" | "ongoing" | "ended";
