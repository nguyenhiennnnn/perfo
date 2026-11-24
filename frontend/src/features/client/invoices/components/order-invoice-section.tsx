import { useState } from "react";
import { FileText, Plus } from "lucide-react";
import { QueryState } from "@/components/common/query-state";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { SubmitButton } from "@/components/common/form/submit-button";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { useDialogOpen } from "@/stores/dialog-store";
import { InvoiceRequestStatus, OrderStatus } from "@/types/enum";
import type { OrderDetailDto } from "@/features/client/orders/types";
import {
  useCreateInvoiceRequest,
  useInvoiceProfiles,
  useOrderInvoiceRequests,
} from "../hooks/use-invoices";
import type { InvoiceProfileDto } from "../types";
import { InvoiceProfileDialogContent } from "./invoice-profile-dialog-content";
import { InvoiceRequestCard } from "./invoice-request-card";

interface InvoiceRequestFormProps {
  orderId: number;
}

const InvoiceRequestForm = ({ orderId }: InvoiceRequestFormProps) => {
  const openDialog = useDialogOpen();
  const profilesQuery = useInvoiceProfiles();
  const createRequest = useCreateInvoiceRequest(orderId);
  const [selectedId, setSelectedId] = useState<number | null>(null);

  const openCreateProfile = () =>
    openDialog({
      title: "Thêm hồ sơ xuất hóa đơn",
      description: "Thông tin được lưu để dùng lại cho các đơn sau.",
      content: <InvoiceProfileDialogContent onSaved={(profile) => setSelectedId(profile.invoiceProfileId)} />,
    });

  return (
    <QueryState
      query={profilesQuery}
      skeleton={<CardSkeleton className="h-32" />}
      errorTitle="Không tải được hồ sơ xuất hóa đơn"
    >
      {(profiles: InvoiceProfileDto[]) => {
        const effectiveId =
          selectedId !== null && profiles.some((p) => p.invoiceProfileId === selectedId)
            ? selectedId
            : (profiles[0]?.invoiceProfileId ?? null);

        if (profiles.length === 0) {
          return (
            <div className="space-y-3 cut-md bg-card p-4">
              <p className="text-sm text-muted-foreground">
                Bạn chưa có hồ sơ xuất hóa đơn. Thêm mã số thuế và thông tin đơn vị để gửi yêu cầu.
              </p>
              <Button variant="outline" onClick={openCreateProfile}>
                <Plus className="size-4" />
                Thêm hồ sơ xuất hóa đơn
              </Button>
            </div>
          );
        }

        return (
          <div className="space-y-4 cut-md bg-card p-4">
            <fieldset className="space-y-2" disabled={createRequest.isPending}>
              <legend className="mb-2 text-sm font-medium text-foreground">Chọn thông tin xuất hóa đơn</legend>
              {profiles.map((profile) => {
                const selected = profile.invoiceProfileId === effectiveId;
                return (
                  <label
                    key={profile.invoiceProfileId}
                    className={cn(
                      "flex cursor-pointer items-start gap-3 cut-sm border p-3 text-sm transition-colors",
                      selected ? "border-primary bg-primary/5" : "border-border hover:bg-muted/50",
                    )}
                  >
                    <input
                      type="radio"
                      name="invoice-profile"
                      className="mt-1 accent-primary"
                      checked={selected}
                      onChange={() => setSelectedId(profile.invoiceProfileId)}
                    />
                    <span className="min-w-0">
                      <span className="block font-medium wrap-break-word text-foreground">{profile.companyName}</span>
                      <span className="t-code block text-muted-foreground">MST {profile.taxCode}</span>
                      <span className="block wrap-break-word text-muted-foreground">{profile.address}</span>
                      <span className="block break-all text-muted-foreground">{profile.email}</span>
                    </span>
                  </label>
                );
              })}
            </fieldset>

            <div className="flex flex-wrap justify-between gap-2">
              <Button variant="outline" onClick={openCreateProfile} disabled={createRequest.isPending}>
                <Plus className="size-4" />
                Thêm hồ sơ mới
              </Button>
              <SubmitButton
                label="Gửi yêu cầu xuất hóa đơn"
                pendingLabel="Đang gửi"
                isPending={createRequest.isPending}
                disabled={effectiveId === null}
                onClick={() => {
                  if (effectiveId !== null) createRequest.mutate({ invoiceProfileId: effectiveId });
                }}
              />
            </div>
          </div>
        );
      }}
    </QueryState>
  );
};

interface OrderInvoiceSectionProps {
  order: OrderDetailDto;
}

export const OrderInvoiceSection = ({ order }: OrderInvoiceSectionProps) => {
  const isEligible = order.status === OrderStatus.Confirmed;
  const requestsQuery = useOrderInvoiceRequests(order.orderId, isEligible);

  if (!isEligible) return null;

  return (
    <section aria-labelledby="order-invoice-title" className="space-y-3" data-testid="order-invoice-section">
      <div className="flex items-center gap-2">
        <FileText className="size-5 text-muted-foreground" />
        <h2 id="order-invoice-title" className="t-h2 text-foreground">
          Hóa đơn VAT
        </h2>
      </div>
      <p className="text-sm text-muted-foreground">
        Giá trên đơn đã gồm VAT. Gửi yêu cầu để rạp xuất hóa đơn cho đơn vị của bạn trong thời hạn quy định.
      </p>

      <QueryState
        query={requestsQuery}
        skeleton={<CardSkeleton className="h-32" />}
        errorTitle="Không tải được yêu cầu hóa đơn"
      >
        {(requests) => {
          const hasActive = requests.some((r) => r.status !== InvoiceRequestStatus.Rejected);
          return (
            <div className="space-y-3">
              {requests.map((request) => (
                <InvoiceRequestCard key={request.invoiceRequestId} request={request} />
              ))}
              {!hasActive && <InvoiceRequestForm orderId={order.orderId} />}
            </div>
          );
        }}
      </QueryState>
    </section>
  );
};
