using Splittat.API.Data.Entities;

namespace Splittat.API.Models.Responses;

/// <summary>
/// Response model for a receipt with its items
/// </summary>
public class ReceiptResponse
{
    public Guid Id { get; set; }
    public string MerchantName { get; set; } = string.Empty;
    public DateTime? Date { get; set; }
    public decimal Total { get; set; }
    public decimal? Tax { get; set; }
    public decimal? Tip { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public ReceiptStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ReceiptItemResponse> Items { get; set; } = new();
}
