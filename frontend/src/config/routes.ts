import { Role } from "@/types/enum";

export const ROUTES = {
  home: "/",
  forbidden: "/403",
  notFound: "/404",
  schedule: "/schedule",
  membership: "/membership",
  unsubscribe: "/unsubscribe",
  policy: (slug: string) => `/policies/${slug}`,
  movies: {
    list: "/movies",
    detail: (id: number | string) => `/movies/${id}`,
  },
  booking: {
    root: "/booking",
    detail: (showtimeId: number | string | undefined) =>
      `/booking/${showtimeId}`,
    result: (orderId: number | string) =>
      `/booking/result?bookingId=${orderId}`,
  },
  auth: {
    root: "/auth",
    login: "/auth/login",
    register: "/auth/register",
    forgotPassword: "/auth/forgot-password",
  },
  account: {
    root: "/account",
    ticketDetail: (bookingId: number | string) =>
      `/account/tickets/${bookingId}`,
    profile: "/account/profile",
    tickets: "/account/tickets",
    reviews: "/account/reviews",
    changePassword: "/account/change-password",
  },
  admin: {
    root: "/admin",
    login: "/admin/login",
    resetPassword: "/admin/reset-password",
    changePassword: "/admin/change-password",
    dashboard: "/admin/dashboard",
    counter: "/admin/bookings/counter",
    checkin: "/admin/checkin",
    movies: "/admin/movies",
    showtimes: "/admin/showtimes",
    rooms: "/admin/rooms",
    combos: "/admin/combos",
    orders: "/admin/orders",
    cashRefunds: "/admin/cash-refunds",
    auditLogs: "/admin/audit-logs",
    promotions: "/admin/promotions",
    promotionReports: "/admin/promotions/reports",
    promotionRedemptions: (promotionId: number | string) =>
      `/admin/promotions/${promotionId}/redemptions`,
    promotionReport: (promotionId: number | string) =>
      `/admin/promotions/${promotionId}/report`,
    pricingTicketTypes: "/admin/pricing/ticket-types",
    pricingRules: "/admin/pricing/rules",
    pricingRuleHistory: "/admin/pricing/rule-history",
    pricingRuleHistoryByRule: (ruleId: number | string) =>
      `/admin/pricing/rule-history?entityId=${ruleId}`,
    pricingSpecialDays: "/admin/pricing/special-days",
    pricingPreview: "/admin/pricing/preview",
    reports: "/admin/reports",
    users: "/admin/users",
    employees: "/admin/employees",
    reviews: "/admin/reviews",
    catalog: "/admin/catalog",
    catalogGenres: "/admin/catalog/genres",
    catalogLanguages: "/admin/catalog/languages",
    catalogAgeRatings: "/admin/catalog/age-ratings",
    catalogSeatTypes: "/admin/catalog/seat-types",
    catalogScreenTypes: "/admin/catalog/screen-types",
    catalogMembershipTiers: "/admin/catalog/membership-tiers",
    catalogPositions: "/admin/catalog/positions",
    catalogActors: "/admin/catalog/actors",
    catalogDirectors: "/admin/catalog/directors",
    orderDetail: (orderId: number | string) => `/admin/orders/${orderId}`,
    ordersByShowtime: (showtimeId: number | string) =>
      `/admin/orders?showtimeId=${showtimeId}`,
    roomSeats: (roomId: number | string) => `/admin/rooms/${roomId}/seats`,
    importExportHistory: "/admin/import-export",
    settings: "/admin/settings",
  },
} as const;

export const getAdminHomePath = (role?: string | null) =>
  role === Role.Employee ? ROUTES.admin.counter : ROUTES.admin.dashboard;
