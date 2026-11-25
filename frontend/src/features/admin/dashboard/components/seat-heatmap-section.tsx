import { RotateCcw, Armchair } from "lucide-react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { DateFilterPicker } from "@/components/common/filter/date-filter-picker";
import { EnumSelectFilter } from "@/components/common/filter/enum-select-filter";
import { SelectField } from "@/components/common/select-field";
import { QueryState } from "@/components/common/query-state";
import { EmptyState } from "@/components/common/empty-state";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { LOOKUP_PAGE_SIZE } from "@/config/default-constants";
import { cn } from "@/lib/utils";
import { useAdminDashboardSeatHeatmapParams, type SeatHeatmapMode } from "@/hooks/use-query-params";
import { useAdminRooms } from "@/features/admin/rooms/hooks/use-admin-rooms";
import { useAdminMovies } from "@/features/admin/movies/hooks/use-admin-movies";
import { useAdminGenres } from "@/features/admin/catalog/hooks/use-admin-genre";
import { getDefaultReportRange } from "@/features/admin/reports/utils/default-range";
import { useDashboardSeatHeatmap } from "../hooks/use-dashboard";
import { SEAT_HEATMAP_MODE_OPTIONS } from "../utils/seat-heatmap";
import { SeatHeatmapMap } from "./seat-heatmap-map";
import { FilterBar } from "@/components/common/filter/filter-bar";

const DEFAULT_RANGE_DAYS = 14;
const MAP_SKELETON_CLASS = "h-96";

export const SeatHeatmapSection = () => {
  const [params, setParams] = useAdminDashboardSeatHeatmapParams();

  const rooms = useAdminRooms({ pageSize: LOOKUP_PAGE_SIZE });
  const movies = useAdminMovies({ pageSize: LOOKUP_PAGE_SIZE });
  const genres = useAdminGenres({ pageSize: LOOKUP_PAGE_SIZE });

  const roomItems = rooms.data?.items ?? [];
  const roomId = params.heatRoom ?? roomItems.find((r) => r.isActive)?.roomId ?? roomItems[0]?.roomId ?? null;
  const room = roomItems.find((r) => r.roomId === roomId);

  const defaultRange = getDefaultReportRange(DEFAULT_RANGE_DAYS);
  const from = params.heatFrom ?? defaultRange.fromDate;
  const to = params.heatTo ?? defaultRange.toDate;

  const query = useDashboardSeatHeatmap(
    roomId == null
      ? null
      : {
          roomId,
          from,
          to,
          movieId: params.heatMovie ?? undefined,
          genreId: params.heatGenre ?? undefined,
        },
  );

  const movieOptions = (movies.data?.items ?? []).map((m) => ({
    value: String(m.movieId),
    label: m.title,
  }));
  const genreOptions = (genres.data?.items ?? []).map((g) => ({
    value: String(g.genreId),
    label: g.name,
  }));

  const toNumber = (value: string | null) => (value == null ? null : Number(value));

  const handleReset = () =>
    void setParams({
      heatRoom: null,
      heatFrom: null,
      heatTo: null,
      heatMovie: null,
      heatGenre: null,
      heatMode: null,
    });

  return (
    <Card>
      <CardHeader>
        <CardTitle>Bản đồ nhiệt ghế</CardTitle>
        <CardDescription>
          Mức độ được chọn của từng ghế theo đúng sơ đồ phòng, không tính suất riêng và sự kiện.
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        <FilterBar>
          <SelectField
            className="sm:w-48"
            value={roomId}
            onChange={(value) => void setParams({ heatRoom: Number(value) })}
            options={roomItems}
            getOptionValue={(r) => r.roomId}
            getOptionLabel={(r) => r.roomName}
            placeholder="Chọn phòng"
          />
          <DateFilterPicker
            value={from}
            onChange={(value) => void setParams({ heatFrom: value })}
            placeholder="Từ ngày"
          />
          <DateFilterPicker
            value={to}
            onChange={(value) => void setParams({ heatTo: value })}
            placeholder="Đến ngày"
          />
          <EnumSelectFilter
            value={params.heatMovie == null ? null : String(params.heatMovie)}
            onChange={(value) => void setParams({ heatMovie: toNumber(value) })}
            options={movieOptions}
            placeholder="Phim"
            allLabel="Tất cả phim"
          />
          <EnumSelectFilter
            value={params.heatGenre == null ? null : String(params.heatGenre)}
            onChange={(value) => void setParams({ heatGenre: toNumber(value) })}
            options={genreOptions}
            placeholder="Thể loại"
            allLabel="Tất cả thể loại"
          />
          <Button variant="outline" onClick={handleReset} className="h-10">
            <RotateCcw className="size-4" />
            <span>Đặt lại</span>
          </Button>
        </FilterBar>

        <Tabs
          value={params.heatMode}
          onValueChange={(value) => void setParams({ heatMode: value as SeatHeatmapMode })}
        >
          <TabsList>
            {SEAT_HEATMAP_MODE_OPTIONS.map((option) => (
              <TabsTrigger key={option.value} value={option.value}>
                {option.label}
              </TabsTrigger>
            ))}
          </TabsList>
        </Tabs>

        {rooms.isLoading ? (
          <CardSkeleton className={MAP_SKELETON_CLASS} />
        ) : roomId == null ? (
          <EmptyState
            icon={Armchair}
            title="Chưa có phòng chiếu"
            description="Thêm phòng chiếu để xem bản đồ nhiệt ghế."
          />
        ) : (
          <QueryState
            query={query}
            skeleton={<CardSkeleton className={MAP_SKELETON_CLASS} />}
            isEmpty={(data) => data.seats.length === 0}
            empty={
              <EmptyState
                icon={Armchair}
                title="Phòng chưa có ghế"
                description="Phòng này chưa có sơ đồ ghế để hiển thị."
              />
            }
            errorTitle="Không tải được bản đồ nhiệt ghế"
          >
            {(data) => (
              <div className={cn("transition-opacity", query.isPlaceholderData && "opacity-60")}>
                <SeatHeatmapMap
                  data={data}
                  fallbackRows={room?.rows ?? 0}
                  fallbackCols={room?.cols ?? 0}
                  mode={params.heatMode}
                />
              </div>
            )}
          </QueryState>
        )}
      </CardContent>
    </Card>
  );
};
