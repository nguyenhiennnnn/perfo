import { useCallback, useRef } from "react";
import { CHECKIN_SOUND } from "@/config/default-constants";

export function useCheckinSound() {
  const contextRef = useRef<AudioContext | null>(null);

  const getContext = useCallback(() => {
    if (!contextRef.current) {
      contextRef.current = new AudioContext();
    }
    return contextRef.current;
  }, []);

  const playTone = useCallback(
    (frequency: number, duration: number) => {
      const ctx = getContext();
      const oscillator = ctx.createOscillator();
      const gain = ctx.createGain();
      oscillator.type = "sine";
      oscillator.frequency.value = frequency;
      gain.gain.setValueAtTime(0.2, ctx.currentTime);
      gain.gain.exponentialRampToValueAtTime(
        0.0001,
        ctx.currentTime + duration,
      );
      oscillator.connect(gain);
      gain.connect(ctx.destination);
      oscillator.start();
      oscillator.stop(ctx.currentTime + duration);
    },
    [getContext],
  );

  const playSuccess = useCallback(() => playTone(CHECKIN_SOUND.success.frequencyHz, CHECKIN_SOUND.success.durationSeconds), [playTone]);
  const playError = useCallback(() => playTone(CHECKIN_SOUND.error.frequencyHz, CHECKIN_SOUND.error.durationSeconds), [playTone]);

  return { playSuccess, playError };
}
