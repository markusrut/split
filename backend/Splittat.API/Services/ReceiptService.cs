using Microsoft.EntityFrameworkCore;
using Splittat.API.Data;
using Splittat.API.Data.Entities;
using Splittat.API.Models;
using Splittat.API.Models.Requests;
using Splittat.API.Models.Responses;

namespace Splittat.API.Services;

public interface IReceiptService
{
    // Old synchronous method (deprecated, kept for backward compatibility)
    Task<ReceiptResponse> ProcessReceiptAsync(IFormFile file, Guid userId);

    // New async workflow methods
    Task<ReceiptResponse> UploadReceiptAsync(IFormFile file, Guid userId);
    Task ProcessOcrAsync(Guid receiptId);

    // Query methods
    Task<List<ReceiptResponse>> GetUserReceiptsAsync(Guid userId, int page = 1, int pageSize = 20);
    Task<ReceiptResponse?> GetReceiptByIdAsync(Guid receiptId, Guid userId);

    // Update/Delete methods
    Task<ReceiptResponse> UpdateReceiptItemsAsync(Guid receiptId, UpdateReceiptItemsRequest request, Guid userId);
    Task<bool> DeleteReceiptAsync(Guid receiptId, Guid userId);
}

public class ReceiptService : IReceiptService
{
    private readonly AppDbContext _context;
    private readonly FileStorageService _fileStorageService;
    private readonly IOcrService _ocrService;
    private readonly ILogger<ReceiptService> _logger;

    // Cached JsonSerializerOptions for OCR result serialization
    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public ReceiptService(
        AppDbContext context,
        FileStorageService fileStorageService,
        IOcrService ocrService,
        ILogger<ReceiptService> logger)
    {
        _context = context;
        _fileStorageService = fileStorageService;
        _ocrService = ocrService;
        _logger = logger;
    }

