import { useParams } from "react-router";
import { ArrowLeft } from "lucide-react";

import { PageHeader } from "@/components/common/page-header";
import { ButtonLink } from "@/components/common/button-link";
import { ROUTES } from "@/config/routes";

const AdminOrderDetailPage = () => {
  const { id } = useParams();

  return (
    <div>
      <PageHeader
        title={`Đơn hàng #${id}`}
        description="Trang chi tiết đơn hàng đang được hoàn thiện."
        action={
          <ButtonLink to={ROUTES.admin.orders} variant="outline">
            <ArrowLeft /> Quay lại danh sách
          </ButtonLink>
        }
      />
    </div>
  );
};

export default AdminOrderDetailPage;
