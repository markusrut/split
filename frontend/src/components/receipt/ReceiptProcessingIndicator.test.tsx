import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import { ReceiptProcessingIndicator } from "./ReceiptProcessingIndicator";
import { ReceiptStatus } from "@/types";

describe("ReceiptProcessingIndicator", () => {
  describe("Processing states", () => {
    it("renders Uploaded status with spinner and message", () => {
      const { container } = render(
        <ReceiptProcessingIndicator status={ReceiptStatus.Uploaded} />
      );
      expect(
        screen.getByText(/Receipt uploaded. Starting OCR processing/i)
      ).toBeInTheDocument();
      expect(
        screen.getByText(/This may take a few moments/i)
      ).toBeInTheDocument();
      // Check for spinner - should exist in the component
      const svg = container.querySelector("svg");
      expect(svg).not.toBeNull();
    });

    it("renders OcrInProgress status with spinner and message", () => {
      render(
        <ReceiptProcessingIndicator status={ReceiptStatus.OcrInProgress} />
      );
      expect(
        screen.getByText(/Extracting text from receipt image/i)
      ).toBeInTheDocument();
      expect(
        screen.getByText(/This may take a few moments/i)
      ).toBeInTheDocument();
    });

    it("renders OcrCompleted status with spinner and message", () => {
      render(
        <ReceiptProcessingIndicator status={ReceiptStatus.OcrCompleted} />
      );
      expect(
        screen.getByText(/OCR complete. Parsing receipt items/i)
      ).toBeInTheDocument();
      expect(
        screen.getByText(/This may take a few moments/i)
      ).toBeInTheDocument();
    });

    it("renders Processing status with spinner and message", () => {
      render(<ReceiptProcessingIndicator status={ReceiptStatus.Processing} />);
      expect(screen.getByText(/Processing receipt data/i)).toBeInTheDocument();
      expect(
        screen.getByText(/This may take a few moments/i)
      ).toBeInTheDocument();
    });
  });

  describe("Ready state", () => {
    it("renders Ready status with high confidence", () => {
      render(
        <ReceiptProcessingIndicator
          status={ReceiptStatus.Ready}
          confidence={0.95}
        />
      );
      expect(
        screen.getByText(/Receipt processed successfully! \(95% confidence\)/i)
      ).toBeInTheDocument();
      expect(
        screen.queryByText(/Low confidence detected/i)
      ).not.toBeInTheDocument();
    });

    it("shows warning for low confidence (< 0.8)", () => {
      render(
        <ReceiptProcessingIndicator
          status={ReceiptStatus.Ready}
          confidence={0.65}
        />
      );
      expect(
        screen.getByText(/Receipt processed successfully! \(65% confidence\)/i)
      ).toBeInTheDocument();
      expect(screen.getByText(/Low confidence detected/i)).toBeInTheDocument();
      expect(
        screen.getByText(/Please review the extracted items for accuracy/i)
      ).toBeInTheDocument();
    });

    it("does not render for Ready status without confidence", () => {
      const { container } = render(
        <ReceiptProcessingIndicator status={ReceiptStatus.Ready} />
      );
      expect(container.firstChild).toBeNull();
    });
  });

  describe("Failed states", () => {
    it("renders ParseFailed status with error icon", () => {
      render(<ReceiptProcessingIndicator status={ReceiptStatus.ParseFailed} />);
      expect(
        screen.getByText(/Failed to parse receipt items/i)
      ).toBeInTheDocument();
      expect(
        screen.getByText(/You can still manually add items/i)
      ).toBeInTheDocument();
    });

    it("renders Failed status with error icon", () => {
      render(<ReceiptProcessingIndicator status={ReceiptStatus.Failed} />);
      expect(
        screen.getByText(/Receipt processing failed/i)
      ).toBeInTheDocument();
      expect(
        screen.getByText(/You can still manually add items/i)
      ).toBeInTheDocument();
    });

    it("displays error message when provided", () => {
      const errorMessage = "OCR service unavailable";
      render(
        <ReceiptProcessingIndicator
          status={ReceiptStatus.Failed}
          errorMessage={errorMessage}
        />
      );
      expect(screen.getByText(errorMessage)).toBeInTheDocument();
    });

    it("does not display error message when not provided", () => {
      render(<ReceiptProcessingIndicator status={ReceiptStatus.Failed} />);
      expect(
        screen.queryByText(/OCR service unavailable/i)
      ).not.toBeInTheDocument();
    });
  });

  describe("Styling", () => {
    it("applies custom className", () => {
      const { container } = render(
        <ReceiptProcessingIndicator
          status={ReceiptStatus.Processing}
          className="custom-test-class"
        />
      );
      expect(container.querySelector(".custom-test-class")).toBeInTheDocument();
    });

    it("has correct border and padding classes", () => {
      const { container } = render(
        <ReceiptProcessingIndicator status={ReceiptStatus.Processing} />
      );
      const mainDiv = container.querySelector("div");
      expect(mainDiv?.className).toMatch(/rounded-lg/);
      expect(mainDiv?.className).toMatch(/border/);
      expect(mainDiv?.className).toMatch(/p-4/);
    });
  });

  describe("Snapshots", () => {
    it("matches snapshot for Uploaded status", () => {
      const { container } = render(
        <ReceiptProcessingIndicator status={ReceiptStatus.Uploaded} />
      );
      expect(container.firstChild).toMatchSnapshot();
    });

    it("matches snapshot for OcrInProgress status", () => {
      const { container } = render(
        <ReceiptProcessingIndicator status={ReceiptStatus.OcrInProgress} />
      );
      expect(container.firstChild).toMatchSnapshot();
    });

    it("matches snapshot for Ready with high confidence", () => {
      const { container } = render(
        <ReceiptProcessingIndicator
          status={ReceiptStatus.Ready}
          confidence={0.95}
        />
      );
      expect(container.firstChild).toMatchSnapshot();
    });

    it("matches snapshot for Ready with low confidence", () => {
      const { container } = render(
        <ReceiptProcessingIndicator
          status={ReceiptStatus.Ready}
          confidence={0.65}
        />
      );
      expect(container.firstChild).toMatchSnapshot();
    });

    it("matches snapshot for ParseFailed status", () => {
      const { container } = render(
        <ReceiptProcessingIndicator status={ReceiptStatus.ParseFailed} />
      );
      expect(container.firstChild).toMatchSnapshot();
    });

    it("matches snapshot for Failed status with error message", () => {
      const { container } = render(
        <ReceiptProcessingIndicator
          status={ReceiptStatus.Failed}
          errorMessage="OCR service timeout"
        />
      );
      expect(container.firstChild).toMatchSnapshot();
    });
  });
});
