import { rowLabelToIndex, seatCellKey } from "@/components/common/seat-map/seat-map-types";
import type { EditorCell, ValidationIssue } from "../types/seat-editor";

const cellKeyOf = (c: EditorCell) => seatCellKey(c.seatRow, c.seatNumber);

function validateBounds(list: EditorCell[], rows: number, cols: number): ValidationIssue[] {
  return list
    .filter((c) => {
      const rowIndex = rowLabelToIndex(c.seatRow);
      return rowIndex < 0 || rowIndex >= rows || c.seatNumber < 1 || c.seatNumber > cols;
    })
    .map((c) => ({
      level: "error",
      message: `Ghế ${c.seatRow}${c.seatNumber} nằm ngoài kích thước phòng đã khai báo (${rows} hàng x ${cols} cột).`,
      cellKeys: [cellKeyOf(c)],
    }));
}

function validateDuplicates(list: EditorCell[]): ValidationIssue[] {
  const issues: ValidationIssue[] = [];
  const seenKeys = new Set<string>();
  list.forEach((c) => {
    const key = cellKeyOf(c);
    if (seenKeys.has(key)) {
      issues.push({
        level: "error",
        message: `Ghế ${c.seatRow}${c.seatNumber} bị trùng vị trí.`,
        cellKeys: [key],
      });
    }
    seenKeys.add(key);
  });
  return issues;
}

function validateCouples(list: EditorCell[], coupleTypeIds: ReadonlySet<number>): ValidationIssue[] {
  const issues: ValidationIssue[] = [];
  const groups = new Map<string, EditorCell[]>();
  list.forEach((c) => {
    if (!c.coupleId) return;
    groups.set(c.coupleId, [...(groups.get(c.coupleId) ?? []), c]);
  });

  groups.forEach((group) => {
    const cellKeys = group.map(cellKeyOf);
    if (group.length !== 2) {
      const where = group[0] ? `hàng ${group[0].seatRow}` : "";
      issues.push({
        level: "error",
        message: `Có cặp ghế đôi ${where} gồm ${group.length} ghế thay vì 2 (ghế đôi bị lẻ).`,
        cellKeys,
      });
      return;
    }

    const [a, b] = group.sort((x, y) => x.seatNumber - y.seatNumber);
    const range = `${a.seatRow}${a.seatNumber} - ${b.seatRow}${b.seatNumber}`;

    if (a.seatRow !== b.seatRow || b.seatNumber - a.seatNumber !== 1) {
      issues.push({
        level: "error",
        message: `Cặp ghế đôi ${range} không liền kề nhau.`,
        cellKeys,
      });
    }
    if (a.isAisle || b.isAisle) {
      issues.push({
        level: "error",
        message: `Cặp ghế đôi ${range} nằm trên ô lối đi.`,
        cellKeys,
      });
    }
    if (a.seatTypeId !== b.seatTypeId) {
      issues.push({
        level: "error",
        message: `Hai ghế của cặp ghế đôi ${range} khác loại ghế.`,
        cellKeys,
      });
    }
    if (a.isActive !== b.isActive) {
      issues.push({
        level: "error",
        message: `Hai ghế của cặp ghế đôi ${range} khác trạng thái bật/tắt.`,
        cellKeys,
      });
    }
    if (!a.isAisle && !b.isAisle) {
      const wrongType = group.filter((c) => c.seatTypeId === null || !coupleTypeIds.has(c.seatTypeId));
      if (wrongType.length > 0) {
        issues.push({
          level: "error",
          message: `Cặp ghế đôi ${range} phải dùng loại ghế đôi.`,
          cellKeys: wrongType.map(cellKeyOf),
        });
      }
    }
  });
  return issues;
}

function validateCoupleTypeSeats(list: EditorCell[], coupleTypeIds: ReadonlySet<number>): ValidationIssue[] {
  return list
    .filter(
      (c) => !c.isAisle && !c.coupleId && c.seatTypeId !== null && coupleTypeIds.has(c.seatTypeId),
    )
    .map((c) => ({
      level: "error",
      message: `Ghế ${c.seatRow}${c.seatNumber} dùng loại ghế đôi nhưng không thuộc cặp ghế đôi nào.`,
      cellKeys: [cellKeyOf(c)],
    }));
}

function validateAisles(list: EditorCell[], cols: number): ValidationIssue[] {
  if (cols <= 1) return [];
  const activeByRow = new Map<string, string[]>();
  const aisleRows = new Set<string>();
  list.forEach((c) => {
    if (c.isAisle) aisleRows.add(c.seatRow);
    else if (c.isActive) activeByRow.set(c.seatRow, [...(activeByRow.get(c.seatRow) ?? []), cellKeyOf(c)]);
  });

  const issues: ValidationIssue[] = [];
  activeByRow.forEach((keys, rowLabel) => {
    if (keys.length > 0 && !aisleRows.has(rowLabel)) {
      issues.push({
        level: "warning",
        message: `Hàng ${rowLabel} không có ô lối đi nào — ghế trong hàng có thể bị "mồ côi", không có lối vào/ra.`,
        cellKeys: keys,
      });
    }
  });
  return issues;
}

export function validateSeatLayout(
  list: EditorCell[],
  rows: number,
  cols: number,
  coupleTypeIds: ReadonlySet<number> = new Set(),
): ValidationIssue[] {
  return [
    ...validateBounds(list, rows, cols),
    ...validateDuplicates(list),
    ...validateCouples(list, coupleTypeIds),
    ...validateCoupleTypeSeats(list, coupleTypeIds),
    ...validateAisles(list, cols),
  ];
}
