import { describe, it, expect, beforeEach, afterEach } from "vitest";
import { renderHook, act } from "@testing-library/react";
import { useDarkMode, useDarkModeStore } from "./useDarkMode";

describe("useDarkMode", () => {
  beforeEach(() => {
    // Clear localStorage before each test
    localStorage.clear();
    // Remove dark class from document
    document.documentElement.classList.remove("dark");
    // Reset the store state
    useDarkModeStore.setState({ isDark: false });
  });

  afterEach(() => {
    localStorage.clear();
    document.documentElement.classList.remove("dark");
  });

  it("should initialize with dark mode off", () => {
    const { result } = renderHook(() => useDarkMode());

    expect(result.current.isDark).toBe(false);
    expect(document.documentElement.classList.contains("dark")).toBe(false);
  });

  it("should toggle dark mode on", () => {
    const { result } = renderHook(() => useDarkMode());

    act(() => {
      result.current.toggle();
    });

    expect(result.current.isDark).toBe(true);
    expect(document.documentElement.classList.contains("dark")).toBe(true);
  });

  it("should toggle dark mode off", () => {
    // Start with dark mode on
    useDarkModeStore.setState({ isDark: true });

    const { result } = renderHook(() => useDarkMode());

    act(() => {
      result.current.toggle();
    });

    expect(result.current.isDark).toBe(false);
    expect(document.documentElement.classList.contains("dark")).toBe(false);
  });

  it("should set dark mode to true", () => {
    const { result } = renderHook(() => useDarkMode());

    act(() => {
      result.current.setDark(true);
    });

    expect(result.current.isDark).toBe(true);
    expect(document.documentElement.classList.contains("dark")).toBe(true);
  });

  it("should set dark mode to false", () => {
    // Start with dark mode on
    useDarkModeStore.setState({ isDark: true });

    const { result } = renderHook(() => useDarkMode());

    act(() => {
      result.current.setDark(false);
    });

    expect(result.current.isDark).toBe(false);
    expect(document.documentElement.classList.contains("dark")).toBe(false);
  });

  it("should apply dark class to document when dark mode is enabled", () => {
    const { result } = renderHook(() => useDarkMode());

    expect(document.documentElement.classList.contains("dark")).toBe(false);

    act(() => {
      result.current.setDark(true);
    });

    expect(document.documentElement.classList.contains("dark")).toBe(true);
  });

  it("should remove dark class from document when dark mode is disabled", () => {
    useDarkModeStore.setState({ isDark: true });
    document.documentElement.classList.add("dark");

    const { result } = renderHook(() => useDarkMode());

    act(() => {
      result.current.setDark(false);
    });

    expect(document.documentElement.classList.contains("dark")).toBe(false);
  });

  it("should toggle multiple times correctly", () => {
    const { result } = renderHook(() => useDarkMode());

    // Toggle on
    act(() => {
      result.current.toggle();
    });
    expect(result.current.isDark).toBe(true);

    // Toggle off
    act(() => {
      result.current.toggle();
    });
    expect(result.current.isDark).toBe(false);

    // Toggle on again
    act(() => {
      result.current.toggle();
    });
    expect(result.current.isDark).toBe(true);
  });
});
