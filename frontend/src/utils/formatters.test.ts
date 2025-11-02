import { describe, it, expect } from "vitest";
import {
  formatCurrency,
  formatDate,
  formatDateLong,
  formatDateTime,
  formatRelativeTime,
  formatFileSize,
  formatPercentage,
  truncateText,
} from "./formatters";

describe("formatters", () => {
  describe("formatCurrency", () => {
    it("should format positive amounts", () => {
      expect(formatCurrency(100)).toBe("$100.00");
      expect(formatCurrency(1234.56)).toBe("$1,234.56");
    });

    it("should format zero", () => {
      expect(formatCurrency(0)).toBe("$0.00");
    });

    it("should format negative amounts", () => {
      expect(formatCurrency(-50.25)).toBe("-$50.25");
    });

    it("should format decimals correctly", () => {
      expect(formatCurrency(10.5)).toBe("$10.50");
      expect(formatCurrency(10.005)).toBe("$10.01");
    });
  });

  describe("formatDate", () => {
    it("should format ISO date strings", () => {
      const result = formatDate("2024-01-15T10:30:00Z");
      expect(result).toMatch(/Jan 15, 2024/);
    });

    it("should format Date objects", () => {
      const date = new Date("2024-12-25T00:00:00Z");
      const result = formatDate(date);
      expect(result).toMatch(/Dec 25, 2024/);
    });
  });

  describe("formatDateLong", () => {
    it("should format with full month name", () => {
      const result = formatDateLong("2024-03-20T10:30:00Z");
      expect(result).toMatch(/March 20, 2024/);
    });

    it("should format Date objects with full month", () => {
      const date = new Date("2024-07-04T00:00:00Z");
      const result = formatDateLong(date);
      expect(result).toMatch(/July 4, 2024/);
    });
  });

  describe("formatDateTime", () => {
    it("should format date with time", () => {
      const result = formatDateTime("2024-01-15T14:30:00Z");
      // Result will vary by timezone, just check it contains date and time elements
      expect(result).toMatch(/2024/);
      expect(result).toMatch(/PM|AM/);
    });
  });

  describe("formatRelativeTime", () => {
    it("should return 'just now' for very recent dates", () => {
      const now = new Date();
      expect(formatRelativeTime(now)).toBe("just now");
    });

    it("should return minutes ago", () => {
      const date = new Date(Date.now() - 5 * 60 * 1000); // 5 minutes ago
      expect(formatRelativeTime(date)).toBe("5 minutes ago");
    });

    it("should return hours ago", () => {
      const date = new Date(Date.now() - 3 * 60 * 60 * 1000); // 3 hours ago
      expect(formatRelativeTime(date)).toBe("3 hours ago");
    });

    it("should return days ago", () => {
      const date = new Date(Date.now() - 2 * 24 * 60 * 60 * 1000); // 2 days ago
      expect(formatRelativeTime(date)).toBe("2 days ago");
    });

    it("should return formatted date for old dates", () => {
      const date = new Date(Date.now() - 10 * 24 * 60 * 60 * 1000); // 10 days ago
      const result = formatRelativeTime(date);
      expect(result).not.toContain("ago");
      expect(result).toMatch(/\d{4}/); // Should contain year
    });

    it("should handle singular forms correctly", () => {
      const oneMinuteAgo = new Date(Date.now() - 60 * 1000);
      expect(formatRelativeTime(oneMinuteAgo)).toBe("1 minute ago");

      const oneHourAgo = new Date(Date.now() - 60 * 60 * 1000);
      expect(formatRelativeTime(oneHourAgo)).toBe("1 hour ago");

      const oneDayAgo = new Date(Date.now() - 24 * 60 * 60 * 1000);
      expect(formatRelativeTime(oneDayAgo)).toBe("1 day ago");
    });
  });

  describe("formatFileSize", () => {
    it("should format bytes", () => {
      expect(formatFileSize(0)).toBe("0 Bytes");
      expect(formatFileSize(500)).toBe("500 Bytes");
    });

    it("should format kilobytes", () => {
      expect(formatFileSize(1024)).toBe("1 KB");
      expect(formatFileSize(1536)).toBe("1.5 KB");
    });

    it("should format megabytes", () => {
      expect(formatFileSize(1048576)).toBe("1 MB");
      expect(formatFileSize(2621440)).toBe("2.5 MB");
    });

    it("should format gigabytes", () => {
      expect(formatFileSize(1073741824)).toBe("1 GB");
      expect(formatFileSize(5368709120)).toBe("5 GB");
    });
  });

  describe("formatPercentage", () => {
    it("should format decimal values (0-1)", () => {
      expect(formatPercentage(0.5)).toBe("50%");
      expect(formatPercentage(0.75)).toBe("75%");
      expect(formatPercentage(1)).toBe("100%");
    });

    it("should format with decimals", () => {
      expect(formatPercentage(0.333, 2)).toBe("33.30%");
      expect(formatPercentage(0.6666, 1)).toBe("66.7%");
    });

    it("should handle whole number percentages", () => {
      expect(formatPercentage(75, 0, true)).toBe("75%");
      expect(formatPercentage(33.5, 1, true)).toBe("33.5%");
    });

    it("should handle zero", () => {
      expect(formatPercentage(0)).toBe("0%");
    });

    it("should handle values over 100%", () => {
      expect(formatPercentage(1.5)).toBe("150%");
      expect(formatPercentage(200, 0, true)).toBe("200%");
    });
  });

  describe("truncateText", () => {
    it("should not truncate short text", () => {
      expect(truncateText("Hello", 10)).toBe("Hello");
    });

    it("should truncate long text", () => {
      expect(truncateText("Hello World!", 5)).toBe("Hello...");
      expect(truncateText("This is a long text", 10)).toBe("This is a ...");
    });

    it("should handle text exactly at max length", () => {
      expect(truncateText("12345", 5)).toBe("12345");
    });

    it("should handle empty string", () => {
      expect(truncateText("", 5)).toBe("");
    });
  });
});
