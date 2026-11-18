import { useEffect } from "react";
import { cn } from "@/lib/utils";
import { addDays } from "date-fns";
import { CalendarX } from "lucide-react";
import { DateStrip } from "@/components/common/date-strip";
import { EmptyState } from "@/components/common/empty-state";
import { ScheduleFilterBar } from "@/features/client/schedule/components/schedule-filter-bar";
import { ScheduleMovieRow } from "@/features/client/schedule/components/schedule-movie-row";
import { useSchedule } from "@/features/client/showtimes/hooks/use-showtimes";
import { PageContainer } from "@/components/common/page-container";
import { PageTitle } from "@/components/common/page-title";
import { QueryState } from "@/components/common/query-state";
import { SkeletonList } from "@/components/common/skeleton-list";
import { useScheduleParams } from "@/hooks/use-query-params";
import { getBusinessToday, getBusinessTodayDate } from "@/lib/business-time";
import { toDateKey } from "@/lib/cinema-format";
import { useCinemaSettings } from "@/features/client/settings/hooks/use-settings";
import { Seo } from "@/components/common/seo";
import { BRAND } from "@/config/brand";

const SchedulePage = () => {
  const [{ date, format }, setScheduleParams] = useScheduleParams();
  const { data: settings } = useCinemaSettings();
  const daysCount = settings?.showtimeLookaheadDays ?? 7;
  const today = getBusinessToday();
  const maxDate = toDateKey(addDays(getBusinessTodayDate(), daysCount - 1));
  const isOutOfRange =
    !!settings && !!date && (!/^\d{4}-\d{2}-\d{2}$/.test(date) || date < today || date > maxDate);
  const selectedDate = !date || isOutOfRange ? today : date;

  useEffect(() => {
    if (isOutOfRange) setScheduleParams({ date: null, format: null });
  }, [isOutOfRange, setScheduleParams]);

  const scheduleQuery = useSchedule(selectedDate, format);
  const { data, isPlaceholderData } = scheduleQuery;
  const movies = data?.movies ?? [];

  return (
    <PageContainer size="md">
      <Seo
        title="Lịch chiếu phim"
        description={`Tra cứu lịch chiếu toàn bộ phim tại rạp ${BRAND.name} theo từng ngày. Chọn suất chiếu yêu thích và đặt vé ngay.`}
        canonical="/schedule"
      />
      <PageTitle title="Lịch chiếu phim" description="Tra cứu lịch chiếu toàn bộ phim tại rạp theo từng ngày." />

      <DateStrip
        value={selectedDate}
        daysCount={daysCount}
        onChange={(value) => setScheduleParams({ date: value, format: null })}
      />

      <div className="mt-4">
        <ScheduleFilterBar
          formats={data?.formats ?? []}
          activeFormat={format}
          onFormatChange={(value) => setScheduleParams({ format: value })}
        />
      </div>

      <div
        aria-busy={isPlaceholderData || undefined}
        className={cn("mt-6 space-y-4", isPlaceholderData && "pointer-events-none opacity-60 transition-opacity")}
      >
        <QueryState
          query={scheduleQuery}
          skeleton={<SkeletonList count={4} className="h-40 w-full" />}
          isEmpty={movies.length === 0}
          empty={
            format ? (
              <EmptyState
                variant="filtered"
                title="Không có suất chiếu ở định dạng này"
                description="Thử chọn định dạng khác hoặc ngày khác."
                onClearFilters={() => setScheduleParams({ format: null })}
              />
            ) : (
              <EmptyState icon={CalendarX} title="Chưa có suất chiếu" description="Không có phim nào chiếu vào ngày đã chọn. Chọn ngày khác để xem tiếp." />
            )
          }
          errorTitle="Không tải được lịch chiếu"
        >
          {movies.map((movie) => (
            <ScheduleMovieRow key={movie.movieId} movie={movie} />
          ))}
        </QueryState>
      </div>
    </PageContainer>
  );
}

export default SchedulePage;