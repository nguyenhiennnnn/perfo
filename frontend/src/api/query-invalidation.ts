import type { QueryClient } from "@tanstack/react-query";
import { queryKeys as k } from "./query-keys";

type KeyList = readonly (readonly unknown[])[];

const dashboard = (): KeyList => [k.adminDashboard.all(), k.adminReports.all()];
const movieViews = (): KeyList => [k.adminMovies.all(), k.movies.all()];
const showtimeViews = (): KeyList => [
  k.adminShowtimes.all(),
  k.showtimes.all(),
];
const orderViews = (): KeyList => [
  k.adminOrders.all(),
  k.adminCashRefunds.all(),
  k.adminRoomChangeRefunds.all(),
  k.orders.all(),
];

export const invalidations = {
  movieChanged: (): KeyList => [
    ...movieViews(),
    ...showtimeViews(),
    k.adminReviews.all(),
    ...dashboard(),
  ],
  movieInterestChanged: (): KeyList => [k.movieInterests.all()],
  reconciliationChanged: (): KeyList => [k.adminReconciliation.all(), k.adminAuditLogs.all()],
  showtimeChanged: (): KeyList => [
    ...showtimeViews(),
    k.adminRoomClosures.all(),
    k.movies.all(),
    ...dashboard(),
  ],
  seatBlocksChanged: (): KeyList => [
    ...showtimeViews(),
    k.adminAuditLogs.all(),
    ...dashboard(),
  ],
  showtimeRoomChanged: (): KeyList => [
    ...invalidations.showtimeChanged(),
    ...orderViews(),
    k.adminAuditLogs.all(),
  ],
  roomClosureChanged: (): KeyList => [
    k.adminRoomClosures.all(),
    ...showtimeViews(),
    k.adminAuditLogs.all(),
    ...dashboard(),
  ],
  showtimeCancelled: (): KeyList => [
    ...invalidations.showtimeChanged(),
    ...orderViews(),
  ],

  bookingChanged: (): KeyList => [
    ...orderViews(),
    ...showtimeViews(),
    k.users.me(),
    ...dashboard(),
    k.adminInventory.all(),
    k.adminComboPickups.all(),
    k.combos.all(),
    k.giftCards.all(),
    k.loyalty.all(),
    k.waitlist.all(),
  ],

  counterShiftChanged: (): KeyList => [
    k.adminCounterShifts.all(),
    k.adminDailyClosings.all(),
    k.adminAuditLogs.all(),
  ],

  ledgerAdjustmentChanged: (): KeyList => [
    k.adminLedgerAdjustments.all(),
    k.adminDailyClosings.all(),
    k.adminAuditLogs.all(),
    ...dashboard(),
  ],

  adminGiftCardChanged: (): KeyList => [
    k.adminGiftCards.all(),
    k.giftCards.all(),
    ...orderViews(),
    k.adminAuditLogs.all(),
    ...dashboard(),
  ],

  dailyClosingChanged: (): KeyList => [
    k.adminDailyClosings.all(),
    k.adminCounterShifts.all(),
    k.adminAuditLogs.all(),
    ...dashboard(),
  ],

  invoiceProfileChanged: (): KeyList => [k.invoices.profiles()],

  invoiceRequestChanged: (): KeyList => [
    k.invoices.all(),
    k.adminInvoiceRequests.all(),
    k.adminAuditLogs.all(),
  ],

  roomRentalTrackingChanged: (): KeyList => [k.roomRentals.trackingAll()],

  supportRequestChanged: (): KeyList => [k.support.all()],

  supportStaffChanged: (): KeyList => [
    k.adminSupportRequests.all(),
    k.support.all(),
    k.adminAuditLogs.all(),
  ],

  refundProposalChanged: (): KeyList => [
    k.adminRefundProposals.all(),
    k.adminSupportRequests.all(),
    k.support.all(),
    ...orderViews(),
    ...dashboard(),
    k.adminAuditLogs.all(),
  ],

  loyaltyChanged: (): KeyList => [k.loyalty.all(), k.promotions.mine(), k.users.me()],

  orderGiftCardChanged: (): KeyList => [...orderViews(), k.giftCards.all()],
  orderPromotionChanged: (): KeyList => [...orderViews(), k.promotions.mine()],

  promotionChanged: (): KeyList => [
    k.adminPromotions.all(),
    k.promotions.mine(),
    k.adminAuditLogs.all(),
  ],

  loyaltyRewardChanged: (): KeyList => [
    k.adminLoyaltyRewards.all(),
    k.adminLoyaltyChallenges.all(),
    k.loyalty.all(),
    k.adminAuditLogs.all(),
  ],

  loyaltyPointsAdjusted: (): KeyList => [
    k.adminLoyaltyCustomers.all(),
    k.adminReports.all(),
    k.loyalty.all(),
    k.adminAuditLogs.all(),
  ],

  loyaltyChallengeChanged: (): KeyList => [
    k.adminLoyaltyChallenges.all(),
    k.loyalty.all(),
    k.adminAuditLogs.all(),
  ],

  comboChanged: (): KeyList => [k.adminCombos.all(), k.combos.all()],

  inventoryChanged: (): KeyList => [
    k.adminInventory.all(),
    k.adminCombos.all(),
    k.combos.all(),
    k.adminAuditLogs.all(),
  ],

  ticketTypeChanged: (): KeyList => [
    k.adminTicketTypes.all(),
    k.adminPricingPreview.all(),
    k.adminAuditLogs.all(),
  ],
  pricingRuleChanged: (): KeyList => [
    k.adminPricingRules.all(),
    k.adminPricingPreview.all(),
    k.adminAuditLogs.all(),
  ],
  specialDayChanged: (): KeyList => [
    k.adminSpecialDays.all(),
    k.adminPricingPreview.all(),
    k.adminAuditLogs.all(),
  ],

  roomChanged: (): KeyList => [
    k.adminRooms.all(),
    ...showtimeViews(),
    ...dashboard(),
  ],
  seatLayoutChanged: (): KeyList => [k.adminRooms.all(), ...showtimeViews()],

  genreChanged: (): KeyList => [
    k.adminCatalog.genres(),
    k.catalog.genres(),
    ...movieViews(),
  ],
  languageChanged: (): KeyList => [
    k.adminCatalog.languages(),
    k.catalog.languages(),
    ...movieViews(),
  ],
  ageRatingChanged: (): KeyList => [
    k.adminCatalog.ageRatings(),
    k.catalog.ageRatings(),
    ...movieViews(),
  ],
  actorChanged: (): KeyList => [k.adminCatalog.actors(), ...movieViews()],
  directorChanged: (): KeyList => [k.adminCatalog.directors(), ...movieViews()],
  genreCreated: (): KeyList => [k.adminCatalog.genres(), k.catalog.genres()],
  languageCreated: (): KeyList => [
    k.adminCatalog.languages(),
    k.catalog.languages(),
  ],
  ageRatingCreated: (): KeyList => [
    k.adminCatalog.ageRatings(),
    k.catalog.ageRatings(),
  ],
  actorCreated: (): KeyList => [k.adminCatalog.actors()],
  directorCreated: (): KeyList => [k.adminCatalog.directors()],
  positionCreated: (): KeyList => [k.adminCatalog.positions()],
  screenTypeCreated: (): KeyList => [k.adminCatalog.screenTypes()],
  membershipTierChanged: (): KeyList => [
    k.adminCatalog.membershipTiers(),
    k.catalog.membershipTiers(),
    k.adminUsers.all(),
    k.users.me(),
    k.adminAuditLogs.all(),
  ],
  positionChanged: (): KeyList => [
    k.adminCatalog.positions(),
    k.adminEmployees.all(),
  ],
  screenTypeChanged: (): KeyList => [
    k.adminCatalog.screenTypes(),
    k.adminRooms.all(),
    ...showtimeViews(),
  ],
  seatTypeChanged: (): KeyList => [
    k.adminCatalog.seatTypes(),
    k.adminRooms.all(),
    ...showtimeViews(),
  ],

  userChanged: (): KeyList => [k.adminUsers.all(), k.adminEmployees.all()],
  employeeChanged: (): KeyList => [
    k.adminEmployees.all(),
    k.adminUsers.all(),
    k.adminReports.all(),
  ],
  profileChanged: (): KeyList => [
    k.users.me(),
    k.auth.me(),
    k.adminUsers.all(),
  ],
  passwordChanged: (): KeyList => [
    k.users.me(),
    k.auth.me(),
    k.adminUsers.all(),
    k.adminEmployees.all(),
  ],

  customerRestrictionChanged: (): KeyList => [
    k.adminCustomerRestrictions.all(),
    k.adminAuditLogs.all(),
  ],
  verificationChanged: (): KeyList => [k.verification.all()],

  surveyChanged: (): KeyList => [k.surveys.all()],

  waitlistChanged: (): KeyList => [k.waitlist.all()],

  reviewChanged: (): KeyList => [
    k.adminReviews.all(),
    ...movieViews(),
    k.users.myReviews(),
    k.adminDashboard.all(),
  ],

  roomRentalChanged: (): KeyList => [k.adminRoomRentals.all(), k.adminAuditLogs.all()],
  roomRentalPaymentChanged: (): KeyList => [
    k.adminRoomRentals.all(),
    k.adminAuditLogs.all(),
    ...orderViews(),
    ...showtimeViews(),
    ...dashboard(),
  ],
  roomRentalRateChanged: (): KeyList => [
    k.adminRoomRentalRates.all(),
    k.adminRoomRentals.all(),
    k.adminAuditLogs.all(),
  ],

  settingsChanged: (): KeyList => [
    k.adminSettings.get(),
    k.settings.get(),
    k.adminAuditLogs.all(),
  ],
} satisfies Record<string, () => KeyList>;

export type InvalidationName = keyof typeof invalidations;

type CommonMeta = {
  errorTitle?: string;
  skipErrorToast?: boolean;
};

export type AppQueryMeta = CommonMeta;

export type AppMutationMeta = CommonMeta & {
  skipSuccessToast?: boolean;
  invalidates?: InvalidationName;
};

declare module "@tanstack/react-query" {
  interface Register {
    queryMeta: AppQueryMeta;
    mutationMeta: AppMutationMeta;
  }
}

function trimTrailingUndefined(key: readonly unknown[]): readonly unknown[] {
  let end = key.length;
  while (end > 0 && key[end - 1] === undefined) end -= 1;
  return key.slice(0, end);
}

export async function invalidateRelated(
  client: QueryClient,
  name: InvalidationName | undefined,
): Promise<void> {
  if (!name) return;
  const unique = new Map<string, readonly unknown[]>();
  for (const key of invalidations[name]()) {
    const trimmed = trimTrailingUndefined(key);
    unique.set(JSON.stringify(trimmed), trimmed);
  }
  await Promise.all(
    [...unique.values()].map((queryKey) =>
      client.invalidateQueries({ queryKey }),
    ),
  );
}
