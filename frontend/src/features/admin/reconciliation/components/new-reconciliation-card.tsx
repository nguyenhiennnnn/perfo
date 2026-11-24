import { useState } from "react";
import { FileUp, Play } from "lucide-react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { toast } from "@/components/ui/toast";
import { DateFilterPicker } from "@/components/common/filter/date-filter-picker";
import { ImportWizard } from "@/components/common/import-wizard/import-wizard";
import { useDialogClose, useDialogOpen } from "@/stores/dialog-store";
import { getBusinessTodayDate } from "@/lib/business-time";
import { toDateInputValue } from "@/lib/formatters";
import { subDays } from "date-fns";
import { useReconcileStripe } from "../hooks/use-reconciliation";
import {
  GATEWAY_OPTIONS,
  createStatementImportConfig,
  getGatewayLabel,
} from "../utils/reconciliation-labels";

export const NewReconciliationCard = () => {
  const [gateway, setGateway] = useState<string>("VNPay");
  const today = getBusinessTodayDate();
  const [from, setFrom] = useState<string | null>(toDateInputValue(subDays(today, 6)));
  const [to, setTo] = useState<string | null>(toDateInputValue(today));

  const openDialog = useDialogOpen();
  const closeDialog = useDialogClose();
  const stripe = useReconcileStripe();

  const isStripe = gateway === "Stripe";
  const validRange = Boolean(from && to && from <= to);

  const handleUpload = () =>
    openDialog({
      title: `Tải sao kê ${getGatewayLabel(gateway)}`,
      description: "Chọn file sao kê, xem trước rồi xác nhận để tạo phiên đối soát.",
      size: "xl",
      className: "sm:max-w-5xl",
      content: <ImportWizard config={createStatementImportConfig(gateway)} onClose={closeDialog} />,
    });

  const handleStripe = () => {
    if (!from || !to) return;
    stripe.mutate(
      { from, to },
      {
        onSuccess: () =>
          toast.add({
            title: "Đã bắt đầu đối soát Stripe.",
            description: "Phiên sẽ xuất hiện trong danh sách khi hoàn tất.",
            type: "success",
          }),
      },
    );
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>Tạo phiên đối soát</CardTitle>
        <CardDescription>
          VNPay và MoMo đối soát từ file sao kê; Stripe đối soát trực tiếp theo khoảng ngày.
        </CardDescription>
      </CardHeader>
      <CardContent className="flex flex-wrap items-center gap-3">
        <Tabs value={gateway} onValueChange={setGateway}>
          <TabsList>
            {GATEWAY_OPTIONS.map((o) => (
              <TabsTrigger key={o.value} value={o.value}>
                {o.label}
              </TabsTrigger>
            ))}
          </TabsList>
        </Tabs>

        {isStripe ? (
          <>
            <DateFilterPicker value={from} onChange={setFrom} placeholder="Từ ngày" />
            <DateFilterPicker value={to} onChange={setTo} placeholder="Đến ngày" />
            <Button onClick={handleStripe} disabled={!validRange || stripe.isPending}>
              <Play className="size-4" />
              <span>{stripe.isPending ? "Đang gửi..." : "Chạy đối soát"}</span>
            </Button>
          </>
        ) : (
          <Button onClick={handleUpload}>
            <FileUp className="size-4" />
            <span>Tải file sao kê</span>
          </Button>
        )}
      </CardContent>
    </Card>
  );
};
