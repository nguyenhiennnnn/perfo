import { Clapperboard, Home, Ticket } from "lucide-react";
import { StatusPage } from "@/components/common/status-page";
import { Seo } from "@/components/common/seo";
import { STATE_MESSAGES } from "@/config/error-messages";
import { ROUTES } from "@/config/routes";

const NotFoundPage = () => {
  const { notFound, home } = STATE_MESSAGES.page;

  return (
    <>
      <Seo title={notFound.title} noIndex />
      <StatusPage
        code="404"
        icon={Clapperboard}
        title={notFound.title}
        description={notFound.description}
        actions={[
          { label: home, to: ROUTES.home, icon: Home },
          { label: "Xem lịch chiếu", to: ROUTES.schedule, icon: Ticket, variant: "outline" },
        ]}
      />
    </>
  );
};

export default NotFoundPage;
