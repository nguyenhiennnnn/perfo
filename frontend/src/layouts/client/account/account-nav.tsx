import { NavLink } from "react-router";
import { User, Ticket, LifeBuoy, Gift, Coins, Trophy, UserPlus, Star, KeyRound, LogOut } from "lucide-react";
import { cn } from "@/lib/utils";
import { ROUTES } from "@/config/routes";
import { useAuthStore } from "@/stores/auth-store";

const ACCOUNT_NAV = [
  { to: ROUTES.account.profile, label: "Thông tin cá nhân", icon: User },
  { to: ROUTES.account.tickets, label: "Lịch sử đơn", icon: Ticket },
  { to: ROUTES.account.supportRequests, label: "Yêu cầu hỗ trợ", icon: LifeBuoy },
  { to: ROUTES.account.giftCards, label: "Thẻ quà đã mua", icon: Gift },
  { to: ROUTES.account.points, label: "Điểm thưởng", icon: Coins },
  { to: ROUTES.account.challenges, label: "Thử thách", icon: Trophy },
  { to: ROUTES.account.referrals, label: "Giới thiệu bạn bè", icon: UserPlus },
  { to: ROUTES.account.reviews, label: "Đánh giá của tôi", icon: Star },
  {
    to: ROUTES.account.changePassword,
    label: "Đổi mật khẩu",
    icon: KeyRound,
    requiresPassword: true,
  },
];

interface AccountNavProps {
  onLogout: () => void;
}

export const AccountNav = ({ onLogout }: AccountNavProps) => {
  const hasPassword = useAuthStore((s) => s.user?.hasPassword ?? true);
  const items = ACCOUNT_NAV.filter((item) => hasPassword || !item.requiresPassword);

  return (
    <nav className="mt-4 flex flex-col gap-1 cut-md bg-card p-2">
      {items.map(({ to, label, icon: Icon }) => (
        <NavLink
          key={to}
          to={to}
          className={({ isActive }) =>
            cn(
              "flex items-center gap-2.5 px-3 py-2 text-sm font-medium text-muted-foreground cut-xs transition-colors hover:bg-muted hover:text-foreground",
              isActive && "bg-primary text-primary-foreground font-semibold hover:bg-primary hover:text-primary-foreground",
            )
          }
        >
          <Icon className="size-4" />
          {label}
        </NavLink>
      ))}
      <div className="my-1 border-t border-border" />
      <button
        onClick={onLogout}
        className="flex items-center gap-2.5 px-3 py-2 text-sm font-medium text-destructive cut-xs transition-colors hover:bg-destructive/10 cursor-pointer"
      >
        <LogOut className="size-4" />
        Đăng xuất
      </button>
    </nav>
  );
};
