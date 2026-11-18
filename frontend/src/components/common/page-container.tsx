import type { ComponentProps } from "react";
import { cn } from "@/lib/utils";

const SIZE_CLASS = {
  sm: "max-w-4xl",
  md: "max-w-5xl",
  lg: "max-w-6xl",
  xl: "max-w-7xl",
} as const;

export type PageContainerSize = keyof typeof SIZE_CLASS;

interface PageContainerProps extends ComponentProps<"div"> {
  size?: PageContainerSize;
}

export const PageContainer = ({ size = "xl", className, ...props }: PageContainerProps) => (
  <div
    className={cn("mx-auto w-full px-4 py-8 sm:px-6 lg:px-8", SIZE_CLASS[size], className)}
    {...props}
  />
);
