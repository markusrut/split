using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using Splittat.API.Services;

namespace Splittat.API.Tests;

public class FileStorageServiceTests
{
    private readonly FileStorageService _fileStorageService;
    private readonly string _testWebRootPath;

    public FileStorageServiceTests()
    {
        var mockEnvironment = new Mock<IWebHostEnvironment>();
        var mockLogger = new Mock<ILogger<FileStorageService>>();

        // Create a temporary directory for testing
        _testWebRootPath = Path.Combine(Path.GetTempPath(), "splittat-test-" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testWebRootPath);
        Directory.CreateDirectory(Path.Combine(_testWebRootPath, "uploads"));

        mockEnvironment.Setup(e => e.WebRootPath).Returns(_testWebRootPath);

        _fileStorageService = new FileStorageService(mockEnvironment.Object, mockLogger.Object);
    }

    ~FileStorageServiceTests()
    {
        // Cleanup test directory
        if (Directory.Exists(_testWebRootPath))
        {
            Directory.Delete(_testWebRootPath, true);
        }
    }

    [Fact]
    public void ValidateFile_WithEmptyFile_ReturnsFalse()
    {
        // Arrange
        var mockFile = CreateMockFormFile("test.jpg", "image/jpeg", []);

        // Act
        var result = _fileStorageService.ValidateFile(mockFile, out var errorMessage);

        // Assert
        Assert.False(result);
        Assert.NotNull(errorMessage);
        Assert.Contains("empty", errorMessage.ToLower());
    }

    [Fact]
    public void ValidateFile_WithOversizedFile_ReturnsFalse()
    {
        // Arrange
        var largeData = new byte[11 * 1024 * 1024]; // 11MB (exceeds 10MB limit)
        var mockFile = CreateMockFormFile("test.jpg", "image/jpeg", largeData);

        // Act
        var result = _fileStorageService.ValidateFile(mockFile, out var errorMessage);

        // Assert
        Assert.False(result);
        Assert.NotNull(errorMessage);
        Assert.Contains("exceeds", errorMessage.ToLower());
    }

    [Fact]
    public void ValidateFile_WithInvalidContentType_ReturnsFalse()
    {
        // Arrange
        var data = new byte[1024];
        var mockFile = CreateMockFormFile("test.txt", "text/plain", data);

        // Act
        var result = _fileStorageService.ValidateFile(mockFile, out var errorMessage);

        // Assert
        Assert.False(result);
        Assert.NotNull(errorMessage);
        Assert.Contains("not allowed", errorMessage.ToLower());
    }

