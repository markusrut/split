using Azure;
using Azure.AI.Vision.ImageAnalysis;
using Splittat.API.Models;
using System.Text.RegularExpressions;

namespace Splittat.API.Services;

public interface IOcrService
{
    Task<OcrResult> ProcessReceiptAsync(string imageFilePath);
    Task<OcrResult> ProcessReceiptAsync(Stream imageStream);
}

public class OcrService : IOcrService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<OcrService> _logger;
    private readonly ImageAnalysisClient? _client;

    // Regex patterns for parsing
    private static readonly Regex DatePattern = new(
        @"(\d{1,2}[-/]\d{1,2}[-/]\d{2,4})|(\d{4}[-/]\d{1,2}[-/]\d{1,2})|" +
        @"((?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)[a-z]* \d{1,2},? \d{4})",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private static readonly Regex PricePattern = new(
        @"\$?\s*(\d+\.\d{2})",
        RegexOptions.Compiled
    );

    private static readonly Regex LineItemPattern = new(
        @"^(.+?)\s+\$?\s*(\d+\.\d{2})$",
        RegexOptions.Compiled
    );

    private static readonly string[] TaxKeywords = { "tax", "sales tax", "gst", "hst", "pst" };
    private static readonly string[] TipKeywords = { "tip", "gratuity", "service" };
    private static readonly string[] TotalKeywords = { "total", "amount", "balance", "grand total" };
    private static readonly string[] SubtotalKeywords = { "subtotal", "sub total", "sub-total" };

    public OcrService(IConfiguration configuration, ILogger<OcrService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        var endpoint = _configuration["Azure:ComputerVision:Endpoint"];
        var apiKey = _configuration["Azure:ComputerVision:ApiKey"];

        if (!string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(apiKey))
        {
            _client = new ImageAnalysisClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        }
        else
        {
            _logger.LogWarning("Azure Computer Vision credentials not configured. OCR service will not be available.");
        }
    }

    public async Task<OcrResult> ProcessReceiptAsync(string imageFilePath)
    {
        try
        {
            if (!File.Exists(imageFilePath))
            {
                return new OcrResult
                {
                    Success = false,
                    ErrorMessage = "Image file not found"
                };
            }

            using var fileStream = File.OpenRead(imageFilePath);
            return await ProcessReceiptAsync(fileStream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing receipt from file: {FilePath}", imageFilePath);
            return new OcrResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<OcrResult> ProcessReceiptAsync(Stream imageStream)
    {
        if (_client == null)
        {
            return new OcrResult
            {
                Success = false,
                ErrorMessage = "Azure Computer Vision is not configured"
            };
        }

        try
        {
            // Call Azure Computer Vision API with retry logic
            var result = await CallAzureVisionWithRetryAsync(imageStream);

            if (result == null || string.IsNullOrEmpty(result.RawText))
            {
                return new OcrResult
                {
                    Success = false,
                    ErrorMessage = "No text could be extracted from the image"
                };
            }

            // Parse the extracted text
            ParseReceiptData(result);

            result.Success = true;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing receipt image");
            return new OcrResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<OcrResult> CallAzureVisionWithRetryAsync(Stream imageStream)
    {
        const int maxRetries = 3;
        const int delayMs = 1000;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                // Reset stream position
                if (imageStream.CanSeek)
                {
                    imageStream.Position = 0;
                }

                // Read image data
                using var memoryStream = new MemoryStream();
                await imageStream.CopyToAsync(memoryStream);
                var imageData = BinaryData.FromBytes(memoryStream.ToArray());

                // Call Azure Computer Vision API
                var result = await _client!.AnalyzeAsync(
                    imageData,
                    VisualFeatures.Read,
                    new ImageAnalysisOptions { Language = "en" }
                );

                // Extract text from result
                var rawText = string.Join("\n", result.Value.Read.Blocks
                    .SelectMany(block => block.Lines)
                    .Select(line => line.Text));

                var confidence = result.Value.Read.Blocks
                    .SelectMany(block => block.Lines)
                    .SelectMany(line => line.Words)
                    .Average(word => word.Confidence);

                return new OcrResult
                {
                    RawText = rawText,
                    Confidence = confidence
                };
            }
            catch (RequestFailedException ex) when (attempt < maxRetries && IsTransientError(ex))
            {
                _logger.LogWarning(ex, "Transient error calling Azure Computer Vision (attempt {Attempt}/{MaxRetries})", attempt, maxRetries);
                await Task.Delay(delayMs * attempt); // Exponential backoff
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Azure Computer Vision API");
                throw;
            }
        }

        throw new Exception("Failed to process image after multiple retries");
    }

    private static bool IsTransientError(RequestFailedException ex)
    {
        // Retry on 429 (Too Many Requests), 500, 502, 503, 504
        return ex.Status is 429 or >= 500 and <= 504;
    }

    private void ParseReceiptData(OcrResult result)
    {
        var lines = result.RawText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (lines.Length == 0)
        {
            return;
        }

        // Extract merchant name (usually first 1-3 lines)
        result.MerchantName = ExtractMerchantName(lines);

        // Extract date
        result.Date = ExtractDate(lines);

        // Extract line items and totals
        ExtractLineItemsAndTotals(lines, result);
    }

    private string? ExtractMerchantName(string[] lines)
    {
        // Take the first non-empty line that doesn't look like a date or number
        // Usually the merchant name is at the top of the receipt
        foreach (var line in lines.Take(5))
        {
            if (line.Length > 2 &&
                !DatePattern.IsMatch(line) &&
                !decimal.TryParse(line.Replace("$", "").Trim(), out _))
            {
                return line;
            }
        }

        return lines.FirstOrDefault();
    }

    private DateTime? ExtractDate(string[] lines)
    {
        foreach (var line in lines)
        {
            var match = DatePattern.Match(line);
            if (match.Success)
            {
                if (DateTime.TryParse(match.Value, out var date))
                {
                    return date;
                }
            }
        }

        return null;
    }

    private void ExtractLineItemsAndTotals(string[] lines, OcrResult result)
    {
        var lineItems = new List<OcrLineItem>();
        decimal? subtotal = null;
        decimal? tax = null;
        decimal? tip = null;
        decimal? total = null;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var lowerLine = line.ToLowerInvariant();

            // Check for tax
            if (ContainsKeyword(lowerLine, TaxKeywords))
            {
                tax = ExtractAmount(line);
                continue;
            }

            // Check for tip
            if (ContainsKeyword(lowerLine, TipKeywords))
            {
                tip = ExtractAmount(line);
                continue;
            }

            // Check for total
            if (ContainsKeyword(lowerLine, TotalKeywords))
            {
                total = ExtractAmount(line);
                continue;
            }

            // Check for subtotal
            if (ContainsKeyword(lowerLine, SubtotalKeywords))
            {
                subtotal = ExtractAmount(line);
                continue;
            }

            // Try to parse as line item (name + price)
            var itemMatch = LineItemPattern.Match(line);
            if (itemMatch.Success)
            {
                var name = itemMatch.Groups[1].Value.Trim();
                var priceStr = itemMatch.Groups[2].Value;

                if (decimal.TryParse(priceStr, out var price) && price > 0)
                {
                    // Skip if this looks like a total/tax/tip line
                    if (!ContainsKeyword(lowerLine, TotalKeywords.Concat(TaxKeywords).Concat(TipKeywords).ToArray()))
                    {
                        lineItems.Add(new OcrLineItem
                        {
                            Name = name,
                            Price = price,
                            Quantity = 1,
                            LineNumber = i + 1,
                            Confidence = 0.8 // Default confidence for pattern-matched items
                        });
                    }
                }
            }
        }

        result.LineItems = lineItems;
        result.Subtotal = subtotal;
        result.Tax = tax;
        result.Tip = tip;
        result.Total = total;

        // Calculate subtotal if not found
        if (result.Subtotal == null && lineItems.Count > 0)
        {
            result.Subtotal = lineItems.Sum(item => item.Price * item.Quantity);
        }

        // Calculate total if not found
        if (result.Total == null)
        {
            result.Total = (result.Subtotal ?? 0) + (result.Tax ?? 0) + (result.Tip ?? 0);
        }
    }

    private static bool ContainsKeyword(string line, string[] keywords)
    {
        return keywords.Any(keyword => line.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private static decimal? ExtractAmount(string line)
    {
        var match = PricePattern.Match(line);
        if (match.Success && decimal.TryParse(match.Groups[1].Value, out var amount))
        {
            return amount;
        }

        return null;
    }
}
