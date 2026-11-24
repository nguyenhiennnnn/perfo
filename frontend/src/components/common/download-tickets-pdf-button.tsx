import { useMutation } from "@tanstack/react-query";
import { FileDown } from "lucide-react";
import { apiClient } from "@/api/axios";
import { Spinner } from "@/components/common/spinner";
import { Button } from "@/components/ui/button";
import { EXPORT_TIMEOUT_MS } from "@/config/default-constants";
import {
  getFilenameFromContentDisposition,
  normalizeBlobError,
  saveBlob,
} from "@/lib/download";
import { cn } from "@/lib/utils";

interface DownloadTicketsPdfButtonProps {
  endpoint: string;
  fallbackFilename: string;
  label?: string;
  errorTitle?: string;
  disabled?: boolean;
  size?: "sm" | "default";
  className?: string;
}

export const DownloadTicketsPdfButton = ({
  endpoint,
  fallbackFilename,
  label = "Tải vé PDF",
  errorTitle = "Không thể tải vé PDF",
  disabled = false,
  size = "sm",
  className,
}: DownloadTicketsPdfButtonProps) => {
  const { mutate, isPending } = useMutation({
    meta: { errorTitle, skipSuccessToast: true },
    mutationFn: async () => {
      try {
        const response = await apiClient.get<Blob>(endpoint, {
          responseType: "blob",
          timeout: EXPORT_TIMEOUT_MS,
        });
        const filename =
          getFilenameFromContentDisposition(
            response.headers["content-disposition"],
          ) ?? fallbackFilename;
        saveBlob(response.data, filename);
      } catch (error) {
        throw await normalizeBlobError(error);
      }
    },
  });

  return (
    <Button
      variant="outline"
      size={size}
      disabled={disabled || isPending}
      className={cn("gap-2", className)}
      onClick={() => mutate()}
    >
      {isPending ? <Spinner size="sm" decorative /> : <FileDown />}
      <span>{isPending ? "Đang tải" : label}</span>
    </Button>
  );
};
