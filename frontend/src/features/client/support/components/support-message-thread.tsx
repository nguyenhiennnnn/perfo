import { cn } from "@/lib/utils";
import { formatDateTime } from "@/lib/formatters";
import { SupportMessageAuthorType } from "@/types/enum";
import type { SupportMessageDto } from "../types";
import { getSupportAuthorLabel } from "../utils/support-labels";

interface SupportMessageThreadProps {
  messages: SupportMessageDto[];
}

export const SupportMessageThread = ({ messages }: SupportMessageThreadProps) => (
  <ol className="space-y-3" aria-label="Trao đổi" data-testid="support-message-thread">
    {messages.map((message) => {
      const isCustomer = message.authorType === SupportMessageAuthorType.Customer;
      const isSystem = message.authorType === SupportMessageAuthorType.System;

      return (
        <li
          key={message.supportMessageId}
          className={cn("flex", isCustomer ? "justify-end" : "justify-start", isSystem && "justify-center")}
        >
          <div
            className={cn(
              "max-w-[90%] space-y-1 cut-sm px-3 py-2 sm:max-w-[80%]",
              isCustomer && "bg-primary/10",
              !isCustomer && !isSystem && "bg-card",
              isSystem && "bg-muted text-center",
            )}
          >
            <p className="text-xs font-medium text-muted-foreground">
              {getSupportAuthorLabel(message.authorType, message.authorName)}
              <span className="font-normal"> · {formatDateTime(message.createdAt)}</span>
            </p>
            <p className="text-sm whitespace-pre-wrap wrap-break-word text-foreground">{message.content}</p>
          </div>
        </li>
      );
    })}
  </ol>
);
