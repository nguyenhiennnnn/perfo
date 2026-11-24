import type { InvalidationName } from "@/api/query-invalidation";
import type { ImportSummary } from "@/types/import";

export interface ImportColumnConfig {
  key: string;
  header: string;
}

export interface ImportWizardConfig {
  title: string;
  description: string;
  baseUrl: string;
  itemLabel: string;
  templateFilename: string;
  errorsFilename: string;
  requestParams?: Record<string, string | number>;
  columns: ImportColumnConfig[];
  invalidates: InvalidationName;
  summarizeApply?: (data: unknown) => ImportSummary;
}
