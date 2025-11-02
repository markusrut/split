import { useEffect, useRef } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { receiptsApi } from "@/api/receipts";
import { ReceiptStatus } from "@/types";

const POLL_INTERVAL_MS = 3000; // Poll every 3 seconds
const MAX_POLL_ATTEMPTS = 60; // Stop after 3 minutes (60 * 3s = 180s)

/**
 * Hook to poll receipt status when it's processing
 * Used as a fallback if SignalR is not available
 *
 * @param receiptId The receipt ID to poll
 * @param currentStatus Current status of the receipt
 * @param enabled Whether polling is enabled
 */
export const useReceiptPolling = (
  receiptId: string | undefined,
  currentStatus: ReceiptStatus | undefined,
  enabled: boolean = true
) => {
  const queryClient = useQueryClient();
  const pollCountRef = useRef(0);
  const intervalRef = useRef<number | null>(null);

  useEffect(() => {
    // Don't poll if disabled or no receipt ID
    if (!enabled || !receiptId || !currentStatus) {
      return;
    }

    // Only poll if receipt is in a processing state
    const isProcessing =
      currentStatus === ReceiptStatus.Uploaded ||
      currentStatus === ReceiptStatus.OcrInProgress ||
      currentStatus === ReceiptStatus.OcrCompleted;

    if (!isProcessing) {
      // Clear interval if status is final
      if (intervalRef.current) {
        clearInterval(intervalRef.current);
        intervalRef.current = null;
        pollCountRef.current = 0;
      }
      return;
    }

    // Reset poll count when starting
    pollCountRef.current = 0;

    console.log(
      `[useReceiptPolling] Starting polling for receipt ${receiptId} (status: ${currentStatus})`
    );

    // Start polling
    intervalRef.current = setInterval(async () => {
      pollCountRef.current += 1;

      console.log(
        `[useReceiptPolling] Poll attempt ${pollCountRef.current}/${MAX_POLL_ATTEMPTS} for receipt ${receiptId}`
      );

      try {
        const updatedReceipt = await receiptsApi.getById(receiptId);

        // Update cache
        queryClient.setQueryData(["receipts", receiptId], updatedReceipt);
        queryClient.invalidateQueries({ queryKey: ["receipts"] });

        // Check if we should stop polling
        const isFinalStatus =
          updatedReceipt.status === ReceiptStatus.Ready ||
          updatedReceipt.status === ReceiptStatus.Failed ||
          updatedReceipt.status === ReceiptStatus.ParseFailed;

        if (isFinalStatus) {
          console.log(
            `[useReceiptPolling] Receipt ${receiptId} reached final status: ${updatedReceipt.status}. Stopping poll.`
          );
          if (intervalRef.current) {
            clearInterval(intervalRef.current);
            intervalRef.current = null;
            pollCountRef.current = 0;
          }
        } else if (pollCountRef.current >= MAX_POLL_ATTEMPTS) {
          console.warn(
            `[useReceiptPolling] Max poll attempts reached for receipt ${receiptId}. Stopping poll.`
          );
          if (intervalRef.current) {
            clearInterval(intervalRef.current);
            intervalRef.current = null;
            pollCountRef.current = 0;
          }
        }
      } catch (error) {
        console.error(
          `[useReceiptPolling] Polling error for receipt ${receiptId}:`,
          error
        );
        // Continue polling even on error (might be a temporary network issue)
      }
    }, POLL_INTERVAL_MS);

    // Cleanup on unmount or dependency change
    return () => {
      if (intervalRef.current) {
        console.log(
          `[useReceiptPolling] Cleanup: Stopping poll for receipt ${receiptId}`
        );
        clearInterval(intervalRef.current);
        intervalRef.current = null;
        pollCountRef.current = 0;
      }
    };
  }, [receiptId, currentStatus, enabled, queryClient]);
};
