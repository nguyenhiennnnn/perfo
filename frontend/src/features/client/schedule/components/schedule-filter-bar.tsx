import { Chip, ChipGroup } from "@/components/common/chip";

interface ScheduleFilterBarProps {
  formats: string[];
  activeFormat: string | null;
  onFormatChange: (format: string | null) => void;
}

export const ScheduleFilterBar = ({ formats, activeFormat, onFormatChange }: ScheduleFilterBarProps) => {
  if (formats.length === 0) return null;

  return (
    <ChipGroup aria-label="Định dạng chiếu">
      <Chip selected={activeFormat === null} onClick={() => onFormatChange(null)}>
        Tất cả định dạng
      </Chip>
      {formats.map((format) => (
        <Chip key={format} selected={activeFormat === format} onClick={() => onFormatChange(format)}>
          {format}
        </Chip>
      ))}
    </ChipGroup>
  );
}