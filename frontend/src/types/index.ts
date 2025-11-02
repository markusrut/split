// User types
export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  createdAt: string;
}

// Auth request/response types
export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
}

export interface AuthResponse {
  token: string;
  user: User;
}

// Receipt types
export const ReceiptStatus = {
  Uploaded: "Uploaded",
  OcrInProgress: "OcrInProgress",
  OcrCompleted: "OcrCompleted",
  Ready: "Ready",
  ParseFailed: "ParseFailed",
  Failed: "Failed",
} as const;

export type ReceiptStatus = (typeof ReceiptStatus)[keyof typeof ReceiptStatus];

export interface ReceiptItem {
  id: string;
  name: string;
  price: number;
  quantity?: number;
  lineNumber?: number; // NEW: Line number on receipt for ordering
}

export interface Receipt {
  id: string;
  userId: string;
  merchantName: string;
  date: string;
  subtotal: number;
  tax: number;
  tip: number;
  total: number;
  imageUrl: string;
  status: ReceiptStatus;
  items: ReceiptItem[];
  createdAt: string;
  updatedAt: string;
  ocrConfidence?: number; // 0-1 (0.0 to 1.0)
  processedAt?: string;
  errorMessage?: string;
}

export interface ReceiptListItem {
  id: string;
  merchantName: string;
  date: string;
  total: number;
  imageUrl: string;
  status: ReceiptStatus;
  createdAt: string;
}

export interface UpdateReceiptItemsRequest {
  items: Omit<ReceiptItem, "id">[];
}

export interface OcrDebugInfo {
  receiptId: string;
  status: ReceiptStatus;
  processedAt?: string;
  confidence?: number;
  rawOcrResult: unknown;
}

// API Error type
export interface ApiError {
  message: string;
  errors?: Record<string, string[]>;
}
