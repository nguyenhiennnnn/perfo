import { Link, Navigate, useParams } from "react-router";
import { PageContainer } from "@/components/common/page-container";
import { Seo } from "@/components/common/seo";
import { cn } from "@/lib/utils";
import { useCinemaSettings } from "@/features/client/settings/hooks/use-settings";
import { getPolicy, POLICIES } from "../policy-content";
import { ROUTES } from "@/config/routes";

const PolicyPage = () => {
  const { slug } = useParams();
  const policy = getPolicy(slug);
  const { data: settings } = useCinemaSettings();

  if (!policy) return <Navigate to={ROUTES.notFound} replace />;

  const sections = policy.sections({
    cancelDeadlineHours: settings?.cancelDeadlineHours,
    cancelRefundPercent: settings?.cancelRefundPercent,
    stopSellingMinutesBefore: settings?.stopSellingMinutesBefore,
  });

  return (
    <PageContainer size="md" className="grid gap-8 md:grid-cols-[14rem_1fr]">
      <Seo
        title={policy.title}
        description={policy.description}
        canonical={ROUTES.policy(policy.slug)}
      />

      <aside className="md:sticky md:top-24 md:self-start">
        <p className="mb-3 text-[13px] font-medium text-foreground">Chính sách</p>
        <nav className="flex flex-row flex-wrap gap-1 md:flex-col">
          {POLICIES.map((item) => (
            <Link
              key={item.slug}
              to={ROUTES.policy(item.slug)}
              aria-current={item.slug === policy.slug ? "page" : undefined}
              className={cn(
                "px-3 py-2 text-sm text-muted-foreground transition-colors hover:bg-sidebar-accent hover:text-foreground",
                item.slug === policy.slug &&
                  "bg-sidebar-primary font-medium text-sidebar-primary-foreground hover:bg-sidebar-primary hover:text-sidebar-primary-foreground",
              )}
            >
              {item.title}
            </Link>
          ))}
        </nav>
      </aside>

      <article className="min-w-0 cut-md bg-card p-6 md:p-10">
        <h1 className="t-h1 text-foreground">
          {policy.title}
        </h1>
        <p className="mt-1 text-xs text-muted-foreground">
          Cập nhật lần cuối: {policy.updatedAt}
        </p>
        <div className="perf-h my-6" />

        <div className="flex flex-col gap-6">
          {sections.map((section) => (
            <section key={section.heading}>
              <h2 className="t-h2 mb-2 text-foreground">
                {section.heading}
              </h2>
              {section.list ? (
                <ul className="list-disc space-y-1.5 pl-5 text-[15px] leading-relaxed text-foreground md:text-base">
                  {section.body.map((line) => (
                    <li key={line}>{line}</li>
                  ))}
                </ul>
              ) : (
                section.body.map((line) => (
                  <p
                    key={line}
                    className="text-[15px] leading-relaxed text-foreground md:text-base"
                  >
                    {line}
                  </p>
                ))
              )}
            </section>
          ))}
        </div>
      </article>
    </PageContainer>
  );
};

export default PolicyPage;
