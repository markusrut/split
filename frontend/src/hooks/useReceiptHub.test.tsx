import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { renderHook, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useReceiptHub } from "./useReceiptHub";
import * as signalrModule from "@/api/signalr";
import * as receiptsApiModule from "@/api/receipts";

// Mock the SignalR module
vi.mock("@/api/signalr", () => ({
  createReceiptHubConnection: vi.fn(),
  stopReceiptHubConnection: vi.fn(),
}));

// Mock the receipts API
vi.mock("@/api/receipts", () => ({
  receiptsApi: {
    getById: vi.fn(),
  },
}));

describe("useReceiptHub", () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
        mutations: { retry: false },
      },
    });
    vi.clearAllMocks();
  });

  afterEach(() => {
    queryClient.clear();
  });

  const wrapper = ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );

  it("creates SignalR connection when enabled", () => {
    const mockConnection = vi.fn();
    vi.mocked(signalrModule.createReceiptHubConnection).mockReturnValue(
      mockConnection as unknown as ReturnType<
        typeof signalrModule.createReceiptHubConnection
      >
    );

    renderHook(() => useReceiptHub(true), { wrapper });

    expect(signalrModule.createReceiptHubConnection).toHaveBeenCalledOnce();
    expect(signalrModule.createReceiptHubConnection).toHaveBeenCalledWith(
      expect.any(Function), // onStatusUpdate callback
      expect.any(Function) // onProcessed callback
    );
  });

  it("does not create connection when disabled", () => {
    renderHook(() => useReceiptHub(false), { wrapper });

    expect(signalrModule.createReceiptHubConnection).not.toHaveBeenCalled();
  });

  it("stops connection on unmount", () => {
    const mockConnection = vi.fn();
    vi.mocked(signalrModule.createReceiptHubConnection).mockReturnValue(
      mockConnection as unknown as ReturnType<
        typeof signalrModule.createReceiptHubConnection
      >
    );

    const { unmount } = renderHook(() => useReceiptHub(true), { wrapper });

    unmount();

    expect(signalrModule.stopReceiptHubConnection).toHaveBeenCalledOnce();
  });

  it("fetches updated receipt on status update", async () => {
    const mockReceipt = {
      id: "receipt-123",
      status: "OcrInProgress",
      merchantName: "Test Store",
      date: "2025-11-02",
      total: 50,
      items: [],
      imageUrl: "/test.jpg",
    };

    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    let onStatusUpdateCallback: any = null;

    vi.mocked(signalrModule.createReceiptHubConnection).mockImplementation(
      (onStatusUpdate) => {
        onStatusUpdateCallback = onStatusUpdate;
        return null;
      }
    );

    vi.mocked(receiptsApiModule.receiptsApi.getById).mockResolvedValue(
      mockReceipt as never
    );

    renderHook(() => useReceiptHub(true), { wrapper });

    // Simulate SignalR status update
    if (onStatusUpdateCallback) {
      await onStatusUpdateCallback({
        receiptId: "receipt-123",
        status: "OcrInProgress",
        timestamp: new Date().toISOString(),
      });
    }

    await waitFor(() => {
      expect(receiptsApiModule.receiptsApi.getById).toHaveBeenCalledWith(
        "receipt-123"
      );
    });
  });

  it("fetches updated receipt on processed event", async () => {
    const mockReceipt = {
      id: "receipt-456",
      status: "Ready",
      merchantName: "Test Store",
      date: "2025-11-02",
      total: 50,
      items: [],
      imageUrl: "/test.jpg",
    };

    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    let onProcessedCallback: any = null;

    vi.mocked(signalrModule.createReceiptHubConnection).mockImplementation(
      (_onStatusUpdate, onProcessed) => {
        onProcessedCallback = onProcessed;
        return null;
      }
    );

    vi.mocked(receiptsApiModule.receiptsApi.getById).mockResolvedValue(
      mockReceipt as never
    );

    renderHook(() => useReceiptHub(true), { wrapper });

    // Simulate SignalR processed event
    if (onProcessedCallback) {
      await onProcessedCallback({
        receiptId: "receipt-456",
        status: "Ready",
        itemCount: 5,
        confidence: 0.95,
        timestamp: new Date().toISOString(),
      });
    }

    await waitFor(() => {
      expect(receiptsApiModule.receiptsApi.getById).toHaveBeenCalledWith(
        "receipt-456"
      );
    });
  });

  it("handles errors when fetching updated receipt", async () => {
    const consoleErrorSpy = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});

    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    let onStatusUpdateCallback: any = null;

    vi.mocked(signalrModule.createReceiptHubConnection).mockImplementation(
      (onStatusUpdate) => {
        onStatusUpdateCallback = onStatusUpdate;
        return null;
      }
    );

    vi.mocked(receiptsApiModule.receiptsApi.getById).mockRejectedValue(
      new Error("Network error") as never
    );

    renderHook(() => useReceiptHub(true), { wrapper });

    // Simulate SignalR status update
    if (onStatusUpdateCallback) {
      await onStatusUpdateCallback({
        receiptId: "receipt-error",
        status: "Failed",
        timestamp: new Date().toISOString(),
      });
    }

    await waitFor(() => {
      expect(consoleErrorSpy).toHaveBeenCalledWith(
        expect.stringContaining("Failed to fetch updated receipt"),
        expect.any(Error)
      );
    });

    consoleErrorSpy.mockRestore();
  });
});
