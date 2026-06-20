import { act, renderHook } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { useToast } from "./useToast";

describe("useToast", () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("starts with no toast", () => {
    const { result } = renderHook(() => useToast());
    expect(result.current.toast).toBeNull();
  });

  it("shows a toast with the requested message and variant", () => {
    const { result } = renderHook(() => useToast());

    act(() => result.current.showToast("Saved", "success"));

    expect(result.current.toast).toEqual({
      message: "Saved",
      variant: "success",
    });
  });

  it("defaults to the info variant", () => {
    const { result } = renderHook(() => useToast());

    act(() => result.current.showToast("Heads up"));

    expect(result.current.toast?.variant).toBe("info");
  });

  it("auto-dismisses after the configured duration", () => {
    const { result } = renderHook(() => useToast(1000));

    act(() => result.current.showToast("Temporary"));
    expect(result.current.toast).not.toBeNull();

    act(() => vi.advanceTimersByTime(1000));
    expect(result.current.toast).toBeNull();
  });

  it("dismisses immediately when requested", () => {
    const { result } = renderHook(() => useToast());

    act(() => result.current.showToast("Closable"));
    act(() => result.current.dismissToast());

    expect(result.current.toast).toBeNull();
  });

  it("resets the auto-dismiss timer when a new toast replaces an old one", () => {
    const { result } = renderHook(() => useToast(1000));

    act(() => result.current.showToast("First"));
    act(() => vi.advanceTimersByTime(800));

    act(() => result.current.showToast("Second"));
    act(() => vi.advanceTimersByTime(800));
    // The original timer would have fired by now; the replacement keeps it alive.
    expect(result.current.toast?.message).toBe("Second");

    act(() => vi.advanceTimersByTime(200));
    expect(result.current.toast).toBeNull();
  });
});