    [Fact]
    public void ValidateFile_WithValidJpeg_ReturnsTrue()
    {
        // Arrange
        var data = CreateJpegFileData();
        var mockFile = CreateMockFormFile("test.jpg", "image/jpeg", data);

        // Act
        var result = _fileStorageService.ValidateFile(mockFile, out var errorMessage);

        // Assert
        Assert.True(result);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void ValidateFile_WithValidPng_ReturnsTrue()
    {
        // Arrange
        var data = CreatePngFileData();
        var mockFile = CreateMockFormFile("test.png", "image/png", data);

        // Act
        var result = _fileStorageService.ValidateFile(mockFile, out var errorMessage);

        // Assert
        Assert.True(result);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void ValidateFile_WithValidPdf_ReturnsTrue()
    {
        // Arrange
        var data = CreatePdfFileData();
        var mockFile = CreateMockFormFile("test.pdf", "application/pdf", data);

        // Act
        var result = _fileStorageService.ValidateFile(mockFile, out var errorMessage);

        // Assert
        Assert.True(result);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void ValidateFile_WithMismatchedContentType_ReturnsFalse()
    {
        // Arrange - Create JPEG data but claim it is PNG
        var jpegData = CreateJpegFileData();
        var mockFile = CreateMockFormFile("test.png", "image/png", jpegData);

        // Act
        var result = _fileStorageService.ValidateFile(mockFile, out var errorMessage);

        // Assert
        Assert.False(result);
        Assert.NotNull(errorMessage);
        Assert.Contains("does not match claimed type", errorMessage);
    }

    [Fact]
    public void ValidateFile_WithInvalidMagicBytes_ReturnsFalse()
    {
        // Arrange - Create data with invalid magic bytes
        var invalidData = new byte[1024];
        for (int i = 0; i < invalidData.Length; i++)
            invalidData[i] = (byte)(i % 256);
        var mockFile = CreateMockFormFile("test.jpg", "image/jpeg", invalidData);

        // Act
        var result = _fileStorageService.ValidateFile(mockFile, out var errorMessage);

        // Assert
        Assert.False(result);
        Assert.NotNull(errorMessage);
        Assert.Contains("Unable to determine file type", errorMessage);
    }

    [Fact]
    public async Task SaveFileAsync_WithValidPdf_SavesFileToDisk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var pdfData = CreatePdfFileData();
        var mockFile = CreateMockFormFile("receipt.pdf", "application/pdf", pdfData);

        // Act
        var result = await _fileStorageService.SaveFileAsync(mockFile, userId);

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("/uploads/", result);
        Assert.EndsWith(".pdf", result);

        // Verify file exists on disk
        var filePath = Path.Combine(_testWebRootPath, result.TrimStart('/'));
        Assert.True(File.Exists(filePath), $"File should exist at {filePath}");

        // Verify file content (PDF files are saved directly without processing)
        var savedData = await File.ReadAllBytesAsync(filePath);
        Assert.Equal(pdfData.Length, savedData.Length);
        Assert.Equal(pdfData[0], savedData[0]); // Check PDF magic bytes
        Assert.Equal(pdfData[1], savedData[1]);
        Assert.Equal(pdfData[2], savedData[2]);
        Assert.Equal(pdfData[3], savedData[3]);
    }

    [Fact]
    public async Task SaveFileAsync_WithValidJpeg_SavesOptimizedImageToDisk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var jpegData = CreateJpegFileData();
        var mockFile = CreateMockFormFile("receipt.jpg", "image/jpeg", jpegData);

        // Act
        var result = await _fileStorageService.SaveFileAsync(mockFile, userId);

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("/uploads/", result);
        Assert.EndsWith(".jpg", result);

        // Verify file exists on disk
        var filePath = Path.Combine(_testWebRootPath, result.TrimStart('/'));
        Assert.True(File.Exists(filePath), $"File should exist at {filePath}");

        // Verify it's a valid JPEG file
        var savedData = await File.ReadAllBytesAsync(filePath);
        Assert.True(savedData.Length > 0);
        // Check JPEG magic bytes
        Assert.Equal(0xFF, savedData[0]);
        Assert.Equal(0xD8, savedData[1]);
        Assert.Equal(0xFF, savedData[2]);
    }

    [Fact]
    public async Task SaveFileAsync_WithPngImage_ConvertsToJpeg()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var pngData = CreatePngFileData();
        var mockFile = CreateMockFormFile("receipt.png", "image/png", pngData);

        // Act
        var result = await _fileStorageService.SaveFileAsync(mockFile, userId);

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("/uploads/", result);
        // Should be converted to .jpg
        Assert.EndsWith(".jpg", result);

        // Verify file exists on disk
        var filePath = Path.Combine(_testWebRootPath, result.TrimStart('/'));
        Assert.True(File.Exists(filePath), $"File should exist at {filePath}");

        // Verify it's actually a JPEG file (converted from PNG)
        var savedData = await File.ReadAllBytesAsync(filePath);
        Assert.Equal(0xFF, savedData[0]);
        Assert.Equal(0xD8, savedData[1]);
        Assert.Equal(0xFF, savedData[2]);
    }

