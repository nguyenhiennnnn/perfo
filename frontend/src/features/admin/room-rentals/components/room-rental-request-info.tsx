import type { ReactNode } from "react";
import { ROOM_RENTAL_AUDIENCE_LABELS } from "@/features/client/room-rentals/utils/room-rental-labels";
import { formatDateTime } from "@/lib/formatters";
import type { RoomRentalAdminDto } from "../types";

const InfoRow = ({ label, children }: { label: string; children: ReactNode }) => (
  <div className="grid grid-cols-[8.5rem_minmax(0,1fr)] gap-2 py-1.5 text-sm">
    <dt className="text-muted-foreground">{label}</dt>
    <dd className="break-words text-foreground">{children}</dd>
  </div>
);

export const RoomRentalRequestInfo = ({ request }: { request: RoomRentalAdminDto }) => (
  <section className="space-y-2 cut-md bg-card p-4">
    <h2 className="t-h3 text-foreground">Thông tin yêu cầu</h2>
    <dl className="divide-y divide-border">
      <InfoRow label="Khách hàng">{request.fullName}</InfoRow>
      <InfoRow label="Đối tượng">
        {ROOM_RENTAL_AUDIENCE_LABELS[request.audienceType]}
        {request.organizationName ? ` · ${request.organizationName}` : ""}
      </InfoRow>
      <InfoRow label="Liên hệ">
        <a href={`tel:${request.contactPhone}`} className="underline-offset-4 hover:underline">
          {request.contactPhone}
        </a>
        {" · "}
        <a href={`mailto:${request.contactEmail}`} className="underline-offset-4 hover:underline">
          {request.contactEmail}
        </a>
      </InfoRow>
      <InfoRow label="Ngày giờ mong muốn">
        <span className="tabular-nums">{formatDateTime(request.desiredStartAt)}</span>
      </InfoRow>
      {request.flexibleFromAt && request.flexibleToAt && (
        <InfoRow label="Khoảng dự phòng">
          <span className="tabular-nums">
            {formatDateTime(request.flexibleFromAt)} – {formatDateTime(request.flexibleToAt)}
          </span>
        </InfoRow>
      )}
      <InfoRow label="Số người">{request.attendeeCount}</InfoRow>
      <InfoRow label="Nội dung chiếu">
        {request.bringsOwnContent
          ? `Khách tự mang nội dung${request.contentNote ? `: ${request.contentNote}` : ""}`
          : (request.movieTitle ?? "Chưa chọn phim")}
      </InfoRow>
      <InfoRow label="Định dạng phòng">{request.screenTypeName ?? "Không yêu cầu"}</InfoRow>
      <InfoRow label="Combo mong muốn">{request.comboRequest ?? "Không"}</InfoRow>
      <InfoRow label="Yêu cầu khác">{request.notes ?? "Không"}</InfoRow>
      <InfoRow label="Gửi lúc">
        <span className="tabular-nums">{formatDateTime(request.createdAt)}</span>
      </InfoRow>
      {request.statusNote && <InfoRow label="Ghi chú trạng thái">{request.statusNote}</InfoRow>}
    </dl>
  </section>
);
