import { useInfiniteQuery, useMutation, useQuery } from "@tanstack/react-query";
import { accountApi } from "../api/account-api";
import { queryKeys } from "@/api/query-keys";
import { useAuthStore } from "@/stores/auth-store";
import type { MyReviewListParams, UpdateProfileRequest } from "@/features/client/account/types";
import type { ChangePasswordRequest } from "@/features/auth/types";

export function useMyProfile() {
  return useQuery({
    queryKey: queryKeys.users.me(),
    queryFn: () => accountApi.getMyProfile(),
    select: (res) => res.data,
  });
}

export function useUpdateMyProfile() {
  return useMutation({
    meta: { invalidates: "profileChanged" },
    mutationFn: (body: UpdateProfileRequest) =>
      accountApi.updateMyProfile(body),
    onSuccess: (res) => {
      // Chỉ cập nhật phần đã đổi. Trước đây ghi đè cả user bằng setCredentials nên mất
      // membershipTierName/points/mustChangePassword/hasPassword sau khi lưu hồ sơ.
      const { fullName, avatar } = res.data;
      useAuthStore.getState().updateUser({ fullName, avatar });
    },
  });
}

export function useMyReviews(params: Omit<MyReviewListParams, "cursor"> = {}) {
  return useInfiniteQuery({
    queryKey: queryKeys.users.myReviews(params),
    queryFn: ({ pageParam }) =>
      accountApi.getMyReviews({ ...params, cursor: pageParam }),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (lastPage) =>
      lastPage.data.hasMore
        ? (lastPage.data.nextCursor ?? undefined)
        : undefined,
    select: (data) => ({
      reviews: data.pages.flatMap((p) => p.data.items),
      hasMore: data.pages.at(-1)?.data.hasMore ?? false,
    }),
  });
}

export function useChangePassword(options?: { skipSuccessToast?: boolean }) {
  return useMutation({
    meta: { invalidates: "passwordChanged", skipSuccessToast: options?.skipSuccessToast },
    mutationFn: (body: ChangePasswordRequest) =>
      accountApi.changePassword(body),
  });
}
