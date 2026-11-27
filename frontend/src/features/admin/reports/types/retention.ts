export interface RetentionMonthDto {
  monthOffset: number;
  returnedCustomers: number;
  returnRatePercent: number | null;
  isComplete: boolean;
}

export interface RetentionCohortDto {
  cohortMonth: string;
  cohortSize: number;
  months: RetentionMonthDto[];
}

export interface RetentionReportDto {
  cohorts: RetentionCohortDto[];
  updatedAt: string | null;
}
