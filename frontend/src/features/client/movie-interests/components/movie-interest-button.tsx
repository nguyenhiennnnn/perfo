import { useId, type MouseEvent } from "react";
import { useLocation, useNavigate } from "react-router";
import { Bell, BellRing } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import { Spinner } from "@/components/common/spinner";
import { LineSkeleton } from "@/components/common/skeleton-shapes";
import { ROUTES } from "@/config/routes";
import { cn } from "@/lib/utils";
import { useAlertDialogOpen } from "@/stores/alert-dialog-store";
import { useAuthRole, useIsAuthenticated } from "@/stores/auth-store";
import { MovieStatus, Role } from "@/types/enum";
import {
  useAddMovieInterest,
  useMyMovieInterests,
  useRemoveMovieInterest,
} from "../hooks/use-movie-interests";

const INTEREST_HINT = "Chúng tôi sẽ báo khi phim mở bán suất chiếu đầu tiên.";

interface MovieInterestButtonProps {
  movieId: number;
  status: MovieStatus;
  variant?: "card" | "hero";
  className?: string;
}

export const MovieInterestButton = ({
  movieId,
  status,
  variant = "card",
  className,
}: MovieInterestButtonProps) => {
  const navigate = useNavigate();
  const location = useLocation();
  const isAuthenticated = useIsAuthenticated();
  const role = useAuthRole();
  const openAlertDialog = useAlertDialogOpen();
  const hintId = useId();
  const { data: interestedIds, isLoading } = useMyMovieInterests();
  const add = useAddMovieInterest();
  const remove = useRemoveMovieInterest();

  if (status !== MovieStatus.ComingSoon) return null;
  if (isAuthenticated && role !== Role.Customer) return null;

  const isHero = variant === "hero";

  if (isAuthenticated && isLoading) {
    return <LineSkeleton className={cn(isHero ? "h-12 w-36" : "mt-3 h-8 w-full", className)} />;
  }

  const interested = interestedIds?.has(movieId) ?? false;
  const isPending = add.isPending || remove.isPending;

  const handleClick = (event: MouseEvent<HTMLButtonElement>) => {
    event.preventDefault();
    event.stopPropagation();
    if (isPending) return;

    if (!isAuthenticated) {
      const redirectTo = encodeURIComponent(`${location.pathname}${location.search}`);
      openAlertDialog({
        title: "Yêu cầu đăng nhập",
        description: "Bạn cần đăng nhập để nhận thông báo khi phim mở bán. Bạn có muốn đăng nhập ngay không?",
        confirmText: "Đăng nhập ngay",
        cancelText: "Để sau",
        onConfirm: () => navigate(`${ROUTES.auth.login}?redirect=${redirectTo}`),
      });
      return;
    }

    if (interested) remove.mutate(movieId);
    else add.mutate(movieId);
  };

  const Icon = interested ? BellRing : Bell;
  const button = (
    <Button
      type="button"
      variant={interested ? "default" : "outline"}
      size={isHero ? "lg" : "sm"}
      className={cn(!isHero && "mt-3 w-full", !isHero && className)}
      aria-pressed={interested}
      aria-busy={isPending}
      aria-describedby={isHero ? hintId : undefined}
      disabled={isPending}
      onClick={handleClick}
    >
      {isPending ? <Spinner size="sm" decorative className="text-current" /> : <Icon />}
      <span>{interested ? "Đã quan tâm" : "Quan tâm"}</span>
    </Button>
  );

  if (isHero) {
    return (
      <div className={cn("flex flex-wrap items-center gap-x-3 gap-y-1.5", className)}>
        {button}
        <p id={hintId} className="max-w-xs text-xs text-muted-foreground">
          {INTEREST_HINT}
        </p>
      </div>
    );
  }

  return (
    <Tooltip>
      <TooltipTrigger render={button} />
      <TooltipContent>{INTEREST_HINT}</TooltipContent>
    </Tooltip>
  );
};
