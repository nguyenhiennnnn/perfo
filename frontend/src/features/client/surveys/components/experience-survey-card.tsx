import { CheckCircle2 } from "lucide-react";
import { RatingStars } from "@/components/common/rating-stars";
import { Skeleton } from "@/components/ui/skeleton";
import { isNotFoundError } from "@/lib/error";
import { formatDateTime } from "@/lib/formatters";
import { useMyExperienceSurvey, useSubmitExperienceSurvey } from "../hooks/use-surveys";
import { SURVEY_CRITERIA } from "../utils/survey-criteria";
import { ExperienceSurveyForm } from "./experience-survey-form";

interface ExperienceSurveyCardProps {
  showtimeId: number;
  title?: string;
}

export const ExperienceSurveyCard = ({ showtimeId, title = "Khảo sát trải nghiệm tại rạp" }: ExperienceSurveyCardProps) => {
  const surveyQuery = useMyExperienceSurvey(showtimeId);
  const submit = useSubmitExperienceSurvey(showtimeId);

  if (surveyQuery.isLoading) return <Skeleton className="h-48 w-full" />;

  const survey = surveyQuery.data;
  const canSubmit = !survey && surveyQuery.isError && isNotFoundError(surveyQuery.error);
  if (!survey && !canSubmit) return null;

  return (
    <section className="cut-md bg-card p-4" aria-labelledby={`survey-title-${showtimeId}`}>
      <h3 id={`survey-title-${showtimeId}`} className="t-h3 text-foreground">
        {title}
      </h3>

      {survey ? (
        <div className="mt-3">
          <p className="flex items-center gap-2 text-sm text-success">
            <CheckCircle2 className="size-4" />
            Bạn đã gửi khảo sát lúc {formatDateTime(survey.createdAt)}. Cảm ơn bạn!
          </p>
          <div className="mt-2 divide-y divide-border">
            {SURVEY_CRITERIA.map(({ key, label }) => (
              <div key={key} className="flex items-center justify-between gap-3 py-2">
                <span className="text-sm text-muted-foreground">{label}</span>
                <RatingStars value={survey[key]} size="sm" />
              </div>
            ))}
          </div>
        </div>
      ) : (
        <div className="mt-1">
          <p className="text-sm text-muted-foreground">Chấm từ 1 đến 5 sao để giúp rạp cải thiện chất lượng phục vụ.</p>
          <ExperienceSurveyForm isSubmitting={submit.isPending} onSubmit={(body) => submit.mutate(body)} />
        </div>
      )}
    </section>
  );
};
