import { NavLink } from "react-router";
import { cn } from "@/lib/utils";
import { useAuthStore } from "@/stores/auth-store";
import { BrandMark, BrandWordmark } from "@/components/common/brand-mark";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import { ADMIN_NAV_GROUPS, filterNavGroupsByRole, type AdminNavItem } from "@/config/admin-nav";
import { getRoleLabel } from "@/components/common/status-badge.config";

const AdminNavLink = ({
  item: { to, label, icon: Icon },
  collapsed,
  onNavigate,
}: {
  item: AdminNavItem;
  collapsed: boolean;
  onNavigate?: () => void;
}) => {
  const link = (
    <NavLink
      to={to}
      end={true}
      title={collapsed ? label : undefined}
      onClick={onNavigate}
      className={({ isActive }) =>
        cn(
          "flex w-full items-center gap-2.5 px-4 py-2 text-base font-medium text-sidebar-foreground/70 transition-colors hover:bg-sidebar-accent hover:text-sidebar-foreground",
          isActive &&
            "bg-sidebar-primary text-sidebar-primary-foreground hover:bg-sidebar-primary hover:text-sidebar-primary-foreground",
          collapsed && "justify-center px-0",
        )
      }
    >
      <Icon className="size-4 shrink-0" />
      {!collapsed && <span className="truncate">{label}</span>}
    </NavLink>
  );

  if (!collapsed) return link;

  return (
    <Tooltip>
      <TooltipTrigger render={link} />
      <TooltipContent side="right">{label}</TooltipContent>
    </Tooltip>
  );
};

export const AdminSidebarContent = ({
  collapsed,
  onNavigate,
}: {
  collapsed: boolean;
  onNavigate?: () => void;
}) => {
  const { user } = useAuthStore();
  const visibleGroups = filterNavGroupsByRole(ADMIN_NAV_GROUPS, user?.role);

  return (
    <>
      <div className="flex h-16 items-center gap-2 border-b border-sidebar-border px-4">
        <BrandMark className="size-7 shrink-0" />
        {!collapsed && (
          <BrandWordmark className="h-4 w-auto" />
        )}
      </div>

      <nav className="flex-1 overflow-y-auto py-3">
        {visibleGroups.map((group) => (
          <div key={group.label} className="mb-2">
            {!collapsed && (
              <p className="px-4 pb-1 pt-2 text-sm font-medium text-sidebar-foreground/60">
                {group.label}
              </p>
            )}
            {group.items.map((item) => (
              <AdminNavLink key={item.to} item={item} collapsed={collapsed} onNavigate={onNavigate} />
            ))}
          </div>
        ))}
      </nav>

      <div className="flex items-center gap-2 border-t border-sidebar-border p-3">
        <img
          src={user?.avatar ?? "/icons.svg"}
          alt={user?.fullName}
          className="size-8 shrink-0 border border-sidebar-border object-cover"
        />
        {!collapsed && (
          <div className="min-w-0">
            <p className="truncate text-xs font-medium text-sidebar-foreground">
              {user?.fullName}
            </p>
            <p className="truncate text-xs text-sidebar-foreground/60">
              {getRoleLabel(user?.role)}
            </p>
          </div>
        )}
      </div>
    </>
  );
};

export const AdminSidebar = ({ collapsed }: { collapsed: boolean }) => (
  <aside
    className={cn(
      "fixed top-0 bottom-0 left-0 z-40 hidden flex-col border-r border-sidebar-border bg-sidebar transition-all duration-300 lg:flex",
      collapsed ? "w-18" : "w-64",
    )}
  >
    <AdminSidebarContent collapsed={collapsed} />
  </aside>
);
