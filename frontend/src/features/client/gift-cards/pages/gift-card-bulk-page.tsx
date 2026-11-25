import { useMemo, useRef } from "react";
import { useNavigate } from "react-router";
import { Gift } from "lucide-react";
import { ButtonLink } from "@/components/common/button-link";
import { EmptyState } from "@/components/common/empty-state";
import { ImportWizard } from "@/components/common/import-wizard/import-wizard";
import { PageContainer } from "@/components/common/page-container";
import { PageTitle } from "@/components/common/page-title";
import { Seo } from "@/components/common/seo";
import { ROUTES } from "@/config/routes";
import { useIsAuthenticated } from "@/stores/auth-store";
import { createGiftCardBulkImportConfig } from "../config/gift-card-bulk-import-config";

const GiftCardBulkPage = () => {
  const navigate = useNavigate();
  const isAuthenticated = useIsAuthenticated();
  const createdOrderIdRef = useRef<number | null>(null);

  const config = useMemo(
    () =>
      createGiftCardBulkImportConfig((order) => {
        createdOrderIdRef.current = order.orderId;
      }),
    [],
  );

  const handleCompleted = () => {
    const orderId = createdOrderIdRef.current;
    if (orderId === null) return;
    navigate(ROUTES.giftCardResume(orderId));
  };

  return (
    <PageContainer size="md">
      <Seo
        title="Quà tặng doanh nghiệp"
        description="Mua nhiều thẻ quà tặng một lần bằng cách tải lên danh sách người nhận."
        canonical={ROUTES.giftCardBulk}
      />
      <PageTitle
        title="Quà tặng doanh nghiệp"
        description="Mua nhiều thẻ quà cho nhân viên hoặc đối tác trong một đơn. Từng thẻ được gửi khi đơn thanh toán xong."
        action={
          <ButtonLink to={ROUTES.giftCards} variant="outline">
            Mua một thẻ
          </ButtonLink>
        }
      />

      {isAuthenticated ? (
        <div className="cut-md bg-card p-4 sm:p-6">
          <ImportWizard
            config={config}
            onClose={() => navigate(ROUTES.giftCards)}
            onCompleted={handleCompleted}
          />
        </div>
      ) : (
        <EmptyState
          icon={Gift}
          title="Đăng nhập để mua quà doanh nghiệp"
          description="Bạn cần đăng nhập để tải danh sách người nhận và tạo đơn."
          action={
            <ButtonLink to={`${ROUTES.auth.login}?redirect=${encodeURIComponent(ROUTES.giftCardBulk)}`}>
              Đăng nhập
            </ButtonLink>
          }
        />
      )}
    </PageContainer>
  );
};

export default GiftCardBulkPage;
