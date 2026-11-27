import { Sparkles } from "lucide-react";

import { Badge } from "@/components/ui/badge";

export const ShowtimeEventBadge = ({ className }: { className?: string }) => (
  <Badge variant="warning" className={className}>
    <Sparkles aria-hidden /> Suất sự kiện
  </Badge>
);
