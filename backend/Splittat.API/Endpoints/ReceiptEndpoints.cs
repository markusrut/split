using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Splittat.API.Infrastructure;
using Splittat.API.Jobs;
using Splittat.API.Models.Requests;
using Splittat.API.Services;

namespace Splittat.API.Endpoints;

public static class ReceiptEndpoints
{
    public static void MapReceiptEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/receipts")
            .RequireAuthorization()
            .WithTags("Receipts");

        // POST /api/receipts - Upload receipt (returns immediately, processes OCR in background)
        group.MapPost("/", async (
            HttpContext httpContext,
            IReceiptService receiptService,
            IBackgroundJobClient backgroundJobClient,
            ILogger<IReceiptService> logger) =>
        {
            try
            {
                var userId = httpContext.User.GetUserId();

                // Get the uploaded file
                var file = httpContext.Request.Form.Files.FirstOrDefault();
                if (file == null || file.Length == 0)
                {
                    logger.LogWarning("Upload request missing file for user {UserId}", userId);
                    return Results.BadRequest(new { error = "No file uploaded" });
                }

                // Upload receipt and save file (returns immediately with Status=Uploaded)
                var result = await receiptService.UploadReceiptAsync(file, userId);
                logger.LogInformation("Receipt {ReceiptId} uploaded successfully by user {UserId}, queueing OCR job", result.Id, userId);

                // Enqueue background job for OCR processing
                var jobId = backgroundJobClient.Enqueue<OcrBackgroundJob>(
                    job => job.ProcessReceiptOcrAsync(result.Id));

                logger.LogInformation("OCR background job {JobId} enqueued for receipt {ReceiptId}", jobId, result.Id);

                return Results.Created($"/api/receipts/{result.Id}", new
                {
                    result.Id,
                    result.Status,
                    result.ImageUrl,
                    result.CreatedAt,
                    Message = "Receipt uploaded successfully. Processing will complete in a few seconds. Poll this endpoint or connect to SignalR for real-time updates.",
                    JobId = jobId
                });
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning("Receipt upload validation failed: {Error}", ex.Message);
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.LogWarning("Unauthorized receipt upload attempt: {Error}", ex.Message);
                return Results.Unauthorized();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error uploading receipt");
                return Results.Problem("An error occurred while uploading the receipt");
            }
        })
        .WithName("UploadReceipt")
        .WithDescription("Upload a receipt image for asynchronous OCR processing. Returns immediately with Uploaded status. Connect to SignalR hub at /hubs/receipt for real-time status updates.")
        .DisableAntiforgery()
        .Accepts<IFormFile>("multipart/form-data")
        .Produces(201)
        .Produces(400)
        .Produces(401);

