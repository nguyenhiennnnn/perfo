import { create } from "zustand";
import type { CustomerRestrictionNotice } from "@/lib/error";

interface CustomerGateStore {
  restriction: CustomerRestrictionNotice | null;
  restrictionPromptSeq: number;
  verificationPromptSeq: number;
  reportRestriction: (notice: CustomerRestrictionNotice) => void;
  clearRestriction: () => void;
  requestVerification: () => void;
}

export const useCustomerGateStore = create<CustomerGateStore>((set) => ({
  restriction: null,
  restrictionPromptSeq: 0,
  verificationPromptSeq: 0,
  reportRestriction: (notice) =>
    set((s) => ({ restriction: notice, restrictionPromptSeq: s.restrictionPromptSeq + 1 })),
  clearRestriction: () => set({ restriction: null }),
  requestVerification: () => set((s) => ({ verificationPromptSeq: s.verificationPromptSeq + 1 })),
}));
