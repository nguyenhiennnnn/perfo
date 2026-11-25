import { Gift } from "lucide-react";
import { ButtonLink } from "@/components/common/button-link";
import { EmptyState } from "@/components/common/empty-state";
import { InfiniteScrollTrigger } from "@/components/common/infinite-scroll-trigger";
import { PageTitle } from "@/components/common/page-title";
import { QueryState } from "@/components/common/query-state";
import { SkeletonList } from "@/components/common/skeleton-list";
import { ACCOUNT_LIST_PAGE_LIMIT, SKELETON_LIST_COUNT } from "@/config/default-constants";
import { ROUTES } from "@/config/routes";
import { PurchasedGiftCardItem } from "../components/purchased-gift-card-item";
import { usePurchasedGiftCards } from "../hooks/use-gift-cards";

const PurchasedGiftCardsPage = () => {
  const cardsQuery = usePurchasedGiftCards({ limit: ACCOUNT_LIST_PAGE_LIMIT });
  const { data, hasNextPage, isFetchingNextPage, fetchNextPage } = cardsQuery;
  const cards = data?.cards ?? [];

  return (
    <div>
      <PageTitle
        title="Thẻ quà đã mua"
        description="Các thẻ quà bạn đã mua và trạng thái gửi. Đơn chưa thanh toán xem ở mục Lịch sử đơn."
        action={<ButtonLink to={ROUTES.giftCards}>Mua thẻ quà</ButtonLink>}
      />

      <div className="space-y-3" aria-busy={cardsQuery.isLoading || undefined}>
        <QueryState
          query={cardsQuery}
          skeleton={<SkeletonList count={SKELETON_LIST_COUNT} className="h-40" />}
          isEmpty={cards.length === 0}
          empty={
            <EmptyState
              icon={Gift}
              title="Chưa có thẻ quà nào"
              description="Mua thẻ quà để tặng người thân, thẻ đã thanh toán sẽ hiện ở đây."
              action={<ButtonLink to={ROUTES.giftCards} variant="secondary">Mua thẻ quà</ButtonLink>}
            />
          }
          errorTitle="Không tải được danh sách thẻ quà"
        >
          {cards.map((card) => (
            <PurchasedGiftCardItem key={card.giftCardId} card={card} />
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

export default PurchasedGiftCardsPage;
