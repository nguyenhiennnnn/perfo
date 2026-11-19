import type { ReactNode } from "react";
import { Check, type LucideIcon } from "lucide-react";
import { cn } from "@/lib/utils";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import { SeatEditorKbd } from "./seat-editor-kbd";

const DEFAULT_ACTIVE = "bg-foreground text-background";
const INACTIVE = "bg-secondary text-foreground hover:bg-[color-mix(in_oklch,var(--secondary),var(--foreground)_8%)]";

interface SeatEditorToolButtonProps {
  active: boolean;
  onClick: () => void;
  icon?: LucideIcon;
  activeClassName?: string;
  tooltip?: string;
  shortcut?: string;
  children: ReactNode;
}

export const SeatEditorToolButton = ({
  active,
  onClick,
  icon: Icon,
  activeClassName = DEFAULT_ACTIVE,
  tooltip,
  shortcut,
  children,
}: SeatEditorToolButtonProps) => {
  const button = (
    <button
      type="button"
      aria-pressed={active}
      onClick={onClick}
      className={cn(
        "flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium outline-none transition-colors focus-visible:ring-2 focus-visible:ring-ring/50",
        active ? activeClassName : INACTIVE,
      )}
    >
      {active ? <Check className="size-3.5" /> : Icon && <Icon className="size-3.5" />}
      {children}
      {shortcut && <SeatEditorKbd>{shortcut}</SeatEditorKbd>}
    </button>
  );

  if (!tooltip) return button;

  return (
    <Tooltip>
      <TooltipTrigger render={button} />
      <TooltipContent>{tooltip}</TooltipContent>
    </Tooltip>
  );
};
