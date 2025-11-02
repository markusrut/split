import { apiClient } from "./client";
import type {
  Receipt,
  ReceiptListItem,
  UpdateReceiptItemsRequest,
  OcrDebugInfo,
} from "@/types";

export const receiptsApi = {
  // Upload receipt
  upload: async (file: File): Promise<Receipt> => {
    const formData = new FormData();
    formData.append("file", file);

    const response = await apiClient.post<Receipt>("/receipts", formData, {
      headers: {
        "Content-Type": "multipart/form-data",
      },
    });
    return response.data;
  },

  // Get all receipts for current user
  getAll: async (): Promise<ReceiptListItem[]> => {
    const response = await apiClient.get<ReceiptListItem[]>("/receipts");
    return response.data;
  },

  // Get receipt by ID
  getById: async (id: string): Promise<Receipt> => {
    const response = await apiClient.get<Receipt>(`/receipts/${id}`);
    return response.data;
  },

  // Update receipt items (manual editing)
  updateItems: async (
    id: string,
    data: UpdateReceiptItemsRequest
  ): Promise<Receipt> => {
    const response = await apiClient.put<Receipt>(
      `/receipts/${id}/items`,
      data
    );
    return response.data;
  },

  // Delete receipt
  delete: async (id: string): Promise<void> => {
    await apiClient.delete(`/receipts/${id}`);
  },

  // Get OCR debug info (raw OCR result)
  getOcrDebug: async (id: string): Promise<OcrDebugInfo> => {
    const response = await apiClient.get<OcrDebugInfo>(`/receipts/${id}/ocr`);
    return response.data;
  },
};
