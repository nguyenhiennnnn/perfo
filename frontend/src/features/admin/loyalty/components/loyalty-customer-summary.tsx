import { SlidersHorizontal, X } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { cn } from "@/lib/utils";
import { formatPoints } from "@/lib/formatters";

import type { LoyaltyCustomerDetailDto } from "../types";

interface LoyaltyCustomerSummaryProps {
  customer: LoyaltyCustomerDetailDto;
  onAdjust: () => void;
  onClear: () => void;
}

interface StatProps {
  label: string;
  value: string;
  tone?: "default" | "destructive";
}

const Stat = ({ label, value, tone = "default" }: StatProps) => (
  <div className="space-y-0.5">
    <p className="text-xs text-muted-foreground">{label}</p>
    <p
      className={cn(
        "text-xl font-semibold tabular-nums",
        tone === "destructive" ? "text-destructive" : "text-foreground",
      )}
    >
      {value}
    </p>
  </div>
);

export const LoyaltyCustomerSummary = ({
  customer,
  onAdjust,
  onClear,
}: LoyaltyCustomerSummaryProps) => (
  <Card>
    <CardHeader className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
      <div className="min-w-0 space-y-1">
        <CardTitle className="flex flex-wrap items-center gap-2">
          <span className="truncate">{customer.fullName}</span>
          {customer.tierName && <Badge variant="outline">{customer.tierName}</Badge>}
        </CardTitle>
        <p className="truncate text-sm text-muted-foreground">
          {[customer.email, customer.phone].filter(Boolean).join(" · ")}
        </p>
      </div>
      <div className="flex gap-2">
        <Button onClick={onAdjust}>
          <SlidersHorizontal className="size-4" />
          <span>Điều chỉnh điểm</span>
        </Button>
        <Button variant="outline" size="icon" className="size-10" onClick={onClear} aria-label="Bỏ chọn khách">
          <X className="size-4" />
        </Button>
      </div>
    </CardHeader>
    <CardContent className="grid grid-cols-2 gap-4 sm:grid-cols-3">
      <Stat label="Điểm xếp hạng" value={formatPoints(customer.rankPoints)} />
      <Stat label="Số dư điểm đổi quà" value={formatPoints(customer.balance)} />
      <Stat
        label="Điểm đang nợ"
        value={formatPoints(customer.debt)}
        tone={customer.debt > 0 ? "destructive" : "default"}
      />
    </CardContent>
  </Card>
);
