import type { ReactNode } from "react";

export const Section = ({ title, children }: { title: string; children: ReactNode }) => (
  <section className="flex flex-col gap-2">
    <h3 className="text-sm font-medium text-muted-foreground">{title}</h3>
    {children}
  </section>
);

export const InfoRow = ({ label, children }: { label: string; children: ReactNode }) => (
  <div className="flex items-start justify-between gap-3 text-sm">
    <dt className="shrink-0 text-muted-foreground">{label}</dt>
    <dd className="min-w-0 text-right font-medium wrap-break-word text-foreground">
      {children}
    </dd>
  </div>
);
