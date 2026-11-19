import { Star } from "lucide-react";
import { EmptyState } from "@/components/common/empty-state";
import { PageTitle } from "@/components/common/page-title";
import { QueryState } from "@/components/common/query-state";
import { ButtonLink } from "@/components/common/button-link";
import { SkeletonList } from "@/components/common/skeleton-list";
import { InfiniteScrollTrigger } from "@/components/common/infinite-scroll-trigger";
import { AccountReviewCard } from "@/features/client/account/components/account-review-card";
import { useMyReviews } from "@/features/client/account/hooks/use-account";
import { ACCOUNT_LIST_PAGE_LIMIT, SKELETON_LIST_COUNT } from "@/config/default-constants";
import { ROUTES } from "@/config/routes";

const AccountReviewsPage = () => {
  const reviewsQuery = useMyReviews({ limit: ACCOUNT_LIST_PAGE_LIMIT });
  const { data, hasNextPage, isFetchingNextPage, fetchNextPage } = reviewsQuery;
  const reviews = data?.reviews ?? [];

  return (
    <div>
      <PageTitle title="Đánh giá của tôi" />

      <div className="space-y-3" aria-busy={reviewsQuery.isLoading || undefined}>
        <QueryState
          query={reviewsQuery}
          skeleton={<SkeletonList count={SKELETON_LIST_COUNT} />}
          isEmpty={reviews.length === 0}
          empty={
            <EmptyState
              icon={Star}
              title="Bạn chưa có đánh giá nào"
              description="Đánh giá phim sau khi xem để nó hiện ở đây."
              action={<ButtonLink to={ROUTES.movies.list} variant="secondary">Xem danh sách phim</ButtonLink>}
            />
          }
          errorTitle="Không tải được đánh giá"
        >
          {reviews.map((review) => (
            <AccountReviewCard key={review.reviewId} review={review} />
          ))}
          <InfiniteScrollTrigger
            hasNextPage={hasNextPage}
            isFetchingNextPage={isFetchingNextPage}
            fetchNextPage={fetchNextPage}
          />
        </QueryState>
      </div>
    </div>
  );
};

export default AccountReviewsPage;