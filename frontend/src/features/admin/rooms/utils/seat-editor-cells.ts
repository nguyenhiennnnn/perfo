import {
  generateRowLabel,
  seatCellKey,
} from "@/components/common/seat-map/seat-map-types";
import type { SeatTypeDto } from "@/features/admin/catalog/types";
import type { SeatDto, UpdateSeatLayoutRequest } from "../types";
import type {
  AutoGenerateOptions,
  BrushResult,
  CopiedRow,
  EditorCell,
  EditorCellMap,
  SeatEditorBrush,
  SeatTypeRules,
} from "../types/seat-editor";

type SolidBrush = Exclude<SeatEditorBrush, { type: "pairCouple" }>;

function newCoupleId(seed: string): string {
  return typeof crypto !== "undefined" && "randomUUID" in crypto
    ? crypto.randomUUID()
    : `couple-${seed}-${Date.now()}-${Math.random().toString(16).slice(2)}`;
}

const createDefaultCell = (
  seatRow: string,
  seatNumber: number,
  seatTypeId: number | null,
): EditorCell => ({
  seatRow,
  seatNumber,
  seatTypeId,
  isActive: true,
  isAisle: false,
  coupleId: null,
});

const warn = (cells: EditorCellMap, warning: string): BrushResult => ({ cells, warning });

export function buildSeatTypeRules(
  seatTypes: SeatTypeDto[] | undefined,
  defaultSeatTypeId: number | null,
): SeatTypeRules {
  const coupleTypes = (seatTypes ?? []).filter((t) => t.isCouple);
  return {
    defaultSeatTypeId,
    coupleSeatTypeId: coupleTypes[0]?.seatTypeId ?? null,
    coupleTypeIds: new Set(coupleTypes.map((t) => t.seatTypeId)),
  };
}

export function listBrushSeatTypes(seatTypes: SeatTypeDto[] | undefined): SeatTypeDto[] {
  const defaultCoupleId = seatTypes?.find((t) => t.isCouple)?.seatTypeId;
  return (seatTypes ?? []).filter((t) => !t.isCouple || t.seatTypeId !== defaultCoupleId);
}

export function buildInitialCells(
  rows: number,
  cols: number,
  seats: SeatDto[] | undefined,
  defaultSeatTypeId: number | null,
): EditorCellMap {
  const bySeat = new Map<string, SeatDto>();
  seats?.forEach((s) => bySeat.set(seatCellKey(s.seatRow, s.seatNumber), s));

  const map: EditorCellMap = new Map();
  for (let r = 0; r < rows; r++) {
    const rowLabel = generateRowLabel(r);
    for (let c = 1; c <= cols; c++) {
      const key = seatCellKey(rowLabel, c);
      const existing = bySeat.get(key);
      map.set(
        key,
        existing
          ? {
              seatRow: existing.seatRow,
              seatNumber: existing.seatNumber,
              seatTypeId: existing.seatTypeId ?? defaultSeatTypeId,
              isActive: existing.isActive,
              isAisle: existing.isAisle,
              coupleId: existing.coupleId ?? null,
            }
          : createDefaultCell(rowLabel, c, defaultSeatTypeId),
      );
    }
  }
  return map;
}

function releaseFromCouple(cell: EditorCell, rules: SeatTypeRules): EditorCell {
  const isCoupleType = cell.seatTypeId !== null && rules.coupleTypeIds.has(cell.seatTypeId);
  return {
    ...cell,
    coupleId: null,
    seatTypeId: isCoupleType ? (rules.defaultSeatTypeId ?? cell.seatTypeId) : cell.seatTypeId,
  };
}

export function sanitizeCouples(cells: EditorCellMap, rules: SeatTypeRules): EditorCellMap {
  const groups = new Map<string, string[]>();
  cells.forEach((cell, key) => {
    if (cell.coupleId) groups.set(cell.coupleId, [...(groups.get(cell.coupleId) ?? []), key]);
  });

  const next = new Map(cells);
  groups.forEach((keys) => {
    if (keys.length === 2) return;
    keys.forEach((key) => {
      const cell = next.get(key);
      if (cell) next.set(key, releaseFromCouple(cell, rules));
    });
  });
  return next;
}

export function resizeCells(
  cells: EditorCellMap,
  newRows: number,
  newCols: number,
  rules: SeatTypeRules,
): EditorCellMap {
  const next: EditorCellMap = new Map();
  for (let r = 0; r < newRows; r++) {
    const rowLabel = generateRowLabel(r);
    for (let c = 1; c <= newCols; c++) {
      const key = seatCellKey(rowLabel, c);
      next.set(key, cells.get(key) ?? createDefaultCell(rowLabel, c, rules.defaultSeatTypeId));
    }
  }
  return sanitizeCouples(next, rules);
}

function membersOf(cells: EditorCellMap, key: string, cell: EditorCell): string[] {
  if (!cell.coupleId) return [key];
  const keys: string[] = [];
  cells.forEach((c, k) => {
    if (c.coupleId === cell.coupleId) keys.push(k);
  });
  return keys;
}

