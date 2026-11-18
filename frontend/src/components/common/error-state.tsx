import { AlertTriangle } from "lucide-react";
import { EmptyState } from "@/components/common/empty-state";

interface ErrorStateProps {
  title: string;
  description: string;
  onRetry: () => void;
  className?: string;
}

export const ErrorState = ({ title, description, onRetry, className }: ErrorStateProps) => (
  <EmptyState
    icon={AlertTriangle}
    title={title}
    description={description}
    actionLabel="Thử lại"
    onAction={onRetry}
    className={className}
  />
);
