import { describe, it, expect, beforeEach, vi } from "vitest";
import { render } from "@/test/test-utils";
import { ReceiptsPage } from "./ReceiptsPage";
import * as useReceiptsModule from "@/hooks/useReceipts";
import { ReceiptStatus, type ReceiptListItem } from "@/types";

// Mock the useReceipts hook
vi.mock("@/hooks/useReceipts", () => ({
  useReceipts: vi.fn(),
  useReceipt: vi.fn(),
}));

// Mock the useReceiptHub hook
vi.mock("@/hooks/useReceiptHub", () => ({
  useReceiptHub: vi.fn(),
}));

// Mock the receipt components
vi.mock("@/components/receipt", () => ({
  ReceiptUpload: ({ uploading }: { uploading: boolean }) => (
    <div data-testid="receipt-upload">
      Receipt Upload (uploading: {uploading.toString()})
    </div>
  ),
  ReceiptStatusBadge: ({ status }: { status: string }) => (
    <span data-testid="status-badge">{status}</span>
  ),
  ReceiptSummary: () => <div>Receipt Summary</div>,
  ReceiptItemsList: () => <div>Receipt Items List</div>,
}));

// Helper to set viewport size
const setViewport = (width: number, height: number) => {
  Object.defineProperty(window, "innerWidth", {
    writable: true,
    configurable: true,
    value: width,
  });
  Object.defineProperty(window, "innerHeight", {
    writable: true,
    configurable: true,
    value: height,
  });
};

// Mock receipt data
const mockReceipts: ReceiptListItem[] = [
  {
    id: "1",
    merchantName: "Grocery Store",
    date: "2024-01-15T10:30:00Z",
    total: 125.5,
    imageUrl: "https://example.com/receipt1.jpg",
    status: ReceiptStatus.Ready,
    createdAt: "2024-01-15T10:30:00Z",
  },
  {
    id: "2",
    merchantName: "Restaurant",
    date: "2024-01-14T19:45:00Z",
    total: 85.75,
    imageUrl: "https://example.com/receipt2.jpg",
    status: ReceiptStatus.OcrInProgress,
    createdAt: "2024-01-14T19:45:00Z",
  },
];

describe("ReceiptsPage", () => {
  beforeEach(() => {
    // Reset to default viewport
    setViewport(1024, 768);

    // Mock useReceipts hook with default values
    vi.mocked(useReceiptsModule.useReceipts).mockReturnValue({
      receipts: mockReceipts,
      isLoading: false,
      error: null,
      refetch: vi.fn(),
      uploadReceipt: vi.fn(),
      uploadLoading: false,
      uploadError: null,
      deleteReceipt: vi.fn(),
      deleteLoading: false,
      deleteError: null,
    });
  });

  describe("Snapshot Tests - With Data", () => {
    it("matches snapshot on mobile (375px)", () => {
      setViewport(375, 667);
      const { container } = render(<ReceiptsPage />);
      expect(container).toMatchSnapshot();
    });

    it("matches snapshot on tablet (768px)", () => {
      setViewport(768, 1024);
      const { container } = render(<ReceiptsPage />);
      expect(container).toMatchSnapshot();
    });

    it("matches snapshot on desktop (1280px)", () => {
      setViewport(1280, 720);
      const { container } = render(<ReceiptsPage />);
      expect(container).toMatchSnapshot();
    });
  });

  describe("Snapshot Tests - Loading State", () => {
    it("matches snapshot with loading state on mobile", () => {
      setViewport(375, 667);
      vi.mocked(useReceiptsModule.useReceipts).mockReturnValue({
        receipts: undefined,
        isLoading: true,
        error: null,
        refetch: vi.fn(),
        uploadReceipt: vi.fn(),
        uploadLoading: false,
        uploadError: null,
        deleteReceipt: vi.fn(),
        deleteLoading: false,
        deleteError: null,
      });

      const { container } = render(<ReceiptsPage />);
      expect(container).toMatchSnapshot();
    });

    it("matches snapshot with loading state on desktop", () => {
      setViewport(1280, 720);
      vi.mocked(useReceiptsModule.useReceipts).mockReturnValue({
        receipts: undefined,
        isLoading: true,
        error: null,
        refetch: vi.fn(),
        uploadReceipt: vi.fn(),
        uploadLoading: false,
        uploadError: null,
        deleteReceipt: vi.fn(),
        deleteLoading: false,
        deleteError: null,
      });

      const { container } = render(<ReceiptsPage />);
      expect(container).toMatchSnapshot();
    });
  });

  describe("Snapshot Tests - Empty State", () => {
    it("matches snapshot with no receipts on mobile", () => {
      setViewport(375, 667);
      vi.mocked(useReceiptsModule.useReceipts).mockReturnValue({
        receipts: [],
        isLoading: false,
        error: null,
        refetch: vi.fn(),
        uploadReceipt: vi.fn(),
        uploadLoading: false,
        uploadError: null,
        deleteReceipt: vi.fn(),
        deleteLoading: false,
        deleteError: null,
      });

      const { container } = render(<ReceiptsPage />);
      expect(container).toMatchSnapshot();
    });

    it("matches snapshot with no receipts on desktop", () => {
      setViewport(1280, 720);
      vi.mocked(useReceiptsModule.useReceipts).mockReturnValue({
        receipts: [],
        isLoading: false,
        error: null,
        refetch: vi.fn(),
        uploadReceipt: vi.fn(),
        uploadLoading: false,
        uploadError: null,
        deleteReceipt: vi.fn(),
        deleteLoading: false,
        deleteError: null,
      });

      const { container } = render(<ReceiptsPage />);
      expect(container).toMatchSnapshot();
    });
  });

  describe("Snapshot Tests - Error State", () => {
    it("matches snapshot with error state", () => {
      vi.mocked(useReceiptsModule.useReceipts).mockReturnValue({
        receipts: undefined,
        isLoading: false,
        error: new Error("Failed to load receipts"),
        refetch: vi.fn(),
        uploadReceipt: vi.fn(),
        uploadLoading: false,
        uploadError: null,
        deleteReceipt: vi.fn(),
        deleteLoading: false,
        deleteError: null,
      });

      const { container } = render(<ReceiptsPage />);
      expect(container).toMatchSnapshot();
    });
  });

  describe("Content Tests", () => {
    it("renders page title", () => {
      const { getByText } = render(<ReceiptsPage />);
      expect(getByText("My Receipts")).toBeInTheDocument();
    });

    it("renders new receipt button", () => {
      const { getByText } = render(<ReceiptsPage />);
      expect(getByText("New Receipt")).toBeInTheDocument();
    });

    it("renders receipt list when data is available", () => {
      const { getByText } = render(<ReceiptsPage />);
      expect(getByText("Grocery Store")).toBeInTheDocument();
      expect(getByText("Restaurant")).toBeInTheDocument();
    });
  });
});
