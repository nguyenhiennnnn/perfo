export interface SubmitExperienceSurveyRequest {
  roomRating: number;
  soundRating: number;
  pictureRating: number;
  cleanlinessRating: number;
  counterServiceRating: number;
}

export interface ExperienceSurveyDto extends SubmitExperienceSurveyRequest {
  surveyId: number;
  showtimeId: number;
  orderId: number;
  createdAt: string;
}

export type SurveyCriterionKey = keyof SubmitExperienceSurveyRequest;