export function expandToCouples(cells: EditorCellMap, keys: Iterable<string>): string[] {
  const expanded = new Set<string>();
  for (const key of keys) {
    const cell = cells.get(key);
    if (!cell) continue;
    membersOf(cells, key, cell).forEach((k) => expanded.add(k));
  }
  return Array.from(expanded);
}

function updateMembers(
  cells: EditorCellMap,
  keys: string[],
  patch: (cell: EditorCell) => EditorCell,
): EditorCellMap {
  const next = new Map(cells);
  keys.forEach((key) => {
    const cell = next.get(key);
    if (cell) next.set(key, patch(cell));
  });
  return next;
}

function findPartnerKey(cells: EditorCellMap, cell: EditorCell): string | null {
  const candidates = [cell.seatNumber + 1, cell.seatNumber - 1];
  for (const number of candidates) {
    const key = seatCellKey(cell.seatRow, number);
    const candidate = cells.get(key);
    if (candidate && !candidate.isAisle && !candidate.coupleId) return key;
  }
  return null;
}

function pairWithNeighbor(
  cells: EditorCellMap,
  key: string,
  cell: EditorCell,
  seatTypeId: number | null,
): BrushResult {
  if (seatTypeId === null) {
    return warn(
      cells,
      "Chưa có loại ghế đôi. Hãy bật cờ \"Ghế đôi\" cho một loại ghế trong danh mục loại ghế.",
    );
  }

  const partnerKey = findPartnerKey(cells, cell);
  const partner = partnerKey ? cells.get(partnerKey) : undefined;
  if (!partnerKey || !partner) {
    return warn(cells, "Không có ô liền kề còn trống để ghép thành ghế đôi.");
  }

  const coupleId = newCoupleId(partnerKey);
  const next = new Map(cells);
  next.set(key, { ...cell, seatTypeId, coupleId });
  next.set(partnerKey, { ...partner, seatTypeId, coupleId, isActive: cell.isActive });
  return { cells: next, warning: null };
}

function splitCouple(
  cells: EditorCellMap,
  key: string,
  cell: EditorCell,
  rules: SeatTypeRules,
): EditorCellMap {
  return updateMembers(cells, membersOf(cells, key, cell), (c) => ({
    ...c,
    coupleId: null,
    seatTypeId: rules.defaultSeatTypeId ?? c.seatTypeId,
  }));
}

function toggleCoupleAt(
  cells: EditorCellMap,
  key: string,
  cell: EditorCell,
  rules: SeatTypeRules,
): BrushResult {
  if (cell.isAisle) return warn(cells, "Không thể đặt ghế đôi tại ô lối đi.");
  if (cell.coupleId) return { cells: splitCouple(cells, key, cell, rules), warning: null };
  return pairWithNeighbor(cells, key, cell, rules.coupleSeatTypeId);
}

function assignSeatTypeAt(
  cells: EditorCellMap,
  key: string,
  cell: EditorCell,
  seatTypeId: number,
  rules: SeatTypeRules,
): BrushResult {
  if (rules.coupleTypeIds.has(seatTypeId)) {
    if (cell.isAisle) return warn(cells, "Không thể đặt ghế đôi tại ô lối đi.");
    if (!cell.coupleId) return pairWithNeighbor(cells, key, cell, seatTypeId);
    return {
      cells: updateMembers(cells, membersOf(cells, key, cell), (c) => ({ ...c, seatTypeId })),
      warning: null,
    };
  }

  return {
    cells: updateMembers(cells, membersOf(cells, key, cell), (c) => ({
      ...c,
      seatTypeId,
      isAisle: false,
      coupleId: null,
    })),
    warning: null,
  };
}

function toggleAisleAt(
  cells: EditorCellMap,
  key: string,
  cell: EditorCell,
  rules: SeatTypeRules,
): EditorCellMap {
  if (cell.coupleId) {
    return updateMembers(cells, membersOf(cells, key, cell), (c) => ({
      ...c,
      isAisle: true,
      coupleId: null,
      seatTypeId: rules.defaultSeatTypeId ?? c.seatTypeId,
    }));
  }
  return updateMembers(cells, [key], (c) => ({ ...c, isAisle: !c.isAisle }));
}

export function applyBrushAt(
  cells: EditorCellMap,
  key: string,
  brush: SeatEditorBrush,
  rules: SeatTypeRules,
): BrushResult {
  const cell = cells.get(key);
  if (!cell) return { cells, warning: null };

  switch (brush.type) {
    case "pairCouple":
      return toggleCoupleAt(cells, key, cell, rules);
    case "seatType":
      return assignSeatTypeAt(cells, key, cell, brush.seatTypeId, rules);
    case "toggleActive":
      return {
        cells: updateMembers(cells, membersOf(cells, key, cell), (c) => ({
          ...c,
          isActive: !cell.isActive,
        })),
        warning: null,
      };
    case "toggleAisle":
      return { cells: toggleAisleAt(cells, key, cell, rules), warning: null };
  }
}

