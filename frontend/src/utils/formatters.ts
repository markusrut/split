/**
 * Format a number as currency (USD)
 * @param amount - The amount to format
 * @param locale - Optional locale string (defaults to user's locale)
 * @returns Formatted currency string (e.g., "$123.45")
 */
export const formatCurrency = (amount: number, locale?: string): string => {
  return new Intl.NumberFormat(locale, {
    style: "currency",
    currency: "USD",
  }).format(amount);
};

/**
 * Format a date string to a readable date
 * @param dateString - ISO date string or Date object
 * @param locale - Optional locale string (defaults to user's locale)
 * @returns Formatted date string (e.g., "Jan 15, 2024")
 */
export const formatDate = (
  dateString: string | Date,
  locale?: string
): string => {
  const date =
    typeof dateString === "string" ? new Date(dateString) : dateString;
  return date.toLocaleDateString(locale, {
    year: "numeric",
    month: "short",
    day: "numeric",
  });
};

/**
 * Format a date string to a readable date with full month name
 * @param dateString - ISO date string or Date object
 * @param locale - Optional locale string (defaults to user's locale)
 * @returns Formatted date string (e.g., "January 15, 2024")
 */
export const formatDateLong = (
  dateString: string | Date,
  locale?: string
): string => {
  const date =
    typeof dateString === "string" ? new Date(dateString) : dateString;
  return date.toLocaleDateString(locale, {
    year: "numeric",
    month: "long",
    day: "numeric",
  });
};

/**
 * Format a date string to include time
 * @param dateString - ISO date string or Date object
 * @param locale - Optional locale string (defaults to user's locale)
 * @returns Formatted date and time string (e.g., "Jan 15, 2024, 3:30 PM")
 */
export const formatDateTime = (
  dateString: string | Date,
  locale?: string
): string => {
  const date =
    typeof dateString === "string" ? new Date(dateString) : dateString;
  return date.toLocaleString(locale, {
    year: "numeric",
    month: "short",
    day: "numeric",
    hour: "numeric",
    minute: "2-digit",
    hour12: true,
  });
};

/**
 * Format a date to relative time (e.g., "2 hours ago", "3 days ago")
 * @param dateString - ISO date string or Date object
 * @returns Relative time string
 */
export const formatRelativeTime = (dateString: string | Date): string => {
  const date =
    typeof dateString === "string" ? new Date(dateString) : dateString;
  const now = new Date();
  const diffInMs = now.getTime() - date.getTime();
  const diffInSeconds = Math.floor(diffInMs / 1000);
  const diffInMinutes = Math.floor(diffInSeconds / 60);
  const diffInHours = Math.floor(diffInMinutes / 60);
  const diffInDays = Math.floor(diffInHours / 24);

  if (diffInSeconds < 60) {
    return "just now";
  } else if (diffInMinutes < 60) {
    return `${diffInMinutes} ${diffInMinutes === 1 ? "minute" : "minutes"} ago`;
  } else if (diffInHours < 24) {
    return `${diffInHours} ${diffInHours === 1 ? "hour" : "hours"} ago`;
  } else if (diffInDays < 7) {
    return `${diffInDays} ${diffInDays === 1 ? "day" : "days"} ago`;
  } else {
    return formatDate(date);
  }
};

/**
 * Format a file size in bytes to a human-readable string
 * @param bytes - File size in bytes
 * @returns Formatted file size (e.g., "1.5 MB")
 */
export const formatFileSize = (bytes: number): string => {
  if (bytes === 0) return "0 Bytes";

  const k = 1024;
  const sizes = ["Bytes", "KB", "MB", "GB"];
  const i = Math.floor(Math.log(bytes) / Math.log(k));

  return `${parseFloat((bytes / Math.pow(k, i)).toFixed(2))} ${sizes[i]}`;
};

/**
 * Format a number as a percentage
 * @param value - Number between 0 and 1 (or 0-100 if isWhole is true)
 * @param decimals - Number of decimal places (default: 0)
 * @param isWhole - Whether the value is already a whole number percentage (default: false)
 * @returns Formatted percentage string (e.g., "75%")
 */
export const formatPercentage = (
  value: number,
  decimals: number = 0,
  isWhole: boolean = false
): string => {
  const percentage = isWhole ? value : value * 100;
  return `${percentage.toFixed(decimals)}%`;
};

/**
 * Truncate a string to a maximum length and add ellipsis
 * @param text - The text to truncate
 * @param maxLength - Maximum length before truncation
 * @returns Truncated string with ellipsis if needed
 */
export const truncateText = (text: string, maxLength: number): string => {
  if (text.length <= maxLength) return text;
  return `${text.slice(0, maxLength)}...`;
};
