import { useEffect, useRef } from "react";

import { useCustomerGateStore } from "@/stores/customer-gate-store";
import { useDialogOpen } from "@/stores/dialog-store";
import { useIsAuthenticated } from "@/stores/auth-store";

import { useOpenVerificationDialog } from "../hooks/use-verification-dialog";
import { RestrictionDialogContent } from "./restriction-dialog-content";

export const CustomerGateHost = () => {
  const openDialog = useDialogOpen();
  const openVerificationDialog = useOpenVerificationDialog();
  const isAuthenticated = useIsAuthenticated();
  const restrictionSeq = useCustomerGateStore((s) => s.restrictionPromptSeq);
  const verificationSeq = useCustomerGateStore((s) => s.verificationPromptSeq);
  const clearRestriction = useCustomerGateStore((s) => s.clearRestriction);
  const handledRestrictionSeq = useRef(restrictionSeq);
  const handledVerificationSeq = useRef(verificationSeq);

  useEffect(() => {
    if (!isAuthenticated) clearRestriction();
  }, [isAuthenticated, clearRestriction]);

  useEffect(() => {
    if (restrictionSeq === handledRestrictionSeq.current) return;
    handledRestrictionSeq.current = restrictionSeq;
    openDialog({
      title: "Tài khoản đang bị hạn chế giữ ghế",
      content: <RestrictionDialogContent />,
    });
  }, [restrictionSeq, openDialog]);

  useEffect(() => {
    if (verificationSeq === handledVerificationSeq.current) return;
    handledVerificationSeq.current = verificationSeq;
    openVerificationDialog();
  }, [verificationSeq, openVerificationDialog]);

  return null;
};
