import { Users } from "lucide-react";
import { EmptyState } from "@/components/common/empty-state";
import { PageTitle } from "@/components/common/page-title";
import { QueryState } from "@/components/common/query-state";
import { SkeletonList } from "@/components/common/skeleton-list";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { Badge } from "@/components/ui/badge";
import { SKELETON_LIST_COUNT } from "@/config/default-constants";
import { formatDate } from "@/lib/formatters";
import { ReferralCodeCard } from "../components/referral-code-card";
import { useMyReferrals } from "../hooks/use-loyalty";

const ReferralsPage = () => {
  const referralsQuery = useMyReferrals();

  return (
    <div>
      <PageTitle
        title="Giới thiệu bạn bè"
        description="Chia sẻ mã giới thiệu để cả hai cùng nhận điểm thưởng."
      />

      <QueryState
        query={referralsQuery}
        skeleton={
          <div className="space-y-3">
            <CardSkeleton className="h-44" />
            <SkeletonList count={SKELETON_LIST_COUNT} className="h-16" />
          </div>
        }
        errorTitle="Không tải được thông tin giới thiệu"
      >
        {(referrals) => (
          <div className="space-y-6">
            <ReferralCodeCard referrals={referrals} />

            <section aria-label="Người đã giới thiệu" className="space-y-3">
              <h2 className="t-h3 text-foreground">Người đã giới thiệu</h2>
              {referrals.items.length === 0 ? (
                <EmptyState
                  icon={Users}
                  title="Chưa có ai đăng ký bằng mã của bạn"
                  description="Gửi mã hoặc liên kết giới thiệu cho bạn bè để bắt đầu."
                />
              ) : (
                referrals.items.map((item) => (
                  <article
                    key={`${item.referredName}-${item.referredAt}`}
                    className="flex flex-wrap items-center justify-between gap-2 cut-md bg-card p-4"
                  >
                    <div className="min-w-0">
                      <p className="font-medium text-foreground">{item.referredName}</p>
                      <p className="text-xs text-muted-foreground">
                        Đăng ký ngày {formatDate(item.referredAt)}
                      </p>
                    </div>
                    {item.rewardGranted ? (
                      <Badge variant="success">
                        Đã nhận thưởng{item.rewardGrantedAt ? ` ${formatDate(item.rewardGrantedAt)}` : ""}
                      </Badge>
                    ) : (
                      <Badge variant="secondary">Chờ đơn đầu tiên</Badge>
                    )}
                  </article>
                ))
              )}
            </section>
          </div>
        )}
      </QueryState>
    </div>
  );
};

export default ReferralsPage;