export function applyBrushToKeys(
  cells: EditorCellMap,
  keys: Iterable<string>,
  brush: SolidBrush,
  rules: SeatTypeRules,
): EditorCellMap {
  const targets = expandToCouples(cells, keys);
  const selected = targets
    .map((key) => ({ key, cell: cells.get(key) }))
    .filter((entry): entry is { key: string; cell: EditorCell } => !!entry.cell);

  const next = new Map(cells);

  if (brush.type === "toggleActive") {
    const targetActive = !selected.some(({ cell }) => cell.isActive && !cell.isAisle);
    selected.forEach(({ key, cell }) => {
      if (!cell.isAisle) next.set(key, { ...cell, isActive: targetActive });
    });
    return next;
  }

  if (brush.type === "toggleAisle") {
    const targetAisle = selected.some(({ cell }) => !cell.isAisle);
    selected.forEach(({ key, cell }) => {
      if (cell.isAisle === targetAisle) return;
      next.set(
        key,
        targetAisle
          ? {
              ...cell,
              isAisle: true,
              coupleId: null,
              seatTypeId: cell.coupleId
                ? (rules.defaultSeatTypeId ?? cell.seatTypeId)
                : cell.seatTypeId,
            }
          : { ...cell, isAisle: false },
      );
    });
    return next;
  }

  selected.forEach(({ key, cell }) =>
    next.set(key, { ...cell, seatTypeId: brush.seatTypeId, isAisle: false, coupleId: null }),
  );
  return next;
}

export function pasteRowCells(
  cells: EditorCellMap,
  copied: CopiedRow,
  targetRowLabel: string,
  rules: SeatTypeRules,
): EditorCellMap {
  const coupleIdMap = new Map<string, string>();
  const next = new Map(cells);
  copied.cells.forEach((source) => {
    const key = seatCellKey(targetRowLabel, source.seatNumber);
    if (!next.has(key)) return;

    let coupleId: string | null = null;
    if (source.coupleId) {
      if (!coupleIdMap.has(source.coupleId)) {
        coupleIdMap.set(source.coupleId, newCoupleId(`${targetRowLabel}-${source.seatNumber}`));
      }
      coupleId = coupleIdMap.get(source.coupleId)!;
    }

    next.set(key, { ...source, seatRow: targetRowLabel, coupleId });
  });
  return sanitizeCouples(next, rules);
}

function buildCoupleIdsByCol(
  rowLabel: string,
  cols: number,
  aisleCols: number[],
): Map<number, string> {
  const coupleIdByCol = new Map<number, string>();
  let pending: number | null = null;
  for (let c = 1; c <= cols; c++) {
    if (aisleCols.includes(c)) {
      pending = null;
      continue;
    }
    if (pending === null) {
      pending = c;
    } else {
      const id = newCoupleId(`${rowLabel}-${pending}`);
      coupleIdByCol.set(pending, id);
      coupleIdByCol.set(c, id);
      pending = null;
    }
  }
  return coupleIdByCol;
}

export function buildAutoGeneratedCells(options: AutoGenerateOptions): EditorCellMap {
  const {
    rows,
    cols,
    standardSeatTypeId,
    vipSeatTypeId,
    coupleSeatTypeId,
    vipRowCount,
    coupleRowCount,
    aisleCols,
  } = options;
  const next: EditorCellMap = new Map();

  for (let r = 0; r < rows; r++) {
    const rowLabel = generateRowLabel(r);
    const isCoupleRow = coupleRowCount > 0 && coupleSeatTypeId != null && r >= rows - coupleRowCount;
    const isVipRow = !isCoupleRow && vipRowCount > 0 && r >= rows - vipRowCount;
    const rowSeatTypeId = isVipRow && vipSeatTypeId ? vipSeatTypeId : standardSeatTypeId;
    const coupleIdByCol = isCoupleRow
      ? buildCoupleIdsByCol(rowLabel, cols, aisleCols)
      : new Map<number, string>();

    for (let c = 1; c <= cols; c++) {
      const isAisleCol = aisleCols.includes(c);
      const coupleId = isAisleCol ? null : (coupleIdByCol.get(c) ?? null);
      next.set(seatCellKey(rowLabel, c), {
        seatRow: rowLabel,
        seatNumber: c,
        seatTypeId: isAisleCol ? null : coupleId ? coupleSeatTypeId : rowSeatTypeId,
        isActive: !isAisleCol,
        isAisle: isAisleCol,
        coupleId,
      });
    }
  }
  return next;
}

export const buildSeatLayoutPayload = (
  rows: number,
  cols: number,
  cellList: EditorCell[],
): UpdateSeatLayoutRequest => ({
  rows,
  cols,
  seats: cellList.map((c) => ({
    seatRow: c.seatRow,
    seatNumber: c.seatNumber,
    seatTypeId: c.isAisle ? null : c.seatTypeId,
    isActive: c.isAisle ? false : c.isActive,
    isAisle: c.isAisle,
    coupleId: c.coupleId,
  })),
});
