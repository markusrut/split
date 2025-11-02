using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Splittat.API.Data;
using Splittat.API.Data.Entities;
using Splittat.API.Hubs;
using Splittat.API.Services;

namespace Splittat.API.Jobs;

/// <summary>
/// Background job for processing receipt OCR asynchronously
/// Executed by Hangfire after receipt upload
/// </summary>
public class OcrBackgroundJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OcrBackgroundJob> _logger;

    public OcrBackgroundJob(IServiceProvider serviceProvider, ILogger<OcrBackgroundJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Processes OCR for a receipt and sends real-time notifications via SignalR
    /// </summary>
    public async Task ProcessReceiptOcrAsync(Guid receiptId)
    {
        using var scope = _serviceProvider.CreateScope();
        var receiptService = scope.ServiceProvider.GetRequiredService<IReceiptService>();
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<ReceiptHub>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        _logger.LogInformation("Hangfire job started: Processing OCR for receipt {ReceiptId}", receiptId);

        try
        {
            // Send initial notification: OCR processing started
            await hubContext.SendReceiptStatusUpdateAsync(
                receiptId,
                ReceiptStatus.OcrInProgress,
                "OCR processing started"
            );

            // Process OCR (this method updates the receipt status in the database)
            await receiptService.ProcessOcrAsync(receiptId);

            // Load the updated receipt to get final status and details
            var receipt = await dbContext.Receipts
                .Include(r => r.Items)
                .FirstOrDefaultAsync(r => r.Id == receiptId);

            if (receipt == null)
            {
                _logger.LogError("Receipt {ReceiptId} not found after OCR processing", receiptId);
                return;
            }

            // Send completion notification with final status
            if (receipt.Status == ReceiptStatus.Ready)
            {
                await hubContext.SendReceiptProcessedAsync(
                    receiptId,
                    ReceiptStatus.Ready,
                    receipt.Items.Count,
                    receipt.OcrConfidence
                );

                _logger.LogInformation("Hangfire job completed: Receipt {ReceiptId} processed successfully ({ItemCount} items, confidence {Confidence:F2})",
                    receiptId, receipt.Items.Count, receipt.OcrConfidence ?? 0);
            }
            else if (receipt.Status == ReceiptStatus.Failed)
            {
                await hubContext.SendReceiptStatusUpdateAsync(
                    receiptId,
                    ReceiptStatus.Failed,
                    receipt.ErrorMessage ?? "OCR processing failed"
                );

                _logger.LogWarning("Hangfire job completed: Receipt {ReceiptId} processing failed: {Error}",
                    receiptId, receipt.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hangfire job failed: Error processing OCR for receipt {ReceiptId}", receiptId);

            // Send failure notification
            await hubContext.SendReceiptStatusUpdateAsync(
                receiptId,
                ReceiptStatus.Failed,
                $"Unexpected error: {ex.Message}"
            );

            throw; // Re-throw to let Hangfire retry
        }
    }
}
