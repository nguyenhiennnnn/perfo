import { useMemo, useState } from "react";
import { useLocation, useNavigate } from "react-router";
import { ROUTES } from "@/config/routes";
import { getServerNow } from "@/lib/server-clock";
import { useAlertDialogOpen } from "@/stores/alert-dialog-store";
import { useIsAuthenticated } from "@/stores/auth-store";
import { CancelReason, OrderStatus, OrderType, PaymentMethod } from "@/types/enum";
import { useCinemaSettings } from "@/features/client/settings/hooks/use-settings";
import {
  useApplyPayment,
  useCancelOrder,
  useOrderDetail,
} from "@/features/client/orders/hooks/use-orders";
import type { HoldOrderResponse } from "@/features/client/orders/types";
import type { PurchaseGiftCardRequest } from "../types";
import { usePurchaseGiftCard } from "./use-gift-cards";

export type GiftCardPurchaseStep = "form" | "payment";

export function useGiftCardPurchaseFlow(resumeOrderId?: number) {
  const isResumeMode = !!resumeOrderId && resumeOrderId > 0;

  const [localStep, setLocalStep] = useState<GiftCardPurchaseStep>("form");
  const [freshHoldResult, setFreshHoldResult] = useState<HoldOrderResponse | null>(null);
  const [paymentMethod, setPaymentMethod] = useState<PaymentMethod>(PaymentMethod.VNPay);

  const isAuthenticated = useIsAuthenticated();
  const navigate = useNavigate();
  const location = useLocation();
  const openAlertDialog = useAlertDialogOpen();

  const settings = useCinemaSettings();
  const purchaseMutation = usePurchaseGiftCard();
  const cancelOrderMutation = useCancelOrder();

  const resumeOrderQuery = useOrderDetail(isResumeMode ? resumeOrderId : 0);

  const isResumeInvalid = useMemo(() => {
    if (!isResumeMode) return false;
    if (resumeOrderQuery.isError) return true;
    const order = resumeOrderQuery.data;
    if (!order) return false;
    return (
      order.type !== OrderType.GiftCard ||
      order.status !== OrderStatus.Pending ||
      !order.expiresAt ||
      new Date(order.expiresAt).getTime() <= getServerNow()
    );
  }, [isResumeMode, resumeOrderQuery.isError, resumeOrderQuery.data]);

  const holdResult = useMemo<HoldOrderResponse | null>(() => {
    if (!isResumeMode) return freshHoldResult;
    const order = resumeOrderQuery.data;
    if (!order || !order.expiresAt || isResumeInvalid) return null;
    return {
      orderId: order.orderId,
      orderCode: order.orderCode,
      status: order.status,
      totalAmount: order.totalAmount,
      discountAmount: order.discountAmount,
      finalAmount: order.finalAmount,
      expiresAt: order.expiresAt,
    };
  }, [isResumeMode, freshHoldResult, resumeOrderQuery.data, isResumeInvalid]);

  const paymentOrder = useOrderDetail(holdResult?.orderId ?? 0, {
    alwaysRefetchOnMount: true,
  });
  const applyPaymentMutation = useApplyPayment(holdResult?.orderId ?? 0);

  const promptLogin = (description: string) => {
    const redirectTo = encodeURIComponent(`${location.pathname}${location.search}`);
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

  const submitPurchase = (
    body: PurchaseGiftCardRequest,
    onServerError: (error: unknown) => boolean,
  ) => {
    if (!isAuthenticated) {
      promptLogin(
        "Bạn cần đăng nhập tài khoản để mua thẻ quà tặng. Bạn có muốn đăng nhập ngay không?",
      );
      return;
    }
    purchaseMutation.mutate(body, {
      onSuccess: (res) => {
        setFreshHoldResult(res.data);
        setLocalStep("payment");
      },
      onError: (error) => {
        onServerError(error);
      },
    });
  };

  const submitPayment = () => {
    if (!holdResult) return;
    applyPaymentMutation.mutate(
      { paymentMethod },
      {
        onSuccess: (res) => {
          window.location.href = res.data.paymentUrl;
        },
      },
    );
  };

  const cancelHold = () => {
    if (!holdResult) return;
    cancelOrderMutation.mutate(
      { orderId: holdResult.orderId, reason: CancelReason.CustomerChangedMind },
      {
        onSuccess: () => {
          if (isResumeMode) {
            navigate(ROUTES.giftCards, { replace: true });
            return;
          }
          setFreshHoldResult(null);
          setLocalStep("form");
        },
      },
    );
  };

  return {
    step: isResumeMode ? ("payment" as const) : localStep,
    isResumeMode,
    isResumeLoading: isResumeMode && resumeOrderQuery.isLoading,
    isResumeInvalid,
    settings,
    submitPurchase,
    isPurchasing: purchaseMutation.isPending,
    holdResult,
    paymentOrder,
    paymentMethod,
    setPaymentMethod,
    submitPayment,
    isSubmittingPayment: applyPaymentMutation.isPending,
    cancelHold,
    isCancelling: cancelOrderMutation.isPending,
  };
}
