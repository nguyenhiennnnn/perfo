import { useEffect, useRef } from "react";
import { useSearchParams } from "react-router";
import { CheckCircle2, XCircle } from "lucide-react";

import { ButtonLink } from "@/components/common/button-link";
import { PageLoader } from "@/components/common/spinner";
import { InvalidLinkNotice } from "@/features/auth/components/invalid-link-notice";
import { useUnsubscribeMarketing } from "@/features/client/account/hooks/use-account";
import { getDisplayMessage } from "@/lib/error";
import { ROUTES } from "@/config/routes";

const UnsubscribePage = () => {
  const [searchParams] = useSearchParams();
  const token = searchParams.get("token") ?? "";
  const calledRef = useRef(false);
  const { mutate, isPending, isSuccess, isError, error } = useUnsubscribeMarketing();

  useEffect(() => {
    if (!token || calledRef.current) return;
    calledRef.current = true;
    mutate(token);
  }, [token, mutate]);

  if (!token) {
    return (
      <InvalidLinkNotice
        title="Liên kết không hợp lệ"
        description="Thiếu mã hủy đăng ký trong đường dẫn."
        actionLabel="Về trang chủ"
        actionTo={ROUTES.home}
      />
    );
  }

  if (isPending) {
    return <PageLoader variant="section" label="Đang xử lý yêu cầu..." />;
  }

  if (isSuccess) {
    return (
      <div className="flex flex-col items-center gap-3 text-center">
        <CheckCircle2 className="size-10 text-success" />
        <h1 className="t-h2 text-foreground">Đã hủy nhận tiếp thị</h1>
        <p className="text-sm text-muted-foreground">
          Bạn sẽ không còn nhận email và thông báo tiếp thị. Email về đơn hàng và tài khoản vẫn được gửi.
        </p>
        <ButtonLink to={ROUTES.home} variant="secondary">
          Về trang chủ
        </ButtonLink>
      </div>
    );
  }

  if (isError) {
    return (
      <div className="flex flex-col items-center gap-3 text-center">
        <XCircle className="size-10 text-destructive" />
        <h1 className="t-h2 text-foreground">Không thể hủy đăng ký</h1>
        <p className="text-sm text-muted-foreground">{getDisplayMessage(error)}</p>
        <ButtonLink to={ROUTES.account.profile} variant="secondary">
          Quản lý trong trang cá nhân
        </ButtonLink>
      </div>
    );
  }

  return null;
};

export default UnsubscribePage;
