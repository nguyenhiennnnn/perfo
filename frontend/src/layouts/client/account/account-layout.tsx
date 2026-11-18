import { Outlet } from "react-router";
import { PageContainer } from "@/components/common/page-container";
import { AccountSidebar } from "./account-sidebar";

const AccountLayout = () => (
  <PageContainer size="xl" className="flex flex-col gap-6 md:flex-row">
    <AccountSidebar />
    <div className="min-w-0 flex-1">
      <Outlet />
    </div>
  </PageContainer>
);

export default AccountLayout;