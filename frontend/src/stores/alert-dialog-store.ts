import type { ReactNode } from "react";
import { create } from "zustand";
import { useShallow } from "zustand/react/shallow";
import { logDevError } from "@/lib/error";

import { FORM_CANCEL_LABEL } from "@/config/default-constants";

interface AlertDialogOptions {
  title: string;
  description?: string;
  content?: ReactNode;
  confirmText?: string;
  cancelText?: string;
  variant?: "default" | "destructive";
  confirmDisabled?: boolean;
  onConfirm: () => void | Promise<void>;
  onCancel?: () => void;
}

interface AlertDialogStore extends AlertDialogOptions {
  isOpen: boolean;
  isLoading: boolean;
  open: (options: AlertDialogOptions) => void;
  close: () => void;
  confirm: () => Promise<void>;
  setConfirmDisabled: (disabled: boolean) => void;
}

export const useAlertDialogStore = create<AlertDialogStore>((set, get) => ({
  isOpen: false,
  isLoading: false,
  title: "",
  description: undefined,
  content: undefined,
  confirmText: "Xác nhận",
  cancelText: FORM_CANCEL_LABEL,
  variant: "default",
  confirmDisabled: false,
  onConfirm: () => {},
  onCancel: undefined,
  open: ({
    title,
    description,
    content,
    confirmText = "Xác nhận",
    cancelText = FORM_CANCEL_LABEL,
    variant = "default",
    confirmDisabled = false,
    onConfirm,
    onCancel,
  }) =>
    set({
      isOpen: true,
      title,
      description,
      content,
      confirmText,
      cancelText,
      variant,
      confirmDisabled,
      onConfirm,
      onCancel,
    }),
  close: () => {
    get().onCancel?.();
    set({ isOpen: false, isLoading: false });
  },
  confirm: async () => {
    if (get().confirmDisabled) return;
    try {
      set({ isLoading: true });
      await get().onConfirm();
      set({ isOpen: false, isLoading: false });
    } catch (error) {
      logDevError(error, "AlertDialog");
      set({ isLoading: false });
    }
  },
  setConfirmDisabled: (confirmDisabled) => set({ confirmDisabled }),
}));

export const useAlertDialogOpen = () => useAlertDialogStore((s) => s.open);

export const useAlertDialogSetConfirmDisabled = () =>
  useAlertDialogStore((s) => s.setConfirmDisabled);

export const useAlertDialogLoading = () =>
  useAlertDialogStore((s) => s.isLoading);

export const useAlertDialogState = () =>
  useAlertDialogStore(
    useShallow((s) => ({
      isOpen: s.isOpen,
      isLoading: s.isLoading,
      title: s.title,
      description: s.description,
      content: s.content,
      confirmText: s.confirmText,
      cancelText: s.cancelText,
      variant: s.variant,
      confirmDisabled: s.confirmDisabled,
      close: s.close,
      confirm: s.confirm,
    })),
  );
