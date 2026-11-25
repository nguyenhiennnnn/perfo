import type { RouteObject } from "react-router";

import ClientLayout from "@/layouts/client/client-layout";
import AccountLayout from "@/layouts/client/account/account-layout";
import AuthLayout from "@/layouts/client/auth-layout";
import { CustomerProtectedRoute } from "./protected-route/customer-protected-route";
import { GuestRoute } from "./protected-route/guest-route";
import { lazyRoute } from "./lazy-route";
import { ROUTES } from "@/config/routes";

const BookingPage = lazyRoute(() => import("@/features/client/orders/pages/booking-page"));
const BookingResultPage = lazyRoute(() => import("@/features/client/orders/pages/booking-result-page"));
const HomePage = lazyRoute(() => import("@/features/client/home/pages/home-page"));
const MovieDetailPage = lazyRoute(() => import("@/features/client/movies/pages/movie-detail-page"));
const MoviesPage = lazyRoute(() => import("@/features/client/movies/pages/movies-page"));
const SchedulePage = lazyRoute(() => import("@/features/client/schedule/pages/schedule-page"));
const AccountProfilePage = lazyRoute(() => import("@/features/client/account/pages/profile-page"));
const AccountTicketsPage = lazyRoute(() => import("@/features/client/account/pages/tickets-page"));
const AccountReviewsPage = lazyRoute(() => import("@/features/client/account/pages/reviews-page"));
const ChangePasswordPage = lazyRoute(() => import("@/features/client/account/pages/change-password-page"));
const AccountTicketDetailPage = lazyRoute(() => import("@/features/client/account/pages/ticket-detail-page"));
const LoginPage = lazyRoute(() => import("@/features/auth/pages/login-page"));
const RegisterPage = lazyRoute(() => import("@/features/auth/pages/register-page"));
const ConfirmEmailPage = lazyRoute(() => import("@/features/auth/pages/confirm-email-page"));
const ForgotPasswordPage = lazyRoute(() => import("@/features/auth/pages/forgot-password-page"));
const ResetPasswordPage = lazyRoute(() => import("@/features/auth/pages/reset-password-page"));
const UnsubscribePage = lazyRoute(() => import("@/features/client/account/pages/unsubscribe-page"));
const MembershipPage = lazyRoute(() => import("@/features/client/membership/pages/membership-page"));
const GiftCardPurchasePage = lazyRoute(() => import("@/features/client/gift-cards/pages/gift-card-purchase-page"));
const GiftCardBulkPage = lazyRoute(() => import("@/features/client/gift-cards/pages/gift-card-bulk-page"));
const AccountGiftCardsPage = lazyRoute(() => import("@/features/client/gift-cards/pages/purchased-gift-cards-page"));
const PolicyPage = lazyRoute(() => import("@/features/client/policies/pages/policy-page"));

export const clientRoutes: RouteObject[] = [
  {
    path: "/",
    element: <ClientLayout />,
    children: [
      { index: true, lazy: HomePage },
      { path: "movies", lazy: MoviesPage },
      { path: "movies/:id", lazy: MovieDetailPage },
      { path: "schedule", lazy: SchedulePage },
      { path: "membership", lazy: MembershipPage },
      { path: "gift-cards", lazy: GiftCardPurchasePage },
      { path: "gift-cards/bulk", lazy: GiftCardBulkPage },
      { path: "policies/:slug", lazy: PolicyPage },
      { path: "booking/:showtimeId", lazy: BookingPage },
      { path: "booking/result", lazy: BookingResultPage },
      {
        path: "account",
        element: <CustomerProtectedRoute />,
        children: [
          {
            element: <AccountLayout />,
            children: [
              { path: "profile", lazy: AccountProfilePage },
              { path: "tickets", lazy: AccountTicketsPage },
              { path: "gift-cards", lazy: AccountGiftCardsPage },
              { path: "reviews", lazy: AccountReviewsPage },
              { path: "change-password", lazy: ChangePasswordPage },
            ],
          },
          { path: "tickets/:id", lazy: AccountTicketDetailPage },
        ],
      },
    ],
  },
  {
    element: <AuthLayout />,
    children: [{ path: ROUTES.unsubscribe, lazy: UnsubscribePage }],
  },
  {
    path: ROUTES.auth.root,
    element: <GuestRoute redirectTo={ROUTES.home} />,
    children: [
      {
        element: <AuthLayout />,
        children: [
          { path: "login", lazy: LoginPage },
          { path: "register", lazy: RegisterPage },
          { path: "confirm-email", lazy: ConfirmEmailPage },
          { path: "forgot-password", lazy: ForgotPasswordPage },
          { path: "reset-password", lazy: ResetPasswordPage },
        ],
      },
    ],
  },
];
