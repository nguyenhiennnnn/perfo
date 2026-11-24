import { SelectField } from "@/components/common/select-field";
import { Skeleton } from "@/components/ui/skeleton";
import { LOOKUP_PAGE_SIZE } from "@/config/default-constants";
import { useAdminRooms } from "@/features/admin/rooms/hooks/use-admin-rooms";

interface RoomChangeTargetPickerProps {
  sourceRoomId: number;
  value: number | null;
  disabled?: boolean;
  onChange: (roomId: number) => void;
}

export const RoomChangeTargetPicker = ({
  sourceRoomId,
  value,
  disabled,
  onChange,
}: RoomChangeTargetPickerProps) => {
  const rooms = useAdminRooms({ pageSize: LOOKUP_PAGE_SIZE });

  if (rooms.isLoading) return <Skeleton className="h-10 w-full" />;

  const options = (rooms.data?.items ?? []).filter(
    (room) => room.isActive && room.roomId !== sourceRoomId,
  );

  return (
    <div className="flex flex-col gap-1.5">
      <p className="text-sm font-medium text-foreground">Phòng chiếu đích</p>
      <SelectField
        value={value}
        onChange={(roomId) => onChange(Number(roomId))}
        options={options}
        getOptionValue={(room) => room.roomId}
        getOptionLabel={(room) =>
          `${room.roomName} · ${room.totalSeats} ghế${
            room.screenTypeName ? ` · ${room.screenTypeName}` : ""
          }`
        }
        placeholder={
          options.length === 0 ? "Không có phòng khác đang hoạt động" : "Chọn phòng để xem trước"
        }
        disabled={disabled || options.length === 0}
      />
      <p className="text-xs text-muted-foreground">
        Phòng đích phải trống trong khung giờ của suất, không bị đóng và đủ ghế cho khách.
      </p>
    </div>
  );
};
