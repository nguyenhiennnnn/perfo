import { useState } from "react";
import { Send } from "lucide-react";
import { RatingStars } from "@/components/common/rating-stars";
import { Spinner } from "@/components/common/spinner";
import { Button } from "@/components/ui/button";
import { SURVEY_CRITERIA } from "../utils/survey-criteria";
import type { SubmitExperienceSurveyRequest, SurveyCriterionKey } from "../types";

interface ExperienceSurveyFormProps {
  isSubmitting?: boolean;
  onSubmit: (data: SubmitExperienceSurveyRequest) => void;
}

const EMPTY: Record<SurveyCriterionKey, number> = {
  roomRating: 0,
  soundRating: 0,
  pictureRating: 0,
  cleanlinessRating: 0,
  counterServiceRating: 0,
};

export const ExperienceSurveyForm = ({ isSubmitting, onSubmit }: ExperienceSurveyFormProps) => {
  const [ratings, setRatings] = useState(EMPTY);
  const isComplete = SURVEY_CRITERIA.every(({ key }) => ratings[key] > 0);

  return (
    <div className="space-y-3">
      <div className="divide-y divide-border">
        {SURVEY_CRITERIA.map(({ key, label }) => (
          <div key={key} className="flex items-center justify-between gap-3 py-2.5">
            <span className="text-sm text-foreground">{label}</span>
            <RatingStars value={ratings[key]} onChange={(value) => setRatings((prev) => ({ ...prev, [key]: value }))} />
          </div>
        ))}
      </div>
      <div className="flex justify-end">
        <Button disabled={!isComplete || isSubmitting} onClick={() => onSubmit(ratings)}>
          {isSubmitting ? <Spinner size="sm" decorative className="text-current" /> : <Send className="size-4" />}
          <span>Gửi khảo sát</span>
        </Button>
      </div>
    </div>
  );
};
