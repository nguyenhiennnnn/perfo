import { useLocation, useNavigate } from "react-router";
import { ROUTES } from "@/config/routes";
import { useAlertDialogOpen } from "@/stores/alert-dialog-store";
import { useIsAuthenticated } from "@/stores/auth-store";
import { useDialogOpen } from "@/stores/dialog-store";
import { WaitlistRegisterDialogContent } from "../components/waitlist-register-dialog-content";
import { useWithdrawWaitlist } from "./use-waitlist";

export function useWaitlistActions(showtimeId: number) {
  const isAuthenticated = useIsAuthenticated();
  const navigate = useNavigate();
  const location = useLocation();
  const openAlertDialog = useAlertDialogOpen();
  const openDialog = useDialogOpen();
  const withdrawMutation = useWithdrawWaitlist(showtimeId);

  const openRegister = () => {
    if (!isAuthenticated) {
      const redirectTo = encodeURIComponent(`${location.pathname}${location.search}`);
      openAlertDialog({
        title: "Yêu cầu đăng nhập",
        description: "Bạn cần đăng nhập để đăng ký chờ ghế. Bạn có muốn đăng nhập ngay không?",
        confirmText: "Đăng nhập ngay",
        cancelText: "Để sau",
        onConfirm: () => navigate(`${ROUTES.auth.login}?redirect=${redirectTo}`),
      });
      return;
    }
    openDialog({
      title: "Đăng ký chờ ghế",
      description: "Chọn số ghế bạn cần, chúng tôi sẽ báo khi có ghế phù hợp.",
      content: <WaitlistRegisterDialogContent showtimeId={showtimeId} />,
    });
  };

  const confirmWithdraw = () =>
    openAlertDialog({
      title: "Rút đăng ký chờ?",
      description: "Bạn sẽ không còn được thông báo khi suất chiếu này có ghế.",
      confirmText: "Rút đăng ký",
      cancelText: "Giữ đăng ký",
      variant: "destructive",
      onConfirm: () => withdrawMutation.mutateAsync().then(() => undefined),
    });

  return { openRegister, confirmWithdraw, isWithdrawing: withdrawMutation.isPending };
}
