import { useMemo, useState } from "react";
import { useLocation, useNavigate } from "react-router";
import { ROUTES } from "@/config/routes";
import { getServerNow } from "@/lib/server-clock";
import { useAlertDialogOpen } from "@/stores/alert-dialog-store";
import { useIsAuthenticated } from "@/stores/auth-store";
import { CancelReason, OrderStatus, OrderType, PaymentMethod } from "@/types/enum";
import {
  useApplyPayment,
  useCancelOrder,
  useOrderDetail,
} from "@/features/client/orders/hooks/use-orders";
import type { HoldOrderResponse } from "@/features/client/orders/types";
import { usePurchaseMembershipPlan } from "./use-membership-plans";

export function useMembershipPurchaseFlow(resumeOrderId?: number) {
  const isResumeMode = !!resumeOrderId && resumeOrderId > 0;

  const [freshHoldResult, setFreshHoldResult] = useState<HoldOrderResponse | null>(null);
  const [paymentMethod, setPaymentMethod] = useState<PaymentMethod>(PaymentMethod.VNPay);

  const isAuthenticated = useIsAuthenticated();
  const navigate = useNavigate();
  const location = useLocation();
  const openAlertDialog = useAlertDialogOpen();

  const purchaseMutation = usePurchaseMembershipPlan();
  const cancelOrderMutation = useCancelOrder();

  const resumeOrderQuery = useOrderDetail(isResumeMode ? resumeOrderId : 0);

  const isResumeInvalid = useMemo(() => {
    if (!isResumeMode) return false;
    if (resumeOrderQuery.isError) return true;
    const order = resumeOrderQuery.data;
    if (!order) return false;
    return (
      order.type !== OrderType.Membership ||
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

  const promptLogin = () => {
    const redirectTo = encodeURIComponent(`${location.pathname}${location.search}`);
    openAlertDialog({
      title: "Yêu cầu đăng nhập",
      description: "Bạn cần đăng nhập tài khoản để mua gói thành viên. Bạn có muốn đăng nhập ngay không?",
      confirmText: "Đăng nhập ngay",
      cancelText: "Để sau",
      onConfirm: () => {
        navigate(`${ROUTES.auth.login}?redirect=${redirectTo}`);
      },
    });
  };

  const purchase = (planId: number) => {
    if (!isAuthenticated) {
      promptLogin();
      return;
    }
    purchaseMutation.mutate(planId, {
      onSuccess: (res) => {
        setFreshHoldResult(res.data);
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
            navigate(ROUTES.membershipPlans, { replace: true });
            return;
          }
          setFreshHoldResult(null);
        },
      },
    );
  };

  return {
    isResumeMode,
    isResumeLoading: isResumeMode && resumeOrderQuery.isLoading,
    isResumeInvalid,
    isAuthenticated,
    purchase,
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
