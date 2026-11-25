import { Link2 } from "lucide-react";
import { CopyButton } from "@/components/common/copy-button";
import { formatPoints } from "@/lib/formatters";
import type { MyReferralsDto } from "../types";
import { buildReferralLink } from "../utils/referral-link";

interface ReferralCodeCardProps {
  referrals: MyReferralsDto;
}

export const ReferralCodeCard = ({ referrals }: ReferralCodeCardProps) => (
  <section aria-label="Mã giới thiệu của bạn" className="cut-md bg-card p-5">
    <div className="flex flex-wrap items-center justify-between gap-4">
      <div>
        <p className="text-xs text-muted-foreground">Mã giới thiệu của bạn</p>
        {referrals.referralCode ? (
          <div className="mt-1 flex items-center gap-1">
            <span data-testid="referral-code" className="t-code text-2xl tracking-widest text-primary-text">
              {referrals.referralCode}
            </span>
            <CopyButton value={referrals.referralCode} label="mã giới thiệu" />
          </div>
        ) : (
          <p className="mt-1 text-sm text-muted-foreground">Chưa có mã giới thiệu.</p>
        )}
      </div>
      <div className="flex gap-6 text-right">
        <div>
          <p className="text-xs text-muted-foreground">Đã giới thiệu</p>
          <p className="mt-1 text-lg font-bold tabular-nums text-foreground">
            {formatPoints(referrals.totalCount)}
          </p>
        </div>
        <div>
          <p className="text-xs text-muted-foreground">Đã nhận thưởng</p>
          <p className="mt-1 text-lg font-bold tabular-nums text-accent-text">
            {formatPoints(referrals.rewardedCount)}
          </p>
        </div>
      </div>
    </div>

    {referrals.referralCode && (
      <div className="mt-4 flex items-center justify-between gap-2 bg-secondary p-3">
        <p className="flex min-w-0 items-center gap-2 text-sm text-foreground">
          <Link2 className="size-4 shrink-0 text-muted-foreground" aria-hidden />
          <span className="truncate">{buildReferralLink(referrals.referralCode)}</span>
        </p>
        <CopyButton value={buildReferralLink(referrals.referralCode)} label="liên kết giới thiệu" />
      </div>
    )}

    <p className="mt-4 border-t border-border pt-3 text-xs text-muted-foreground">
      Người mới nhập mã khi đăng ký. Thưởng được cấp cho cả hai bên khi đơn đầu tiên của người được
      giới thiệu được xác nhận. Mỗi người chỉ được giới thiệu một lần, không tự giới thiệu, và có giới
      hạn thưởng giới thiệu mỗi tháng.
    </p>
  </section>
);
