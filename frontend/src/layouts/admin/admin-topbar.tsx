import { ModeToggle } from "@/components/common/mode-toggle";
import { Button } from "@/components/ui/button";
import { useAuthUser } from "@/stores/auth-store";
import { ChevronRight, Globe, Menu, PanelLeft } from "lucide-react";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import { Fragment } from "react";
import { Link, useLocation } from "react-router";
import { useLogoutConfirm } from "@/hooks/use-logout-confirm";
import { ROUTES } from "@/config/routes";
import { getAdminBreadcrumbs } from "@/config/admin-nav";
import { getRoleLabel } from "@/components/common/status-badge.config";

export const AdminTopbar = ({ onToggle, onOpenMenu }: { onToggle: () => void; onOpenMenu: () => void }) => {
  const user = useAuthUser();
  const confirmLogout = useLogoutConfirm();
  const { pathname } = useLocation();
  const crumbs = getAdminBreadcrumbs(pathname);

  return (
    <header className="sticky top-0 z-30 flex h-16 w-full items-center gap-4 border-b border-border bg-background px-4">
      <Button variant="ghost" size="icon" className="lg:hidden" aria-label="Mở menu" onClick={onOpenMenu}>
        <Menu className="size-5" />
      </Button>
      <Tooltip>
        <TooltipTrigger
          render={
            <Button
              variant="ghost"
              size="icon"
              className="hidden lg:inline-flex"
              aria-label="Thu gọn / mở rộng menu"
              onClick={onToggle}
            />
          }
        >
          <PanelLeft className="size-5" />
        </TooltipTrigger>
        <TooltipContent>Thu gọn / mở rộng menu</TooltipContent>
      </Tooltip>

      <nav aria-label="Breadcrumb" className="hidden min-w-0 md:block">
        <ol className="flex items-center gap-1.5 text-xs text-muted-foreground">
          <li>
            <Link to={ROUTES.admin.dashboard} className="hover:text-foreground">
              Quản trị
            </Link>
          </li>
          {crumbs.map((crumb, index) => (
            <Fragment key={crumb.to}>
              <li aria-hidden>
                <ChevronRight className="size-4" />
              </li>
              <li className="truncate">
                {index === crumbs.length - 1 ? (
                  <span aria-current="page" className="font-medium text-foreground">
                    {crumb.label}
                  </span>
                ) : (
                  <Link to={crumb.to} className="hover:text-foreground">
                    {crumb.label}
                  </Link>
                )}
              </li>
            </Fragment>
          ))}
        </ol>
      </nav>

      <div className="ml-auto flex items-center gap-1">
        <Tooltip>
          <TooltipTrigger
            render={
              <Button
                variant="ghost"
                size="icon"
                nativeButton={false}
                aria-label="Xem trang khách hàng"
                render={<a href={ROUTES.home} target="_blank" rel="noreferrer" />}
              />
            }
          >
            <Globe className="size-4" />
          </TooltipTrigger>
          <TooltipContent>Xem trang khách hàng</TooltipContent>
        </Tooltip>
        <ModeToggle />

        <DropdownMenu>
          <DropdownMenuTrigger className="ml-1 rounded-full outline-none ring-ring/50 focus-visible:ring-2">
            <img
              src={user?.avatar ?? "/icons.svg"}
              alt={user?.fullName}
              className="size-9 rounded-full border border-border object-cover"
            />
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end" className="w-56">
            <DropdownMenuLabel>
              <p className="font-medium text-foreground">{user?.fullName}</p>
              <p className="text-xs font-normal text-muted-foreground">
                {getRoleLabel(user?.role)}
              </p>
            </DropdownMenuLabel>
            <DropdownMenuSeparator />
            <DropdownMenuItem render={<Link to={ROUTES.admin.settings} />}>
              Cài đặt hệ thống
            </DropdownMenuItem>
            <DropdownMenuSeparator />
            <DropdownMenuItem render={<Link to={ROUTES.admin.changePassword} />}>
              Đổi mật khẩu
            </DropdownMenuItem>
            <DropdownMenuItem className="text-destructive" onClick={confirmLogout}>
              Đăng xuất
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
    </header>
  );
};
