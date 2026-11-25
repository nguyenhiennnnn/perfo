import { Coins, Gift } from "lucide-react";
import { EmptyState } from "@/components/common/empty-state";
import { InfiniteScrollTrigger } from "@/components/common/infinite-scroll-trigger";
import { PageTitle } from "@/components/common/page-title";
import { QueryState } from "@/components/common/query-state";
import { SkeletonList } from "@/components/common/skeleton-list";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { ACCOUNT_LIST_PAGE_LIMIT, SKELETON_LIST_COUNT } from "@/config/default-constants";
import { useDialogOpen } from "@/stores/dialog-store";
import { LoyaltyLotItem } from "../components/loyalty-lot-item";
import { LoyaltyRedeemDialogContent } from "../components/loyalty-redeem-dialog-content";
import { LoyaltyRewardCard } from "../components/loyalty-reward-card";
import { LoyaltySummaryCard } from "../components/loyalty-summary-card";
import { useLoyaltyLots, useLoyaltyRewards, useLoyaltySummary } from "../hooks/use-loyalty";
import type { LoyaltyRewardCatalogItemDto } from "../types";

const LoyaltyPage = () => {
  const openDialog = useDialogOpen();
  const summaryQuery = useLoyaltySummary();
  const rewardsQuery = useLoyaltyRewards();
  const lotsQuery = useLoyaltyLots({ pageSize: ACCOUNT_LIST_PAGE_LIMIT });
  const { hasNextPage, isFetchingNextPage, fetchNextPage } = lotsQuery;

  const balance = summaryQuery.data?.balance ?? 0;

  const handleRedeem = (reward: LoyaltyRewardCatalogItemDto) =>
    openDialog({
      title: "Đổi quà",
      content: <LoyaltyRedeemDialogContent reward={reward} balance={balance} />,
    });

  return (
    <div>
      <PageTitle
        title="Điểm thưởng"
        description="Số dư điểm, lịch sử điểm theo lô và quà bạn có thể đổi."
      />

      <div className="space-y-6">
        <QueryState
          query={summaryQuery}
          skeleton={<CardSkeleton className="h-48" />}
          errorTitle="Không tải được thông tin điểm thưởng"
        >
          {(summary) => <LoyaltySummaryCard summary={summary} />}
        </QueryState>

        <Tabs defaultValue="rewards">
          <TabsList>
            <TabsTrigger value="rewards">Đổi quà</TabsTrigger>
            <TabsTrigger value="history">Lịch sử điểm</TabsTrigger>
          </TabsList>

          <TabsContent value="rewards">
            <div aria-busy={rewardsQuery.isLoading || undefined}>
              <QueryState
                query={rewardsQuery}
                skeleton={
                  <div className="grid gap-3 sm:grid-cols-2">
                    <SkeletonList count={SKELETON_LIST_COUNT} className="h-44" />
                  </div>
                }
                isEmpty={(rewards) => rewards.length === 0}
                empty={
                  <EmptyState
                    icon={Gift}
                    title="Chưa có quà để đổi"
                    description="Danh mục quà sẽ hiện ở đây khi rạp mở đổi quà."
                  />
                }
                errorTitle="Không tải được danh mục quà"
              >
                {(rewards) => (
                  <div className="grid gap-3 sm:grid-cols-2">
                    {rewards.map((reward) => (
                      <LoyaltyRewardCard
                        key={reward.rewardId}
                        reward={reward}
                        balance={balance}
                        onRedeem={handleRedeem}
                      />
                    ))}
                  </div>
                )}
              </QueryState>
            </div>
          </TabsContent>

          <TabsContent value="history">
            <div className="space-y-3" aria-busy={lotsQuery.isLoading || undefined}>
              <QueryState
                query={lotsQuery}
                skeleton={<SkeletonList count={SKELETON_LIST_COUNT} className="h-32" />}
                isEmpty={(data) => data.lots.length === 0}
                empty={
                  <EmptyState
                    icon={Coins}
                    title="Chưa có điểm nào"
                    description="Điểm sẽ được tích khi đơn vé hoặc combo của bạn được xác nhận."
                  />
                }
                errorTitle="Không tải được lịch sử điểm"
              >
                {(data) => (
                  <>
                    {data.lots.map((lot) => (
                      <LoyaltyLotItem key={lot.entryId} lot={lot} />
                    ))}
                    <InfiniteScrollTrigger
                      hasNextPage={hasNextPage}
                      isFetchingNextPage={isFetchingNextPage}
                      fetchNextPage={fetchNextPage}
                    />
                  </>
                )}
              </QueryState>
            </div>
          </TabsContent>
        </Tabs>
      </div>
    </div>
  );
};

export default LoyaltyPage;
