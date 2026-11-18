import { useEffect, useRef, useState } from "react";
import { Check, Copy } from "lucide-react";
import { Button } from "@/components/ui/button";
import { toast } from "@/components/ui/toast";
import { COPY_FEEDBACK_MS } from "@/config/default-constants";

interface CopyButtonProps {
  value: string;
  label: string;
  className?: string;
}

export const CopyButton = ({ value, label, className }: CopyButtonProps) => {
  const [copied, setCopied] = useState(false);
  const timerRef = useRef<number | undefined>(undefined);

  useEffect(() => () => window.clearTimeout(timerRef.current), []);

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(value);
    } catch {
      toast.add({ title: `Không sao chép được ${label}.`, type: "error" });
      return;
    }
    setCopied(true);
    toast.add({ title: `Đã sao chép ${label}.`, type: "success" });
    window.clearTimeout(timerRef.current);
    timerRef.current = window.setTimeout(() => setCopied(false), COPY_FEEDBACK_MS);
  };

  return (
    <Button
      variant="ghost"
      size="icon"
      aria-label={`Sao chép ${label}`}
      className={className}
      onClick={handleCopy}
    >
      {copied ? <Check className="text-success" /> : <Copy />}
    </Button>
  );
};
