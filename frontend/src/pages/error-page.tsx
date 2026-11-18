import { Home, RefreshCw, TriangleAlert } from "lucide-react";
import { StatusPage } from "@/components/common/status-page";

const ErrorPage = () => (
  <StatusPage
    code="500"
    icon={TriangleAlert}
    title="Trang không hiển thị được"
    description="Trang gặp lỗi khi hiển thị. Hãy tải lại trang, nếu vẫn lỗi thì quay về trang chủ."
    actions={[
      { label: "Tải lại trang", icon: RefreshCw, onClick: () => window.location.reload() },
      { label: "Về trang chủ", to: "/", icon: Home, variant: "outline" },
    ]}
  />
);

export default ErrorPage;
