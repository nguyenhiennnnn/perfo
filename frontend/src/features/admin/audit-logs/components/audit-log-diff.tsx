import { useMemo, useState } from "react";

import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

interface AuditLogDiffProps {
  beforeJson?: string | null;
  afterJson?: string | null;
}

type JsonRecord = Record<string, unknown>;

interface DiffRow {
  key: string;
  before: unknown;
  after: unknown;
  changed: boolean;
}

const parseRecord = (json?: string | null): JsonRecord | null => {
  if (!json) return null;
  try {
    const value: unknown = JSON.parse(json);
    return value !== null && typeof value === "object" && !Array.isArray(value)
      ? (value as JsonRecord)
      : { value };
  } catch {
    return { value: json };
  }
};

const formatValue = (value: unknown): string => {
  if (value === undefined || value === null) return "—";
  if (typeof value === "boolean") return value ? "Có" : "Không";
  if (typeof value === "object") return JSON.stringify(value);
  return String(value);
};

const buildRows = (before: JsonRecord | null, after: JsonRecord | null): DiffRow[] => {
  const keys = [...new Set([...Object.keys(before ?? {}), ...Object.keys(after ?? {})])];
  return keys.map((key) => {
    const b = before?.[key];
    const a = after?.[key];
    return { key, before: b, after: a, changed: JSON.stringify(b) !== JSON.stringify(a) };
  });
};

export const AuditLogDiff = ({ beforeJson, afterJson }: AuditLogDiffProps) => {
  const [showAll, setShowAll] = useState(false);

  const rows = useMemo(
    () => buildRows(parseRecord(beforeJson), parseRecord(afterJson)),
    [beforeJson, afterJson],
  );

  const hasBoth = Boolean(beforeJson) && Boolean(afterJson);
  const changedCount = rows.filter((r) => r.changed).length;
  const visibleRows = hasBoth && !showAll ? rows.filter((r) => r.changed) : rows;

  if (rows.length === 0) {
    return <p className="py-6 text-center text-sm text-muted-foreground">Không có dữ liệu.</p>;
  }

  return (
    <div className="space-y-3">
      {hasBoth && (
        <div className="flex items-center justify-between gap-2">
          <span className="text-sm text-muted-foreground">
            {changedCount} trường thay đổi / {rows.length} trường
          </span>
          <Button variant="outline" size="sm" onClick={() => setShowAll((v) => !v)}>
            {showAll ? "Chỉ xem thay đổi" : "Xem tất cả"}
          </Button>
        </div>
      )}

      <div className="hidden grid-cols-[minmax(0,1fr)_minmax(0,1fr)_minmax(0,1fr)] gap-3 px-3 text-xs font-medium text-muted-foreground sm:grid">
        <span>Trường</span>
        <span>Giá trị trước</span>
        <span>Giá trị sau</span>
      </div>

      <div className="max-h-[50vh] divide-y divide-border overflow-y-auto rounded-lg border border-border">
        {visibleRows.map((row) => (
          <div
            key={row.key}
            className={cn(
              "grid grid-cols-1 gap-1 px-3 py-2 text-sm sm:grid-cols-[minmax(0,1fr)_minmax(0,1fr)_minmax(0,1fr)] sm:gap-3",
              row.changed && "bg-warning/5",
            )}
          >
            <span className="break-words font-mono text-xs text-foreground">{row.key}</span>
            <span
              className={cn(
                "break-words tabular-nums",
                row.changed ? "text-destructive" : "text-muted-foreground",
              )}
            >
              <span className="mr-1 text-xs text-muted-foreground sm:hidden">Trước:</span>
              {formatValue(row.before)}
            </span>
            <span
              className={cn(
                "break-words tabular-nums",
                row.changed ? "font-medium text-success" : "text-muted-foreground",
              )}
            >
              <span className="mr-1 text-xs text-muted-foreground sm:hidden">Sau:</span>
              {formatValue(row.after)}
            </span>
          </div>
        ))}
      </div>
    </div>
  );
};
