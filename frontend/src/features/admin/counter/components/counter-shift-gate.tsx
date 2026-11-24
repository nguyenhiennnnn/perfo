import type { ReactNode } from "react";
import { QueryState } from "@/components/common/query-state";
import { CardSkeleton, LineSkeleton } from "@/components/common/skeleton-shapes";
import { useCurrentCounterShift } from "../hooks/use-counter-shift";
import type { CounterShift } from "../types/counter-shift";
import { CounterShiftOpenPanel } from "./counter-shift-open-panel";

interface CounterShiftGateProps {
  children: (shift: CounterShift) => ReactNode;
}

export const CounterShiftGate = ({ children }: CounterShiftGateProps) => {
  const query = useCurrentCounterShift();

  return (
    <QueryState
      query={query}
      skeleton={
        <div aria-busy="true" className="mx-auto flex w-full max-w-md flex-col gap-4 py-6">
          <LineSkeleton className="h-8 w-3/4" />
          <CardSkeleton className="h-32" />
          <CardSkeleton className="h-24" />
        </div>
      }
      errorTitle="Không tải được ca quầy"
    >
      {(shift) => (shift ? children(shift) : <CounterShiftOpenPanel />)}
    </QueryState>
  );
};