    [Fact]
    public async Task SaveFileAsync_WithInvalidFile_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var invalidData = new byte[1024];
        var mockFile = CreateMockFormFile("test.jpg", "image/jpeg", invalidData);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _fileStorageService.SaveFileAsync(mockFile, userId));
    }

    [Fact]
    public async Task SaveFileAsync_WithOversizedFile_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var largeData = new byte[11 * 1024 * 1024]; // 11MB
        // Add JPEG magic bytes
        largeData[0] = 0xFF;
        largeData[1] = 0xD8;
        largeData[2] = 0xFF;
        largeData[3] = 0xE0;
        var mockFile = CreateMockFormFile("large.jpg", "image/jpeg", largeData);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _fileStorageService.SaveFileAsync(mockFile, userId));
        Assert.Contains("exceeds", exception.Message.ToLower());
    }

    [Fact]
    public async Task SaveFileAsync_GeneratesUniqueFileNames()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var jpegData = CreateJpegFileData();
        var mockFile1 = CreateMockFormFile("receipt.jpg", "image/jpeg", jpegData);
        var mockFile2 = CreateMockFormFile("receipt.jpg", "image/jpeg", jpegData);

        // Act
        var result1 = await _fileStorageService.SaveFileAsync(mockFile1, userId);
        var result2 = await _fileStorageService.SaveFileAsync(mockFile2, userId);

        // Assert
        Assert.NotEqual(result1, result2); // Should have different GUIDs in filenames

        // Verify both files exist
        var filePath1 = Path.Combine(_testWebRootPath, result1.TrimStart('/'));
        var filePath2 = Path.Combine(_testWebRootPath, result2.TrimStart('/'));
        Assert.True(File.Exists(filePath1));
        Assert.True(File.Exists(filePath2));
    }

    [Fact]
    public async Task SaveFileAsync_CreatesUploadsDirectoryIfNotExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var jpegData = CreateJpegFileData();
        var mockFile = CreateMockFormFile("receipt.jpg", "image/jpeg", jpegData);

        // Delete the uploads directory to test auto-creation
        var uploadsDir = Path.Combine(_testWebRootPath, "uploads");
        if (Directory.Exists(uploadsDir))
        {
            Directory.Delete(uploadsDir, true);
        }

        // Act
        var result = await _fileStorageService.SaveFileAsync(mockFile, userId);

        // Assert
        Assert.True(Directory.Exists(uploadsDir), "Uploads directory should be auto-created");
        var filePath = Path.Combine(_testWebRootPath, result.TrimStart('/'));
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public async Task DeleteFileAsync_WithNonExistentFile_ReturnsFalse()
    {
        // Act
        var result = await _fileStorageService.DeleteFileAsync("/uploads/nonexistent.jpg");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteFileAsync_WithNullPath_ReturnsFalse()
    {
        // Act
        var result = await _fileStorageService.DeleteFileAsync(null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteFileAsync_WithEmptyPath_ReturnsFalse()
    {
        // Act
        var result = await _fileStorageService.DeleteFileAsync(string.Empty);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteFileAsync_WithExistingFile_ReturnsTrue()
    {
        // Arrange
        var testFilePath = Path.Combine(_testWebRootPath, "uploads", "test-file.txt");
        await File.WriteAllTextAsync(testFilePath, "test content");

        // Act
        var result = await _fileStorageService.DeleteFileAsync("/uploads/test-file.txt");

        // Assert
        Assert.True(result);
        Assert.False(File.Exists(testFilePath));
    }

    // Helper method to create mock IFormFile
    private static IFormFile CreateMockFormFile(string fileName, string contentType, byte[] data)
    {
        var mockFile = new Mock<IFormFile>();

        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.ContentType).Returns(contentType);
        mockFile.Setup(f => f.Length).Returns(data.Length);

        // OpenReadStream must create a new stream each time it's called
        mockFile.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(data));

        mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns((Stream target, CancellationToken token) =>
            {
                var stream = new MemoryStream(data);
                return stream.CopyToAsync(target, token);
            });

        return mockFile.Object;
    }

    // Helper methods to create proper file data with magic bytes
    private static byte[] CreateJpegFileData()
    {
        // Create a minimal valid 1x1 JPEG image using ImageSharp
        using var image = new Image<Rgba32>(1, 1);
        image[0, 0] = Color.Red;

        using var ms = new MemoryStream();
        image.SaveAsJpeg(ms, new JpegEncoder());
        return ms.ToArray();
    }

    private static byte[] CreatePngFileData()
    {
        // Create a minimal valid 1x1 PNG image using ImageSharp
        using var image = new Image<Rgba32>(1, 1);
        image[0, 0] = Color.Blue;

        using var ms = new MemoryStream();
        image.SaveAsPng(ms, new PngEncoder());
        return ms.ToArray();
    }

    private static byte[] CreatePdfFileData()
    {
        // PDF magic bytes: 25 50 44 46 (%PDF)
        var data = new byte[1024];
        data[0] = 0x25; // %
        data[1] = 0x50; // P
        data[2] = 0x44; // D
        data[3] = 0x46; // F
        data[4] = 0x2D; // -
        data[5] = 0x31; // 1
        data[6] = 0x2E; // .
        data[7] = 0x34; // 4
        // Fill rest with dummy data
        for (int i = 8; i < data.Length; i++)
            data[i] = (byte)(i % 256);
        return data;
    }
}
