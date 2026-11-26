import { useState } from "react";
import { ChevronDown } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { formatCurrency, formatDateTime } from "@/lib/formatters";
import { cn } from "@/lib/utils";
import type { RoomRentalTrackingQuoteDto } from "../types";
import { RoomRentalPdfButton } from "./room-rental-pdf-button";
import { RoomRentalQuoteCard } from "./room-rental-quote-card";

interface RoomRentalQuoteHistoryProps {
  versions: RoomRentalTrackingQuoteDto[];
  token: string;
}

export const RoomRentalQuoteHistory = ({ versions, token }: RoomRentalQuoteHistoryProps) => {
  const [expanded, setExpanded] = useState<number | null>(null);
  if (versions.length === 0) return null;

  return (
    <section className="cut-md flex flex-col gap-3 bg-card p-4" aria-label="Các phiên bản báo giá trước">
      <h2 className="t-h3 text-foreground">Phiên bản trước</h2>
      <ul className="flex flex-col gap-3">
        {versions.map((quote) => {
          const isOpen = expanded === quote.versionNumber;
          return (
            <li key={quote.versionNumber} className="flex flex-col gap-3 border border-border p-3">
              <div className="flex items-center justify-between gap-3">
                <div className="min-w-0">
                  <div className="flex items-center gap-2">
                    <p className="text-sm font-semibold text-foreground">
                      Phiên bản {quote.versionNumber}
                    </p>
                    <Badge variant="secondary">Đã thay thế</Badge>
                  </div>
                  <p className="text-xs text-muted-foreground">
                    {formatDateTime(quote.sentAt)} · Tổng {formatCurrency(quote.total)}
                  </p>
                </div>
                <div className="flex shrink-0 items-center gap-1">
                  <RoomRentalPdfButton token={token} version={quote.versionNumber} label="PDF" />
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    aria-expanded={isOpen}
                    aria-label={isOpen ? "Thu gọn phiên bản" : "Xem chi tiết phiên bản"}
                    onClick={() => setExpanded(isOpen ? null : quote.versionNumber)}
                  >
                    <ChevronDown className={cn("transition-transform", isOpen && "rotate-180")} />
                  </Button>
                </div>
              </div>
              {isOpen && <RoomRentalQuoteCard quote={quote} token={token} />}
            </li>
          );
        })}
      </ul>
    </section>
  );
};
