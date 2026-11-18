import { TriangleAlert } from "lucide-react";

import { ButtonLink } from "@/components/common/button-link";
import { EmptyState } from "@/components/common/empty-state";

interface InvalidLinkNoticeProps {
  title: string;
  description: string;
  actionLabel: string;
  actionTo: string;
}

export const InvalidLinkNotice = ({
  title,
  description,
  actionLabel,
  actionTo,
}: InvalidLinkNoticeProps) => (
  <EmptyState
    icon={TriangleAlert}
    title={title}
    description={description}
    action={
      <ButtonLink to={actionTo} variant="secondary">
        {actionLabel}
      </ButtonLink>
    }
    className="bg-transparent px-0 py-6"
  />
);
