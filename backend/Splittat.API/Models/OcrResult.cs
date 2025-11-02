namespace Splittat.API.Models;

/// <summary>
/// Represents the result of OCR processing on a receipt image
/// </summary>
public class OcrResult
{
    /// <summary>
    /// Raw text extracted from the receipt via OCR
    /// </summary>
    public string RawText { get; set; } = string.Empty;

    /// <summary>
    /// Merchant/store name extracted from the receipt
    /// </summary>
    public string? MerchantName { get; set; }

    /// <summary>
    /// Date of the transaction
    /// </summary>
    public DateTime? Date { get; set; }

    /// <summary>
    /// List of line items extracted from the receipt
    /// </summary>
    public List<OcrLineItem> LineItems { get; set; } = new();

    /// <summary>
    /// Subtotal amount (before tax and tip)
    /// </summary>
    public decimal? Subtotal { get; set; }

    /// <summary>
    /// Tax amount
    /// </summary>
    public decimal? Tax { get; set; }

    /// <summary>
    /// Tip amount
    /// </summary>
    public decimal? Tip { get; set; }

    /// <summary>
    /// Total amount
    /// </summary>
    public decimal? Total { get; set; }

    /// <summary>
    /// Confidence score from OCR provider (0.0 to 1.0)
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Whether the OCR processing was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if processing failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Represents a single line item extracted from a receipt
/// </summary>
public class OcrLineItem
{
    /// <summary>
    /// Item name/description
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Item price
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Quantity (defaults to 1 if not detected)
    /// </summary>
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Line number on the receipt (for ordering)
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// Confidence score for this line item (0.0 to 1.0)
    /// </summary>
    public double Confidence { get; set; }
}
