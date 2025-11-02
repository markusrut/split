namespace Splittat.API.Data.Entities;

public class ReceiptItem
{
    public Guid Id { get; set; }
    public Guid ReceiptId { get; set; }
    public required string Name { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; } = 1;
    public int LineNumber { get; set; }

    // Navigation properties
    public Receipt Receipt { get; set; } = null!;
}
