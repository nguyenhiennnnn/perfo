import { useEffect, useEffectEvent } from "react";

export function useRevalidateWhenTouched(
  watchedValue: unknown,
  isTargetTouched: () => boolean,
  revalidateTarget: () => void,
) {
  const run = useEffectEvent(() => {
    if (isTargetTouched()) revalidateTarget();
  });

  useEffect(() => {
    run();
  }, [watchedValue]);
}
