import { KeyRound } from "lucide-react";
import { Navigate } from "react-router";

import { ChangePasswordForm } from "@/features/auth/components/change-password-form";
import { ROUTES } from "@/config/routes";
import { useAuthStore } from "@/stores/auth-store";

const ChangePasswordPage = () => {
  const hasPassword = useAuthStore((s) => s.user?.hasPassword ?? true);

  if (!hasPassword) return <Navigate to={ROUTES.account.profile} replace />;

  return (
    <ChangePasswordForm
      layout="panel"
      heading="Đổi mật khẩu"
      submitLabel="Cập nhật mật khẩu"
      submitIcon={<KeyRound className="size-4" />}
    />
  );
};

export default ChangePasswordPage;
