import { PageHeader } from "@/components/common/page-header";
import { ScheduleReportSection } from "../components/schedule-report-section";
import { ScheduleSuggestionsSection } from "../components/schedule-suggestions-section";

const AdminScheduleAnalysisPage = () => (
  <div className="space-y-6">
    <PageHeader
      title="Phân tích lịch chiếu"
      description="Xem hiệu quả từng phim, phòng, khung giờ và nhận gợi ý tối ưu lịch chiếu."
    />
    <ScheduleReportSection />
    <ScheduleSuggestionsSection />
  </div>
);

export default AdminScheduleAnalysisPage;
