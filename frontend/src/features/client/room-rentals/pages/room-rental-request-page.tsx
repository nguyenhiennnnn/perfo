import { PageContainer } from "@/components/common/page-container";
import { PageTitle } from "@/components/common/page-title";
import { Seo } from "@/components/common/seo";
import { ROUTES } from "@/config/routes";
import { RoomRentalRequestForm } from "../components/room-rental-request-form";

const RoomRentalRequestPage = () => (
  <PageContainer size="sm">
    <Seo
      title="Thuê phòng chiếu riêng"
      description="Gửi yêu cầu thuê phòng chiếu cho doanh nghiệp, trường học hoặc nhóm bạn. Rạp phản hồi báo giá qua email."
      canonical={ROUTES.roomRental.request}
    />
    <PageTitle
      title="Thuê phòng chiếu riêng"
      description="Tổ chức sự kiện, buổi chiếu nội bộ hoặc xem phim cùng nhóm bạn. Gửi yêu cầu để nhận báo giá, không cần tài khoản."
    />
    <RoomRentalRequestForm />
  </PageContainer>
);

export default RoomRentalRequestPage;
