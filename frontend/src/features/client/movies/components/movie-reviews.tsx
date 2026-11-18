import { useState } from "react";
import { MessageSquareText } from "lucide-react";
import { EmptyState } from "@/components/common/empty-state";
import { InfiniteScrollTrigger } from "@/components/common/infinite-scroll-trigger";
import { QueryState } from "@/components/common/query-state";
import { SkeletonList } from "@/components/common/skeleton-list";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { ReviewForm } from "@/features/client/movies/components/review-form";
import { ReviewItem } from "@/features/client/movies/components/review-item";
import { useCanReviewStatus, useCreateReview, useDeleteMyReview, useMovieReviews, useUpdateReview } from "@/features/client/movies/hooks/use-movies";
import { useAuthUser } from "@/stores/auth-store";

interface MovieReviewsProps {
  movieId: number;
}

export const MovieReviews = ({ movieId }: MovieReviewsProps) => {
  const user = useAuthUser();
  const [editingId, setEditingId] = useState<number | null>(null);

  const reviewsQuery = useMovieReviews({ movieId });
  const { data, hasNextPage, isFetchingNextPage, fetchNextPage } = reviewsQuery;
  const { data: canReviewData } = useCanReviewStatus(movieId, !!user);
  const createReview = useCreateReview(movieId);
  const updateReview = useUpdateReview(movieId);
  const deleteReview = useDeleteMyReview(movieId);

  const reviews = data?.reviews ?? [];
  const editingReview = reviews.find((r) => r.reviewId === editingId);

  return (
    <div>
      <h2 className="t-h2 text-foreground">Đánh giá & Bình luận</h2>

      {user && canReviewData?.canReview && !editingId && (
        <div className="mt-4">
          <ReviewForm isSubmitting={createReview.isPending} onSubmit={(payload) => createReview.mutate(payload)} />
        </div>
      )}

      {editingReview && (
        <div className="mt-4">
          <ReviewForm
            initialRating={editingReview.rating}
            initialComment={editingReview.comment ?? ""}
            isSubmitting={updateReview.isPending}
            submitLabel="Cập nhật"
            onCancel={() => setEditingId(null)}
            onSubmit={(payload) => updateReview.mutate({ reviewId: editingReview.reviewId, body: payload }, { onSuccess: () => setEditingId(null) })}
          />
        </div>
      )}

      <div className="mt-6">
        <QueryState
          query={reviewsQuery}
          skeleton={<SkeletonList count={2} shape={CardSkeleton} />}
          isEmpty={(result) => result.reviews.length === 0}
          empty={<EmptyState icon={MessageSquareText} title="Chưa có đánh giá nào" description="Hãy là người đầu tiên chia sẻ cảm nhận về bộ phim này." />}
          errorTitle="Không tải được đánh giá"
        >
          <div>
            {reviews.map((review) => (
              <ReviewItem key={review.reviewId} review={review} isOwner={user?.userId === review.userId} onEdit={() => setEditingId(review.reviewId)} onDelete={() => deleteReview.mutate(review.reviewId)} />
            ))}
            <InfiniteScrollTrigger hasNextPage={hasNextPage} isFetchingNextPage={isFetchingNextPage} fetchNextPage={fetchNextPage} />
          </div>
        </QueryState>
      </div>
    </div>
  );
}