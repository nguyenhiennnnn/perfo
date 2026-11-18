import { PageContainer } from "@/components/common/page-container";
import { QueryState } from "@/components/common/query-state";
import { SkeletonList } from "@/components/common/skeleton-list";
import { TierCard } from "../components/tier-card";
import { TierCompareTable } from "../components/tier-compare-table";
import { MembershipFaq } from "../components/membership-faq";
import { useMembershipTiers } from "@/features/client/catalog/hooks/use-catalog";
import { Seo } from "@/components/common/seo";
import { BRAND } from "@/config/brand";
import { formatCurrency } from "@/lib/formatters";
import { ROUTES } from "@/config/routes";

const TIER_GRID_CLASS = "grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4";

const MembershipPage = () => {
  const tiersQuery = useMembershipTiers();
  const { data: tiers, isLoading } = tiersQuery;

  const sortedTiers = [...(tiers ?? [])].sort(
    (a, b) => a.minPoints - b.minPoints,
  );

  return (
    <PageContainer size="lg">
      <Seo
        title={`Chương trình thành viên ${BRAND.name} Rewards`}
        description={`Tích điểm mỗi lần đặt vé và nhận quyền lợi độc quyền theo hạng thành viên ${BRAND.name} Rewards: Bronze, Silver, Gold, Platinum.`}
        canonical={ROUTES.membership}
      />
      <section className="mb-10 border border-border/60 bg-card p-6 text-center md:p-10">
        <p className="mb-3 text-sm font-medium text-accent">{BRAND.name} Rewards</p>
        <h1 className="t-h1 text-foreground">
          Chương trình Khách hàng Thân thiết {BRAND.name}
        </h1>
        <p className="mx-auto mt-3 max-w-xl text-sm text-muted-foreground md:text-base">
          Mỗi <span className="font-semibold text-foreground">{formatCurrency(10000)}</span>{" "}
          chi tiêu = <span className="font-semibold text-foreground">1</span>{" "}
          điểm thưởng tích lũy. Càng xem nhiều, quyền lợi càng lớn.
        </p>
      </section>

      <section className="mb-14">
        <h2 className="t-h2 mb-5 text-foreground">
          Các hạng thẻ hội viên
        </h2>

        <QueryState
          query={tiersQuery}
          skeleton={
            <div className={TIER_GRID_CLASS}>
              <SkeletonList count={4} className="h-56 w-full" />
            </div>
          }
          errorTitle="Không tải được các hạng thẻ"
          errorDescription="Kiểm tra kết nối mạng rồi thử lại."
        >
          <div className={TIER_GRID_CLASS}>
            {sortedTiers.map((tier, index) => (
              <TierCard key={tier.tierId} tier={tier} index={index} />
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
