export interface TicketTypeDto {
  ticketTypeId: number;
  name: string;
  priceMultiplier: number;
  requiresCardVerification: boolean;
  allowedAgeRatingIds: number[];
  allowedFromTime?: string | null;
  allowedToTime?: string | null;
  allowedDaysOfWeek: number[];
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

export interface UpsertTicketTypeRequest {
  name: string;
  priceMultiplier: number;
  requiresCardVerification: boolean;
  allowedAgeRatingIds: number[];
  allowedFromTime?: string | null;
  allowedToTime?: string | null;
  allowedDaysOfWeek: number[];
  isActive: boolean;
}

export interface TicketTypeListParams {
  page?: number;
  pageSize?: number;
  search?: string;
  isActive?: boolean;
}

export interface SpecialDayDto {
  specialDayId: number;
  name: string;
  startDate: string;
  endDate: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

export interface UpsertSpecialDayRequest {
  name: string;
  startDate: string;
  endDate: string;
  isActive: boolean;
}

export interface SpecialDayListParams {
  page?: number;
  pageSize?: number;
  search?: string;
  isActive?: boolean;
  year?: number;
}

export interface DeletePricingEntityResponse {
  id: number;
  deleted: boolean;
}

export interface PriceAdjustmentDto {
  kind: string;
  pricingRuleId?: number | null;
  ruleName?: string | null;
  ruleType?: string | null;
  adjustmentMode?: string | null;
  value?: number | null;
  amount: number;
  reason: string;
}

export interface PriceQuoteDto {
  showtimeId: number;
  seatId?: number | null;
  seatTypeId?: number | null;
  ticketTypeId?: number | null;
  basePrice: number;
  seatMultiplier: number;
  ticketTypeMultiplier: number;
  referencePrice: number;
  dynamicAdjustment: number;
  listPrice: number;
  isEligible: boolean;
  ineligibleReason?: string | null;
  adjustments: PriceAdjustmentDto[];
}

export interface PricingOccupancyDto {
  totalSeats: number;
  bookedSeats: number;
  heldSeats: number;
  percent: number;
  salesPerHour?: number | null;
}

export interface AppliedPricingRuleDto {
  pricingRuleId: number;
  name: string;
  ruleType: string;
  adjustmentMode: string;
  value: number;
  priority: number;
  reason: string;
}

export interface PricingTicketTypeDto {
  ticketTypeId: number;
  name: string;
  priceMultiplier: number;
  requiresCardVerification: boolean;
}

export interface SeatTypePriceRowDto {
  seatTypeId?: number | null;
  name: string;
  priceMultiplier: number;
  prices: PriceQuoteDto[];
}

export interface ShowtimePriceTableDto {
  showtimeId: number;
  movieId: number;
  movieTitle: string;
  roomId: number;
  roomName: string;
  startTime: string;
  basePrice: number;
  at: string;
  pricingMode: string;
  occupancy: PricingOccupancyDto;
  appliedRules: AppliedPricingRuleDto[];
  ticketTypes: PricingTicketTypeDto[];
  rows: SeatTypePriceRowDto[];
}

export interface PricePreviewParams {
  showtimeId: number;
  at?: string;
}

export type PricingRuleType = "TimeOfDay" | "WeekDay" | "SpecialDay" | "Occupancy";
export type PricingAdjustmentMode = "Multiplier" | "Amount";

export interface OccupancyTierDto {
  fromPercent: number;
  toPercent?: number | null;
  maxHoursBeforeStart?: number | null;
  value: number;
}

export interface PricingRuleConditionDto {
  startTime?: string | null;
  endTime?: string | null;
  daysOfWeek?: number[] | null;
  specialDayIds?: number[] | null;
  tiers?: OccupancyTierDto[] | null;
}

export interface PricingRuleDto {
  pricingRuleId: number;
  name: string;
  ruleType: PricingRuleType;
  condition: PricingRuleConditionDto;
  adjustmentMode: PricingAdjustmentMode;
  adjustmentValue?: number | null;
  priority: number;
  validFrom?: string | null;
  validTo?: string | null;
  roomIds: number[];
  screenTypeIds: number[];
  movieIds: number[];
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

export interface UpsertPricingRuleRequest {
  name: string;
  ruleType: PricingRuleType;
  condition: PricingRuleConditionDto;
  adjustmentMode: PricingAdjustmentMode;
  adjustmentValue?: number | null;
  priority: number;
  validFrom?: string | null;
  validTo?: string | null;
  roomIds: number[];
  screenTypeIds: number[];
  movieIds: number[];
  isActive: boolean;
}

export interface PricingRuleListParams {
  page?: number;
  pageSize?: number;
  search?: string;
  ruleType?: string;
  isActive?: boolean;
}
