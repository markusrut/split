namespace Splittat.API.Models.Requests;

/// <summary>
/// Request model for updating receipt items
/// </summary>
public class UpdateReceiptItemsRequest
{
    public List<UpdateItemDto> Items { get; set; } = new();
}

/// <summary>
/// DTO for updating a single receipt item
/// </summary>
public class UpdateItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; } = 1;
}
