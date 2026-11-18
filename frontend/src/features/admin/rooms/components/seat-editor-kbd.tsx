import type { ReactNode } from "react";

export const SeatEditorKbd = ({ children }: { children: ReactNode }) => (
  <kbd
    data-slot="kbd"
    className="cut-xs bg-current/15 px-1 font-mono text-[10px] leading-4 opacity-80"
  >
    {children}
  </kbd>
);
