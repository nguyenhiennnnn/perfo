import { SearchX, type LucideIcon } from "lucide-react";
import { ButtonLink } from "@/components/common/button-link";
import { EmptyState } from "@/components/common/empty-state";
import { Button } from "@/components/ui/button";
import { STATE_MESSAGES } from "@/config/error-messages";

interface NotFoundStateProps {
  entity: string;
  icon?: LucideIcon;
  description?: string;
  backLabel?: string;
  backTo?: string;
  onBack?: () => void;
  className?: string;
}

export const NotFoundState = ({
  entity,
  icon = SearchX,
  description,
  backLabel,
  backTo,
  onBack,
  className,
}: NotFoundStateProps) => {
  let action = null;
  if (backLabel && backTo) {
    action = <ButtonLink to={backTo} variant="secondary">{backLabel}</ButtonLink>;
  } else if (backLabel && onBack) {
    action = <Button variant="secondary" onClick={onBack}>{backLabel}</Button>;
  }

  return (
    <EmptyState
      icon={icon}
      title={STATE_MESSAGES.notFound.title(entity)}
      description={description ?? STATE_MESSAGES.notFound.description(entity)}
      action={action}
      className={className}
    />
  );
};
