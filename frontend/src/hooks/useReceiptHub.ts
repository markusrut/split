import { useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import {
  createReceiptHubConnection,
  stopReceiptHubConnection,
} from "@/api/signalr";
import { receiptsApi } from "@/api/receipts";

/**
 * Hook to manage SignalR connection for real-time receipt updates
 * @param enabled Whether to enable the SignalR connection
 */
export const useReceiptHub = (enabled: boolean = true) => {
  const queryClient = useQueryClient();

  useEffect(() => {
    if (!enabled) return;

    createReceiptHubConnection(
      // onStatusUpdate callback
      async (data) => {
        console.log(
          `[useReceiptHub] Status update: Receipt ${data.receiptId} -> ${data.status}`
        );

        try {
          // Fetch the updated receipt from the server
          const updatedReceipt = await receiptsApi.getById(data.receiptId);

          // Update the specific receipt in cache
          queryClient.setQueryData(
            ["receipts", data.receiptId],
            updatedReceipt
          );

          // Invalidate the receipts list to refresh it
          queryClient.invalidateQueries({ queryKey: ["receipts"] });
        } catch (error) {
          console.error(
            `[useReceiptHub] Failed to fetch updated receipt ${data.receiptId}:`,
            error
          );
        }
      },
      // onProcessed callback
      async (data) => {
        console.log(
          `[useReceiptHub] Receipt ${data.receiptId} processed: ${data.status} (${data.itemCount} items, confidence: ${data.confidence?.toFixed(2)})`
        );

        try {
          // Fetch the final receipt with all items
          const updatedReceipt = await receiptsApi.getById(data.receiptId);

          // Update cache
          queryClient.setQueryData(
            ["receipts", data.receiptId],
            updatedReceipt
          );

          // Invalidate list
          queryClient.invalidateQueries({ queryKey: ["receipts"] });
        } catch (error) {
          console.error(
            `[useReceiptHub] Failed to fetch processed receipt ${data.receiptId}:`,
            error
          );
        }
      }
    );

    // Cleanup: Disconnect on unmount
    return () => {
      stopReceiptHubConnection();
    };
  }, [enabled, queryClient]);
};
