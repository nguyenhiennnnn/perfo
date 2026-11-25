export const queryKeys = {
  auth: {
    me: () => ["auth", "me"] as const,
  },

  movies: {
    all: () => ["movies"] as const,
    lists: () => ["movies", "list"] as const,
    list: (params: object) => ["movies", "list", params] as const,
    detail: (movieId: number) => ["movies", "detail", movieId] as const,
    reviews: (movieId: number, params?: object) =>
      ["movies", movieId, "reviews", params] as const,
    canReview: (movieId: number) => ["movies", movieId, "can-review"] as const,
  },

  showtimes: {
    all: () => ["showtimes"] as const,
    schedule: (date: string, screenType?: string | null) =>
      ["showtimes", "schedule", date, screenType ?? null] as const,
    seats: (showtimeId: number) => ["showtimes", "seats", showtimeId] as const,
    seatSuggestions: (showtimeId: number, count: number) =>
      ["showtimes", "seat-suggestions", showtimeId, count] as const,
  },

  orders: {
    all: () => ["orders"] as const,
    myList: (params?: object) => ["orders", "my", params] as const,
    detail: (orderId: number) => ["orders", "detail", orderId] as const,
    comboSuggestions: (orderId: number) => ["orders", "combo-suggestions", orderId] as const,
    exchangePreview: (channel: string, orderId: number, lineIds: number[]) =>
      ["orders", "exchange-preview", channel, orderId, lineIds] as const,
  },

  promotions: {
    mine: () => ["promotions", "mine"] as const,
  },

  giftCards: {
    all: () => ["gift-cards"] as const,
    purchased: (params?: object) => ["gift-cards", "purchased", params] as const,
  },

  loyalty: {
    all: () => ["loyalty"] as const,
    summary: () => ["loyalty", "summary"] as const,
    lots: (params?: object) => ["loyalty", "lots", params] as const,
    rewards: () => ["loyalty", "rewards"] as const,
    challenges: () => ["loyalty", "challenges"] as const,
    referrals: () => ["loyalty", "referrals"] as const,
  },

  invoices: {
    all: () => ["invoices"] as const,
    profiles: () => ["invoices", "profiles"] as const,
    orderRequests: (orderId: number) => ["invoices", "order-requests", orderId] as const,
  },

  support: {
    all: () => ["support"] as const,
    list: (params?: object) => ["support", "list", params] as const,
    detail: (requestId: number) => ["support", "detail", requestId] as const,
  },

  combos: {
    all: () => ["combos"] as const,
    active: () => ["combos", "active"] as const,
    suggestions: (showtimeId: number, seatCount: number) =>
      ["combos", "suggestions", showtimeId, seatCount] as const,
  },

  catalog: {
    all: () => ["catalog"] as const,
    genres: () => ["catalog", "genres"] as const,
    languages: () => ["catalog", "languages"] as const,
    ageRatings: () => ["catalog", "age-ratings"] as const,
    membershipTiers: () => ["catalog", "membership-tiers"] as const,
  },

  settings: {
    get: () => ["settings"] as const,
  },

  users: {
    me: () => ["users", "me"] as const,
    myReviews: (params?: object) => ["users", "me", "reviews", params] as const,
    preferences: () => ["users", "me", "preferences"] as const,
  },

  ai: {
    chat: () => ["ai", "chat"] as const,
  },

  adminMovies: {
    all: () => ["admin", "movies"] as const,
    list: (params: object) => ["admin", "movies", "list", params] as const,
  },

  adminShowtimes: {
    all: () => ["admin", "showtimes"] as const,
    list: (params: object) => ["admin", "showtimes", "list", params] as const,
    lists: () => ["admin", "showtimes", "list"] as const,
    calendars: () => ["admin", "showtimes", "calendar"] as const,
    calendar: (params: object) =>
      ["admin", "showtimes", "calendar", params] as const,
    seatBlocks: (showtimeId: number) =>
      ["admin", "showtimes", "seat-blocks", showtimeId] as const,
  },

  adminCounterShifts: {
    all: () => ["admin", "counter-shifts"] as const,
    current: () => ["admin", "counter-shifts", "current"] as const,
    list: (params: object) => ["admin", "counter-shifts", "list", params] as const,
    open: () => ["admin", "counter-shifts", "open"] as const,
  },

  adminDailyClosings: {
    all: () => ["admin", "daily-closings"] as const,
    list: (params: object) => ["admin", "daily-closings", "list", params] as const,
    detail: (date: string) => ["admin", "daily-closings", "detail", date] as const,
    draft: (date: string) => ["admin", "daily-closings", "draft", date] as const,
    report: (date: string, mode: string) =>
      ["admin", "daily-closings", "report", date, mode] as const,
  },

  adminLedgerAdjustments: {
    all: () => ["admin", "ledger-adjustments"] as const,
    list: (params: object) => ["admin", "ledger-adjustments", "list", params] as const,
  },

  adminGiftCards: {
    all: () => ["admin", "gift-cards"] as const,
    list: (params: object) => ["admin", "gift-cards", "list", params] as const,
    history: (id: number) => ["admin", "gift-cards", "history", id] as const,
  },

  adminOrders: {
    all: () => ["admin", "orders"] as const,
    list: (params: object) => ["admin", "orders", "list", params] as const,
    detail: (orderId: number) => ["admin", "orders", "detail", orderId] as const,
  },

  adminPromotions: {
    all: () => ["admin", "promotions"] as const,
    list: (params?: object) => ["admin", "promotions", "list", params] as const,
    usage: (params?: object) => ["admin", "promotions", "usage", params] as const,
    detail: (id: number) => ["admin", "promotions", "detail", id] as const,
    redemptions: (id: number, params?: object) =>
      ["admin", "promotions", "redemptions", id, params] as const,
    reports: (params?: object) => ["admin", "promotions", "reports", params] as const,
    report: (id: number, params?: object) =>
      ["admin", "promotions", "report", id, params] as const,
  },

  adminLoyaltyRewards: {
    all: () => ["admin", "loyalty", "rewards"] as const,
    list: (params?: object) => ["admin", "loyalty", "rewards", "list", params] as const,
  },

  adminLoyaltyChallenges: {
    all: () => ["admin", "loyalty", "challenges"] as const,
    list: (params?: object) => ["admin", "loyalty", "challenges", "list", params] as const,
  },

  adminLoyaltyCustomers: {
    all: () => ["admin", "loyalty", "customers"] as const,
    search: (query: string) => ["admin", "loyalty", "customers", "search", query] as const,
    detail: (userId: number) => ["admin", "loyalty", "customers", "detail", userId] as const,
    transactions: (userId: number, params?: object) =>
      ["admin", "loyalty", "customers", "transactions", userId, params] as const,
  },

  adminTicketTypes: {
    all: () => ["admin", "ticket-types"] as const,
    list: (params?: object) => ["admin", "ticket-types", "list", params] as const,
  },

  adminPricingRules: {
    all: () => ["admin", "pricing-rules"] as const,
    list: (params?: object) => ["admin", "pricing-rules", "list", params] as const,
  },

  adminSpecialDays: {
    all: () => ["admin", "special-days"] as const,
    list: (params?: object) => ["admin", "special-days", "list", params] as const,
  },

  adminPricingPreview: {
    all: () => ["admin", "pricing-preview"] as const,
    table: (showtimeId: number, at?: string) =>
      ["admin", "pricing-preview", showtimeId, at ?? null] as const,
  },

  adminCashRefunds: {
    all: () => ["admin", "cash-refunds"] as const,
    list: (params: object) => ["admin", "cash-refunds", "list", params] as const,
  },

  adminRoomChangeRefunds: {
    all: () => ["admin", "room-change-refunds"] as const,
    list: (params: object) => ["admin", "room-change-refunds", "list", params] as const,
  },

  adminInvoiceRequests: {
    all: () => ["admin", "invoice-requests"] as const,
    list: (params: object) => ["admin", "invoice-requests", "list", params] as const,
  },

  adminSupportRequests: {
    all: () => ["admin", "support-requests"] as const,
    list: (params: object) => ["admin", "support-requests", "list", params] as const,
    detail: (requestId: number) => ["admin", "support-requests", "detail", requestId] as const,
  },

  adminRefundProposals: {
    all: () => ["admin", "refund-proposals"] as const,
    list: (params: object) => ["admin", "refund-proposals", "list", params] as const,
  },

  adminAuditLogs: {
    all: () => ["admin", "audit-logs"] as const,
    list: (params: object) => ["admin", "audit-logs", "list", params] as const,
    detail: (id: number) => ["admin", "audit-logs", "detail", id] as const,
    filterOptions: () => ["admin", "audit-logs", "filter-options"] as const,
  },

  adminRooms: {
    all: () => ["admin", "rooms"] as const,
    list: (params?: object) => ["admin", "rooms", "list", params] as const,
    detail: (roomId: number) => ["admin", "rooms", "detail", roomId] as const,
    seats: (roomId: number) => ["admin", "rooms", "seats", roomId] as const,
  },

  adminRoomClosures: {
    all: () => ["admin", "room-closures"] as const,
    list: (params?: object) =>
      ["admin", "room-closures", "list", params] as const,
    detail: (id: number) => ["admin", "room-closures", "detail", id] as const,
    affected: (id: number) =>
      ["admin", "room-closures", "affected", id] as const,
    affectedPreview: (params: object) =>
      ["admin", "room-closures", "affected-preview", params] as const,
  },

  adminCombos: {
    all: () => ["admin", "combos"] as const,
    list: (params?: object) => ["admin", "combos", "list", params] as const,
  },

  adminInventory: {
    all: () => ["admin", "inventory"] as const,
    items: (params?: object) => ["admin", "inventory", "items", params] as const,
    itemOptions: () => ["admin", "inventory", "item-options"] as const,
    recipes: (params?: object) => ["admin", "inventory", "recipes", params] as const,
    recipe: (comboId: number) => ["admin", "inventory", "recipe", comboId] as const,
    receipts: (params?: object) => ["admin", "inventory", "receipts", params] as const,
    receipt: (id: number) => ["admin", "inventory", "receipt", id] as const,
    stocktakes: (params?: object) => ["admin", "inventory", "stocktakes", params] as const,
    stocktake: (id: number) => ["admin", "inventory", "stocktake", id] as const,
    waste: (params?: object) => ["admin", "inventory", "waste", params] as const,
    wasteSheet: (id: number) => ["admin", "inventory", "waste-sheet", id] as const,
    stock: (params?: object) => ["admin", "inventory", "stock", params] as const,
    alerts: () => ["admin", "inventory", "alerts"] as const,
    ledger: (params?: object) => ["admin", "inventory", "ledger", params] as const,
    valuation: () => ["admin", "inventory", "valuation"] as const,
  },

  adminComboPickups: {
    all: () => ["admin", "combo-pickups"] as const,
    unclaimed: (params?: object) => ["admin", "combo-pickups", "unclaimed", params] as const,
    lookup: (code: string) => ["admin", "combo-pickups", "lookup", code] as const,
  },

  adminReviews: {
    all: () => ["admin", "reviews"] as const,
    list: (params: object) => ["admin", "reviews", "list", params] as const,
  },

  adminUsers: {
    all: () => ["admin", "users"] as const,
    list: (params: object) => ["admin", "users", "list", params] as const,
    admins: (params?: object) => ["admin", "users", "admins", params] as const,
    customers: (params?: object) => ["admin", "users", "customers", params] as const,
  },

  adminEmployees: {
    all: () => ["admin", "employees"] as const,
    list: (params?: object) => ["admin", "employees", "list", params] as const,
  },

  adminSettings: {
    get: () => ["admin", "settings"] as const,
  },

  adminDashboard: {
    all: () => ["admin", "dashboard"] as const,
    summary: () => ["admin", "dashboard", "summary"] as const,
    insights: (params: object) =>
      ["admin", "dashboard", "insights", params] as const,
    occupancy: (params: object) =>
      ["admin", "dashboard", "occupancy", params] as const,
    seatHeatmap: (params: object) =>
      ["admin", "dashboard", "seat-heatmap", params] as const,
    comboRevenue: (params: object) =>
      ["admin", "dashboard", "combo-revenue", params] as const,
    cancellations: (params: object) =>
      ["admin", "dashboard", "cancellations", params] as const,
  },

  adminScheduleAnalysis: {
    all: () => ["admin", "schedule-analysis"] as const,
    report: (params: object) => ["admin", "schedule-analysis", "report", params] as const,
    suggestions: () => ["admin", "schedule-analysis", "suggestions"] as const,
  },

  adminReconciliation: {
    all: () => ["admin", "reconciliation"] as const,
    sessions: (params: object) => ["admin", "reconciliation", "sessions", params] as const,
    session: (id: number) => ["admin", "reconciliation", "session", id] as const,
    items: (params: object) => ["admin", "reconciliation", "items", params] as const,
  },

  adminReports: {
    all: () => ["admin", "reports"] as const,
    revenue: (params: object) =>
      ["admin", "reports", "revenue", params] as const,
    topMovies: (params: object) =>
      ["admin", "reports", "top-movies", params] as const,
    employeePerformance: (params: object) =>
      ["admin", "reports", "employee-performance", params] as const,
    finance: (params: object) =>
      ["admin", "reports", "finance", params] as const,
  },

  adminImportExport: {
    all: () => ["admin", "import-export"] as const,
    lists: () => ["admin", "import-export", "list"] as const,
    list: (params: object) =>
      ["admin", "import-export", "list", params] as const,
    detail: (jobId: string) =>
      ["admin", "import-export", "detail", jobId] as const,
  },

  adminCatalog: {
    all: () => ["admin", "catalog"] as const,
    genres: (params?: object) =>
      ["admin", "catalog", "genres", params] as const,
    languages: (params?: object) =>
      ["admin", "catalog", "languages", params] as const,
    ageRatings: (params?: object) =>
      ["admin", "catalog", "age-ratings", params] as const,
    seatTypes: (params?: object) =>
      ["admin", "catalog", "seat-types", params] as const,
    screenTypes: (params?: object) =>
      ["admin", "catalog", "screen-types", params] as const,
    membershipTiers: (params?: object) =>
      ["admin", "catalog", "membership-tiers", params] as const,
    positions: (params?: object) =>
      ["admin", "catalog", "positions", params] as const,
    actors: (params?: object) =>
      ["admin", "catalog", "actors", params] as const,
    directors: (params?: object) =>
      ["admin", "catalog", "directors", params] as const,
  },
} as const;
