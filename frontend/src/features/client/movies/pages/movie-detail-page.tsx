import { Film } from "lucide-react";
import { useParams } from "react-router";
import { MovieHero } from "@/features/client/movies/components/movie-hero";
import { MovieCastCrew } from "@/features/client/movies/components/movie-cast-crew";
import { MovieShowtimeSelector } from "@/features/client/movies/components/movie-showtime-selector";
import { MovieReviews } from "@/features/client/movies/components/movie-reviews";
import { ForYouSection } from "@/features/client/movies/components/for-you-section";
import { ExpandableText } from "@/components/common/expandable-text";
import { useMovieDetail } from "@/features/client/movies/hooks/use-movies";
import type { MovieDetailDto } from "@/features/client/movies/types";
import { CardSkeleton, LineSkeleton } from "@/components/common/skeleton-shapes";
import { NotFoundState } from "@/components/common/not-found-state";
import { QueryState } from "@/components/common/query-state";
import { PageContainer } from "@/components/common/page-container";
import { Seo } from "@/components/common/seo";
import { BRAND } from "@/config/brand";
import { ROUTES } from "@/config/routes";
import { SEO_DESCRIPTION_MAX_LENGTH } from "@/config/default-constants";

const MOVIE_DETAIL_GRID_CLASS = "grid gap-10 lg:grid-cols-[1fr_22.5rem]";

const MovieDetailSkeleton = () => (
  <div className="space-y-10" aria-busy="true">
    <CardSkeleton className="h-150 sm:h-100" />
    <div className={MOVIE_DETAIL_GRID_CLASS}>
      <div className="space-y-8">
        <div className="space-y-3">
          <LineSkeleton className="h-7 w-1/3" />
          <CardSkeleton className="h-40" />
        </div>
        <CardSkeleton className="h-48" />
      </div>
      <CardSkeleton className="h-80" />
    </div>
  </div>
);

const MovieDetailContent = ({ movie }: { movie: MovieDetailDto }) => {
  const movieStructuredData = {
    "@context": "https://schema.org",
    "@type": "Movie",
    name: movie.title,
    description: movie.description,
    image: movie.posterUrl,
    director: movie.director
      ? { "@type": "Person", name: movie.director.fullName }
      : undefined,
    actor: movie.cast.map((member) => ({
      "@type": "Person",
      name: member.fullName,
      ...(member.characterName ? { characterName: member.characterName } : {}),
    })),
    genre: movie.genres.map((g) => g.name),
    duration: movie.durationMinutes
      ? `PT${movie.durationMinutes}M`
      : undefined,
  };

  const seoDescription = movie.description
    ? movie.description.slice(0, SEO_DESCRIPTION_MAX_LENGTH)
    : `Xem thông tin và đặt vé phim ${movie.title} tại ${BRAND.name}. Chọn suất chiếu, ghế ngồi và thanh toán online nhanh chóng.`;

  return (
    <div className="space-y-10">
      <Seo
        title={movie.title}
        description={seoDescription}
        canonical={ROUTES.movies.detail(movie.movieId)}
        ogImage={movie.posterUrl ?? undefined}
        ogType="article"
        structuredData={movieStructuredData}
      />

      <MovieHero movie={movie} />

      <div className={MOVIE_DETAIL_GRID_CLASS}>
        <div className="space-y-8">
          {movie.description && (
            <div>
              <h2 className="t-h2 text-foreground">Nội dung phim</h2>
              <ExpandableText text={movie.description} className="mt-3" />
            </div>
          )}
          <MovieCastCrew director={movie.director} cast={movie.cast} />
        </div>
        <aside className="lg:sticky lg:top-24 lg:max-h-[calc(100dvh-8rem)] lg:self-start lg:overflow-y-auto">
          <MovieShowtimeSelector showtimes={movie.upcomingShowtimes} />
        </aside>
      </div>

      <ForYouSection excludeMovieId={movie.movieId} />

      <MovieReviews movieId={movie.movieId} />
    </div>
  );
};

const MovieDetailPage = () => {
  const { id } = useParams<{ id: string }>();
  const movieQuery = useMovieDetail(Number(id));

  return (
    <PageContainer size="lg">
      <QueryState
        query={movieQuery}
        skeleton={<MovieDetailSkeleton />}
        notFound={
          <NotFoundState
            entity="phim"
            icon={Film}
            backLabel="Xem danh sách phim"
            backTo={ROUTES.movies.list}
          />
        }
        errorTitle="Không tải được phim này"
      >
        {(movie) => <MovieDetailContent movie={movie} />}
      </QueryState>
    </PageContainer>
  );
};

export default MovieDetailPage