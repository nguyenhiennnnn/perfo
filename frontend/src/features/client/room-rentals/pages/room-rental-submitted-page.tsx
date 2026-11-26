import { CheckCircle2, FileText } from "lucide-react";
import { useSearchParams } from "react-router";
import { ButtonLink } from "@/components/common/button-link";
import { CopyButton } from "@/components/common/copy-button";
import { EmptyState } from "@/components/common/empty-state";
import { PageContainer } from "@/components/common/page-container";
import { Seo } from "@/components/common/seo";
import { ROUTES } from "@/config/routes";

const NEXT_STEPS = [
  "Rạp kiểm tra yêu cầu và liên hệ qua số điện thoại hoặc email bạn đã cung cấp.",
  "Báo giá chi tiết được gửi qua email, kèm đường dẫn riêng để bạn xem và phản hồi.",
  "Sau khi bạn chấp nhận báo giá và đặt cọc, rạp giữ phòng cho buổi chiếu của bạn.",
];

const RoomRentalSubmittedPage = () => {
  const [searchParams] = useSearchParams();
  const requestCode = searchParams.get("code")?.trim() ?? "";

  if (!requestCode) {
    return (
      <PageContainer size="sm" className="py-12">
        <Seo title="Thuê phòng chiếu riêng" noIndex />
        <EmptyState
          icon={FileText}
          title="Chưa có yêu cầu nào được gửi"
          description="Điền biểu mẫu để gửi yêu cầu thuê phòng chiếu riêng."
          action={<ButtonLink to={ROUTES.roomRental.request}>Gửi yêu cầu thuê phòng</ButtonLink>}
        />
      </PageContainer>
    );
  }

  return (
    <PageContainer size="sm" className="flex max-w-2xl flex-col gap-6 py-12">
      <Seo title="Đã gửi yêu cầu thuê phòng" noIndex />

      <div className="flex flex-col items-center gap-3 text-center">
        <CheckCircle2 className="size-12 text-primary" />
        <h1 className="t-h1 text-foreground">Đã gửi yêu cầu thuê phòng</h1>
        <p className="text-sm text-muted-foreground">
          Cảm ơn bạn. Rạp đã nhận yêu cầu và sẽ phản hồi trong thời gian sớm nhất.
        </p>
      </div>

      <div className="cut-md flex items-center justify-between gap-3 bg-card px-4 py-4">
        <div className="min-w-0">
          <p className="text-xs text-muted-foreground">Mã yêu cầu</p>
          <p className="tabular truncate font-mono text-lg font-semibold text-foreground">
            {requestCode}
          </p>
        </div>
        <CopyButton value={requestCode} label="mã yêu cầu" />
      </div>

      <div className="cut-md bg-card px-4 py-4">
        <h2 className="t-h3 mb-3 text-foreground">Các bước tiếp theo</h2>
        <ol className="space-y-3">
          {NEXT_STEPS.map((step, index) => (
            <li key={step} className="flex gap-3 text-sm text-foreground">
              <span className="tabular flex size-6 shrink-0 items-center justify-center rounded-full bg-primary/10 text-xs font-semibold text-primary">
                {index + 1}
              </span>
              <span className="leading-relaxed">{step}</span>
            </li>
          ))}
        </ol>
      </div>

      <div className="flex flex-col gap-3 sm:flex-row sm:justify-center">
        <ButtonLink to={ROUTES.home} className="w-full sm:w-auto">
          Về trang chủ
        </ButtonLink>
        <ButtonLink to={ROUTES.roomRental.request} variant="outline" className="w-full sm:w-auto">
          Gửi yêu cầu khác
        </ButtonLink>
      </div>
    </PageContainer>
  );
};

export default RoomRentalSubmittedPage;
