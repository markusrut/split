namespace Splittat.API.Data.Entities;

public enum ReceiptStatus
{
    Processing,
    Ready,
    Failed
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
    public ReceiptStatus Status { get; set; } = ReceiptStatus.Processing;
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<ReceiptItem> Items { get; set; } = new List<ReceiptItem>();
    public ICollection<Split> Splits { get; set; } = new List<Split>();
}
