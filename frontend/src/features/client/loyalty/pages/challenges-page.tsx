import { Trophy } from "lucide-react";
import { EmptyState } from "@/components/common/empty-state";
import { PageTitle } from "@/components/common/page-title";
import { QueryState } from "@/components/common/query-state";
import { SkeletonList } from "@/components/common/skeleton-list";
import { SKELETON_LIST_COUNT } from "@/config/default-constants";
import { LoyaltyChallengeCard } from "../components/loyalty-challenge-card";
import { useMyChallenges } from "../hooks/use-loyalty";

const ChallengesPage = () => {
  const challengesQuery = useMyChallenges();

  return (
    <div>
      <PageTitle
        title="Thử thách"
        description="Hoàn thành thử thách trong kỳ để nhận điểm hoặc quà. Mỗi thử thách chỉ nhận thưởng một lần."
      />

      <div className="space-y-3" aria-busy={challengesQuery.isLoading || undefined}>
        <QueryState
          query={challengesQuery}
          skeleton={<SkeletonList count={SKELETON_LIST_COUNT} className="h-40" />}
          isEmpty={(challenges) => challenges.length === 0}
          empty={
            <EmptyState
              icon={Trophy}
              title="Chưa có thử thách nào"
              description="Thử thách mới sẽ hiện ở đây khi rạp mở."
            />
          }
          errorTitle="Không tải được danh sách thử thách"
        >
          {(challenges) =>
            challenges.map((challenge) => (
              <LoyaltyChallengeCard key={challenge.challengeId} challenge={challenge} />
            ))
          }
        </QueryState>
      </div>
    </div>
  );
};

export default ChallengesPage;
