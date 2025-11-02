import { useState } from "react";
import { Link } from "react-router-dom";
import {
  Plus,
  Receipt as ReceiptIcon,
  Calendar,
  DollarSign,
} from "lucide-react";
import { Layout } from "@/components/layout";
import { ReceiptUpload, ReceiptStatusBadge } from "@/components/receipt";
import { Button, Card, Loading, ErrorMessage } from "@/components/ui";
import { useReceipts } from "@/hooks/useReceipts";
import { useReceiptHub } from "@/hooks/useReceiptHub";

export const ReceiptsPage = () => {
  const [showUpload, setShowUpload] = useState(false);
  const {
    receipts,
    isLoading,
    error,
    uploadReceipt,
    uploadLoading,
    uploadError,
  } = useReceipts();

  // Enable SignalR for real-time updates on receipt list
  useReceiptHub(true);

  const handleUpload = (file: File) => {
    uploadReceipt(file);
    // Upload success will navigate to detail page via useReceipts hook
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString(undefined, {
      year: "numeric",
      month: "short",
      day: "numeric",
    });
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat(undefined, {
      style: "currency",
      currency: "USD",
    }).format(amount);
  };

  return (
    <Layout>
      <div className="max-w-4xl mx-auto">
        <div className="flex justify-between items-center mb-6">
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white">
            My Receipts
          </h1>
          <Button
            onClick={() => setShowUpload(!showUpload)}
            className="gap-2"
            disabled={uploadLoading}
          >
            <Plus className="h-5 w-5" />
            {showUpload ? "Cancel" : "New Receipt"}
          </Button>
        </div>

        {showUpload && (
          <Card className="mb-6" padding="lg">
            <ReceiptUpload onUpload={handleUpload} uploading={uploadLoading} />
            {uploadError && (
              <ErrorMessage
                title="Upload Failed"
                message={
                  (uploadError as Error).message ||
                  "Failed to upload receipt. Please try again."
                }
                dismissible
                className="mt-4"
              />
            )}
          </Card>
        )}

        {isLoading ? (
          <div className="space-y-4">
            <Loading.Skeleton className="h-32" />
            <Loading.Skeleton className="h-32" />
            <Loading.Skeleton className="h-32" />
          </div>
        ) : error ? (
          <ErrorMessage
            title="Failed to Load Receipts"
            message={
              (error as Error).message ||
              "Unable to load receipts. Please try again."
            }
          />
        ) : !receipts || receipts.length === 0 ? (
          <Card padding="lg" className="text-center">
            <div className="py-8">
              <div className="inline-flex items-center justify-center w-16 h-16 rounded-full bg-gray-100 dark:bg-gray-800 mb-4">
                <ReceiptIcon className="h-8 w-8 text-gray-400 dark:text-gray-600" />
              </div>
              <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-2">
                No receipts yet
              </h3>
              <p className="text-gray-600 dark:text-gray-400 mb-4">
                Upload your first receipt to get started
              </p>
              <Button onClick={() => setShowUpload(true)} className="gap-2">
                <Plus className="h-5 w-5" />
                Upload Receipt
              </Button>
            </div>
          </Card>
        ) : (
          <div className="space-y-4">
            {receipts.map((receipt) => (
              <Link key={receipt.id} to={`/receipts/${receipt.id}`}>
                <Card
                  padding="none"
                  className="hover:shadow-lg transition-shadow cursor-pointer"
                >
                  <div className="flex gap-4 p-4">
                    <div className="shrink-0">
                      <img
                        src={receipt.imageUrl}
                        alt={`Receipt from ${receipt.merchantName}`}
                        className="w-20 h-20 object-cover rounded border border-gray-200 dark:border-gray-700"
                      />
                    </div>

                    <div className="flex-1 min-w-0">
                      <div className="flex items-start justify-between gap-2 mb-2">
                        <h3 className="text-lg font-semibold text-gray-900 dark:text-white truncate">
                          {receipt.merchantName || "Unknown Merchant"}
                        </h3>
                        <ReceiptStatusBadge
                          status={receipt.status}
                          className="shrink-0"
                        />
                      </div>

                      <div className="flex flex-wrap gap-4 text-sm text-gray-600 dark:text-gray-400">
                        <div className="flex items-center gap-1">
                          <Calendar className="h-4 w-4" />
                          <span>{formatDate(receipt.date)}</span>
                        </div>
                        <div className="flex items-center gap-1">
                          <DollarSign className="h-4 w-4" />
                          <span className="font-semibold text-gray-900 dark:text-white">
                            {formatCurrency(receipt.total)}
                          </span>
                        </div>
                      </div>
                    </div>
                  </div>
                </Card>
              </Link>
            ))}
          </div>
        )}
      </div>
    </Layout>
  );
};
