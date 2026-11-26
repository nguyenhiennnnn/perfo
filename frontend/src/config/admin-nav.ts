import {
  LayoutDashboard,
  ShoppingBag,
  ScanLine,
  Film,
  Calendar,
  Wallet,
  Gift,
  Landmark,
  CalendarClock,
  Tv,
  DoorClosed,
  Presentation,
  BadgeDollarSign,
  Popcorn,
  Warehouse,
  Package,
  ClipboardList,
  PackagePlus,
  ClipboardCheck,
  PackageMinus,
  BookOpen,
  Coins,
  PackageX,
  PackageCheck,
  Ticket,
  FileText,
  Receipt,
  Banknote,
  Clock,
  LockKeyhole,
  Archive,
  Scale,
  ArrowRightLeft,
  Tags,
  CalendarHeart,
  Calculator,
  SlidersHorizontal,
  TicketPercent,
  FileClock,
  BarChart3,
  Users,
  UserX,
  ShieldAlert,
  MessageSquare,
  Layers,
  History,
  ScrollText,
  Settings,
  Trophy,
  LifeBuoy,
  HandCoins,
  type LucideIcon,
} from "lucide-react";
import { ROUTES } from "@/config/routes";
import { Role } from "@/types/enum";

export interface AdminNavItem {
  to: string;
  label: string;
  icon: LucideIcon;
  roles: Role[];
}

export interface AdminNavGroup {
  label: string;
  items: AdminNavItem[];
}

