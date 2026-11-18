import { QRCodeSVG } from "qrcode.react";
import { cn } from "@/lib/utils";
import { TICKET_COLORS, TICKET_TITLE_MAX_CHARS } from "@/config/default-constants";

interface PerfoTicketProps {
  movieTitle: string;
  showtime: string;
  room: string;
  seat: string;
  totalAmount: string;
  ticketCode: string;
  qrValue: string;
  isCheckedIn?: boolean;
  className?: string;
}

export const PerfoTicket = ({
  movieTitle,
  showtime,
  room,
  seat,
  totalAmount,
  ticketCode,
  qrValue,
  isCheckedIn = false,
  className,
}: PerfoTicketProps) => {
  const title = movieTitle.length > TICKET_TITLE_MAX_CHARS ? movieTitle.slice(0, TICKET_TITLE_MAX_CHARS - 1) + "…" : movieTitle;
  const isImageQr = qrValue.startsWith("data:image") || qrValue.startsWith("http");

  return (
    <div className={cn("w-full max-w-3xl select-none", className)}>
      <svg
        xmlns="http://www.w3.org/2000/svg"
        viewBox="0 0 920 380"
        width="100%"
        role="img"
        aria-label={`Vé điện tử Perfo — ${movieTitle} — Ghế ${seat}`}
        className="block"
      >
        <defs>
          <clipPath id="stub-clip">
            <rect x="640" width="280" height="380" />
          </clipPath>
          <clipPath id="wordmark-clip">
            <rect x="-2" y="0" width="190" height="48" />
          </clipPath>
        </defs>

        <path
          d="M18 10H618A22 22 0 0 0 662 10H902A8 8 0 0 1 910 18V362A8 8 0 0 1 902 370H662A22 22 0 0 0 618 370H18A8 8 0 0 1 10 362V18A8 8 0 0 1 18 10Z"
          fill={TICKET_COLORS.card}
          stroke={TICKET_COLORS.ink}
          strokeOpacity="0.18"
          strokeWidth="2"
        />
        <path
          d="M18 10H618A22 22 0 0 0 662 10H902A8 8 0 0 1 910 18V362A8 8 0 0 1 902 370H662A22 22 0 0 0 618 370H18A8 8 0 0 1 10 362V18A8 8 0 0 1 18 10Z"
          fill={TICKET_COLORS.primary}
          clipPath="url(#stub-clip)"
        />

        {[40, 64, 88, 112, 136, 160, 184, 208, 232, 256, 280, 304, 328].map((cy) => (
          <circle key={cy} cx="640" cy={cy} r="3.5" fill={TICKET_COLORS.accent} />
        ))}

        <g transform="translate(46.8 26) scale(0.75)">
          <path
            d="M6 14H35A5 5 0 0 0 45 14H58A2 2 0 0 1 60 16V48A2 2 0 0 1 58 50H45A5 5 0 0 0 35 50H6A2 2 0 0 1 4 48V16A2 2 0 0 1 6 14Z"
            fill={TICKET_COLORS.primary}
          />
          <g fill={TICKET_COLORS.paper}>
            {[22, 27, 32, 37, 42].map((cy) => (
              <circle key={cy} cx="40" cy={cy} r="1.5" />
            ))}
            <rect x="47.5" y="24" width="2" height="16" />
            <rect x="51.5" y="26" width="1.2" height="12" />
            <rect x="54.5" y="24" width="2" height="16" />
          </g>
          <path
            d="M15 43V23.5H21.5L24 26V30.5L21.5 33H15"
            fill="none"
            stroke={TICKET_COLORS.paper}
            strokeWidth="5"
            strokeMiterlimit="10"
          />
        </g>

        <g transform="translate(100 36) scale(0.55)">
          <g clipPath="url(#wordmark-clip)" fill="none" stroke={TICKET_COLORS.ink} strokeWidth="9" strokeLinejoin="miter" strokeMiterlimit="10">
            <path transform="translate(0 0)" d="M4.5 48V4.5H23L27.5 9V24L23 28.5H4.5" />
            <path transform="translate(38 0)" d="M27.5 4.5H4.5V43.5H27.5M4.5 24H23" />
            <path transform="translate(76 0)" d="M4.5 48V4.5H23L27.5 9V24L23 28.5H4.5M14 28.5L30.3 52" />
            <path transform="translate(114 0)" d="M27.5 4.5H4.5V48M4.5 24H23" />
            <path transform="translate(152 0)" d="M9 4.5H23L27.5 9V39L23 43.5H9L4.5 39V9Z" />
          </g>
          <circle cx="168" cy="24" r="4.2" fill={TICKET_COLORS.primary} />
        </g>

        <text
          x="50" y="105"
          fontFamily="'Be Vietnam Pro', 'Segoe UI', Arial, sans-serif"
          fontSize="11" fontWeight="600" fill={TICKET_COLORS.muted}
          letterSpacing="2.5"
        >
          PHIM
        </text>
        <text
          x="50" y="140"
          fontFamily="'Archivo Variable', 'Arial Narrow', Impact, sans-serif"
          fontSize="32" fontWeight="700" fill={TICKET_COLORS.ink}
          letterSpacing="0.3"
        >
          {title}
        </text>

        <text
          x="50" y="176"
          fontFamily="'Be Vietnam Pro', 'Segoe UI', Arial, sans-serif"
          fontSize="11" fontWeight="600" fill={TICKET_COLORS.muted}
          letterSpacing="2.5"
        >
          SUẤT CHIẾU
        </text>
        <text
          x="50" y="204"
          fontFamily="'Archivo Variable', 'Arial Narrow', Impact, sans-serif"
          fontSize="20" fontWeight="600" fill={TICKET_COLORS.ink}
        >
          {showtime}
        </text>

        <text
          x="50" y="234"
          fontFamily="'Be Vietnam Pro', 'Segoe UI', Arial, sans-serif"
          fontSize="11" fontWeight="600" fill={TICKET_COLORS.muted}
          letterSpacing="2.5"
        >
          PHÒNG
        </text>
        <text
          x="50" y="262"
          fontFamily="'Archivo Variable', 'Arial Narrow', Impact, sans-serif"
          fontSize={room.length > 22 ? "17" : "20"} fontWeight="600" fill={TICKET_COLORS.ink}
        >
          {room}
        </text>

        <rect
          x="465" y="160"
          width="140" height="106"
          rx="4"
          fill={TICKET_COLORS.stub}
          stroke={TICKET_COLORS.ink}
          strokeOpacity="0.12"
          strokeWidth="1.5"
        />
        <text
          x="535" y="186"
          fontFamily="'Be Vietnam Pro', 'Segoe UI', Arial, sans-serif"
          fontSize="11" fontWeight="700" fill={TICKET_COLORS.muted}
          textAnchor="middle" letterSpacing="2.5"
        >
          GHẾ
        </text>
        <text
          x="535" y="236"
          fontFamily="'Archivo Variable', 'Arial Narrow', Impact, sans-serif"
          fontSize={seat.length > 6 ? "20" : seat.length > 3 ? "28" : "38"}
          fontWeight="800"
          fill={TICKET_COLORS.primary}
          textAnchor="middle"
        >
          {seat}
        </text>

        <path
          d="M50 286H605"
          stroke={TICKET_COLORS.ink}
          strokeOpacity="0.18"
          strokeDasharray="3 5"
          strokeWidth="2"
          strokeLinecap="round"
        />

        <text
          x="50" y="328"
          fontFamily="'Be Vietnam Pro', 'Segoe UI', Arial, sans-serif"
          fontSize="11" fontWeight="600" fill={TICKET_COLORS.muted}
          letterSpacing="2.5"
        >
          TỔNG TIỀN
        </text>
        <text
          x="160" y="332"
          fontFamily="'Archivo Variable', 'Arial Narrow', Impact, sans-serif"
          fontSize="30" fontWeight="700" fill={TICKET_COLORS.primary}
        >
          {totalAmount}
        </text>

        <foreignObject x="676" y="46" width="208" height="208">
          <div
            // @ts-expect-error xmlns needed for SVG foreignObject
            xmlns="http://www.w3.org/1999/xhtml"
            style={{
              width: "100%",
              height: "100%",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              background: TICKET_COLORS.paper,
              padding: "10px",
              boxSizing: "border-box",
            }}
          >
            {isImageQr ? (
              <img
                src={qrValue}
                alt={`Mã QR vé ${ticketCode}`}
                style={{ width: "100%", height: "100%", objectFit: "contain" }}
              />
            ) : (
              <QRCodeSVG
                value={qrValue}
                size={188}
                bgColor={TICKET_COLORS.paper}
                fgColor={TICKET_COLORS.ink}
                level="M"
              />
            )}
          </div>
        </foreignObject>

        <text
          x="780" y="290"
          fontFamily="'Archivo Variable', 'Arial Narrow', monospace"
          fontSize={ticketCode.length > 18 ? "13" : ticketCode.length > 14 ? "15" : "17"}
          fontWeight="700"
          fill={TICKET_COLORS.paper}
          textAnchor="middle"
          letterSpacing="1"
        >
          {ticketCode}
        </text>
        <text
          x="780" y="318"
          fontFamily="'Be Vietnam Pro', 'Segoe UI', Arial, sans-serif"
          fontSize="12" fontWeight="400" fill={TICKET_COLORS.paper}
          textAnchor="middle" opacity="0.8"
        >
          Quét mã tại quầy để vào rạp
        </text>

        {isCheckedIn && (
          <g>
            <rect x="10" y="10" width="630" height="360" fill={TICKET_COLORS.ink} fillOpacity="0.55" />
            <text
              x="325" y="210"
              fontFamily="'Archivo Variable', 'Arial Narrow', sans-serif"
              fontSize="56" fontWeight="800" fill={TICKET_COLORS.card}
              fillOpacity="0.9"
              textAnchor="middle"
              transform="rotate(-20, 325, 200)"
              letterSpacing="2"
            >
              ĐÃ SOÁT VÉ
            </text>
          </g>
        )}
      </svg>
    </div>
  );
};
