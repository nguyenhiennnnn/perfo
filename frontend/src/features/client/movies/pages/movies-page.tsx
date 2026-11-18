import { useClientMovieParams } from "@/hooks/use-query-params";
import { MoviesFilterBar } from "@/features/client/movies/components/movies-filter-bar";
import { MoviesGrid, MoviesGridSkeleton } from "@/features/client/movies/components/movies-grid";
import { useMovies } from "@/features/client/movies/hooks/use-movies";
import { useDebouncedValue } from "@/hooks/use-debounced-value";
import type { MovieStatus } from "@/types/enum";
import { Seo } from "@/components/common/seo";
import { PageContainer } from "@/components/common/page-container";
import { PageTitle } from "@/components/common/page-title";
import { QueryState } from "@/components/common/query-state";
import { BRAND } from "@/config/brand";
import { ROUTES } from "@/config/routes";

const MoviesPage = () => {
  const [params, setParams] = useClientMovieParams();
  const debouncedSearch = useDebouncedValue(params.search);

  const moviesQuery = useMovies({
    search: debouncedSearch || undefined,
    status: (params.status as MovieStatus) || undefined,
    genreId: params.genreId ?? undefined,
    languageId: params.languageId ?? undefined,
    ageRatingId: params.ageRatingId ?? undefined,
    limit: params.limit,
  });
  const { data, isFetchingNextPage, hasNextPage, fetchNextPage } = moviesQuery;
  const hasActiveFilters = Boolean(
    params.search || params.status || params.genreId || params.languageId || params.ageRatingId,
  );

  return (
    <PageContainer size="xl">
      <Seo
        title="Danh sách phim"
        description={`Xem tất cả phim đang và sắp chiếu tại ${BRAND.name}. Lọc theo thể loại, ngôn ngữ và nhiều tiêu chí khác.`}
        canonical={ROUTES.movies.list}
      />
      <PageTitle title="Danh sách phim" description="Phim đang chiếu và sắp chiếu tại rạp." />

      <MoviesFilterBar params={params} setParams={setParams} />

      <div className="mt-6">
        <QueryState
          query={moviesQuery}
          skeleton={<MoviesGridSkeleton />}
          errorTitle="Không tải được danh sách phim"
        >
          <MoviesGrid
            movies={data?.pages ?? []}
            hasNextPage={hasNextPage}
            isFetchingNextPage={isFetchingNextPage}
            fetchNextPage={fetchNextPage}
            hasActiveFilters={hasActiveFilters}
            onResetFilters={() => setParams({ search: "", status: null, genreId: null, languageId: null, ageRatingId: null, cursor: null })}
          />
        </QueryState>
      </div>
    </PageContainer>
  );
}

export default MoviesPage;