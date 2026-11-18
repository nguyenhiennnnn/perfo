import type { ReactNode } from "react";
import { SearchX, type LucideIcon } from "lucide-react";
import { Button } from "@/components/ui/button";
import { STATE_MESSAGES } from "@/config/error-messages";
import { cn } from "@/lib/utils";

interface EmptyVariantProps {
  /** Chưa có dữ liệu: kèm nút tạo mới hoặc hành động tiếp theo. */
  variant?: "empty";
  icon: LucideIcon;
  title: string;
  description?: string;
  actionLabel?: string;
  onAction?: () => void;
  action?: ReactNode;
}

interface FilteredVariantProps {
  /** Có dữ liệu nhưng bộ lọc hoặc từ khóa không khớp: kèm nút xóa bộ lọc. */
  variant: "filtered";
  icon?: LucideIcon;
  title?: string;
  description?: string;
  onClearFilters?: () => void;
}

type EmptyStateProps = (EmptyVariantProps | FilteredVariantProps) & { className?: string };

export const EmptyState = (props: EmptyStateProps) => {
  const { className } = props;

  const isFiltered = props.variant === "filtered";
  const Icon = props.icon ?? SearchX;
  const title = isFiltered ? (props.title ?? STATE_MESSAGES.filtered.title) : props.title;
  const description = isFiltered ? (props.description ?? STATE_MESSAGES.filtered.description) : props.description;

  let action: ReactNode = null;
  if (props.variant === "filtered") {
    if (props.onClearFilters) {
      action = (
        <Button variant="secondary" onClick={props.onClearFilters}>
          {STATE_MESSAGES.clearFilters}
        </Button>
      );
    }
  } else if (props.action) {
    action = props.action;
  } else if (props.actionLabel && props.onAction) {
    action = (
      <Button variant="secondary" onClick={props.onAction}>
        {props.actionLabel}
      </Button>
    );
  }

  return (
    <div className={cn("flex flex-col items-center justify-center gap-3 cut-md bg-card px-6 py-14 text-center", className)}>
      <div className="flex size-12 items-center justify-center cut-sm bg-secondary text-muted-foreground">
        <Icon className="size-6" />
      </div>
      <div className="space-y-1">
        <h3 className="t-h3 text-foreground">{title}</h3>
        {description && <p className="text-sm text-muted-foreground">{description}</p>}
      </div>
      {action && <div className="mt-2">{action}</div>}
    </div>
  );
};
