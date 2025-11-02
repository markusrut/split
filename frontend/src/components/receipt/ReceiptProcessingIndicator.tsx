import { Loader2, AlertCircle } from "lucide-react";
import { ReceiptStatus } from "@/types";

export interface ReceiptProcessingIndicatorProps {
  status: ReceiptStatus;
  errorMessage?: string;
  confidence?: number;
  className?: string;
}

export const ReceiptProcessingIndicator = ({
  status,
  errorMessage,
  confidence,
  className = "",
}: ReceiptProcessingIndicatorProps) => {
  const getStatusMessage = (status: ReceiptStatus) => {
    switch (status) {
      case ReceiptStatus.Uploaded:
        return "Receipt uploaded. Starting OCR processing...";
      case ReceiptStatus.OcrInProgress:
        return "Extracting text from receipt image...";
      case ReceiptStatus.OcrCompleted:
        return "OCR complete. Parsing receipt items...";
      case ReceiptStatus.Processing:
        return "Processing receipt data...";
      case ReceiptStatus.Ready:
        return confidence !== undefined
          ? `Receipt processed successfully! (${(confidence * 100).toFixed(0)}% confidence)`
          : "Receipt processed successfully!";
      case ReceiptStatus.ParseFailed:
        return "Failed to parse receipt items. Please edit manually.";
      case ReceiptStatus.Failed:
        return "Receipt processing failed.";
      default:
        return "Unknown status";
    }
  };

  const isProcessing = (status: ReceiptStatus) => {
    return (
      status === ReceiptStatus.Uploaded ||
      status === ReceiptStatus.OcrInProgress ||
      status === ReceiptStatus.OcrCompleted
    );
  };

  const isFailed = (status: ReceiptStatus) => {
    return (
      status === ReceiptStatus.ParseFailed || status === ReceiptStatus.Failed
    );
  };

  if (status === ReceiptStatus.Ready && !confidence) {
    // Don't show indicator for ready receipts without confidence (likely manually edited)
    return null;
  }

  return (
    <div className={`rounded-lg border p-4 ${className}`}>
      {isProcessing(status) && (
        <div className="flex items-start gap-3">
          <Loader2 className="h-5 w-5 animate-spin text-yellow-600 dark:text-yellow-400 mt-0.5 flex-shrink-0" />
          <div className="flex-1">
            <p className="text-sm font-medium text-gray-900 dark:text-gray-100">
              {getStatusMessage(status)}
            </p>
            <p className="text-xs text-gray-600 dark:text-gray-400 mt-1">
              This may take a few moments. You'll be notified when it's ready.
            </p>
          </div>
        </div>
      )}

      {status === ReceiptStatus.Ready && confidence !== undefined && (
        <div className="flex items-start gap-3">
          <div className="h-5 w-5 rounded-full bg-green-100 dark:bg-green-900 flex items-center justify-center mt-0.5 flex-shrink-0">
            <div className="h-2 w-2 rounded-full bg-green-600 dark:bg-green-400" />
          </div>
          <div className="flex-1">
            <p className="text-sm font-medium text-gray-900 dark:text-gray-100">
              {getStatusMessage(status)}
            </p>
            {confidence < 0.8 && (
              <p className="text-xs text-yellow-700 dark:text-yellow-400 mt-1">
                Low confidence detected. Please review the extracted items for
                accuracy.
              </p>
            )}
          </div>
        </div>
      )}

      {isFailed(status) && (
        <div className="flex items-start gap-3">
          <AlertCircle className="h-5 w-5 text-red-600 dark:text-red-400 mt-0.5 flex-shrink-0" />
          <div className="flex-1">
            <p className="text-sm font-medium text-gray-900 dark:text-gray-100">
              {getStatusMessage(status)}
            </p>
            {errorMessage && (
              <p className="text-xs text-red-700 dark:text-red-400 mt-1 font-mono">
                {errorMessage}
              </p>
            )}
            <p className="text-xs text-gray-600 dark:text-gray-400 mt-2">
              You can still manually add items to this receipt.
            </p>
          </div>
        </div>
      )}
    </div>
  );
};
