import {
  AlertDialog,
  AlertDialogContent,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogCancel,
  AlertDialogAction,
} from "@/components/ui/alert-dialog";
import { useAlertDialogState } from "@/stores/alert-dialog-store";
import { FORM_PROCESSING_LABEL } from "@/config/default-constants";

export const GlobalAlertDialog = () => {
  const {
    isOpen,
    isLoading,
    title,
    description,
    confirmText,
    cancelText,
    variant,
    close,
    confirm,
  } = useAlertDialogState();

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && !isLoading && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>{title}</AlertDialogTitle>
          {description && (
            <AlertDialogDescription>{description}</AlertDialogDescription>
          )}
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={isLoading} onClick={close}>
            {cancelText}
          </AlertDialogCancel>
          <AlertDialogAction
            disabled={isLoading}
            onClick={(e) => {
              e.preventDefault();
              confirm();
            }}
            className={
              variant === "destructive"
                ? "bg-destructive text-destructive-foreground hover:bg-destructive/90"
                : undefined
            }
          >
            {isLoading ? FORM_PROCESSING_LABEL : confirmText}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
};
