import { Home, RefreshCw, TriangleAlert } from "lucide-react";
import { EmptyState } from "@/components/common/empty-state";
import { StatusPage } from "@/components/common/status-page";
import { Button } from "@/components/ui/button";
import { ROUTES } from "@/config/routes";
import { STATE_MESSAGES } from "@/config/error-messages";
import { describeLoadError } from "@/lib/error";

interface ErrorStateProps {
  title?: string;
  error?: unknown;
  cause?: string;
  fix?: string;
  onRetry: () => void;
  variant?: "section" | "page";
  className?: string;
}

export const ErrorState = ({
  title,
  error,
  cause,
  fix,
  onRetry,
  variant = "section",
  className,
}: ErrorStateProps) => {
  if (variant === "page") {
    const description = cause && fix ? `${cause} ${fix}` : STATE_MESSAGES.page.error.description;

    return (
      <StatusPage
        code="500"
        icon={TriangleAlert}
        title={title ?? STATE_MESSAGES.page.error.title}
        description={description}
        actions={[
          { label: STATE_MESSAGES.page.reload, icon: RefreshCw, onClick: onRetry },
          { label: STATE_MESSAGES.page.home, to: ROUTES.home, icon: Home, variant: "outline" },
        ]}
      />
    );
  }

  const derived = describeLoadError(error);

  return (
    <EmptyState
      icon={TriangleAlert}
      title={title ?? STATE_MESSAGES.loadError.title}
      description={`${cause ?? derived.cause} ${fix ?? derived.fix}`}
      action={
        <Button variant="secondary" onClick={onRetry}>
          {STATE_MESSAGES.retry}
        </Button>
      }
      className={className}
    />
  );
};
