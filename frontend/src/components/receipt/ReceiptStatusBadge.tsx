import { Loader2 } from "lucide-react";
import { ReceiptStatus } from "@/types";

export interface ReceiptStatusBadgeProps {
  status: ReceiptStatus;
  className?: string;
}

export const ReceiptStatusBadge = ({
  status,
  className = "",
}: ReceiptStatusBadgeProps) => {
  const getStatusColor = (status: ReceiptStatus) => {
    switch (status) {
      case ReceiptStatus.Ready:
        return "bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-300";
      case ReceiptStatus.Uploaded:
      case ReceiptStatus.OcrInProgress:
      case ReceiptStatus.OcrCompleted:
        return "bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-300";
      case ReceiptStatus.ParseFailed:
      case ReceiptStatus.Failed:
        return "bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-300";
      default:
        return "bg-gray-100 text-gray-800 dark:bg-gray-900 dark:text-gray-300";
    }
  };

  const getStatusLabel = (status: ReceiptStatus) => {
    switch (status) {
      case ReceiptStatus.Uploaded:
        return "Uploaded";
      case ReceiptStatus.OcrInProgress:
        return "Processing OCR...";
      case ReceiptStatus.OcrCompleted:
        return "Parsing...";
      case ReceiptStatus.Processing:
        return "Processing";
      case ReceiptStatus.Ready:
        return "Ready";
      case ReceiptStatus.ParseFailed:
        return "Parse Failed";
      case ReceiptStatus.Failed:
        return "Failed";
      default:
        return status;
    }
  };

  const isProcessing = (status: ReceiptStatus) => {
    return (
      status === ReceiptStatus.Uploaded ||
      status === ReceiptStatus.OcrInProgress ||
      status === ReceiptStatus.OcrCompleted ||
      status === ReceiptStatus.Processing
    );
  };

  return (
    <span
      className={`px-2 py-1 text-xs font-medium rounded-full ${getStatusColor(status)} ${className}`}
    >
      {isProcessing(status) ? (
        <span className="inline-flex items-center gap-1">
          <Loader2 className="h-3 w-3 animate-spin" />
          {getStatusLabel(status)}
        </span>
      ) : (
        getStatusLabel(status)
      )}
    </span>
  );
};
