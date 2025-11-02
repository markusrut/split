import * as signalR from "@microsoft/signalr";

let connection: signalR.HubConnection | null = null;

/**
 * Creates and starts a SignalR connection to the Receipt hub
 * @param onStatusUpdate Callback when receipt status changes
 * @param onProcessed Callback when receipt processing completes
 * @returns SignalR connection instance or null if no auth token
 */
export const createReceiptHubConnection = (
  onStatusUpdate: (data: {
    receiptId: string;
    status: string;
    message?: string;
    timestamp: string;
  }) => void,
  onProcessed: (data: {
    receiptId: string;
    status: string;
    itemCount: number;
    confidence?: number;
    timestamp: string;
  }) => void
) => {
  const token = localStorage.getItem("auth_token");

  if (!token) {
    console.warn("No auth token found, cannot connect to SignalR hub");
    return null;
  }

  // Get base URL without /api suffix
  const baseUrl = import.meta.env.VITE_API_BASE_URL?.replace("/api", "") || "";

  connection = new signalR.HubConnectionBuilder()
    .withUrl(`${baseUrl}/hubs/receipt`, {
      accessTokenFactory: () => token,
    })
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Information)
    .build();

  // Handle receipt status updates
  connection.on("ReceiptStatusUpdated", (data) => {
    console.log(
      `[SignalR] Receipt ${data.receiptId} status updated to: ${data.status}`
    );
    onStatusUpdate(data);
  });

  // Handle receipt processing completion
  connection.on("ReceiptProcessed", (data) => {
    console.log(
      `[SignalR] Receipt ${data.receiptId} processed (${data.status}): ${data.itemCount} items`
    );
    onProcessed(data);
  });

  // Start connection
  connection
    .start()
    .then(() => console.log("[SignalR] Connected to Receipt hub"))
    .catch((err) => console.error("[SignalR] Connection error:", err));

  return connection;
};

/**
 * Stops the active SignalR connection
 */
export const stopReceiptHubConnection = async () => {
  if (connection) {
    await connection.stop();
    console.log("[SignalR] Disconnected from Receipt hub");
    connection = null;
  }
};

/**
 * Subscribe to updates for a specific receipt
 * @param receiptId Receipt ID to subscribe to
 */
export const subscribeToReceipt = async (receiptId: string) => {
  if (connection && connection.state === signalR.HubConnectionState.Connected) {
    try {
      await connection.invoke("SubscribeToReceipt", receiptId);
      console.log(`[SignalR] Subscribed to receipt ${receiptId}`);
    } catch (err) {
      console.error(
        `[SignalR] Failed to subscribe to receipt ${receiptId}:`,
        err
      );
    }
  }
};

/**
 * Unsubscribe from updates for a specific receipt
 * @param receiptId Receipt ID to unsubscribe from
 */
export const unsubscribeFromReceipt = async (receiptId: string) => {
  if (connection && connection.state === signalR.HubConnectionState.Connected) {
    try {
      await connection.invoke("UnsubscribeFromReceipt", receiptId);
      console.log(`[SignalR] Unsubscribed from receipt ${receiptId}`);
    } catch (err) {
      console.error(
        `[SignalR] Failed to unsubscribe from receipt ${receiptId}:`,
        err
      );
    }
  }
};

/**
 * Get the current connection state
 */
export const getConnectionState = () => {
  return connection?.state ?? signalR.HubConnectionState.Disconnected;
};
