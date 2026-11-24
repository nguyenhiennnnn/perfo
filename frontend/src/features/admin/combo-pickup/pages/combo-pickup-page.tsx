import { PackageOpen } from "lucide-react";
import { PageHeader } from "@/components/common/page-header";
import { EmptyState } from "@/components/common/empty-state";
import { Card } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { ComboPickupLookupPanel } from "../components/combo-pickup-lookup-panel";
import { ComboPickupOrderCard } from "../components/combo-pickup-order-card";
import { useComboPickupFlow } from "../hooks/use-combo-pickup-flow";

const AdminComboPickupPage = () => {
  const flow = useComboPickupFlow();

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="Nhận combo"
        description="Quét QR hoặc nhập mã đơn để giao combo cho khách, có thể giao từng phần."
      />

      <div className="grid grid-cols-1 gap-6 md:grid-cols-5">
        <div className="md:col-span-2">
          <ComboPickupLookupPanel flow={flow} />
        </div>

        <div className="md:col-span-3">
          {flow.isLooking && !flow.order ? (
            <Card className="gap-3 p-5" aria-busy="true">
              <Skeleton className="h-6 w-40" />
              <Skeleton className="h-16 w-full" />
              <Skeleton className="h-16 w-full" />
            </Card>
          ) : flow.order ? (
            <ComboPickupOrderCard flow={flow} order={flow.order} />
          ) : (
            <Card className="p-5">
              <EmptyState
                icon={PackageOpen}
                title="Chưa chọn đơn"
                description="Quét QR hoặc nhập mã đơn để xem combo khách cần nhận."
                className="py-8"
              />
            </Card>
          )}
        </div>
      </div>
    </div>
  );
};

export default AdminComboPickupPage;
