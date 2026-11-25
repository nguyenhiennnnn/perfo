import type { SurveyCriterionKey } from "../types";

export const SURVEY_CRITERIA: ReadonlyArray<{ key: SurveyCriterionKey; label: string }> = [
  { key: "roomRating", label: "Phòng chiếu" },
  { key: "soundRating", label: "Âm thanh" },
  { key: "pictureRating", label: "Hình ảnh" },
  { key: "cleanlinessRating", label: "Vệ sinh" },
  { key: "counterServiceRating", label: "Dịch vụ quầy" },
];
