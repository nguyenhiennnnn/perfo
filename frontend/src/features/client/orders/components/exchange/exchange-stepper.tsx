import { cn } from "@/lib/utils";

export type ExchangeStep = "showtime" | "seats" | "payment" | "done";

const STEPS = [
  { key: "showtime", label: "Chọn suất" },
  { key: "seats", label: "Chọn ghế" },
  { key: "payment", label: "Hoàn tất" },
] as const;

const STEP_INDEX: Record<ExchangeStep, number> = { showtime: 0, seats: 1, payment: 2, done: 2 };

export const ExchangeStepper = ({ step }: { step: ExchangeStep }) => {
  const current = STEP_INDEX[step];
  return (
    <ol className="flex items-center gap-2 text-sm" aria-label="Các bước đổi suất">
      {STEPS.map((item, index) => (
        <li
          key={item.key}
          aria-current={index === current ? "step" : undefined}
          className={cn(
            "flex items-center gap-2 font-medium",
            index <= current ? "text-foreground" : "text-muted-foreground",
          )}
        >
          <span
            className={cn(
              "flex size-6 items-center justify-center text-xs tabular-nums",
              index <= current ? "bg-primary text-primary-foreground" : "bg-muted text-muted-foreground",
            )}
          >
            {index + 1}
          </span>
          {item.label}
          {index < STEPS.length - 1 && <span aria-hidden className="mx-1 h-px w-6 bg-border" />}
        </li>
      ))}
    </ol>
  );
};
