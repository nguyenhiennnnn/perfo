import type { ComponentProps, ReactNode } from "react";
import { Search } from "lucide-react";

import { Input } from "@/components/ui/input";
import { SEARCH_PLACEHOLDER } from "@/config/default-constants";
import { cn } from "@/lib/utils";

export const FILTER_WIDTH = {
  search: "w-full sm:w-72 lg:w-80",
  select: "w-full sm:w-48",
  date: "w-full sm:w-44",
} as const;

interface FilterBarProps {
  children: ReactNode;
  className?: string;
}

export const FilterBar = ({ children, className }: FilterBarProps) => (
  <div className={cn("flex flex-wrap items-center gap-2", className)}>{children}</div>
);

interface FilterSearchProps
  extends Omit<ComponentProps<typeof Input>, "value" | "onChange" | "className"> {
  value: string;
  onChange: (value: string) => void;
  className?: string;
}

export const FilterSearch = ({
  value,
  onChange,
  placeholder = SEARCH_PLACEHOLDER,
  className,
  ...props
}: FilterSearchProps) => (
  <div className={cn("relative shrink-0", FILTER_WIDTH.search, className)}>
    <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground" />
    <Input
      {...props}
      placeholder={placeholder}
      value={value}
      onChange={(e) => onChange(e.target.value)}
      className="pl-9"
    />
  </div>
);
