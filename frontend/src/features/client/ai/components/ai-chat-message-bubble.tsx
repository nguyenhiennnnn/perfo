import { Bot, User } from "lucide-react";
import { cn } from "@/lib/utils";
import { renderMarkdownLite } from "@/lib/markdown-lite";

interface AiChatMessageBubbleProps {
  role: "user" | "assistant";
  content: string;
}

export const AiChatMessageBubble = ({ role, content }: AiChatMessageBubbleProps) => {
  const isUser = role === "user";

  return (
    <div className={cn("flex items-end gap-2", isUser && "flex-row-reverse")}>
      <div
        className={cn(
          "flex size-7 shrink-0 items-center justify-center",
          isUser
            ? "bg-secondary text-secondary-foreground"
            : "bg-primary/15 text-primary",
        )}
      >
        {isUser ? <User className="size-3.5" /> : <Bot className="size-3.5" />}
      </div>
      <div
        className={cn(
          "max-w-[78%] px-3.5 py-2.5 text-sm leading-relaxed",
          isUser
            ? "bg-primary text-primary-foreground"
            : "bg-secondary text-foreground",
        )}
      >
        {isUser ? content : renderMarkdownLite(content)}
      </div>
    </div>
  );
};