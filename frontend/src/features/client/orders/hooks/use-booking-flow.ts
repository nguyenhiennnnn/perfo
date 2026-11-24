import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { useLocation, useNavigate } from "react-router";
import { toast } from "@/components/ui/toast";
import {
  BOOKING_HOLD_POLL_INTERVAL_MS,
  BOOKING_COMBO_SECTION_ID,
  BOOKING_HOLD_POLL_MAX_FAILURES,
} from "@/config/default-constants";
import { queryKeys } from "@/api/query-keys";
import {
  CancelReason,
  OrderStatus,
  PaymentMethod,
  SeatGapRuleMode,
} from "@/types/enum";
import {
  findLoneSeats,
  toggleSeatSelection,
} from "@/components/common/seat-map/seat-map-utils";
import { useCinemaSettings } from "@/features/client/settings/hooks/use-settings";
import { useActiveCombos } from "@/features/client/combos/hooks/use-combos";
import { useShowtimeSeats } from "@/features/client/showtimes/hooks/use-showtimes";
import { useSeatTicketTypes } from "@/features/client/showtimes/hooks/use-seat-ticket-types";
import { usePriceChangeConfirm } from "@/hooks/use-price-change-confirm";
import { getPriceChangeRequired, isGenericConflictError } from "@/lib/error";
import { useRealtimeSeatMap } from "@/hooks/use-realtime-seat-map";
import { isValidVnPhone } from "@/lib/validators";
import { useAuthUser, useIsAuthenticated } from "@/stores/auth-store";
import { useAlertDialogOpen } from "@/stores/alert-dialog-store";
import {
  useApplyPayment,
  useCancelOrder,
  useHoldSeats,
  useOrderDetail,
} from "./use-orders";
import { ordersApi } from "../api/orders-api";
import type { HoldOrderResponse } from "../types";
import { ROUTES } from "@/config/routes";
import { getServerNow } from "@/lib/server-clock";

export type BookingStep = "select" | "payment";

