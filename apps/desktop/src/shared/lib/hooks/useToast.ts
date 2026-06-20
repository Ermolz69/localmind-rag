import { useCallback, useEffect, useRef, useState } from "react";

import type { ToastVariant } from "@shared/ui";

export type ToastState = {
  message: string;
  variant: ToastVariant;
};

const DEFAULT_DURATION_MS = 4000;

/**
 * Drives the {@link Toast} primitive: shows a transient message with a variant
 * and auto-dismisses after `durationMs`. Replacing or dismissing a toast clears
 * any pending timer, and the timer is cleaned up on unmount.
 */
export function useToast(durationMs = DEFAULT_DURATION_MS) {
  const [toast, setToast] = useState<ToastState | null>(null);
  const timeoutRef = useRef<number | null>(null);

  const clearTimer = useCallback(() => {
    if (timeoutRef.current !== null) {
      window.clearTimeout(timeoutRef.current);
      timeoutRef.current = null;
    }
  }, []);

  const dismissToast = useCallback(() => {
    clearTimer();
    setToast(null);
  }, [clearTimer]);

  const showToast = useCallback(
    (message: string, variant: ToastVariant = "info") => {
      clearTimer();
      setToast({ message, variant });
      timeoutRef.current = window.setTimeout(() => {
        timeoutRef.current = null;
        setToast(null);
      }, durationMs);
    },
    [clearTimer, durationMs],
  );

  useEffect(() => clearTimer, [clearTimer]);

  return { toast, showToast, dismissToast };
}
