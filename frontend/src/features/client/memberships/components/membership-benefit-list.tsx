import { Check } from "lucide-react";
import { cn } from "@/lib/utils";
import type { MembershipPlanDto } from "../types";
import { getPlanBenefits } from "../utils/membership-plans";

interface MembershipBenefitListProps {
  plan: MembershipPlanDto;
  className?: string;
}

export const MembershipBenefitList = ({ plan, className }: MembershipBenefitListProps) => (
  <ul className={cn("flex flex-col gap-2", className)}>
    {getPlanBenefits(plan).map((benefit) => (
      <li key={benefit.key} className="flex items-start gap-2 text-sm text-foreground">
        <Check className="mt-0.5 size-4 shrink-0 text-success" aria-hidden />
        <span>{benefit.label}</span>
      </li>
    ))}
  </ul>
);
