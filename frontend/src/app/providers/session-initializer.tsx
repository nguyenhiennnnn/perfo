import { useEffect } from "react";
import type { AxiosError } from "axios";
import { refreshAccessToken } from "@/api/axios";
import { useAuthStore } from "@/stores/auth-store";
import { sessionHint } from "@/lib/session-hint";
import { logDevError } from "@/lib/error";
import { HTTP_STATUS } from "@/config/http-status";

async function restoreSession(source: string) {
  try {
    await refreshAccessToken();
  } catch (err) {
    logDevError(err, source);

    const status = (err as AxiosError)?.response?.status;
    if (status === HTTP_STATUS.unauthorized || status === HTTP_STATUS.forbidden) {
      useAuthStore.getState().logout();
    }
  }
}

export const SessionInitializer = () => {
  const { setInitialized, isInitialized } = useAuthStore();

  useEffect(() => {
    if (isInitialized) return;

    if (!sessionHint.has()) {
      setInitialized();
      return;
    }

    void restoreSession("SessionInitializer").finally(setInitialized);
  }, [isInitialized, setInitialized]);

  useEffect(
    () =>
      sessionHint.subscribe((present) => {
        const { isInitialized: ready, isAuthenticated } =
          useAuthStore.getState();
        if (!ready) return;

        if (!present && isAuthenticated) {
          useAuthStore.getState().logout();
        } else if (present && !isAuthenticated) {
          void restoreSession("SessionSync");
        }
      }),
    [],
  );

  return null;
};
