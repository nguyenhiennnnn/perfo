import { useState } from "react";
import { Outlet } from "react-router";
import { cn } from "@/lib/utils";
import { Sheet, SheetContent, SheetTitle } from "@/components/ui/sheet";
import { useImportExportJobHub } from "@/hooks/use-import-export-job-hub";
import { JobProgressPanel } from "@/features/admin/import-export/components/job-progress-panel";
import { AdminSidebar, AdminSidebarContent } from "./admin-sidebar";
import { AdminTopbar } from "./admin-topbar";

const AdminLayout = () => {
  const [collapsed, setCollapsed] = useState(false);
  const [mobileOpen, setMobileOpen] = useState(false);
  useImportExportJobHub();

  return (
    <div className="min-h-screen bg-background">
      <AdminSidebar collapsed={collapsed} />
      <Sheet open={mobileOpen} onOpenChange={setMobileOpen}>
        <SheetContent
          side="left"
          className="w-64 gap-0 bg-sidebar p-0 text-sidebar-foreground sm:max-w-64 lg:hidden"
        >
          <SheetTitle className="sr-only">Menu quản trị</SheetTitle>
          <AdminSidebarContent collapsed={false} onNavigate={() => setMobileOpen(false)} />
        </SheetContent>
      </Sheet>
      <div
        className={cn(
          "flex min-h-screen flex-col transition-all duration-300",
          collapsed ? "lg:pl-18" : "lg:pl-64",
        )}
      >
        <AdminTopbar
          onToggle={() => setCollapsed((c) => !c)}
          onOpenMenu={() => setMobileOpen(true)}
        />
        <main className="flex-1 p-4 md:p-6">
          <Outlet />
        </main>
      </div>
      <JobProgressPanel />
    </div>
  );
};

export default AdminLayout;
