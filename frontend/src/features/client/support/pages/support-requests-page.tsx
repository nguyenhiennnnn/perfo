import { useId, useState } from "react";
import { LifeBuoy } from "lucide-react";
import { ButtonLink } from "@/components/common/button-link";
import { Chip, ChipGroup } from "@/components/common/chip";
import { EmptyState } from "@/components/common/empty-state";
import { InfiniteScrollTrigger } from "@/components/common/infinite-scroll-trigger";
import { PageTitle } from "@/components/common/page-title";
import { QueryState } from "@/components/common/query-state";
import { SkeletonList } from "@/components/common/skeleton-list";
import { ACCOUNT_LIST_PAGE_LIMIT, SKELETON_LIST_COUNT } from "@/config/default-constants";
import { ROUTES } from "@/config/routes";
import type { SupportRequestStatus } from "@/types/enum";
import { SupportRequestCard } from "../components/support-request-card";
import { useMySupportRequests } from "../hooks/use-support-requests";
import { SUPPORT_STATUS_TABS } from "../utils/support-labels";

const TAB_ALL_KEY = "all";

const AccountSupportRequestsPage = () => {
  const [status, setStatus] = useState<SupportRequestStatus | null>(null);
  const tabsId = useId();
  const requestsQuery = useMySupportRequests({
    status: status ?? undefined,
    pageSize: ACCOUNT_LIST_PAGE_LIMIT,
  });
  const { data, hasNextPage, isFetchingNextPage, fetchNextPage } = requestsQuery;

  const requests = data?.requests ?? [];
  const getTabId = (value: string | null) => `${tabsId}-${value ?? TAB_ALL_KEY}`;

  return (
    <div>
      <PageTitle
        title="Yêu cầu hỗ trợ"
        description="Theo dõi tiến độ và trao đổi với rạp. Để gửi yêu cầu mới, mở chi tiết đơn hàng và chọn Gửi yêu cầu hỗ trợ."
      />

      <ChipGroup role="tablist" aria-label="Trạng thái yêu cầu">
        {SUPPORT_STATUS_TABS.map((tab) => (
          <Chip
            key={tab.label}
            role="tab"
            id={getTabId(tab.value)}
            selected={status === tab.value}
            onClick={() => setStatus(tab.value)}
          >
            {tab.label}
          </Chip>
        ))}
      </ChipGroup>

      <div
        role="tabpanel"
        aria-labelledby={getTabId(status)}
        className="mt-5 space-y-3"
        aria-busy={requestsQuery.isLoading || undefined}
      >
        <QueryState
          query={requestsQuery}
          skeleton={<SkeletonList count={SKELETON_LIST_COUNT} className="h-28" />}
          isEmpty={requests.length === 0}
          empty={
            status ? (
              <EmptyState
                variant="filtered"
                title="Không có yêu cầu ở trạng thái này"
                description="Chọn trạng thái khác hoặc xem tất cả yêu cầu."
                onClearFilters={() => setStatus(null)}
              />
            ) : (
              <EmptyState
                icon={LifeBuoy}
                title="Bạn chưa có yêu cầu hỗ trợ nào"
                description="Cần hỗ trợ về một đơn hàng? Mở đơn đó và gửi yêu cầu cho rạp."
                action={
                  <ButtonLink to={ROUTES.account.tickets} variant="secondary">
                    Chọn đơn hàng
                  </ButtonLink>
                }
              />
            )
          }
          errorTitle="Không tải được danh sách yêu cầu hỗ trợ"
        >
          {requests.map((request) => (
            <SupportRequestCard key={request.supportRequestId} request={request} />
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

export default AccountSupportRequestsPage;
