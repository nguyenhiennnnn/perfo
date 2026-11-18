import type { ReactNode } from "react";
import { cn } from "@/lib/utils";

interface PageTitleProps {
  title: string;
  description?: string;
  action?: ReactNode;
  className?: string;
}

export const PageTitle = ({ title, description, action, className }: PageTitleProps) => (
  <div className={cn("mb-6 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between", className)}>
    <div>
      <h1 className="t-h1 text-foreground">{title}</h1>
      {description && <p className="mt-1 text-sm text-muted-foreground">{description}</p>}
    </div>
    {action}
  </div>
);
