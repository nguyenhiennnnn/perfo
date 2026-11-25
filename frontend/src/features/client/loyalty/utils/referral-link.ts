import { ROUTES } from "@/config/routes";

export const REFERRAL_QUERY_PARAM = "ref";
export const REFERRAL_CODE_MAX_LENGTH = 16;

export const buildReferralLink = (code: string) =>
  `${window.location.origin}${ROUTES.auth.register}?${REFERRAL_QUERY_PARAM}=${encodeURIComponent(code)}`;
