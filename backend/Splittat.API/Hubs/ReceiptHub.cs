using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Splittat.API.Data.Entities;

namespace Splittat.API.Hubs;

/// <summary>
/// SignalR Hub for real-time receipt processing notifications
/// </summary>
[Authorize]
public class ReceiptHub : Hub
{
    private readonly ILogger<ReceiptHub> _logger;

    public ReceiptHub(ILogger<ReceiptHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Called when a client connects to the hub
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        _logger.LogInformation("User {UserId} connected to ReceiptHub (ConnectionId: {ConnectionId})",
            userId, Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (exception != null)
        {
            _logger.LogWarning(exception, "User {UserId} disconnected from ReceiptHub with error (ConnectionId: {ConnectionId})",
                userId, Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation("User {UserId} disconnected from ReceiptHub (ConnectionId: {ConnectionId})",
                userId, Context.ConnectionId);
        }
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Client subscribes to receive updates for a specific receipt
    /// </summary>
    public async Task SubscribeToReceipt(Guid receiptId)
    {
        var groupName = $"receipt_{receiptId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Connection {ConnectionId} subscribed to receipt {ReceiptId}",
            Context.ConnectionId, receiptId);
    }

    /// <summary>
    /// Client unsubscribes from receipt updates
    /// </summary>
    public async Task UnsubscribeFromReceipt(Guid receiptId)
    {
        var groupName = $"receipt_{receiptId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Connection {ConnectionId} unsubscribed from receipt {ReceiptId}",
            Context.ConnectionId, receiptId);
    }
}

/// <summary>
/// Extension class to send notifications from outside the Hub (e.g., background jobs)
/// </summary>
public static class ReceiptHubExtensions
{
    /// <summary>
    /// Sends a status update notification for a specific receipt
    /// </summary>
    public static async Task SendReceiptStatusUpdateAsync(
        this IHubContext<ReceiptHub> hubContext,
        Guid receiptId,
        ReceiptStatus status,
        string? message = null)
    {
        var groupName = $"receipt_{receiptId}";
        await hubContext.Clients.Group(groupName).SendAsync("ReceiptStatusUpdated", new
        {
            receiptId,
            status = status.ToString(),
            message,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Sends a notification when receipt processing is complete
    /// </summary>
    public static async Task SendReceiptProcessedAsync(
        this IHubContext<ReceiptHub> hubContext,
        Guid receiptId,
        ReceiptStatus finalStatus,
        int itemCount = 0,
        double? confidence = null)
    {
        var groupName = $"receipt_{receiptId}";
        await hubContext.Clients.Group(groupName).SendAsync("ReceiptProcessed", new
        {
            receiptId,
            status = finalStatus.ToString(),
            itemCount,
            confidence,
            timestamp = DateTime.UtcNow
        });
    }
}
