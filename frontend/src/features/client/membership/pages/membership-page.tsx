import type { CSSProperties } from "react";
import { Award } from "lucide-react";
import { ButtonLink } from "@/components/common/button-link";
import { EmptyState } from "@/components/common/empty-state";
import { PageContainer } from "@/components/common/page-container";
import { QueryState } from "@/components/common/query-state";
import { SkeletonList } from "@/components/common/skeleton-list";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { TierCard } from "../components/tier-card";
import { TierCompareTable } from "../components/tier-compare-table";
import { MembershipFaq } from "../components/membership-faq";
import { useMembershipTiers } from "@/features/client/catalog/hooks/use-catalog";
import { useCinemaSettings } from "@/features/client/settings/hooks/use-settings";
import { useIsAuthenticated } from "@/stores/auth-store";
import { TIER_SKELETON_COUNT } from "../constants";
import { getTierGridColumns } from "../utils";
import { Seo } from "@/components/common/seo";
import { BRAND } from "@/config/brand";
import { formatCurrency } from "@/lib/formatters";
import { ROUTES } from "@/config/routes";
import { MOVIE_LIST_STATUS_QUERY } from "@/config/default-constants";

const TIER_GRID_CLASS = "grid grid-cols-1 items-end gap-4 sm:grid-cols-2 lg:grid-cols-(--tier-columns)";

const getTierGridStyle = (count: number) => ({ "--tier-columns": getTierGridColumns(count) }) as CSSProperties;

const MembershipPage = () => {
  const tiersQuery = useMembershipTiers();
  const { data: tiers, isLoading } = tiersQuery;
  const { data: settings, isLoading: isSettingsLoading } = useCinemaSettings();
  const isAuthenticated = useIsAuthenticated();
  const pointsPerAmount = settings?.pointsPerAmount ?? 0;

  const sortedTiers = [...(tiers ?? [])].sort(
    (a, b) => a.minPoints - b.minPoints,
  );

  return (
    <PageContainer size="lg">
      <Seo
        title={`Chương trình thành viên ${BRAND.name} Rewards`}
        description={`Tích điểm mỗi lần đặt vé và nhận quyền lợi độc quyền theo hạng thành viên ${BRAND.name} Rewards.`}
        canonical={ROUTES.membership}
      />
      <section className="mb-14 grid gap-6 md:grid-cols-[1.4fr_1fr] md:items-end">
        <div className="text-left">
          <h1 className="t-h1 text-foreground">
            Chương trình Khách hàng Thân thiết {BRAND.name}
          </h1>
          <p className="mt-3 max-w-xl text-sm text-muted-foreground md:text-base">
            Điểm thưởng được cộng tự động sau mỗi đơn vé hoặc bắp nước hoàn tất. Càng xem nhiều, quyền lợi càng lớn.
          </p>
          <div className="mt-6 flex flex-wrap gap-3">
            {isAuthenticated ? (
              <ButtonLink to={ROUTES.account.profile}>Xem điểm của tôi</ButtonLink>
            ) : (
              <ButtonLink to={ROUTES.auth.register}>Đăng ký thành viên</ButtonLink>
            )}
            <ButtonLink
              to={`${ROUTES.movies.list}?status=${MOVIE_LIST_STATUS_QUERY.nowShowing}`}
              variant="secondary"
            >
              Xem phim đang chiếu
            </ButtonLink>
          </div>
        </div>
        {pointsPerAmount > 0 ? (
          <div className="cut-lg bg-card p-6 text-left md:p-8">
            <p className="t-numeral text-5xl text-primary md:text-6xl">{formatCurrency(pointsPerAmount)}</p>
            <p className="t-numeral mt-1 text-3xl text-foreground md:text-4xl">= 1 điểm</p>
            <p className="mt-3 text-xs text-muted-foreground">chi tiêu cho mỗi điểm thưởng tích lũy</p>
          </div>
        ) : (
          isSettingsLoading && <CardSkeleton cut="lg" className="h-40" />
        )}
      </section>

      <section className="mb-14">
        <h2 className="t-h2 mb-5 text-foreground">
          Các hạng thẻ hội viên
        </h2>

        <QueryState
          query={tiersQuery}
          skeleton={
            <div aria-busy="true" className={TIER_GRID_CLASS} style={getTierGridStyle(TIER_SKELETON_COUNT)}>
              <SkeletonList count={TIER_SKELETON_COUNT} className="h-56" />
            </div>
          }
          isEmpty={sortedTiers.length === 0}
          empty={<EmptyState icon={Award} title="Chưa có hạng thẻ" description="Các hạng thành viên sẽ hiện ở đây khi rạp công bố chương trình." />}
          errorTitle="Không tải được các hạng thẻ"
        >
          <div className={TIER_GRID_CLASS} style={getTierGridStyle(sortedTiers.length)}>
            {sortedTiers.map((tier, index) => (
              <TierCard key={tier.tierId} tier={tier} index={index} count={sortedTiers.length} />
            ))}
          </div>
        </QueryState>
      </section>

      {!isLoading && sortedTiers.length > 0 && (
        <section className="mb-14">
          <h2 className="t-h2 mb-5 text-foreground">
            So sánh quyền lợi chi tiết
          </h2>
          <TierCompareTable tiers={sortedTiers} />
        </section>
      )}

      <section>
        <h2 className="t-h2 mb-5 text-foreground">
          Câu hỏi thường gặp
        </h2>
        <MembershipFaq />
      </section>
    </PageContainer>
  );
};

export default MembershipPage;
