export interface WholeCinemaShowtimeSlotDto {
  showtimeId: number;
  roomId: number;
  roomName: string;
  screenType?: string | null;
  startTime: string;
  endTime: string;
  basePrice: number;
  availableSeats: number;
  totalSeats: number;
  status: string;
}

export interface WholeCinemaMovieDto {
  movieId: number;
  movieTitle: string;
  moviePosterUrl?: string | null;
  durationMinutes: number;
  ageRatingCode?: string | null;
  showtimes: WholeCinemaShowtimeSlotDto[];
}

export interface WholeCinemaScheduleDto {
  date: string;
  movies: WholeCinemaMovieDto[];
  formats: string[];
}

export interface ShowtimeSeatItemDto {
  showtimeSeatId: number;
  seatRow: string;
  seatNumber: number;
  seatTypeId?: number | null;
  seatType?: string | null;
  isCouple?: boolean;
  coupleId?: string | null;
  status: string;
  price: number;
  heldByMe?: boolean;
}

export interface ShowtimeMyHoldDto {
  orderId: number;
  orderCode: string;
  expiresAt: string;
  showtimeSeatIds: number[];
}

export interface AislePositionDto {
  seatRow: string;
  seatNumber: number;
}

export interface ShowtimeSeatsResponseDto {
  showtimeId: number;
  roomName: string;
  movieTitle: string;
  startTime: string;
  rows: number;
  cols: number;
  aisles: AislePositionDto[];
  seats: ShowtimeSeatItemDto[];
  moviePosterUrl?: string | null;
  ageRatingCode?: string | null;
  screenType?: string | null;
  myHold?: ShowtimeMyHoldDto | null;
}

export interface SeatSuggestionDto {
  seatRow: string;
  seatNumbers: number[];
  showtimeSeatIds: number[];
  totalPrice: number;
  score: number;
}
