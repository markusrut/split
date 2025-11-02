namespace Splittat.API.Models.Responses;

/// <summary>
/// Response model for a receipt line item
/// </summary>
public class ReceiptItemResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public int LineNumber { get; set; }
}
