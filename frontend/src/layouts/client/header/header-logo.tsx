import { Link } from "react-router";
import { BRAND } from "@/config/brand";
import { BrandMark, BrandWordmark } from "@/components/common/brand-mark";
import { ROUTES } from "@/config/routes";

export const HeaderLogo = () => (
  <Link to={ROUTES.home} aria-label={BRAND.name} className="group flex items-center gap-2">
    <BrandMark className="size-8 shrink-0" />
    <BrandWordmark className="hidden h-5 w-auto sm:block" />
  </Link>
);
