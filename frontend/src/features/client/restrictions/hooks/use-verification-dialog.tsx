import { useDialogOpen } from "@/stores/dialog-store";

import { VerificationDialogContent } from "../components/verification-dialog-content";

export function useOpenVerificationDialog() {
  const openDialog = useDialogOpen();

  return () =>
    openDialog({
      title: "Xác minh tài khoản",
      description: "Bước xác minh bổ sung trước khi tiếp tục đặt vé.",
      content: <VerificationDialogContent />,
    });
}
