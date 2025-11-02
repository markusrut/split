namespace Splittat.API.Data.Entities;

public class ItemAssignment
{
    public Guid Id { get; set; }
    public Guid SplitId { get; set; }
    public Guid ReceiptItemId { get; set; }
    public Guid UserId { get; set; }
    public decimal Percentage { get; set; } = 1.0m; // Default 100% (1.0)
    public decimal Amount { get; set; }

    // Navigation properties
    public Split Split { get; set; } = null!;
    public ReceiptItem ReceiptItem { get; set; } = null!;
    public User User { get; set; } = null!;
}