export const ADMIN_NAV_GROUPS: AdminNavGroup[] = [
  {
    label: "Tổng quan",
    items: [
      {
        to: ROUTES.admin.dashboard,
        label: "Dashboard",
        icon: LayoutDashboard,
        roles: [Role.Admin],
      },
    ],
  },
  {
    label: "Nghiệp vụ rạp",
    items: [
      {
        to: ROUTES.admin.counter,
        label: "Bán hàng tại quầy",
        icon: ShoppingBag,
        roles: [Role.Admin, Role.Employee],
      },
      {
        to: ROUTES.admin.checkin,
        label: "Soát vé",
        icon: ScanLine,
        roles: [Role.Admin, Role.Employee],
      },
      {
        to: ROUTES.admin.comboPickup,
        label: "Nhận combo",
        icon: PackageCheck,
        roles: [Role.Admin, Role.Employee],
      },
    ],
  },
  {
    label: "Quản lý rạp & phim",
    items: [
      {
        to: ROUTES.admin.movies,
        label: "Quản lý phim",
        icon: Film,
        roles: [Role.Admin, Role.Employee],
      },
      {
        to: ROUTES.admin.showtimes,
        label: "Suất chiếu",
        icon: Calendar,
        roles: [Role.Admin, Role.Employee],
      },
      {
        to: ROUTES.admin.scheduleAnalysis,
        label: "Phân tích lịch chiếu",
        icon: CalendarClock,
        roles: [Role.Admin],
      },
      { to: ROUTES.admin.rooms, label: "Phòng chiếu", icon: Tv, roles: [Role.Admin] },
      {
        to: ROUTES.admin.roomClosures,
        label: "Đóng phòng",
        icon: DoorClosed,
        roles: [Role.Admin],
      },
      {
        to: ROUTES.admin.roomRentals,
        label: "Yêu cầu thuê phòng",
        icon: Presentation,
        roles: [Role.Admin],
      },
      {
        to: ROUTES.admin.roomRentalCalendar,
        label: "Lịch phòng thuê",
        icon: CalendarClock,
        roles: [Role.Admin],
      },
      {
        to: ROUTES.admin.roomRentalReport,
        label: "Báo cáo thuê phòng",
        icon: Banknote,
        roles: [Role.Admin],
      },
      {
        to: ROUTES.admin.roomRentalRates,
        label: "Bảng giá thuê phòng",
        icon: BadgeDollarSign,
        roles: [Role.Admin],
      },
      {
        to: ROUTES.admin.combos,
        label: "Bắp nước & Combo",
        icon: Popcorn,
        roles: [Role.Admin],
      },
    ],
  },
  {
    label: "Kho bắp nước",
    items: [
      {
        to: ROUTES.admin.inventory,
        label: "Tồn kho",
        icon: Warehouse,
        roles: [Role.Admin, Role.Employee],
      },
      {
        to: ROUTES.admin.inventoryItems,
        label: "Sản phẩm kho",
        icon: Package,
        roles: [Role.Admin],
      },
      {
        to: ROUTES.admin.inventoryRecipes,
        label: "Công thức combo",
        icon: ClipboardList,
        roles: [Role.Admin],
      },
      {
        to: ROUTES.admin.inventoryReceipts,
        label: "Nhập kho",
        icon: PackagePlus,
        roles: [Role.Admin],
      },
      {
        to: ROUTES.admin.inventoryStocktakes,
        label: "Kiểm kê",
        icon: ClipboardCheck,
        roles: [Role.Admin],
      },
      {
        to: ROUTES.admin.inventoryWaste,
        label: "Hao hụt",
        icon: PackageMinus,
        roles: [Role.Admin],
      },
      {
        to: ROUTES.admin.inventoryLedger,
        label: "Sổ cái kho",
        icon: BookOpen,
        roles: [Role.Admin],
      },
      {
        to: ROUTES.admin.inventoryValuation,
        label: "Giá trị tồn",
        icon: Coins,
        roles: [Role.Admin],
      },
      {
        to: ROUTES.admin.unclaimedComboOrders,
        label: "Combo chưa nhận",
        icon: PackageX,
        roles: [Role.Admin],
      },
    ],
  },
  {
    label: "Đơn hàng & tài chính",
    items: [
      {
        to: ROUTES.admin.orders,
        label: "Quản lý đơn hàng",
        icon: Ticket,
        roles: [Role.Admin, Role.Employee],
      },
      {
        to: ROUTES.admin.giftCards,
        label: "Thẻ quà tặng",
        icon: Gift,
        roles: [Role.Admin],
      },
      {
        to: ROUTES.admin.counterShifts,
        label: "Ca quầy",
        icon: Clock,
        roles: [Role.Admin],
      },
      {
        to: ROUTES.admin.dailyClosings,
        label: "Chốt doanh thu ngày",
        icon: LockKeyhole,
        roles: [Role.Admin],
      },
      {
        to: ROUTES.admin.dailyClosingHistory,
        label: "Lịch sử chốt ngày",
        icon: Archive,
        roles: [Role.Admin],
      },
      {
        to: ROUTES.admin.ledgerAdjustments,
        label: "Bút toán điều chỉnh",
        icon: Scale,
        roles: [Role.Admin],
      },
      {
        to: ROUTES.admin.invoiceRequests,
        label: "Yêu cầu hóa đơn VAT",
        icon: FileText,
        roles: [Role.Admin],
      },
      {
        to: ROUTES.admin.supportRequests,
        label: "Yêu cầu hỗ trợ",
        icon: LifeBuoy,
        roles: [Role.Admin, Role.Employee],
      },
      {
        to: ROUTES.admin.refundProposals,
        label: "Hoàn tiền ngoại lệ",
        icon: HandCoins,
        roles: [Role.Admin, Role.Employee],
      },
      {
        to: ROUTES.admin.cashRefunds,
        label: "Đơn tiền mặt cần hoàn",
        icon: Banknote,
        roles: [Role.Admin, Role.Employee],
      },
      {
        to: ROUTES.admin.roomChangeRefunds,
        label: "Hoàn chênh lệch đổi phòng",
        icon: ArrowRightLeft,
        roles: [Role.Admin],
      },
      {
        to: ROUTES.admin.reports,
        label: "Báo cáo & doanh thu",
        icon: Banknote,
        roles: [Role.Admin],
      },
      {
        to: ROUTES.admin.financeReport,
        label: "Báo cáo tài chính",
        icon: Wallet,
        roles: [Role.Admin],
      },
      {
        to: ROUTES.admin.taxReport,
        label: "Báo cáo thuế",
        icon: Receipt,
        roles: [Role.Admin],
      },
      {
        to: ROUTES.admin.reconciliation,
        label: "Đối soát thanh toán",
        icon: Landmark,
        roles: [Role.Admin],
      },
    ],
  },
  {
    label: "Khuyến mãi",
    items: [
      {
        to: ROUTES.admin.promotions,
        label: "Quản lý khuyến mãi",
        icon: TicketPercent,
        roles: [Role.Admin],
      },
      {
        to: ROUTES.admin.promotionReports,
        label: "Báo cáo khuyến mãi",
        icon: Banknote,
        roles: [Role.Admin],
      },
    ],
  },
  {
    label: "Tích điểm",
    items: [
      {
        to: ROUTES.admin.loyaltyRewards,
        label: "Quản lý quà",
        icon: Gift,
        roles: [Role.Admin],
      },
      {
        to: ROUTES.admin.loyaltyChallenges,
        label: "Quản lý thử thách",
        icon: Trophy,
        roles: [Role.Admin],
      },
      {
        to: ROUTES.admin.loyaltyAdjustments,
        label: "Điều chỉnh điểm",
        icon: Coins,
        roles: [Role.Admin],
      },
      {
        to: ROUTES.admin.loyaltyReport,
        label: "Báo cáo điểm",
        icon: Banknote,
        roles: [Role.Admin],
      },
    ],
  },
  {
    label: "Giá vé",
    items: [
      {
        to: ROUTES.admin.pricingTicketTypes,
        label: "Loại vé",
        icon: Tags,
        roles: [Role.Admin],
      },
      {
        to: ROUTES.admin.pricingRules,
        label: "Quy tắc giá",
        icon: SlidersHorizontal,
        roles: [Role.Admin],
      },
      {
        to: ROUTES.admin.pricingSpecialDays,
        label: "Lịch ngày lễ",
        icon: CalendarHeart,
        roles: [Role.Admin],
      },
      {
        to: ROUTES.admin.pricingPreview,
        label: "Xem trước giá",
        icon: Calculator,
        roles: [Role.Admin],
      },
      {
        to: ROUTES.admin.pricingRuleHistory,
        label: "Lịch sử quy tắc giá",
        icon: FileClock,
        roles: [Role.Admin],
      },
    ],
  },
  {
    label: "Người dùng & đánh giá",
    items: [
      { to: ROUTES.admin.users, label: "Khách hàng", icon: Users, roles: [Role.Admin] },
      {
        to: ROUTES.admin.customerRestrictions,
        label: "Khách bị hạn chế",
        icon: UserX,
        roles: [Role.Admin],
      },
      {
        to: ROUTES.admin.employees,
        label: "Nhân viên",
        icon: ShieldAlert,
        roles: [Role.Admin],
      },
      {
        to: ROUTES.admin.reviews,
        label: "Đánh giá phim",
        icon: MessageSquare,
        roles: [Role.Admin],
      },
    ],
  },
  {
    label: "Hệ thống & danh mục",
    items: [
      { to: ROUTES.admin.catalog, label: "Trung tâm danh mục", icon: Layers, roles: [Role.Admin] },
      {
        to: ROUTES.admin.importExportHistory,
        label: "Lịch sử Import/Export",
        icon: History,
        roles: [Role.Admin],
      },
      {
        to: ROUTES.admin.auditLogs,
        label: "Nhật ký thao tác",
        icon: ScrollText,
        roles: [Role.Admin],
      },
      {
        to: ROUTES.admin.settings,
        label: "Cài đặt hệ thống",
        icon: Settings,
        roles: [Role.Admin],
      },
    ],
  },
];

