import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import { ReceiptStatusBadge } from "./ReceiptStatusBadge";
import { ReceiptStatus } from "@/types";

describe("ReceiptStatusBadge", () => {
  it("renders Ready status correctly", () => {
    render(<ReceiptStatusBadge status={ReceiptStatus.Ready} />);
    expect(screen.getByText("Ready")).toBeInTheDocument();
    expect(screen.queryByRole("img", { hidden: true })).not.toBeInTheDocument(); // No spinner
  });

  it("renders Uploaded status with spinner", () => {
    render(<ReceiptStatusBadge status={ReceiptStatus.Uploaded} />);
    expect(screen.getByText("Uploaded")).toBeInTheDocument();
    // Spinner should be present for processing statuses
    const badge = screen.getByText("Uploaded").closest("span");
    expect(badge?.querySelector("svg")).toBeInTheDocument();
  });

  it("renders OcrInProgress status with spinner", () => {
    render(<ReceiptStatusBadge status={ReceiptStatus.OcrInProgress} />);
    expect(screen.getByText("Processing OCR...")).toBeInTheDocument();
    const badge = screen.getByText("Processing OCR...").closest("span");
    expect(badge?.querySelector("svg")).toBeInTheDocument();
  });

  it("renders OcrCompleted status with spinner", () => {
    render(<ReceiptStatusBadge status={ReceiptStatus.OcrCompleted} />);
    expect(screen.getByText("Parsing...")).toBeInTheDocument();
    const badge = screen.getByText("Parsing...").closest("span");
    expect(badge?.querySelector("svg")).toBeInTheDocument();
  });

  it("renders Processing status with spinner", () => {
    render(<ReceiptStatusBadge status={ReceiptStatus.Processing} />);
    expect(screen.getByText("Processing")).toBeInTheDocument();
    const badge = screen.getByText("Processing").closest("span");
    expect(badge?.querySelector("svg")).toBeInTheDocument();
  });

  it("renders ParseFailed status correctly", () => {
    render(<ReceiptStatusBadge status={ReceiptStatus.ParseFailed} />);
    expect(screen.getByText("Parse Failed")).toBeInTheDocument();
    expect(screen.queryByRole("img", { hidden: true })).not.toBeInTheDocument(); // No spinner
  });

  it("renders Failed status correctly", () => {
    render(<ReceiptStatusBadge status={ReceiptStatus.Failed} />);
    expect(screen.getByText("Failed")).toBeInTheDocument();
    expect(screen.queryByRole("img", { hidden: true })).not.toBeInTheDocument(); // No spinner
  });

  it("applies custom className", () => {
    const { container } = render(
      <ReceiptStatusBadge
        status={ReceiptStatus.Ready}
        className="custom-class"
      />
    );
    expect(container.querySelector(".custom-class")).toBeInTheDocument();
  });

  it("applies correct color classes for Ready status", () => {
    const { container } = render(
      <ReceiptStatusBadge status={ReceiptStatus.Ready} />
    );
    const badge = container.querySelector("span");
    expect(badge?.className).toMatch(/bg-green-100/);
    expect(badge?.className).toMatch(/text-green-800/);
  });

  it("applies correct color classes for processing statuses", () => {
    const { container } = render(
      <ReceiptStatusBadge status={ReceiptStatus.OcrInProgress} />
    );
    const badge = container.querySelector("span");
    expect(badge?.className).toMatch(/bg-yellow-100/);
    expect(badge?.className).toMatch(/text-yellow-800/);
  });

  it("applies correct color classes for failed statuses", () => {
    const { container } = render(
      <ReceiptStatusBadge status={ReceiptStatus.Failed} />
    );
    const badge = container.querySelector("span");
    expect(badge?.className).toMatch(/bg-red-100/);
    expect(badge?.className).toMatch(/text-red-800/);
  });
});
