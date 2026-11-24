import { FileText, Pencil, Plus, Trash2 } from "lucide-react";
import { EmptyState } from "@/components/common/empty-state";
import { QueryState } from "@/components/common/query-state";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { Button } from "@/components/ui/button";
import { useAlertDialogStore } from "@/stores/alert-dialog-store";
import { useDialogOpen } from "@/stores/dialog-store";
import { useDeleteInvoiceProfile, useInvoiceProfiles } from "../hooks/use-invoices";
import type { InvoiceProfileDto } from "../types";
import { InvoiceProfileDialogContent } from "./invoice-profile-dialog-content";

export const InvoiceProfilesCard = () => {
  const query = useInvoiceProfiles();
  const openDialog = useDialogOpen();
  const openAlert = useAlertDialogStore((s) => s.open);
  const deleteProfile = useDeleteInvoiceProfile();

  const openCreate = () =>
    openDialog({
      title: "Thêm hồ sơ xuất hóa đơn",
      content: <InvoiceProfileDialogContent />,
    });

  const openEdit = (profile: InvoiceProfileDto) =>
    openDialog({
      title: "Sửa hồ sơ xuất hóa đơn",
      description: `MST ${profile.taxCode}`,
      content: <InvoiceProfileDialogContent profile={profile} />,
    });

  const confirmDelete = (profile: InvoiceProfileDto) =>
    openAlert({
      title: "Xóa hồ sơ xuất hóa đơn",
      description: `Hồ sơ ${profile.companyName} (MST ${profile.taxCode}) sẽ bị xóa. Các yêu cầu hóa đơn đã gửi không bị ảnh hưởng.`,
      confirmText: "Xóa hồ sơ",
      variant: "destructive",
      onConfirm: () => { deleteProfile.mutateAsync(profile.invoiceProfileId); },
    });

  return (
    <section className="bg-card p-5" data-testid="invoice-profiles-card">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h2 className="t-h2 text-foreground">Hồ sơ xuất hóa đơn</h2>
          <p className="mt-1 text-sm text-muted-foreground">
            Lưu thông tin đơn vị để yêu cầu hóa đơn VAT nhanh hơn ở chi tiết đơn hàng.
          </p>
        </div>
        <Button variant="outline" size="sm" onClick={openCreate}>
          <Plus className="size-4" />
          Thêm hồ sơ
        </Button>
      </div>

      <div className="mt-5">
        <QueryState
          query={query}
          skeleton={<CardSkeleton className="h-28" />}
          errorTitle="Không tải được hồ sơ xuất hóa đơn"
        >
          {(profiles) =>
            profiles.length === 0 ? (
              <EmptyState
                icon={FileText}
                title="Chưa có hồ sơ xuất hóa đơn"
                description="Thêm mã số thuế và thông tin đơn vị để dùng khi cần hóa đơn."
              />
            ) : (
              <ul className="divide-y divide-border">
                {profiles.map((profile) => (
                  <li key={profile.invoiceProfileId} className="flex items-start justify-between gap-3 py-3">
                    <div className="min-w-0 text-sm">
                      <p className="font-medium wrap-break-word text-foreground">{profile.companyName}</p>
                      <p className="t-code text-muted-foreground">MST {profile.taxCode}</p>
                      <p className="wrap-break-word text-muted-foreground">{profile.address}</p>
                      <p className="break-all text-muted-foreground">{profile.email}</p>
                    </div>
                    <div className="flex shrink-0 gap-1">
                      <Button
                        variant="ghost"
                        size="icon"
                        aria-label={`Sửa hồ sơ ${profile.companyName}`}
                        onClick={() => openEdit(profile)}
                      >
                        <Pencil className="size-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="icon"
                        aria-label={`Xóa hồ sơ ${profile.companyName}`}
                        onClick={() => confirmDelete(profile)}
                      >
                        <Trash2 className="size-4" />
                      </Button>
                    </div>
                  </li>
                ))}
              </ul>
            )
          }
        </QueryState>
      </div>
    </section>
  );
};
