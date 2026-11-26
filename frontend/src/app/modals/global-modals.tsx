import { CustomerGateHost } from "@/features/client/restrictions/components/customer-gate-host";
import { GlobalAlertDialog } from "./global-alert-dialog";
import { GlobalDialog } from "./global-dialog";
import { GlobalSheet } from "./global-sheet";

export const GlobalModals = () => {
  return (
    <>
      <GlobalDialog />
      <GlobalAlertDialog />
      <GlobalSheet />
      <CustomerGateHost />
    </>
  );
};
