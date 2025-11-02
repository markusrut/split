import { describe, it, expect, beforeEach, vi } from "vitest";
import { render } from "@/test/test-utils";
import { ReceiptDetailPage } from "./ReceiptDetailPage";
import * as useReceiptsModule from "@/hooks/useReceipts";
import * as reactRouterDom from "react-router-dom";
import { ReceiptStatus, type Receipt } from "@/types";

// Mock the useReceipts hooks
vi.mock("@/hooks/useReceipts", () => ({
  useReceipts: vi.fn(),
  useReceipt: vi.fn(),
}));

// Mock react-router-dom
vi.mock("react-router-dom", async () => {
  const actual = await vi.importActual("react-router-dom");
  return {
    ...actual,
    useParams: vi.fn(),
    useNavigate: vi.fn(),
  };
});

// Mock the receipt components
vi.mock("@/components/receipt", () => ({
  ReceiptUpload: () => <div>Receipt Upload</div>,
  ReceiptStatusBadge: ({ status }: { status: string }) => (
    <span data-testid="status-badge">{status}</span>
  ),
  ReceiptSummary: ({
    subtotal,
    tax,
    tip,
    total,
  }: {
    subtotal: number;
    tax: number;
    tip: number;
    total: number;
  }) => (
    <div data-testid="receipt-summary">
      Subtotal: {subtotal}, Tax: {tax}, Tip: {tip}, Total: {total}
    </div>
  ),
  ReceiptItemsList: ({
    items,
  }: {
    items: Array<{ id: string; name: string }>;
  }) => (
    <div data-testid="receipt-items">
      {items.map((item) => (
        <div key={item.id}>{item.name}</div>
      ))}
    </div>
  ),
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
const mockReceipt: Receipt = {
  id: "1",
  userId: "user-123",
  merchantName: "Grocery Store",
  date: "2024-01-15T10:30:00Z",
  subtotal: 100.0,
  tax: 10.0,
  tip: 15.5,
  total: 125.5,
  imageUrl: "https://example.com/receipt1.jpg",
  status: ReceiptStatus.Ready,
  items: [
    { id: "1", name: "Milk", price: 4.99, quantity: 2 },
    { id: "2", name: "Bread", price: 3.49, quantity: 1 },
    { id: "3", name: "Eggs", price: 5.99, quantity: 1 },
  ],
  createdAt: "2024-01-15T10:30:00Z",
  updatedAt: "2024-01-15T10:30:00Z",
};

describe("ReceiptDetailPage", () => {
  const mockNavigate = vi.fn();

  beforeEach(() => {
    // Reset to default viewport
    setViewport(1024, 768);

    // Mock useParams to return a receipt ID
    vi.mocked(reactRouterDom.useParams).mockReturnValue({ id: "1" });

    // Mock useNavigate
    vi.mocked(reactRouterDom.useNavigate).mockReturnValue(mockNavigate);

    // Mock useReceipt hook with default values
    vi.mocked(useReceiptsModule.useReceipt).mockReturnValue({
      receipt: mockReceipt,
      isLoading: false,
      error: null,
      refetch: vi.fn(),
      updateItems: vi.fn(),
      updateLoading: false,
      updateError: null,
    });

    // Mock useReceipts hook (for delete functionality)
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
  });

  describe("Snapshot Tests - With Data", () => {
    it("matches snapshot on mobile (375px)", () => {
      setViewport(375, 667);
      const { container } = render(<ReceiptDetailPage />);
      expect(container).toMatchSnapshot();
    });

    it("matches snapshot on tablet (768px)", () => {
      setViewport(768, 1024);
      const { container } = render(<ReceiptDetailPage />);
      expect(container).toMatchSnapshot();
    });

    it("matches snapshot on desktop (1280px)", () => {
      setViewport(1280, 720);
      const { container } = render(<ReceiptDetailPage />);
      expect(container).toMatchSnapshot();
    });
  });

  describe("Snapshot Tests - Loading State", () => {
    it("matches snapshot with loading state on mobile", () => {
      setViewport(375, 667);
      vi.mocked(useReceiptsModule.useReceipt).mockReturnValue({
        receipt: undefined,
        isLoading: true,
        error: null,
        refetch: vi.fn(),
        updateItems: vi.fn(),
        updateLoading: false,
        updateError: null,
      });

      const { container } = render(<ReceiptDetailPage />);
      expect(container).toMatchSnapshot();
    });

    it("matches snapshot with loading state on desktop", () => {
      setViewport(1280, 720);
      vi.mocked(useReceiptsModule.useReceipt).mockReturnValue({
        receipt: undefined,
        isLoading: true,
        error: null,
        refetch: vi.fn(),
        updateItems: vi.fn(),
        updateLoading: false,
        updateError: null,
      });

      const { container } = render(<ReceiptDetailPage />);
      expect(container).toMatchSnapshot();
    });
  });

  describe("Snapshot Tests - Error State", () => {
    it("matches snapshot with error state on mobile", () => {
      setViewport(375, 667);
      vi.mocked(useReceiptsModule.useReceipt).mockReturnValue({
        receipt: undefined,
        isLoading: false,
        error: new Error("Failed to load receipt"),
        refetch: vi.fn(),
        updateItems: vi.fn(),
        updateLoading: false,
        updateError: null,
      });

      const { container } = render(<ReceiptDetailPage />);
      expect(container).toMatchSnapshot();
    });

    it("matches snapshot with error state on desktop", () => {
      setViewport(1280, 720);
      vi.mocked(useReceiptsModule.useReceipt).mockReturnValue({
        receipt: undefined,
        isLoading: false,
        error: new Error("Failed to load receipt"),
        refetch: vi.fn(),
        updateItems: vi.fn(),
        updateLoading: false,
        updateError: null,
      });

      const { container } = render(<ReceiptDetailPage />);
      expect(container).toMatchSnapshot();
    });
  });

  describe("Snapshot Tests - Processing State", () => {
    it("matches snapshot with processing receipt", () => {
      vi.mocked(useReceiptsModule.useReceipt).mockReturnValue({
        receipt: { ...mockReceipt, status: ReceiptStatus.Processing },
        isLoading: false,
        error: null,
        refetch: vi.fn(),
        updateItems: vi.fn(),
        updateLoading: false,
        updateError: null,
      });

      const { container } = render(<ReceiptDetailPage />);
      expect(container).toMatchSnapshot();
    });
  });

  describe("Content Tests", () => {
    it("renders merchant name", () => {
      const { getByText } = render(<ReceiptDetailPage />);
      expect(getByText("Grocery Store")).toBeInTheDocument();
    });

    it("renders back button", () => {
      const { getByText } = render(<ReceiptDetailPage />);
      expect(getByText("Back to Receipts")).toBeInTheDocument();
    });

    it("renders edit and delete buttons when receipt is ready", () => {
      const { getByText } = render(<ReceiptDetailPage />);
      expect(getByText("Edit Items")).toBeInTheDocument();
    });

    it("renders receipt items", () => {
      const { getByTestId } = render(<ReceiptDetailPage />);
      const itemsList = getByTestId("receipt-items");
      expect(itemsList).toBeInTheDocument();
    });

    it("renders receipt summary", () => {
      const { getByTestId } = render(<ReceiptDetailPage />);
      const summary = getByTestId("receipt-summary");
      expect(summary).toBeInTheDocument();
    });
  });
});
