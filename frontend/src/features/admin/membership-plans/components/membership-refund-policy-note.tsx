import { ShieldCheck } from "lucide-react";

import { Skeleton } from "@/components/ui/skeleton";
import { useAdminSettings } from "@/features/admin/settings/hooks/use-admin-settings";

export const MembershipRefundPolicyNote = () => {
  const settings = useAdminSettings();
  const startedRefundEnabled = settings.data?.membershipRefundStartedEnabled;

  return (
    <section className="flex flex-col gap-2 cut-sm bg-muted px-3 py-3 text-sm">
      <p className="flex items-center gap-2 font-medium text-foreground">
        <ShieldCheck className="size-4 text-primary-text" />
        Chính sách hoàn gói
      </p>
      <p className="text-xs text-muted-foreground">
        Áp dụng chung cho mọi gói, cấu hình ở cài đặt rạp.
      </p>
      <ul className="list-disc space-y-1 pl-5 text-muted-foreground">
        <li>Gói chưa bắt đầu hiệu lực: khách được hoàn đủ.</li>
        {settings.isLoading ? (
          <li>
            <Skeleton className="inline-block h-4 w-56 align-middle" />
          </li>
        ) : (
          <li>
            Gói đã bắt đầu hiệu lực:{" "}
            {startedRefundEnabled === undefined
              ? "xem cài đặt rạp."
              : startedRefundEnabled
                ? "khách được tự yêu cầu hoàn phần các tháng chưa ghi nhận doanh thu."
                : "khách không được tự yêu cầu hoàn; chỉ quản trị hoàn ngoại lệ."}
          </li>
        )}
        <li>
          Hoàn gói dừng ghi nhận doanh thu các tháng còn lại và thu hồi vé miễn phí chưa dùng.
        </li>
        <li>Không hoàn được khi còn vé miễn phí đang giữ cho đơn chờ thanh toán.</li>
      </ul>
    </section>
  );
};