        // GET /api/receipts - List user's receipts
        group.MapGet("/", async (
            HttpContext httpContext,
            IReceiptService receiptService,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20) =>
        {
            try
            {
                var userId = httpContext.User.GetUserId();

                if (page < 1 || pageSize < 1 || pageSize > 100)
                {
                    return Results.BadRequest(new { error = "Invalid pagination parameters" });
                }

                var receipts = await receiptService.GetUserReceiptsAsync(userId, page, pageSize);
                return Results.Ok(receipts);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
            catch (Exception)
            {
                return Results.Problem("An error occurred while fetching receipts");
            }
        })
        .WithName("GetReceipts")
        .WithDescription("Get a paginated list of user's receipts")
        .Produces(200)
        .Produces(400)
        .Produces(401);

        // GET /api/receipts/{id} - Get receipt details
        group.MapGet("/{id:guid}", async (
            Guid id,
            HttpContext httpContext,
            IReceiptService receiptService) =>
        {
            try
            {
                var userId = httpContext.User.GetUserId();
                var receipt = await receiptService.GetReceiptByIdAsync(id, userId);

                if (receipt == null)
                {
                    return Results.NotFound(new { error = "Receipt not found" });
                }

                return Results.Ok(receipt);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
            catch (Exception)
            {
                return Results.Problem("An error occurred while fetching the receipt");
            }
        })
        .WithName("GetReceiptById")
        .WithDescription("Get detailed information about a specific receipt")
        .Produces(200)
        .Produces(404)
        .Produces(401);

        // PUT /api/receipts/{id}/items - Update receipt items
        group.MapPut("/{id:guid}/items", async (
            Guid id,
            [FromBody] UpdateReceiptItemsRequest request,
            HttpContext httpContext,
            IReceiptService receiptService,
            ILogger<IReceiptService> logger) =>
        {
            try
            {
                var userId = httpContext.User.GetUserId();

                if (request.Items == null || request.Items.Count == 0)
                {
                    return Results.BadRequest(new { error = "No items provided" });
                }

                var result = await receiptService.UpdateReceiptItemsAsync(id, request, userId);
                logger.LogInformation("Receipt {ReceiptId} items updated by user {UserId}", id, userId);

                return Results.Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.LogWarning("Unauthorized receipt update attempt for receipt {ReceiptId}: {Error}", id, ex.Message);
                return Results.NotFound(new { error = "Receipt not found" });
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning("Receipt update validation failed: {Error}", ex.Message);
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating receipt {ReceiptId}", id);
                return Results.Problem("An error occurred while updating the receipt");
            }
        })
        .WithName("UpdateReceiptItems")
        .WithDescription("Update the items in a receipt")
        .Produces(200)
        .Produces(400)
        .Produces(404)
        .Produces(401);

        // GET /api/receipts/{id}/ocr - Get raw OCR result for debugging
        group.MapGet("/{id:guid}/ocr", async (
            Guid id,
            HttpContext httpContext,
            FileStorageService fileStorageService,
            IReceiptService receiptService,
            ILogger<IReceiptService> logger) =>
        {
            try
            {
                var userId = httpContext.User.GetUserId();

                // Verify receipt ownership
                var receipt = await receiptService.GetReceiptByIdAsync(id, userId);
                if (receipt == null)
                {
                    return Results.NotFound(new { error = "Receipt not found" });
                }

                // Retrieve raw OCR JSON
                var ocrJson = await fileStorageService.GetOcrResultAsync(id);
                if (ocrJson == null)
                {
                    return Results.NotFound(new { error = "OCR result not found. This receipt may not have been processed yet." });
                }

                return Results.Ok(new
                {
                    receiptId = id,
                    status = receipt.Status,
                    processedAt = receipt.ProcessedAt,
                    confidence = receipt.OcrConfidence,
                    rawOcrResult = System.Text.Json.JsonDocument.Parse(ocrJson)
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving OCR result for receipt {ReceiptId}", id);
                return Results.Problem("An error occurred while retrieving the OCR result");
            }
        })
        .WithName("GetReceiptOcr")
        .WithDescription("Get the raw OCR result for a receipt (for debugging/troubleshooting)")
        .Produces(200)
        .Produces(404)
        .Produces(401);

        // DELETE /api/receipts/{id} - Delete receipt
        group.MapDelete("/{id:guid}", async (
            Guid id,
            HttpContext httpContext,
            IReceiptService receiptService,
            ILogger<IReceiptService> logger) =>
        {
            try
            {
                var userId = httpContext.User.GetUserId();
                var deleted = await receiptService.DeleteReceiptAsync(id, userId);

                if (!deleted)
                {
                    return Results.NotFound(new { error = "Receipt not found" });
                }

                logger.LogInformation("Receipt {ReceiptId} deleted by user {UserId}", id, userId);
                return Results.NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting receipt {ReceiptId}", id);
                return Results.Problem("An error occurred while deleting the receipt");
            }
        })
        .WithName("DeleteReceipt")
        .WithDescription("Delete a receipt and its associated image")
        .Produces(204)
        .Produces(404)
        .Produces(401);
    }
}