    /// <summary>
    /// NEW ASYNC WORKFLOW: Uploads receipt image and creates record with Uploaded status
    /// Returns immediately without waiting for OCR processing
    /// </summary>
    public async Task<ReceiptResponse> UploadReceiptAsync(IFormFile file, Guid userId)
    {
        using var correlationId = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = Guid.NewGuid(),
            ["UserId"] = userId
        });

        _logger.LogInformation("Uploading receipt for user {UserId}", userId);

        // Validate file
        if (!_fileStorageService.ValidateFile(file, out var errorMessage))
        {
            _logger.LogWarning("Receipt validation failed for user {UserId}: {Error}", userId, errorMessage);
            throw new ArgumentException(errorMessage);
        }

        // Save image file
        var imageUrl = await _fileStorageService.SaveFileAsync(file, userId);
        _logger.LogInformation("Receipt image saved: {ImageUrl}", imageUrl);

        // Create receipt entity with Uploaded status
        var receipt = new Receipt
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ImageUrl = imageUrl,
            Status = ReceiptStatus.Uploaded,
            CreatedAt = DateTime.UtcNow,
            MerchantName = "Processing...",
            Total = 0
        };

        _context.Receipts.Add(receipt);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Receipt {ReceiptId} created with Uploaded status, queued for OCR processing", receipt.Id);

        // Return response immediately (OCR will be processed in background)
        return new ReceiptResponse
        {
            Id = receipt.Id,
            MerchantName = receipt.MerchantName,
            Date = receipt.Date,
            Total = receipt.Total,
            Tax = receipt.Tax,
            Tip = receipt.Tip,
            ImageUrl = receipt.ImageUrl,
            Status = receipt.Status,
            CreatedAt = receipt.CreatedAt,
            Items = []
        };
    }

    /// <summary>
    /// NEW ASYNC WORKFLOW: Processes OCR for a receipt in the background
    /// Called by Hangfire background job
    /// </summary>
    public async Task ProcessOcrAsync(Guid receiptId)
    {
        using var correlationId = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = Guid.NewGuid(),
            ["ReceiptId"] = receiptId
        });

        _logger.LogInformation("Starting background OCR processing for receipt {ReceiptId}", receiptId);

        // Load receipt from database
        var receipt = await _context.Receipts
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == receiptId);

        if (receipt == null)
        {
            _logger.LogError("Receipt {ReceiptId} not found for OCR processing", receiptId);
            throw new InvalidOperationException($"Receipt {receiptId} not found");
        }

        // Update status to OcrInProgress
        receipt.Status = ReceiptStatus.OcrInProgress;
        await _context.SaveChangesAsync();
        _logger.LogInformation("Receipt {ReceiptId} status updated to OcrInProgress", receiptId);

        try
        {
            // Get physical file path
            var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", receipt.ImageUrl.TrimStart('/'));

            if (!File.Exists(physicalPath))
            {
                throw new FileNotFoundException($"Receipt image not found at {physicalPath}");
            }

            // Process OCR
            var ocrResult = await _ocrService.ProcessReceiptAsync(physicalPath);

            // Save raw OCR result to temporary file
            var ocrJson = System.Text.Json.JsonSerializer.Serialize(ocrResult, JsonOptions);
            await _fileStorageService.SaveOcrResultAsync(receiptId, ocrJson);
            _logger.LogInformation("Raw OCR result saved for receipt {ReceiptId}", receiptId);

            // Parse and update receipt
            await ParseOcrResultAsync(receipt, ocrResult);

            _logger.LogInformation("OCR processing completed successfully for receipt {ReceiptId}", receiptId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during OCR processing for receipt {ReceiptId}", receiptId);
            receipt.Status = ReceiptStatus.Failed;
            receipt.ErrorMessage = ex.Message;
            receipt.ProcessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            throw; // Re-throw for Hangfire retry logic
        }
    }

    /// <summary>
    /// Helper method to parse OCR results and update receipt
    /// </summary>
    private async Task ParseOcrResultAsync(Receipt receipt, OcrResult ocrResult)
    {
        if (ocrResult.Success)
        {
            // Update receipt with OCR data
            receipt.MerchantName = ocrResult.MerchantName ?? "Unknown Merchant";
            receipt.Date = ocrResult.Date;
            receipt.Total = ocrResult.Total ?? 0;
            receipt.Tax = ocrResult.Tax;
            receipt.Tip = ocrResult.Tip;
            receipt.OcrConfidence = ocrResult.Confidence;
            receipt.ProcessedAt = DateTime.UtcNow;
            receipt.Status = ReceiptStatus.Ready;

            // Clear existing items (in case of reprocessing)
            _context.ReceiptItems.RemoveRange(receipt.Items);

            // Create receipt items from OCR line items
            foreach (var ocrItem in ocrResult.LineItems)
            {
                var receiptItem = new ReceiptItem
                {
                    Id = Guid.NewGuid(),
                    ReceiptId = receipt.Id,
                    Name = ocrItem.Name,
                    Price = ocrItem.Price,
                    Quantity = ocrItem.Quantity,
                    LineNumber = ocrItem.LineNumber
                };
                _context.ReceiptItems.Add(receiptItem);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("OCR parsing successful for receipt {ReceiptId}: {ItemCount} items extracted, confidence {Confidence:F2}",
                receipt.Id, ocrResult.LineItems.Count, ocrResult.Confidence);
        }
        else
        {
            // OCR failed
            receipt.Status = ReceiptStatus.Failed;
            receipt.ErrorMessage = ocrResult.ErrorMessage ?? "OCR processing failed";
            receipt.ProcessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogWarning("OCR parsing failed for receipt {ReceiptId}: {Error}",
                receipt.Id, ocrResult.ErrorMessage);
        }
    }

    /// <summary>
    /// OLD WORKFLOW: Processes receipt synchronously (deprecated)
    /// Kept for backward compatibility - will be removed in future version
    /// </summary>
    public async Task<ReceiptResponse> ProcessReceiptAsync(IFormFile file, Guid userId)
    {
        _logger.LogInformation("Processing receipt upload for user {UserId}", userId);

        // Validate file
        if (!_fileStorageService.ValidateFile(file, out var errorMessage))
        {
            _logger.LogWarning("Receipt validation failed for user {UserId}: {Error}", userId, errorMessage);
            throw new ArgumentException(errorMessage);
        }

        // Save image file
        var imageUrl = await _fileStorageService.SaveFileAsync(file, userId);
        _logger.LogInformation("Receipt image saved: {ImageUrl}", imageUrl);

        // Create receipt entity with Uploaded status (will be updated after OCR)
        var receipt = new Receipt
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ImageUrl = imageUrl,
            Status = ReceiptStatus.OcrInProgress,
            CreatedAt = DateTime.UtcNow,
            MerchantName = "Processing...",
            Total = 0
        };

        _context.Receipts.Add(receipt);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Receipt created with ID {ReceiptId}, starting OCR processing", receipt.Id);

        try
        {
            // Get the physical file path for OCR processing
            var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), imageUrl.TrimStart('/'));

            // Process OCR
            var ocrResult = await _ocrService.ProcessReceiptAsync(physicalPath);

            if (ocrResult.Success)
            {
                // Update receipt with OCR data
                receipt.MerchantName = ocrResult.MerchantName ?? "Unknown Merchant";
                receipt.Date = ocrResult.Date;
                receipt.Total = ocrResult.Total ?? 0;
                receipt.Tax = ocrResult.Tax;
                receipt.Tip = ocrResult.Tip;
                receipt.Status = ReceiptStatus.Ready;

                // Create receipt items from OCR line items
                foreach (var ocrItem in ocrResult.LineItems)
                {
                    var receiptItem = new ReceiptItem
                    {
                        Id = Guid.NewGuid(),
                        ReceiptId = receipt.Id,
                        Name = ocrItem.Name,
                        Price = ocrItem.Price,
                        Quantity = ocrItem.Quantity,
                        LineNumber = ocrItem.LineNumber
                    };
                    _context.ReceiptItems.Add(receiptItem);
                }

                _logger.LogInformation("OCR processing successful for receipt {ReceiptId}, extracted {ItemCount} items",
                    receipt.Id, ocrResult.LineItems.Count);
            }
            else
            {
                // OCR failed
                receipt.Status = ReceiptStatus.Failed;
                receipt.MerchantName = "OCR Processing Failed";
                _logger.LogWarning("OCR processing failed for receipt {ReceiptId}: {Error}",
                    receipt.Id, ocrResult.ErrorMessage);
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during OCR processing for receipt {ReceiptId}", receipt.Id);
            receipt.Status = ReceiptStatus.Failed;
            receipt.MerchantName = "Processing Error";
            await _context.SaveChangesAsync();
        }

        // Return response with items
        return await GetReceiptByIdAsync(receipt.Id, userId)
            ?? throw new InvalidOperationException("Failed to retrieve created receipt");
    }

    public async Task<List<ReceiptResponse>> GetUserReceiptsAsync(Guid userId, int page = 1, int pageSize = 20)
    {
        _logger.LogInformation("Fetching receipts for user {UserId}, page {Page}, pageSize {PageSize}",
            userId, page, pageSize);

        var receipts = await _context.Receipts
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(r => r.Items)
            .Select(r => new ReceiptResponse
            {
                Id = r.Id,
                MerchantName = r.MerchantName ?? "Unknown",
                Date = r.Date,
                Total = r.Total,
                Tax = r.Tax,
                Tip = r.Tip,
                ImageUrl = r.ImageUrl,
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                OcrConfidence = r.OcrConfidence,
                ProcessedAt = r.ProcessedAt,
                ErrorMessage = r.ErrorMessage,
                Items = r.Items.OrderBy(i => i.LineNumber).Select(i => new ReceiptItemResponse
                {
                    Id = i.Id,
                    Name = i.Name,
                    Price = i.Price,
                    Quantity = i.Quantity,
                    LineNumber = i.LineNumber
                }).ToList()
            })
            .ToListAsync();

        _logger.LogInformation("Retrieved {Count} receipts for user {UserId}", receipts.Count, userId);
        return receipts;
    }

    public async Task<ReceiptResponse?> GetReceiptByIdAsync(Guid receiptId, Guid userId)
    {
        _logger.LogInformation("Fetching receipt {ReceiptId} for user {UserId}", receiptId, userId);

        var receipt = await _context.Receipts
            .Where(r => r.Id == receiptId && r.UserId == userId)
            .Include(r => r.Items)
            .Select(r => new ReceiptResponse
            {
                Id = r.Id,
                MerchantName = r.MerchantName ?? "Unknown",
                Date = r.Date,
                Total = r.Total,
                Tax = r.Tax,
                Tip = r.Tip,
                ImageUrl = r.ImageUrl,
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                OcrConfidence = r.OcrConfidence,
                ProcessedAt = r.ProcessedAt,
                ErrorMessage = r.ErrorMessage,
                Items = r.Items.OrderBy(i => i.LineNumber).Select(i => new ReceiptItemResponse
                {
                    Id = i.Id,
                    Name = i.Name,
                    Price = i.Price,
                    Quantity = i.Quantity,
                    LineNumber = i.LineNumber
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (receipt == null)
        {
            _logger.LogWarning("Receipt {ReceiptId} not found for user {UserId}", receiptId, userId);
        }

        return receipt;
    }

    public async Task<ReceiptResponse> UpdateReceiptItemsAsync(Guid receiptId, UpdateReceiptItemsRequest request, Guid userId)
    {
        _logger.LogInformation("Updating items for receipt {ReceiptId} by user {UserId}", receiptId, userId);

        // Verify ownership
        var receipt = await _context.Receipts
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == receiptId && r.UserId == userId);

        if (receipt == null)
        {
            _logger.LogWarning("Receipt {ReceiptId} not found for user {UserId}", receiptId, userId);
            throw new UnauthorizedAccessException("Receipt not found or access denied");
        }

        // Update existing items
        foreach (var updateDto in request.Items)
        {
            var existingItem = receipt.Items.FirstOrDefault(i => i.Id == updateDto.Id);
            if (existingItem != null)
            {
                existingItem.Name = updateDto.Name;
                existingItem.Price = updateDto.Price;
                existingItem.Quantity = updateDto.Quantity;
            }
            else
            {
                _logger.LogWarning("Item {ItemId} not found in receipt {ReceiptId}", updateDto.Id, receiptId);
            }
        }

        // Recalculate total from items
        receipt.Total = receipt.Items.Sum(i => i.Price * i.Quantity);

        await _context.SaveChangesAsync();
        _logger.LogInformation("Receipt {ReceiptId} items updated, new total: {Total}", receiptId, receipt.Total);

        return await GetReceiptByIdAsync(receiptId, userId)
            ?? throw new InvalidOperationException("Failed to retrieve updated receipt");
    }

    public async Task<bool> DeleteReceiptAsync(Guid receiptId, Guid userId)
    {
        _logger.LogInformation("Deleting receipt {ReceiptId} for user {UserId}", receiptId, userId);

        // Verify ownership
        var receipt = await _context.Receipts
            .FirstOrDefaultAsync(r => r.Id == receiptId && r.UserId == userId);

        if (receipt == null)
        {
            _logger.LogWarning("Receipt {ReceiptId} not found for user {UserId}", receiptId, userId);
            return false;
        }

        // Delete the image file
        var deleted = await _fileStorageService.DeleteFileAsync(receipt.ImageUrl);
        if (!deleted)
        {
            _logger.LogWarning("Failed to delete image file {ImageUrl} for receipt {ReceiptId}",
                receipt.ImageUrl, receiptId);
        }

        // Delete receipt (cascade will delete items)
        _context.Receipts.Remove(receipt);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Receipt {ReceiptId} deleted successfully", receiptId);
        return true;
    }
}
