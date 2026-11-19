import { useEffect, useEffectEvent, useRef } from "react";
import { useNavigate, useSearchParams } from "react-router";

import { useTheme } from "@/app/providers/theme-provider";
import { toast } from "@/components/ui/toast";
import { extractApiError, logDevError } from "@/lib/error";
import { loadGoogleIdentity } from "@/lib/google-identity";
import type { GoogleCredentialResponse } from "@/types/google-identity";

import { useGoogleLogin } from "../hooks/use-auth";
import { ENV } from "@/config/env";
import { GOOGLE_SIGN_IN_BUTTON_WIDTH_PX } from "@/config/default-constants";
import { getSafeRedirect } from "@/lib/utils";

const GOOGLE_CLIENT_ID = ENV.GOOGLE_CLIENT_ID;

interface GoogleSignInButtonProps {
  redirectTo?: string;
}

export const GoogleSignInButton = ({ redirectTo }: GoogleSignInButtonProps) => {
  const containerRef = useRef<HTMLDivElement>(null);
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { mutate: googleLogin } = useGoogleLogin();
  const { theme } = useTheme();

  const isDark =
    theme === "dark" ||
    (theme === "system" &&
      typeof window !== "undefined" &&
      window.matchMedia("(prefers-color-scheme: dark)").matches);

  const onCredential = useEffectEvent((response: GoogleCredentialResponse) => {
    googleLogin(
      { idToken: response.credential },
      {
        onSuccess: () => {
          navigate(getSafeRedirect(redirectTo ?? searchParams.get("redirect")), {
            replace: true,
          });
        },
        onError: (error) => {
          toast.add({
            title: "Đăng nhập Google thất bại",
            description: extractApiError(error).message,
            type: "error",
          });
        },
      },
    );
  });

  useEffect(() => {
    if (!GOOGLE_CLIENT_ID) return;

    let cancelled = false;

    loadGoogleIdentity()
      .then(() => {
        if (cancelled || !containerRef.current || !window.google) return;

        containerRef.current.innerHTML = "";

        window.google.accounts.id.initialize({
          client_id: GOOGLE_CLIENT_ID,
          callback: (response) => onCredential(response),
          cancel_on_tap_outside: true,
        });

        window.google.accounts.id.renderButton(containerRef.current, {
          type: "standard",
          theme: isDark ? "filled_black" : "outline",
          size: "large",
          text: "continue_with",
          shape: "rectangular",
          width: GOOGLE_SIGN_IN_BUTTON_WIDTH_PX,
        });
      })
      .catch((error: unknown) => logDevError(error, "Google Identity"));

    return () => {
      cancelled = true;
    };
  }, [isDark]);

  if (!GOOGLE_CLIENT_ID) return null;

  return <div ref={containerRef} className="flex w-full justify-center" />;
};
