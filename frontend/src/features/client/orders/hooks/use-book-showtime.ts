import { useCallback } from "react";
import { useNavigate } from "react-router";
import { useAuthStore, useIsAuthenticated } from "@/stores/auth-store";
import { useAlertDialogOpen } from "@/stores/alert-dialog-store";
import { ROUTES } from "@/config/routes";

export function useBookShowtime() {
  const navigate = useNavigate();
  const isAuthenticated = useIsAuthenticated();
  const isInitialized = useAuthStore((s) => s.isInitialized);
  const openAlertDialog = useAlertDialogOpen();

  const bookShowtime = useCallback(
    (showtimeId: number) => {
      if (isAuthenticated) {
        navigate(ROUTES.booking.detail(showtimeId));
        return;
      }

      const redirectTo = encodeURIComponent(ROUTES.booking.detail(showtimeId));
      openAlertDialog({
        title: "Yêu cầu đăng nhập",
        description: "Bạn cần đăng nhập để đặt vé. Bạn có muốn đăng nhập ngay không?",
        confirmText: "Đăng nhập ngay",
        cancelText: "Để sau",
        onConfirm: () => {
          navigate(`${ROUTES.auth.login}?redirect=${redirectTo}`);
        },
      });
    },
    [isAuthenticated, navigate, openAlertDialog],
  );

  return { bookShowtime, isReady: isInitialized };
}
