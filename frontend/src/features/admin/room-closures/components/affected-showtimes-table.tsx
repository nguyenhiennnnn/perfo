import { ArrowRightLeft, Ban } from "lucide-react";

import { Button } from "@/components/ui/button";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { formatDate, formatNumber, formatTime } from "@/lib/formatters";

import type { AffectedShowtimeDto } from "../types";

interface AffectedShowtimesTableProps {
  showtimes: AffectedShowtimeDto[];
  onChangeRoom: (showtime: AffectedShowtimeDto) => void;
  onCancel: (showtime: AffectedShowtimeDto) => void;
  readOnly?: boolean;
}

export const AffectedShowtimesTable = ({
  showtimes,
  onChangeRoom,
  onCancel,
  readOnly = false,
}: AffectedShowtimesTableProps) => (
  <Table>
    <TableHeader>
      <TableRow>
        <TableHead>Suất chiếu</TableHead>
        <TableHead>Giờ chiếu</TableHead>
        <TableHead className="text-right">Đơn</TableHead>
        <TableHead className="text-right">Khách</TableHead>
        <TableHead className="text-right">Vé</TableHead>
        {!readOnly && <TableHead className="text-right">Xử lý</TableHead>}
      </TableRow>
    </TableHeader>
    <TableBody>
      {showtimes.map((showtime) => (
        <TableRow key={showtime.showtimeId}>
          <TableCell>
            <div className="flex flex-col">
              <span className="font-medium text-foreground">{showtime.movieTitle}</span>
              <span className="text-xs text-muted-foreground tabular-nums">
                #{showtime.showtimeId}
              </span>
            </div>
          </TableCell>
          <TableCell>
            <div className="flex flex-col text-sm tabular-nums">
              <span className="text-foreground">{formatDate(showtime.startTime)}</span>
              <span className="text-muted-foreground">
                {formatTime(showtime.startTime)} - {formatTime(showtime.endTime)}
              </span>
            </div>
          </TableCell>
          <TableCell className="text-right tabular-nums">{formatNumber(showtime.orderCount)}</TableCell>
          <TableCell className="text-right tabular-nums">{formatNumber(showtime.customerCount)}</TableCell>
          <TableCell className="text-right tabular-nums">{formatNumber(showtime.ticketCount)}</TableCell>
          {!readOnly && (
            <TableCell>
              <div className="flex justify-end gap-2">
                <Button variant="outline" size="sm" onClick={() => onChangeRoom(showtime)}>
                  <ArrowRightLeft className="size-4" />
                  <span>Chuyển phòng</span>
                </Button>
                <Button variant="destructive" size="sm" onClick={() => onCancel(showtime)}>
                  <Ban className="size-4" />
                  <span>Hủy suất</span>
                </Button>
              </div>
            </TableCell>
          )}
        </TableRow>
      ))}
    </TableBody>
  </Table>
);
