import { Wrench } from "lucide-react";
import { Link } from "react-router";

import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import { ROUTES } from "@/config/routes";
import { formatDateTime } from "@/lib/formatters";
import { cn } from "@/lib/utils";
import type { PlacedClosure } from "@/features/admin/showtimes/utils/calendar-layout";

const BLOCK_GAP = 2;
const MIN_BLOCK_WIDTH = 40;

export const CLOSURE_ROW_CLASS =
  "closure-hatch flex items-center gap-1.5 px-3 py-1.5 text-xs text-foreground";

export const describeClosure = ({ closure }: PlacedClosure) =>
  `Bảo trì phòng ${closure.roomName}, từ ${formatDateTime(closure.startTime)} đến ${formatDateTime(closure.endTime)}, lý do: ${closure.reason}`;

export const ClosureTooltipBody = ({ placed }: { placed: PlacedClosure }) => {
  const { closure } = placed;
  return (
    <>
      <span className="font-medium">Bảo trì phòng {closure.roomName}</span>
      <span>
        {formatDateTime(closure.startTime)} ~ {formatDateTime(closure.endTime)}
      </span>
      <span>Lý do: {closure.reason}</span>
    </>
  );
};

interface ShowtimeCalendarClosureProps {
  placed: PlacedClosure;
  windowStart: number;
  windowSpan: number;
  rowHeight: number;
  interactive: boolean;
}

export const ShowtimeCalendarClosure = ({
  placed,
  windowStart,
  windowSpan,
  rowHeight,
  interactive,
}: ShowtimeCalendarClosureProps) => {
  const { closure, startMin, endMin } = placed;
  const leftPercent = ((startMin - windowStart) / windowSpan) * 100;
  const widthPercent = ((endMin - startMin) / windowSpan) * 100;

  const sharedProps = {
    "aria-label": describeClosure(placed),
    style: {
      left: `${leftPercent}%`,
      width: `max(calc(${widthPercent}% - ${BLOCK_GAP}px), ${MIN_BLOCK_WIDTH}px)`,
      top: 4,
      height: rowHeight - 8,
    },
    className: cn(
      "closure-hatch group absolute z-[5] flex items-start gap-1 overflow-hidden px-2 py-1 text-left text-xs text-foreground",
      interactive ? "cursor-pointer hover:bg-warning/25" : "cursor-default",
    ),
  };

  const body = (
    <span className="flex min-w-0 items-start gap-1 bg-card/70 px-1 py-0.5">
      <Wrench className="mt-0.5 size-3.5 shrink-0" aria-hidden />
      <span
        className={cn(
          "line-clamp-2 leading-tight font-medium",
          interactive && "group-hover:underline",
        )}
      >
        Bảo trì · {closure.reason}
      </span>
    </span>
  );

  return (
    <Tooltip>
      {interactive ? (
        <TooltipTrigger
          render={<Link to={ROUTES.admin.roomClosureDetail(closure.roomClosureId)} />}
          {...sharedProps}
        >
          {body}
        </TooltipTrigger>
      ) : (
        <TooltipTrigger
          render={<div role="group" tabIndex={0} />}
          {...sharedProps}
        >
          {body}
        </TooltipTrigger>
      )}
      <TooltipContent side="top" className="flex-col items-start gap-0.5">
        <ClosureTooltipBody placed={placed} />
      </TooltipContent>
    </Tooltip>
  );
};
