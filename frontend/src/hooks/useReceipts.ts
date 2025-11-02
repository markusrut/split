import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { receiptsApi } from "@/api/receipts";
import { useNavigate } from "react-router-dom";
import { useReceiptHub } from "./useReceiptHub";
import { useReceiptPolling } from "./useReceiptPolling";

export const useReceipts = () => {
  const queryClient = useQueryClient();
  const navigate = useNavigate();

  // Get all receipts
  const {
    data: receipts,
    isLoading,
    error,
    refetch,
  } = useQuery({
    queryKey: ["receipts"],
    queryFn: receiptsApi.getAll,
  });

  // Upload receipt mutation
  const uploadMutation = useMutation({
    mutationFn: receiptsApi.upload,
    onSuccess: (data) => {
      // Invalidate receipts list to refetch
      queryClient.invalidateQueries({ queryKey: ["receipts"] });
      // Navigate to the newly created receipt
      navigate(`/receipts/${data.id}`);
    },
  });

  // Delete receipt mutation
  const deleteMutation = useMutation({
    mutationFn: receiptsApi.delete,
    onSuccess: () => {
      // Invalidate receipts list to refetch
      queryClient.invalidateQueries({ queryKey: ["receipts"] });
    },
  });

  return {
    receipts,
    isLoading,
    error,
    refetch,
    uploadReceipt: uploadMutation.mutate,
    uploadLoading: uploadMutation.isPending,
    uploadError: uploadMutation.error,
    deleteReceipt: deleteMutation.mutate,
    deleteLoading: deleteMutation.isPending,
    deleteError: deleteMutation.error,
  };
};

// Hook for single receipt detail
export const useReceipt = (id: string) => {
  const queryClient = useQueryClient();

  const {
    data: receipt,
    isLoading,
    error,
    refetch,
  } = useQuery({
    queryKey: ["receipts", id],
    queryFn: () => receiptsApi.getById(id),
    enabled: !!id,
  });

  // Enable SignalR for real-time updates
  useReceiptHub(!!id);

  // Fallback polling if receipt is processing
  useReceiptPolling(id, receipt?.status, !!id);

  // Update receipt items mutation
  const updateItemsMutation = useMutation({
    mutationFn: (data: Parameters<typeof receiptsApi.updateItems>[1]) =>
      receiptsApi.updateItems(id, data),
    onSuccess: (data) => {
      // Update the receipt in cache
      queryClient.setQueryData(["receipts", id], data);
      // Invalidate receipts list to refetch
      queryClient.invalidateQueries({ queryKey: ["receipts"] });
    },
  });

  return {
    receipt,
    isLoading,
    error,
    refetch,
    updateItems: updateItemsMutation.mutate,
    updateLoading: updateItemsMutation.isPending,
    updateError: updateItemsMutation.error,
  };
};
