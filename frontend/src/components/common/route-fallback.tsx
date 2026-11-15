import { useEffect, useState } from "react";
import { PageLoader } from "@/components/common/spinner";
import { ROUTE_FALLBACK_DELAY_MS } from "@/config/default-constants";

interface RouteFallbackProps {
  delay?: number;
}

export const RouteFallback = ({ delay = ROUTE_FALLBACK_DELAY_MS }: RouteFallbackProps) => {
  const [visible, setVisible] = useState(delay <= 0);

  useEffect(() => {
    if (visible) return;
    const id = window.setTimeout(() => setVisible(true), delay);
    return () => window.clearTimeout(id);
  }, [delay, visible]);

  if (!visible) return null;

  return (
    <PageLoader variant="section" />
  );
};