export const ADMIN_ACCOUNT_PATHS: { to: string; roles: Role[] }[] = [
  { to: ROUTES.admin.changePassword, roles: [Role.Admin, Role.Employee] },
];

export const filterNavGroupsByRole = (
  groups: AdminNavGroup[],
  role: string | undefined,
): AdminNavGroup[] =>
  groups
    .map((group) => ({
      ...group,
      items: group.items.filter((item) => !role || item.roles.includes(role as Role)),
    }))
    .filter((group) => group.items.length > 0);

export const canAccessAdminPath = (pathname: string, role: string | undefined): boolean => {
  const item = [...ADMIN_NAV_GROUPS.flatMap((group) => group.items), ...ADMIN_ACCOUNT_PATHS]
    .filter((nav) => pathname === nav.to || pathname.startsWith(`${nav.to}/`))
    .sort((a, b) => b.to.length - a.to.length)[0];
  return !item || (!!role && item.roles.includes(role as Role));
};

export interface AdminBreadcrumb {
  label: string;
  to: string;
}

const ADMIN_SEGMENT_LABELS: Record<string, string> = {
  "schedule-analysis": "Phân tích lịch chiếu",
  finance: "Báo cáo tài chính",
  tax: "Báo cáo thuế",
  reconciliation: "Đối soát cổng thanh toán",
  genres: "Thể loại phim",
  languages: "Ngôn ngữ",
  "age-ratings": "Phân loại độ tuổi",
  "seat-types": "Loại ghế",
  "screen-types": "Loại màn hình",
  "membership-tiers": "Hạng thành viên",
  positions: "Chức vụ",
  actors: "Diễn viên",
  directors: "Đạo diễn",
  seats: "Sơ đồ ghế",
};

const ADMIN_NAV_LABELS = new Map(
  ADMIN_NAV_GROUPS.flatMap((group) => group.items.map((item) => [item.to, item.label] as const)),
);

export const getAdminBreadcrumbs = (pathname: string): AdminBreadcrumb[] => {
  const segments = pathname.split("/").filter(Boolean).slice(1);

  return segments.flatMap((segment, index) => {
    const to = `${ROUTES.admin.root}/${segments.slice(0, index + 1).join("/")}`;
    const isId = /^\d+$/.test(segment);
    if (isId && index < segments.length - 1) return [];
    const label = isId ? "Chi tiết" : (ADMIN_NAV_LABELS.get(to) ?? ADMIN_SEGMENT_LABELS[segment]);
    return label ? [{ label, to }] : [];
  });
};
