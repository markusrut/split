namespace Splittat.API.Data.Entities;

public enum ReceiptStatus
{
    Uploaded,       // Image saved, queued for OCR
    OcrInProgress,  // OCR is currently running
    OcrCompleted,   // OCR finished, parsing in progress
    Ready,          // Fully processed and ready to use
    ParseFailed,    // OCR succeeded but parsing failed
    Failed          // OCR or processing failed
}

public class Receipt
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? MerchantName { get; set; }
    public DateTime? Date { get; set; }
    public decimal Total { get; set; }
    public decimal? Tax { get; set; }
    public decimal? Tip { get; set; }
    public required string ImageUrl { get; set; }
    public ReceiptStatus Status { get; set; } = ReceiptStatus.Uploaded;
    public DateTime CreatedAt { get; set; }

    // OCR processing tracking
    public double? OcrConfidence { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? ErrorMessage { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<ReceiptItem> Items { get; set; } = new List<ReceiptItem>();
    public ICollection<Split> Splits { get; set; } = new List<Split>();
}
