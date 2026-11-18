import { Home, LogIn, ShieldAlert } from "lucide-react";
import { StatusPage } from "@/components/common/status-page";
import { Seo } from "@/components/common/seo";
import { STATE_MESSAGES } from "@/config/error-messages";
import { ROUTES } from "@/config/routes";

const ForbiddenPage = () => {
  const { forbidden, home } = STATE_MESSAGES.page;

  return (
    <>
      <Seo title={forbidden.title} noIndex />
      <StatusPage
        code="403"
        icon={ShieldAlert}
        title={forbidden.title}
        description={forbidden.description}
        actions={[
          { label: home, to: ROUTES.home, icon: Home },
          { label: "Đăng nhập lại", to: ROUTES.admin.login, icon: LogIn, variant: "outline" },
        ]}
      />
    </>
  );
};

export default ForbiddenPage;