export function useBookingFlow(showtimeId: number, resumeOrderId?: number) {
  const isResumeMode = !!resumeOrderId && resumeOrderId > 0;

  const [step, setStep] = useState<BookingStep>(
    isResumeMode ? "payment" : "select",
  );
  const [selectedSeatIds, setSelectedSeatIds] = useState<Set<number>>(
    new Set(),
  );
  const [comboQuantities, setComboQuantities] = useState<
    Record<number, number>
  >({});
  const [paymentMethod, setPaymentMethod] = useState<PaymentMethod>(
    PaymentMethod.VNPay,
  );
  const [freshHoldResult, setFreshHoldResult] =
    useState<HoldOrderResponse | null>(null);

  const comboQuantitiesRef = useRef<Record<number, number>>({});
  const comboNamesRef = useRef(new Map<number, string>());
  const expiredNotifiedRef = useRef(false);
  const pollFailureCountRef = useRef(0);

  const authUser = useAuthUser();
  const isAuthenticated = useIsAuthenticated();
  const navigate = useNavigate();
  const location = useLocation();
  const openAlertDialog = useAlertDialogOpen();
  const [customerName, setCustomerName] = useState(authUser?.fullName ?? "");
  const [customerPhone, setCustomerPhone] = useState("");

  const promptLogin = (description: string) => {
    const redirectTo = encodeURIComponent(
      `${location.pathname}${location.search}`,
    );
    openAlertDialog({
      title: "Yêu cầu đăng nhập",
      description,
      confirmText: "Đăng nhập ngay",
      cancelText: "Để sau",
      onConfirm: () => {
        navigate(`${ROUTES.auth.login}?redirect=${redirectTo}`);
      },
    });
  };

  useEffect(() => {
    if (authUser?.fullName && !customerName) {
      setCustomerName(authUser.fullName);
    }
  }, [authUser?.fullName, customerName]);

  const showtimeSeats = useShowtimeSeats(showtimeId);
  const activeCombos = useActiveCombos();

  const updateComboQuantities = useCallback(
    (update: (prev: Record<number, number>) => Record<number, number>) => {
      const next = update(comboQuantitiesRef.current);
      comboQuantitiesRef.current = next;
      setComboQuantities(next);
    },
    [],
  );

  const pruneUnavailableCombos = useCallback(
    (combos: { comboId: number; name: string }[], silent: boolean) => {
      const availableIds = new Set(combos.map((combo) => combo.comboId));
      combos.forEach((combo) => comboNamesRef.current.set(combo.comboId, combo.name));

      const removedIds = Object.entries(comboQuantitiesRef.current)
        .filter(([comboId, quantity]) => quantity > 0 && !availableIds.has(Number(comboId)))
        .map(([comboId]) => Number(comboId));
      if (removedIds.length === 0) return;

      updateComboQuantities((prev) =>
        Object.fromEntries(
          Object.entries(prev).filter(([comboId]) => !removedIds.includes(Number(comboId))),
        ),
      );

      if (silent) return;
      const names = removedIds.map(
        (comboId) => comboNamesRef.current.get(comboId) ?? "đã chọn",
      );
      toast.add({
        title:
          names.length === 1
            ? `Combo ${names[0]} đã hết hàng và được bỏ khỏi lựa chọn.`
            : `Các combo ${names.join(", ")} đã hết hàng và được bỏ khỏi lựa chọn.`,
        type: "warning",
      });
    },
    [updateComboQuantities],
  );

  useEffect(() => {
    if (activeCombos.data) pruneUnavailableCombos(activeCombos.data, false);
  }, [activeCombos.data, pruneUnavailableCombos]);

  const resumeOrderQuery = useOrderDetail(isResumeMode ? resumeOrderId! : 0);

  const resumeInvalidReason = useMemo<
    "not-pending" | "expired" | "load-error" | null
  >(() => {
    if (!isResumeMode) return null;
    if (resumeOrderQuery.isError) return "load-error";
    const order = resumeOrderQuery.data;
    if (!order) return null;
    if (order.status !== OrderStatus.Pending || !order.expiresAt)
      return "not-pending";
    if (new Date(order.expiresAt).getTime() <= getServerNow())
      return "expired";
    return null;
  }, [isResumeMode, resumeOrderQuery.isError, resumeOrderQuery.data]);

  const holdResult: HoldOrderResponse | null = useMemo(() => {
    if (!isResumeMode) return freshHoldResult;
    const order = resumeOrderQuery.data;
    if (!order || !order.expiresAt || resumeInvalidReason) return null;
    return {
      orderId: order.orderId,
      orderCode: order.orderCode,
      status: order.status,
      totalAmount: order.totalAmount,
      discountAmount: order.discountAmount,
      finalAmount: order.finalAmount,
      expiresAt: order.expiresAt,
    };
  }, [
    isResumeMode,
    freshHoldResult,
    resumeOrderQuery.data,
    resumeInvalidReason,
  ]);

  const paymentOrder = useOrderDetail(holdResult?.orderId ?? 0, {
    alwaysRefetchOnMount: true,
  });

  const seatStatusMap = useRealtimeSeatMap({
    showtimeId,
    initialSeats: showtimeSeats.data?.seats,
    ownOrderId: holdResult?.orderId ?? null,
    onHoldExpired: () => {
      if (expiredNotifiedRef.current) return;
      expiredNotifiedRef.current = true;
      toast.add({
        title: "Thời gian giữ ghế đã hết hạn, vui lòng chọn lại.",
        type: "warning",
      });
      if (!isResumeMode) resetToSelection();
    },
  });

  const seatTicketTypes = useSeatTicketTypes(showtimeSeats.data, selectedSeatIds);
  const confirmPriceChange = usePriceChangeConfirm();

  const holdSeatsMutation = useHoldSeats();
  const applyPaymentMutation = useApplyPayment(holdResult?.orderId ?? 0);
  const cancelOrderMutation = useCancelOrder();

  const cinemaSettings = useCinemaSettings();
  const loneSeats = useMemo(
    () =>
      findLoneSeats(
        showtimeSeats.data?.seats ?? [],
        seatStatusMap,
        selectedSeatIds,
        cinemaSettings.data?.seatGapRuleMode ?? SeatGapRuleMode.Strict,
      ),
    [
      showtimeSeats.data,
      seatStatusMap,
      selectedSeatIds,
      cinemaSettings.data?.seatGapRuleMode,
    ],
  );

  const combosPayload = useMemo(() => {
    const availableIds = activeCombos.data
      ? new Set(activeCombos.data.map((combo) => combo.comboId))
      : null;
    return Object.entries(comboQuantities)
      .filter(
        ([comboId, qty]) =>
          qty > 0 && (!availableIds || availableIds.has(Number(comboId))),
      )
      .map(([comboId, quantity]) => ({ comboId: Number(comboId), quantity }));
  }, [comboQuantities, activeCombos.data]);

  const toggleSeat = (showtimeSeatId: number) => {
    const allSeats = showtimeSeats.data?.seats ?? [];
    setSelectedSeatIds((prev) =>
      toggleSeatSelection(prev, allSeats, seatStatusMap, showtimeSeatId),
    );
  };

  const selectSeats = (ids: number[]) => setSelectedSeatIds(new Set(ids));

  const setComboQuantity = (comboId: number, quantity: number) => {
    updateComboQuantities((prev) => ({ ...prev, [comboId]: quantity }));
  };

  const effectiveCustomerName = customerName.trim() || (authUser?.fullName ?? '').trim();

  const canHold =
    selectedSeatIds.size > 0 &&
    loneSeats.length === 0 &&
    (!isAuthenticated || effectiveCustomerName.length > 0) &&
    (customerPhone.trim() === "" || isValidVnPhone(customerPhone.trim()));

  const holdAndContinue = async () => {
    if (!isAuthenticated) {
      promptLogin(
        "Bạn cần đăng nhập tài khoản để thực hiện đặt vé và giữ ghế. Bạn có muốn đăng nhập ngay không?",
      );
      return;
    }

    const submitHold = async (acceptPriceChange: boolean) => {
      const res = await holdSeatsMutation.mutateAsync({
        showtimeId,
        seatIds: Array.from(selectedSeatIds),
        combos: combosPayload.length > 0 ? combosPayload : undefined,
        customerName: customerName.trim(),
        customerPhone: customerPhone.trim(),
        tickets: seatTicketTypes.tickets,
        acceptPriceChange,
      });
      setFreshHoldResult(res.data);
      setStep("payment");
    };

    const recoverFromComboConflict = async (error: unknown) => {
      if (combosPayload.length === 0 || !isGenericConflictError(error)) return;
      const refreshed = await activeCombos.refetch();
      if (refreshed.data) pruneUnavailableCombos(refreshed.data, true);
      document
        .getElementById(BOOKING_COMBO_SECTION_ID)
        ?.scrollIntoView({ behavior: "smooth", block: "center" });
    };

    try {
      await submitHold(false);
    } catch (error) {
      const change = getPriceChangeRequired(error);
      if (!change) {
        await recoverFromComboConflict(error);
        return;
      }
      const accepted = await confirmPriceChange(
        change,
        seatTicketTypes.pricedSeats,
        seatTicketTypes.seatsTotal,
      );
      if (!accepted) {
        void showtimeSeats.refetch();
        return;
      }
      try {
        await submitHold(true);
      } catch (retryError) {
        await recoverFromComboConflict(retryError);
        return;
      }
    }
  };

  const resetToSelection = () => {
    setStep("select");
    setSelectedSeatIds(new Set());
    setFreshHoldResult(null);
    seatTicketTypes.reset();
  };

  const holdPollOrderId = freshHoldResult?.orderId ?? 0;
  const isHoldPolling = holdPollOrderId > 0 && step === "payment";

  useEffect(() => {
    if (!isHoldPolling) expiredNotifiedRef.current = false;
  }, [isHoldPolling]);

  useQuery({
    queryKey: [...queryKeys.orders.detail(holdPollOrderId), "hold-poll"],
    enabled: isHoldPolling,
    retry: false,
    meta: { skipErrorToast: true },
    refetchIntervalInBackground: true,
    refetchInterval: () =>
      expiredNotifiedRef.current ? false : BOOKING_HOLD_POLL_INTERVAL_MS,
    queryFn: async () => {
      try {
        const res = await ordersApi.getOrderDetail(holdPollOrderId);
        pollFailureCountRef.current = 0;
        if (
          res.data.status === OrderStatus.Expired &&
          !expiredNotifiedRef.current
        ) {
          expiredNotifiedRef.current = true;
          toast.add({
            title: "Thời gian giữ ghế đã hết hạn, vui lòng chọn lại.",
            type: "warning",
          });
          if (!isResumeMode) resetToSelection();
        }
        return res.data.status;
      } catch (error) {
        pollFailureCountRef.current += 1;
        if (pollFailureCountRef.current === BOOKING_HOLD_POLL_MAX_FAILURES) {
          toast.add({
            title: "Không kiểm tra được trạng thái giữ ghế.",
            description: "Kiểm tra kết nối mạng. Ghế vẫn được giữ đến khi hết hạn.",
            type: "warning",
          });
        }
        throw error;
      }
    },
  });

  const cancelHold = () => {
    if (!holdResult) return;
    cancelOrderMutation.mutate(
      {
        orderId: holdResult.orderId,
        reason: CancelReason.CustomerChangedMind,
      },
      {
        onSuccess: () => {
          if (!isResumeMode) resetToSelection();
        },
      },
    );
  };

  const submitPayment = () => {
    if (!holdResult) return;

    if (!isAuthenticated) {
      promptLogin(
        "Bạn cần đăng nhập tài khoản để thực hiện thanh toán. Bạn có muốn đăng nhập ngay không?",
      );
      return;
    }

    applyPaymentMutation.mutate(
      { paymentMethod },
      {
        onSuccess: (res) => {
          window.location.href = res.data.paymentUrl;
        },
      },
    );
  };

  return {
    step,
    isResumeMode,
    isResumeLoading: isResumeMode && resumeOrderQuery.isLoading,
    resumeInvalidReason,
    showtimeSeats,
    seatStatusMap,
    selectedSeatIds,
    toggleSeat,
    selectSeats,
    seatTicketTypes,
    loneSeats,
    comboQuantities,
    setComboQuantity,
    isAuthenticated,
    customerName,
    setCustomerName,
    customerPhone,
    setCustomerPhone,
    paymentMethod,
    setPaymentMethod,
    canHold,
    holdAndContinue,
    isHolding: holdSeatsMutation.isPending,
    holdResult,
    paymentOrder,
    cancelHold,
    isCancelling: cancelOrderMutation.isPending,
    submitPayment,
    isSubmittingPayment: applyPaymentMutation.isPending,
  };
}
