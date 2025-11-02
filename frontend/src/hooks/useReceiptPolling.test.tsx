import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { renderHook } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useReceiptPolling } from "./useReceiptPolling";
import { ReceiptStatus } from "@/types";
import * as receiptsApiModule from "@/api/receipts";

// Mock the receipts API
vi.mock("@/api/receipts", () => ({
  receiptsApi: {
    getById: vi.fn(),
  },
}));

describe("useReceiptPolling", () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
        mutations: { retry: false },
      },
    });
    vi.clearAllMocks();
    vi.useFakeTimers();
  });

  afterEach(() => {
    queryClient.clear();
    vi.restoreAllMocks();
  });

  const wrapper = ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );

  it("does not poll when disabled", async () => {
    renderHook(
      () => useReceiptPolling("receipt-123", ReceiptStatus.Processing, false),
      { wrapper }
    );

    vi.advanceTimersByTime(5000);

    expect(receiptsApiModule.receiptsApi.getById).not.toHaveBeenCalled();
  });

  it("does not poll when receiptId is undefined", async () => {
    renderHook(
      () => useReceiptPolling(undefined, ReceiptStatus.Processing, true),
      { wrapper }
    );

    vi.advanceTimersByTime(5000);

    expect(receiptsApiModule.receiptsApi.getById).not.toHaveBeenCalled();
  });

  it("does not poll when status is undefined", async () => {
    renderHook(() => useReceiptPolling("receipt-123", undefined, true), {
      wrapper,
    });

    vi.advanceTimersByTime(5000);

    expect(receiptsApiModule.receiptsApi.getById).not.toHaveBeenCalled();
  });

  it("does not poll for Ready status", async () => {
    renderHook(
      () => useReceiptPolling("receipt-123", ReceiptStatus.Ready, true),
      { wrapper }
    );

    vi.advanceTimersByTime(5000);

    expect(receiptsApiModule.receiptsApi.getById).not.toHaveBeenCalled();
  });

  it("does not poll for Failed status", async () => {
    renderHook(
      () => useReceiptPolling("receipt-123", ReceiptStatus.Failed, true),
      { wrapper }
    );

    vi.advanceTimersByTime(5000);

    expect(receiptsApiModule.receiptsApi.getById).not.toHaveBeenCalled();
  });

  it("polls for Uploaded status", async () => {
    const mockReceipt = {
      id: "receipt-123",
      status: ReceiptStatus.Ready,
      merchantName: "Test",
      date: "2025-11-02",
      total: 50,
      items: [],
      imageUrl: "/test.jpg",
    };

    vi.mocked(receiptsApiModule.receiptsApi.getById).mockResolvedValue(
      mockReceipt as never
    );

    renderHook(
      () => useReceiptPolling("receipt-123", ReceiptStatus.Uploaded, true),
      { wrapper }
    );

    // Advance past first poll interval (3 seconds) and run pending timers
    vi.advanceTimersByTime(3100);
    await vi.runOnlyPendingTimersAsync();

    expect(receiptsApiModule.receiptsApi.getById).toHaveBeenCalledWith(
      "receipt-123"
    );
  });

  it("polls for OcrInProgress status", async () => {
    const mockReceipt = {
      id: "receipt-456",
      status: ReceiptStatus.Ready,
      merchantName: "Test",
      date: "2025-11-02",
      total: 50,
      items: [],
      imageUrl: "/test.jpg",
    };

    vi.mocked(receiptsApiModule.receiptsApi.getById).mockResolvedValue(
      mockReceipt as never
    );

    renderHook(
      () => useReceiptPolling("receipt-456", ReceiptStatus.OcrInProgress, true),
      { wrapper }
    );

    vi.advanceTimersByTime(3100);
    await vi.runOnlyPendingTimersAsync();

    expect(receiptsApiModule.receiptsApi.getById).toHaveBeenCalledWith(
      "receipt-456"
    );
  });

  it("stops polling when receipt reaches Ready status", async () => {
    const mockReceipt = {
      id: "receipt-789",
      status: ReceiptStatus.Ready,
      merchantName: "Test",
      date: "2025-11-02",
      total: 50,
      items: [],
      imageUrl: "/test.jpg",
    };

    vi.mocked(receiptsApiModule.receiptsApi.getById).mockResolvedValue(
      mockReceipt as never
    );

    renderHook(
      () => useReceiptPolling("receipt-789", ReceiptStatus.Processing, true),
      { wrapper }
    );

    // First poll
    vi.advanceTimersByTime(3100);
    await vi.runOnlyPendingTimersAsync();

    expect(receiptsApiModule.receiptsApi.getById).toHaveBeenCalledTimes(1);

    // Clear mock calls
    vi.mocked(receiptsApiModule.receiptsApi.getById).mockClear();

    // Advance more time - should not poll again since status is Ready
    vi.advanceTimersByTime(10000);
    await vi.runOnlyPendingTimersAsync();

    expect(receiptsApiModule.receiptsApi.getById).not.toHaveBeenCalled();
  });

  it("stops polling when receipt reaches Failed status", async () => {
    const mockReceipt = {
      id: "receipt-fail",
      status: ReceiptStatus.Failed,
      merchantName: "Test",
      date: "2025-11-02",
      total: 50,
      items: [],
      imageUrl: "/test.jpg",
    };

    vi.mocked(receiptsApiModule.receiptsApi.getById).mockResolvedValue(
      mockReceipt as never
    );

    renderHook(
      () =>
        useReceiptPolling("receipt-fail", ReceiptStatus.OcrInProgress, true),
      { wrapper }
    );

    vi.advanceTimersByTime(3100);
    await vi.runOnlyPendingTimersAsync();

    expect(receiptsApiModule.receiptsApi.getById).toHaveBeenCalledTimes(1);

    vi.mocked(receiptsApiModule.receiptsApi.getById).mockClear();

    // Should not poll again
    vi.advanceTimersByTime(10000);
    await vi.runOnlyPendingTimersAsync();

    expect(receiptsApiModule.receiptsApi.getById).not.toHaveBeenCalled();
  });

  it("continues polling on error", async () => {
    const consoleErrorSpy = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});

    vi.mocked(receiptsApiModule.receiptsApi.getById)
      .mockRejectedValueOnce(new Error("Network error") as never)
      .mockResolvedValue({
        id: "receipt-retry",
        status: ReceiptStatus.Ready,
        merchantName: "Test",
        date: "2025-11-02",
        total: 50,
        items: [],
        imageUrl: "/test.jpg",
      } as never);

    renderHook(
      () => useReceiptPolling("receipt-retry", ReceiptStatus.Processing, true),
      { wrapper }
    );

    // Advance timers to trigger polls - the error will happen, then continue polling
    vi.advanceTimersByTime(3100);
    await vi.runOnlyPendingTimersAsync();

    // Advance again for next poll - this one succeeds with Ready status
    vi.advanceTimersByTime(3100);
    await vi.runOnlyPendingTimersAsync();

    // Should have attempted twice total (1 error + 1 success)
    expect(receiptsApiModule.receiptsApi.getById).toHaveBeenCalledTimes(2);

    // Verify error was logged
    expect(consoleErrorSpy).toHaveBeenCalled();

    consoleErrorSpy.mockRestore();
  });

  it("cleans up interval on unmount", async () => {
    const mockReceipt = {
      id: "receipt-cleanup",
      status: ReceiptStatus.Ready,
      merchantName: "Test",
      date: "2025-11-02",
      total: 50,
      items: [],
      imageUrl: "/test.jpg",
    };

    vi.mocked(receiptsApiModule.receiptsApi.getById).mockResolvedValue(
      mockReceipt as never
    );

    const { unmount } = renderHook(
      () =>
        useReceiptPolling("receipt-cleanup", ReceiptStatus.Processing, true),
      { wrapper }
    );

    vi.advanceTimersByTime(3100);
    await vi.runOnlyPendingTimersAsync();

    expect(receiptsApiModule.receiptsApi.getById).toHaveBeenCalledTimes(1);

    unmount();

    vi.mocked(receiptsApiModule.receiptsApi.getById).mockClear();

    // Should not poll after unmount
    vi.advanceTimersByTime(10000);
    await vi.runOnlyPendingTimersAsync();

    expect(receiptsApiModule.receiptsApi.getById).not.toHaveBeenCalled();
  });
});
