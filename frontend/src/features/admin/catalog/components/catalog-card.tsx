import { Link } from "react-router";
import type { LucideIcon } from "lucide-react";
import { LineSkeleton } from "@/components/common/skeleton-shapes";
import { cn } from "@/lib/utils";

export interface CatalogCardProps {
  to: string;
  icon: LucideIcon;
  title: string;
  description: string;
  count?: number;
  isLoading: boolean;
  isError?: boolean;
  accent: string;
}

export const CatalogCard = ({
  to,
  icon: Icon,
  title,
  description,
  count,
  isLoading,
  isError = false,
  accent,
}: CatalogCardProps) => {
  return (
    <Link
      to={to}
      className={cn(
        "group relative flex flex-col justify-between overflow-hidden bg-card p-5 transition-colors duration-150",
        "hover:bg-secondary",
      )}
    >
      <div className="flex items-center justify-between">
        <div
          className={cn(
            "flex size-11 items-center justify-center",
            accent,
          )}
        >
          <Icon className="size-5" />
        </div>
        <div className="text-right">
          {isLoading ? (
            <LineSkeleton className="ml-auto h-8 w-10" />
          ) : (
            <span className="font-heading text-2xl font-semibold tabular text-foreground">
              {isError ? "—" : (count ?? 0)}
            </span>
          )}
          <p className={cn("text-xs", isError ? "text-destructive" : "text-muted-foreground")}>
            {isError ? "Không tải được" : "bản ghi"}
          </p>
        </div>
      </div>

      <div className="mt-4">
        <h3 className="t-h3 text-foreground transition-colors group-hover:text-primary-text">
          {title}
        </h3>
        <p className="mt-1 line-clamp-2 text-xs text-muted-foreground">
          {description}
        </p>
      </div>
    </Link>
  );
};
