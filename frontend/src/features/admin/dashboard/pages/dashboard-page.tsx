import { useState } from "react";
import { PageHeader } from "@/components/common/page-header";
import { useDashboardSummary, useDashboardInsights } from "../hooks/use-dashboard";
import { DashboardStatCards } from "../components/dashboard-stat-cards";
import { RevenueTrendChart } from "../components/revenue-trend-chart";
import { TopMoviesCard } from "../components/top-movies-card";
import { TodayShowtimesCard } from "../components/today-showtimes-card";
import { DashboardDaysFilter } from "../components/dashboard-days-filter";
import { OccupancySection } from "../components/occupancy-section";
import { SeatHeatmapSection } from "../components/seat-heatmap-section";

const AdminDashboardPage = () => {
  const [days, setDays] = useState(14);

  const summary = useDashboardSummary();
  const insights = useDashboardInsights({ days });

  return (
    <div className="space-y-6">
      <PageHeader
        title="Tổng quan"
        description="Theo dõi doanh thu, vé bán và tình hình rạp theo thời gian thực."
        action={<DashboardDaysFilter value={days} onChange={setDays} />}
      />

      <DashboardStatCards query={summary} />

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
        <RevenueTrendChart query={insights} />
        <TopMoviesCard query={insights} />
      </div>

      <OccupancySection />

      <SeatHeatmapSection />

      <TodayShowtimesCard query={insights} />
    </div>
  );
};

export default AdminDashboardPage;