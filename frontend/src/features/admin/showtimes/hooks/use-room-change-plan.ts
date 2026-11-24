import { useState } from "react";

import { overridesToList } from "../utils/room-change";
import type {
  RoomChangePreviewResponse,
  ShowtimeRoomChangeResponse,
} from "../types";
import {
  useAdminChangeShowtimeRoom,
  useAdminPreviewRoomChange,
} from "./use-admin-showtimes";

type Overrides = Record<number, number>;

export function useRoomChangePlan(showtimeId: number) {
  const previewMutation = useAdminPreviewRoomChange();
  const applyMutation = useAdminChangeShowtimeRoom();

  const [targetRoomId, setTargetRoomId] = useState<number | null>(null);
  const [overrides, setOverrides] = useState<Overrides>({});
  const [note, setNote] = useState("");
  const [plan, setPlan] = useState<RoomChangePreviewResponse | null>(null);
  const [previewError, setPreviewError] = useState<unknown>(null);
  const [result, setResult] = useState<ShowtimeRoomChangeResponse | null>(null);

  const runPreview = (
    roomId: number,
    nextOverrides: Overrides,
    onFailed?: () => void,
  ) => {
    setPreviewError(null);
    previewMutation.mutate(
      {
        id: showtimeId,
        body: { targetRoomId: roomId, overrides: overridesToList(nextOverrides) },
      },
      {
        onSuccess: (res) => setPlan(res.data ?? null),
        onError: (error) => {
          setPreviewError(error);
          onFailed?.();
        },
      },
    );
  };

  const selectRoom = (roomId: number) => {
    setTargetRoomId(roomId);
    setOverrides({});
    setPlan(null);
    runPreview(roomId, {});
  };

  const assignSeat = (showtimeSeatId: number, targetSeatId: number) => {
    if (targetRoomId === null) return;
    const previous = overrides;
    const next: Overrides = {};
    for (const [key, value] of Object.entries(previous)) {
      if (Number(key) !== showtimeSeatId && value !== targetSeatId) {
        next[Number(key)] = value;
      }
    }
    next[showtimeSeatId] = targetSeatId;
    setOverrides(next);
    runPreview(targetRoomId, next, () => setOverrides(previous));
  };

  const clearOverride = (showtimeSeatId: number) => {
    if (targetRoomId === null || !(showtimeSeatId in overrides)) return;
    const previous = overrides;
    const next: Overrides = { ...previous };
    delete next[showtimeSeatId];
    setOverrides(next);
    runPreview(targetRoomId, next, () => setOverrides(previous));
  };

  const refreshPreview = () => {
    if (targetRoomId === null) return;
    runPreview(targetRoomId, overrides);
  };

  const apply = () => {
    if (targetRoomId === null || !plan?.canApply) return;
    const trimmed = note.trim();
    applyMutation.mutate(
      {
        id: showtimeId,
        body: {
          targetRoomId,
          overrides: overridesToList(overrides),
          note: trimmed ? trimmed : null,
        },
      },
      {
        onSuccess: (res) => setResult(res.data ?? null),
        onError: () => runPreview(targetRoomId, overrides),
      },
    );
  };

  const isPreviewing = previewMutation.isPending;
  const isApplying = applyMutation.isPending;

  return {
    targetRoomId,
    overrides,
    note,
    setNote,
    plan,
    previewError,
    result,
    isPreviewing,
    isApplying,
    canApply: Boolean(plan?.canApply) && !isPreviewing && !isApplying,
    selectRoom,
    assignSeat,
    clearOverride,
    refreshPreview,
    apply,
  };
}
