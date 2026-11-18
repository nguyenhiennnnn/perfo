import { Star } from "lucide-react";
import { EmptyState } from "@/components/common/empty-state";
import { PageTitle } from "@/components/common/page-title";
import { QueryState } from "@/components/common/query-state";
import { SkeletonList } from "@/components/common/skeleton-list";
import { InfiniteScrollTrigger } from "@/components/common/infinite-scroll-trigger";
import { AccountReviewCard } from "@/features/client/account/components/account-review-card";
import { useMyReviews } from "@/features/client/account/hooks/use-account";
import { ACCOUNT_LIST_PAGE_LIMIT } from "@/config/default-constants";

const AccountReviewsPage = () => {
  const reviewsQuery = useMyReviews({ limit: ACCOUNT_LIST_PAGE_LIMIT });
  const { data, hasNextPage, isFetchingNextPage, fetchNextPage } = reviewsQuery;
  const reviews = data?.reviews ?? [];

  return (
    <div>
      <PageTitle title="Đánh giá của tôi" />

      <div className="space-y-3">
        <QueryState
          query={reviewsQuery}
          skeleton={<SkeletonList count={3} className="h-24 w-full" />}
          isEmpty={reviews.length === 0}
          empty={<EmptyState icon={Star} title="Bạn chưa có đánh giá nào" description="Hãy chia sẻ cảm nhận sau khi xem phim tại rạp." />}
          errorTitle="Không tải được đánh giá"
          errorDescription="Kiểm tra kết nối mạng rồi thử lại."
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