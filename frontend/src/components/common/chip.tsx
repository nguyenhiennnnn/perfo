import type { ComponentProps, KeyboardEvent } from "react";
import { cn } from "@/lib/utils";

type ChipRole = "tab" | "radio";

interface ChipProps extends Omit<ComponentProps<"button">, "role" | "type"> {
  selected: boolean;
  role?: ChipRole;
}

type ChipGroupRole = "group" | "tablist" | "radiogroup";

interface ChipGroupProps extends Omit<ComponentProps<"div">, "role"> {
  role?: ChipGroupRole;
}

const ARROW_STEP: Record<string, number> = {
  ArrowRight: 1,
  ArrowDown: 1,
  ArrowLeft: -1,
  ArrowUp: -1,
};

const ROVING_SELECTOR = "[role=tab]:not(:disabled), [role=radio]:not(:disabled)";

export const Chip = ({ selected, role, className, ...props }: ChipProps) => {
  const semantics =
    role === "tab"
      ? { role, "aria-selected": selected, tabIndex: selected ? 0 : -1 }
      : role === "radio"
        ? { role, "aria-checked": selected, tabIndex: selected ? 0 : -1 }
        : { "aria-pressed": selected };

  return (
    <button
      type="button"
      {...semantics}
      {...props}
      className={cn(
        "inline-flex h-11 cursor-pointer items-center justify-center cut-sm px-4 text-sm font-medium transition-colors select-none disabled:pointer-events-none disabled:opacity-50",
        selected ? "bg-primary text-primary-foreground" : "bg-secondary text-secondary-foreground hover:bg-muted",
        className,
      )}
    />
  );
};

export const ChipGroup = ({ role = "group", onKeyDown, className, ...props }: ChipGroupProps) => {
  const handleKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    onKeyDown?.(event);
    const step = ARROW_STEP[event.key];
    if (event.defaultPrevented || !step || role === "group") return;
    const chips = Array.from(event.currentTarget.querySelectorAll<HTMLButtonElement>(ROVING_SELECTOR));
    const current = chips.findIndex((chip) => chip === document.activeElement);
    if (current < 0) return;
    event.preventDefault();
    const next = chips[(current + step + chips.length) % chips.length];
    next.focus();
    next.click();
  };

  return <div role={role} onKeyDown={handleKeyDown} className={cn("flex flex-wrap items-center gap-2", className)} {...props} />;
};
