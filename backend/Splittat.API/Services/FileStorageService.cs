using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace Splittat.API.Services;

public class FileStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<FileStorageService> _logger;
    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB
    private readonly string[] _allowedContentTypes = ["image/jpeg", "image/png", "application/pdf"];
    private const int MaxImageWidth = 2000;
    private const int JpegQuality = 85;

    public FileStorageService(IWebHostEnvironment environment, ILogger<FileStorageService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    /// <summary>
    /// Validates the uploaded file (size, content type, and actual file signature via magic bytes)
    /// </summary>
    public bool ValidateFile(IFormFile file, out string? errorMessage)
    {
        errorMessage = null;

        if (file.Length == 0)
        {
            errorMessage = "File is empty";
            return false;
        }

        if (file.Length > MaxFileSize)
        {
            errorMessage = $"File size exceeds maximum allowed size of {MaxFileSize / 1024 / 1024}MB";
            _logger.LogWarning("File upload rejected: Size {FileSize} bytes exceeds limit", file.Length);
            return false;
        }

        // First check claimed content type
        if (!_allowedContentTypes.Contains(file.ContentType.ToLower()))
        {
            errorMessage = $"File type '{file.ContentType}' is not allowed. Allowed types: {string.Join(", ", _allowedContentTypes)}";
            _logger.LogWarning("File upload rejected: Invalid content type {ContentType}", file.ContentType);
            return false;
        }

        // Verify actual file type using magic bytes
        var actualFileType = GetFileTypeFromMagicBytes(file);
        if (actualFileType == null)
        {
            errorMessage = "Unable to determine file type from file content. The file may be corrupted or unsupported.";
            _logger.LogWarning("File upload rejected: Could not determine file type from magic bytes");
            return false;
        }

        // Verify the claimed content type matches the actual file type
        var isValidFileType = actualFileType switch
        {
            FileType.Jpeg => file.ContentType.ToLower() is "image/jpeg" or "image/jpg",
            FileType.Png => file.ContentType.ToLower() == "image/png",
            FileType.Pdf => file.ContentType.ToLower() == "application/pdf",
            _ => false
        };

        if (!isValidFileType)
        {
            errorMessage = $"File content does not match claimed type '{file.ContentType}'. Actual file type: {actualFileType}";
            _logger.LogWarning("File upload rejected: Content type mismatch. Claimed: {ClaimedType}, Actual: {ActualType}",
                file.ContentType, actualFileType);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Determines the actual file type by reading magic bytes (file signature)
    /// </summary>
    private FileType? GetFileTypeFromMagicBytes(IFormFile file)
    {
        try
        {
            using var stream = file.OpenReadStream();
            var buffer = new byte[8]; // Read first 8 bytes (sufficient for most signatures)
            var bytesRead = stream.Read(buffer, 0, buffer.Length);

            if (bytesRead < 2)
                return null;

            // JPEG: FF D8 FF
            if (buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF)
                return FileType.Jpeg;

            // PNG: 89 50 4E 47 0D 0A 1A 0A
            if (bytesRead >= 8 &&
                buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47 &&
                buffer[4] == 0x0D && buffer[5] == 0x0A && buffer[6] == 0x1A && buffer[7] == 0x0A)
                return FileType.Png;

            // PDF: 25 50 44 46 (%PDF)
            if (bytesRead >= 4 &&
                buffer[0] == 0x25 && buffer[1] == 0x50 && buffer[2] == 0x44 && buffer[3] == 0x46)
                return FileType.Pdf;

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading file magic bytes");
            return null;
        }
    }

    /// <summary>
    /// File type enumeration for magic byte validation
    /// </summary>
    private enum FileType
    {
        Jpeg,
        Png,
        Pdf
    }

    /// <summary>
    /// Saves the uploaded file and returns the relative file path
    /// </summary>
    public async Task<string> SaveFileAsync(IFormFile file, Guid userId)
    {
        _logger.LogInformation("Saving file for user {UserId}: {FileName} ({FileSize} bytes)",
            userId, file.FileName, file.Length);

        // Validate file
        if (!ValidateFile(file, out var errorMessage))
        {
            throw new ArgumentException(errorMessage);
        }

        // Generate unique filename
        var fileExtension = Path.GetExtension(file.FileName).ToLower();
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
        var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads");
        var filePath = Path.Combine(uploadsPath, uniqueFileName);

        // Ensure directory exists
        Directory.CreateDirectory(uploadsPath);

        // Handle image optimization or direct save for PDFs
        string actualFileName;
        if (file.ContentType.StartsWith("image/"))
        {
            await SaveAndOptimizeImageAsync(file, filePath);
            // Images are always saved as .jpg
            actualFileName = Path.ChangeExtension(uniqueFileName, ".jpg");
        }
        else
        {
            // Save PDF directly
            await using var fileStream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(fileStream);
            actualFileName = uniqueFileName;
        }

        _logger.LogInformation("File saved successfully: {FilePath}", actualFileName);

        // Return relative path for storage in database
        return $"/uploads/{actualFileName}";
    }

    /// <summary>
    /// Saves and optimizes image files (resize if needed, convert to JPEG, compress)
    /// </summary>
    private async Task SaveAndOptimizeImageAsync(IFormFile file, string filePath)
    {
        using var image = await Image.LoadAsync(file.OpenReadStream());

        // Resize if width exceeds maximum
        if (image.Width > MaxImageWidth)
        {
            var ratio = (double)MaxImageWidth / image.Width;
            var newHeight = (int)(image.Height * ratio);

            _logger.LogInformation("Resizing image from {OriginalWidth}x{OriginalHeight} to {NewWidth}x{NewHeight}",
                image.Width, image.Height, MaxImageWidth, newHeight);

            image.Mutate(x => x.Resize(MaxImageWidth, newHeight));
        }

        // Save as JPEG with compression (convert PNG to JPEG for consistency)
        var jpegFilePath = Path.ChangeExtension(filePath, ".jpg");
        await image.SaveAsJpegAsync(jpegFilePath, new JpegEncoder
        {
            Quality = JpegQuality
        });

        _logger.LogInformation("Image optimized and saved as JPEG with {Quality}% quality", JpegQuality);
    }

    /// <summary>
    /// Deletes a file from storage
    /// </summary>
    public async Task<bool> DeleteFileAsync(string relativeFilePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(relativeFilePath))
            {
                return false;
            }

            // Remove leading slash if present
            var normalizedPath = relativeFilePath.TrimStart('/');
            var fullPath = Path.Combine(_environment.WebRootPath, normalizedPath);

            if (File.Exists(fullPath))
            {
                await Task.Run(() => File.Delete(fullPath));
                _logger.LogInformation("File deleted successfully: {FilePath}", relativeFilePath);
                return true;
            }

            _logger.LogWarning("File not found for deletion: {FilePath}", relativeFilePath);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FilePath}", relativeFilePath);
            return false;
        }
    }
}
