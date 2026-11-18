import type { CSSProperties } from "react";
import { QRCodeSVG } from "qrcode.react";
import { BrandMark, BrandWordmark } from "@/components/common/brand-mark";
import { BRAND } from "@/config/brand";
import { formatCurrency, formatDate, formatTime } from "@/lib/formatters";
import { cn } from "@/lib/utils";

interface PerfoTicketProps {
  movieTitle: string;
  startTime: string;
  room: string;
  seat: string;
  price: number;
  ticketCode: string;
  qrValue: string;
  isCheckedIn?: boolean;
  isTorn?: boolean;
  className?: string;
}

const SEAM_CENTER_STYLE = { "--x": "50%" } as CSSProperties;

export const PerfoTicket = ({
  movieTitle,
  startTime,
  room,
  seat,
  price,
  ticketCode,
  qrValue,
  isCheckedIn = false,
  isTorn = false,
  className,
}: PerfoTicketProps) => {
  const isImageQr = qrValue.startsWith("data:image") || qrValue.startsWith("http");

  return (
    <div className={cn("@container w-full pr-4 pb-3", className)}>
      <div
        role="group"
        aria-label={`Vé điện tử ${BRAND.name}, ${movieTitle}, ghế ${seat}`}
        className="flex flex-col @xl:flex-row"
      >
        <div className="relative flex flex-1 flex-col gap-5 bg-card p-5 cut-lg @xl:p-6">
          <div className="flex items-center gap-2">
            <BrandMark className="size-6 shrink-0" />
            <BrandWordmark className="h-4 w-auto" />
          </div>

          <p className="t-h1 line-clamp-2 text-balance text-foreground">{movieTitle}</p>

          <div>
            <p className="t-numeral text-5xl text-foreground @xl:text-[3.5rem]">{formatTime(startTime)}</p>
            <p className="mt-1 text-sm text-muted-foreground">{formatDate(startTime)}</p>
          </div>

          <dl className="mt-auto grid grid-cols-2 gap-x-6 gap-y-3 @md:grid-cols-[1fr_auto_auto]">
            <div className="col-span-2 min-w-0 @md:col-span-1">
              <dt className="text-xs text-muted-foreground">Phòng</dt>
              <dd className="t-code text-xl wrap-break-word text-foreground">{room}</dd>
            </div>
            <div>
              <dt className="text-xs text-muted-foreground">Ghế</dt>
              <dd className="t-code text-xl wrap-break-word text-primary-text">{seat}</dd>
            </div>
            <div>
              <dt className="text-xs text-muted-foreground">Giá vé</dt>
              <dd className="t-code text-xl text-foreground">{formatCurrency(price)}</dd>
            </div>
          </dl>

          {isCheckedIn && (
            <div className="absolute inset-0 flex items-center justify-center bg-foreground/40">
              <p className="t-h2 bg-foreground px-4 py-2 text-background">Đã soát vé</p>
            </div>
          )}
        </div>

        <div aria-hidden className="notch-x flex h-6 items-center bg-card px-6 @xl:hidden">
          <div className="perf-h w-full" />
        </div>
        <div
          aria-hidden
          style={SEAM_CENTER_STYLE}
          className="notch-y hidden w-6 justify-center bg-card py-6 @xl:flex"
        >
          <div className="perf-v h-full" />
        </div>

        <div
          data-torn={isTorn}
          className={cn(
            "tear-stub flex flex-col items-center justify-center gap-3 bg-primary p-5 text-primary-foreground cut-lg cut-flip @xl:w-60 @xl:shrink-0",
            isCheckedIn && "opacity-60",
          )}
        >
          <div className="size-40 bg-primary-foreground p-2 text-scrim">
            {isImageQr ? (
              <img src={qrValue} alt={`Mã QR vé ${ticketCode}`} className="size-full object-contain" />
            ) : (
              <QRCodeSVG
                value={qrValue}
                title={`Mã QR vé ${ticketCode}`}
                bgColor="transparent"
                fgColor="currentColor"
                level="M"
                className="size-full"
              />
            )}
          </div>
          <p className="t-code text-center text-sm break-all">{ticketCode}</p>
          <p className="text-center text-xs">Quét mã tại quầy để vào rạp</p>
        </div>
      </div>
    </div>
  );
};
