import { useState } from "react";
import { useParams, useNavigate, Link } from "react-router-dom";
import { ArrowLeft, Calendar, Edit2, Save, X, Trash2 } from "lucide-react";
import { Layout } from "@/components/layout";
import { Button, Card, Loading, ErrorMessage } from "@/components/ui";
import {
  ReceiptStatusBadge,
  ReceiptSummary,
  ReceiptItemsList,
  ReceiptProcessingIndicator,
} from "@/components/receipt";
import { useReceipt, useReceipts } from "@/hooks/useReceipts";
import { ReceiptStatus, type ReceiptItem } from "@/types";

export const ReceiptDetailPage = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { receipt, isLoading, error, updateItems, updateLoading } = useReceipt(
    id!
  );
  const { deleteReceipt, deleteLoading } = useReceipts();

  const [isEditing, setIsEditing] = useState(false);
  const [editedItems, setEditedItems] = useState<Omit<ReceiptItem, "id">[]>([]);

  const startEditing = () => {
    if (receipt) {
      // eslint-disable-next-line @typescript-eslint/no-unused-vars
      setEditedItems(receipt.items.map(({ id: _id, ...item }) => item));
      setIsEditing(true);
    }
  };

  const cancelEditing = () => {
    setIsEditing(false);
    setEditedItems([]);
  };

  const handleSave = () => {
    updateItems(
      { items: editedItems },
      {
        onSuccess: () => {
          setIsEditing(false);
          setEditedItems([]);
        },
      }
    );
  };

  const handleItemChange = (
    index: number,
    field: keyof Omit<ReceiptItem, "id">,
    value: string | number
  ) => {
    const newItems = [...editedItems];
    newItems[index] = { ...newItems[index], [field]: value };
    setEditedItems(newItems);
  };

  const handleAddItem = () => {
    setEditedItems([...editedItems, { name: "", price: 0, quantity: 1 }]);
  };

  const handleRemoveItem = (index: number) => {
    const newItems = editedItems.filter((_, i) => i !== index);
    setEditedItems(newItems);
  };

  const handleDelete = () => {
    if (
      window.confirm(
        "Are you sure you want to delete this receipt? This action cannot be undone."
      )
    ) {
      deleteReceipt(id!, {
        onSuccess: () => {
          navigate("/receipts");
        },
      });
    }
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString(undefined, {
      year: "numeric",
      month: "long",
      day: "numeric",
    });
  };

  if (isLoading) {
    return (
      <Layout>
        <div className="max-w-4xl mx-auto">
          <Loading.Skeleton className="h-8 w-48 mb-6" />
          <Loading.Skeleton className="h-96 mb-4" />
          <Loading.Skeleton className="h-64" />
        </div>
      </Layout>
    );
  }

  if (error || !receipt) {
    return (
      <Layout>
        <div className="max-w-4xl mx-auto">
          <ErrorMessage
            title="Failed to Load Receipt"
            message={
              (error as Error)?.message ||
              "Unable to load receipt details. Please try again."
            }
          />
          <Link to="/receipts">
            <Button variant="outline" className="mt-4 gap-2">
              <ArrowLeft className="h-4 w-4" />
              Back to Receipts
            </Button>
          </Link>
        </div>
      </Layout>
    );
  }

  return (
    <Layout>
      <div className="max-w-4xl mx-auto">
        {/* Header */}
        <div className="mb-6">
          <Link to="/receipts">
            <Button variant="ghost" className="gap-2 mb-4">
              <ArrowLeft className="h-4 w-4" />
              Back to Receipts
            </Button>
          </Link>

          <div className="flex justify-between items-start gap-4">
            <div>
              <h1 className="text-2xl font-bold text-gray-900 dark:text-white mb-2">
                {receipt.merchantName || "Unknown Merchant"}
              </h1>
              <div className="flex items-center gap-4 text-sm text-gray-600 dark:text-gray-400">
                <div className="flex items-center gap-1">
                  <Calendar className="h-4 w-4" />
                  <span>{formatDate(receipt.date)}</span>
                </div>
                <ReceiptStatusBadge status={receipt.status} />
              </div>
            </div>

            <div className="flex gap-2">
              {!isEditing ? (
                <>
                  <Button
                    onClick={startEditing}
                    variant="outline"
                    className="gap-2"
                    disabled={receipt.status !== ReceiptStatus.Ready}
                  >
                    <Edit2 className="h-4 w-4" />
                    Edit Items
                  </Button>
                  <Button
                    onClick={handleDelete}
                    variant="danger"
                    disabled={deleteLoading}
                    loading={deleteLoading}
                  >
                    <Trash2 className="h-4 w-4" />
                  </Button>
                </>
              ) : (
                <>
                  <Button
                    onClick={handleSave}
                    className="gap-2"
                    disabled={updateLoading}
                    loading={updateLoading}
                  >
                    <Save className="h-4 w-4" />
                    Save
                  </Button>
                  <Button
                    onClick={cancelEditing}
                    variant="outline"
                    disabled={updateLoading}
                  >
                    <X className="h-4 w-4" />
                  </Button>
                </>
              )}
            </div>
          </div>
        </div>

        {/* Processing/Error Indicator */}
        {receipt.status !== ReceiptStatus.Ready && (
          <ReceiptProcessingIndicator
            status={receipt.status}
            errorMessage={receipt.errorMessage}
            confidence={receipt.ocrConfidence}
            className="mb-6"
          />
        )}

        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {/* Receipt Image */}
          <Card padding="none">
            <img
              src={receipt.imageUrl}
              alt={`Receipt from ${receipt.merchantName}`}
              className="w-full h-auto rounded-lg"
            />
          </Card>

          {/* Receipt Details */}
          <div className="space-y-6">
            {/* Low confidence warning for Ready receipts */}
            {receipt.status === ReceiptStatus.Ready &&
              receipt.ocrConfidence !== undefined &&
              receipt.ocrConfidence < 0.8 && (
                <ReceiptProcessingIndicator
                  status={receipt.status}
                  confidence={receipt.ocrConfidence}
                />
              )}

            {/* Items */}
            <ReceiptItemsList
              items={receipt.items}
              isEditing={isEditing}
              editedItems={editedItems}
              onItemChange={handleItemChange}
              onAddItem={handleAddItem}
              onRemoveItem={handleRemoveItem}
            />

            {/* Totals */}
            <ReceiptSummary
              subtotal={receipt.subtotal}
              tax={receipt.tax}
              tip={receipt.tip}
              total={receipt.total}
            />
          </div>
        </div>
      </div>
    </Layout>
  );
};
