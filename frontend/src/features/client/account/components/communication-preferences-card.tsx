import { QueryState } from "@/components/common/query-state";
import { CopyButton } from "@/components/common/copy-button";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { Field, FieldContent, FieldDescription, FieldGroup, FieldLabel } from "@/components/ui/field";
import { Switch } from "@/components/ui/switch";
import {
  useMyPreferences,
  useUpdateMyPreferences,
} from "@/features/client/account/hooks/use-account";
import type { UpdateCommunicationPreferencesRequest } from "@/features/client/account/types";

type PreferenceKey = keyof UpdateCommunicationPreferencesRequest;

const OPTIONS: { key: PreferenceKey; label: string; description: string }[] = [
  {
    key: "marketingOptIn",
    label: "Email và thông báo tiếp thị",
    description: "Nhận ưu đãi, khuyến mãi và tin phim mới qua email và thông báo.",
  },
  {
    key: "personalizedRecommendations",
    label: "Gợi ý phim cá nhân hóa",
    description: "Hiển thị phim phù hợp với sở thích của bạn. Khi tắt, bạn chỉ thấy danh sách phổ biến chung.",
  },
  {
    key: "reminderAndReviewInvites",
    label: "Nhắc lịch chiếu và mời đánh giá",
    description: "Nhận nhắc trước giờ chiếu và lời mời đánh giá sau khi xem phim.",
  },
];

export const CommunicationPreferencesCard = () => {
  const query = useMyPreferences();
  const update = useUpdateMyPreferences();

  return (
    <QueryState
      query={query}
      skeleton={<CardSkeleton className="h-64" />}
      errorTitle="Không tải được lựa chọn nhận thông tin"
    >
      {(prefs) => {
        const toggle = (key: PreferenceKey, value: boolean) => {
          update.mutate({
            marketingOptIn: prefs.marketingOptIn,
            personalizedRecommendations: prefs.personalizedRecommendations,
            reminderAndReviewInvites: prefs.reminderAndReviewInvites,
            [key]: value,
          });
        };

        return (
          <section className="bg-card p-5">
            <h2 className="t-h2 text-foreground">Nhận thông tin</h2>
            <p className="mt-1 text-sm text-muted-foreground">
              Email liên quan đến đơn hàng và tài khoản luôn được gửi.
            </p>

            <FieldGroup className="mt-5 gap-4" disabled={update.isPending}>
              {OPTIONS.map((option) => (
                <Field key={option.key} orientation="horizontal" className="items-start justify-between gap-4">
                  <FieldContent>
                    <FieldLabel htmlFor={`pref-${option.key}`}>{option.label}</FieldLabel>
                    <FieldDescription>{option.description}</FieldDescription>
                  </FieldContent>
                  <Switch
                    id={`pref-${option.key}`}
                    checked={prefs[option.key]}
                    disabled={update.isPending}
                    onCheckedChange={(value) => toggle(option.key, value)}
                  />
                </Field>
              ))}
            </FieldGroup>

            {prefs.referralCode && (
              <div className="mt-6 flex flex-col gap-2 border-t border-border pt-5 sm:flex-row sm:items-center sm:justify-between">
                <div>
                  <p className="text-sm font-medium text-foreground">Mã giới thiệu của bạn</p>
                  <p className="font-mono text-lg tracking-widest text-primary-text">{prefs.referralCode}</p>
                </div>
                <CopyButton value={prefs.referralCode} label="mã giới thiệu" />
              </div>
            )}
          </section>
        );
      }}
    </QueryState>
  );
};
