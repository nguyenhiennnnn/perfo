import { cn } from "@/lib/utils";
import { formatDateTime } from "@/lib/formatters";
import { SupportMessageAuthorType } from "@/types/enum";
import type { StaffSupportMessageDto } from "../types";
import { getStaffAuthorLabel } from "../utils/support-labels";

interface StaffMessageThreadProps {
  messages: StaffSupportMessageDto[];
}

export const StaffMessageThread = ({ messages }: StaffMessageThreadProps) => (
  <ol className="space-y-3" aria-label="Trao đổi" data-testid="staff-support-message-thread">
    {messages.map((message) => {
      const isCustomer = message.authorType === SupportMessageAuthorType.Customer;
      const isSystem = message.authorType === SupportMessageAuthorType.System;

      return (
        <li
          key={message.supportMessageId}
          className={cn("flex", isCustomer ? "justify-start" : "justify-end", isSystem && "justify-center")}
        >
          <div
            className={cn(
              "max-w-[90%] space-y-1 cut-sm px-3 py-2 sm:max-w-[80%]",
              isCustomer && "bg-card",
              !isCustomer && !isSystem && "bg-primary/10",
              isSystem && "bg-muted text-center",
            )}
          >
            <p className="text-xs font-medium text-muted-foreground">
              {getStaffAuthorLabel(message.authorType, message.authorName)}
              <span className="font-normal"> · {formatDateTime(message.createdAt)}</span>
            </p>
            <p className="text-sm whitespace-pre-wrap wrap-break-word text-foreground">{message.content}</p>
          </div>
        </li>
      );
    })}
  </ol>
);
