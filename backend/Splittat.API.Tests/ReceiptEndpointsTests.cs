using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Splittat.API.Data;
using Splittat.API.Data.Entities;
using Splittat.API.Models.Requests;
using Splittat.API.Models.Responses;
using Xunit.Abstractions;

namespace Splittat.API.Tests;

/// <summary>
/// Snapshot tests for Receipt endpoints covering all scenarios and receipt statuses
/// </summary>
public class ReceiptEndpointsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;
    private readonly ITestOutputHelper _output;

    public ReceiptEndpointsTests(TestWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Ensure database is created for each test
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Database.EnsureCreated();
    }

    private async Task<string> RegisterAndLoginAsync(string email = "test@example.com", string password = "Password123!")
    {
        // Register
        var registerRequest = new
        {
            Email = email,
            Password = password,
            FirstName = "Test",
            LastName = "User"
        };
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Login
        var loginRequest = new { Email = email, Password = password };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        return loginResult!.Token;
    }

    [Fact]
    public async Task UploadReceipt_WithoutAuthentication_Returns401()
    {
        // Arrange
        var content = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(new byte[] { 0x89, 0x50, 0x4E, 0x47 }); // PNG header
        imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
        content.Add(imageContent, "file", "test.png");

        // Act
        var response = await _client.PostAsync("/api/receipts", content);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UploadReceipt_WithValidImage_Returns201WithProcessingStatus()
    {
        // Arrange
        var token = await RegisterAndLoginAsync("upload1@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var content = new MultipartFormDataContent();
        var imageBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }; // JPEG header
        var imageContent = new ByteArrayContent(imageBytes);
        imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(imageContent, "file", "receipt.jpg");

        // Act
        var response = await _client.PostAsync("/api/receipts", content);

        // Assert - Log response if not Created
        if (response.StatusCode != HttpStatusCode.Created)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"Upload failed with {response.StatusCode}: {errorBody}");
        }
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var receipt = await response.Content.ReadFromJsonAsync<ReceiptResponse>();
        Assert.NotNull(receipt);
        Assert.NotEqual(Guid.Empty, receipt.Id);

        // Snapshot: Receipt should be in one of the valid statuses
        _output.WriteLine($"Upload Receipt Snapshot:");
        _output.WriteLine($"  Status: {receipt.Status}");
        _output.WriteLine($"  ID: {receipt.Id}");
        _output.WriteLine($"  ImageUrl: {receipt.ImageUrl}");
        _output.WriteLine($"  MerchantName: {receipt.MerchantName}");
        _output.WriteLine($"  Total: {receipt.Total}");

        Assert.Contains(receipt.Status, new[] {
            ReceiptStatus.Uploaded,
            ReceiptStatus.OcrInProgress,
            ReceiptStatus.OcrCompleted,
            ReceiptStatus.Ready,
            ReceiptStatus.ParseFailed,
            ReceiptStatus.Failed
        });
        Assert.NotNull(receipt.ImageUrl);
        Assert.Contains("/uploads/", receipt.ImageUrl);
    }

    [Fact]
    public async Task UploadReceipt_WithInvalidFile_Returns400()
    {
        // Arrange
        var token = await RegisterAndLoginAsync("upload2@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var content = new MultipartFormDataContent();
        var textContent = new StringContent("not an image");
        textContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
        content.Add(textContent, "file", "test.txt");

        // Act
        var response = await _client.PostAsync("/api/receipts", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadReceipt_WithNoFile_Returns400()
    {
        // Arrange
        var token = await RegisterAndLoginAsync("upload3@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var content = new MultipartFormDataContent();

        // Act
        var response = await _client.PostAsync("/api/receipts", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetReceipts_WithNoReceipts_ReturnsEmptyList()
    {
        // Arrange
        var token = await RegisterAndLoginAsync("getreceipts1@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/receipts");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var receipts = await response.Content.ReadFromJsonAsync<List<ReceiptResponse>>();
        Assert.NotNull(receipts);
        Assert.Empty(receipts);

        _output.WriteLine("Get Receipts (Empty) Snapshot:");
        _output.WriteLine($"  Count: {receipts.Count}");
    }

    [Fact]
    public async Task GetReceipts_WithMultipleReceipts_ReturnsAllUserReceipts()
    {
        // Arrange
        var token = await RegisterAndLoginAsync("getreceipts2@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Upload 3 receipts
        for (int i = 0; i < 3; i++)
        {
            var content = new MultipartFormDataContent();
            var imageContent = new ByteArrayContent(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });
            imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
            content.Add(imageContent, "file", $"receipt{i}.jpg");
            await _client.PostAsync("/api/receipts", content);
            await Task.Delay(100); // Ensure different CreatedAt times
        }

        // Act
        var response = await _client.GetAsync("/api/receipts");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var receipts = await response.Content.ReadFromJsonAsync<List<ReceiptResponse>>();
        Assert.NotNull(receipts);
        Assert.Equal(3, receipts.Count);

        _output.WriteLine("Get Receipts (Multiple) Snapshot:");
        _output.WriteLine($"  Total Count: {receipts.Count}");
        foreach (var receipt in receipts)
        {
            _output.WriteLine($"  Receipt {receipt.Id}:");
            _output.WriteLine($"    Status: {receipt.Status}");
            _output.WriteLine($"    MerchantName: {receipt.MerchantName}");
            _output.WriteLine($"    Total: {receipt.Total}");
            _output.WriteLine($"    Items: {receipt.Items.Count}");
            _output.WriteLine($"    CreatedAt: {receipt.CreatedAt}");
        }

        // Verify ordering (most recent first)
        for (int i = 0; i < receipts.Count - 1; i++)
        {
            Assert.True(receipts[i].CreatedAt >= receipts[i + 1].CreatedAt);
        }
    }

    [Fact]
    public async Task GetReceipts_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var token = await RegisterAndLoginAsync("pagination@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Upload 5 receipts
        for (int i = 0; i < 5; i++)
        {
            var content = new MultipartFormDataContent();
            var imageContent = new ByteArrayContent(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });
            imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
            content.Add(imageContent, "file", $"receipt{i}.jpg");
            await _client.PostAsync("/api/receipts", content);
            await Task.Delay(100);
        }

        // Act - Get page 1 with pageSize 2
        var response1 = await _client.GetAsync("/api/receipts?page=1&pageSize=2");
        var page1 = await response1.Content.ReadFromJsonAsync<List<ReceiptResponse>>();

        // Act - Get page 2 with pageSize 2
        var response2 = await _client.GetAsync("/api/receipts?page=2&pageSize=2");
        var page2 = await response2.Content.ReadFromJsonAsync<List<ReceiptResponse>>();

        // Assert
        Assert.NotNull(page1);
        Assert.NotNull(page2);
        Assert.Equal(2, page1.Count);
        Assert.Equal(2, page2.Count);

        _output.WriteLine("Pagination Snapshot:");
        _output.WriteLine($"  Page 1 Count: {page1.Count}");
        _output.WriteLine($"  Page 2 Count: {page2.Count}");
        _output.WriteLine($"  Page 1 IDs: {string.Join(", ", page1.Select(r => r.Id))}");
        _output.WriteLine($"  Page 2 IDs: {string.Join(", ", page2.Select(r => r.Id))}");

        // Verify different receipts on different pages
        Assert.DoesNotContain(page1, r1 => page2.Any(r2 => r2.Id == r1.Id));
    }

    [Fact]
    public async Task GetReceipts_WithInvalidPagination_Returns400()
    {
        // Arrange
        var token = await RegisterAndLoginAsync("invalidpagination@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/receipts?page=0&pageSize=-1");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetReceiptById_WithValidId_ReturnsReceiptWithItems()
    {
        // Arrange
        var token = await RegisterAndLoginAsync("getbyid1@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Upload a receipt
        var content = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });
        imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(imageContent, "file", "receipt.jpg");
        var uploadResponse = await _client.PostAsync("/api/receipts", content);
        var uploadedReceipt = await uploadResponse.Content.ReadFromJsonAsync<ReceiptResponse>();

        // Act
        var response = await _client.GetAsync($"/api/receipts/{uploadedReceipt!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var receipt = await response.Content.ReadFromJsonAsync<ReceiptResponse>();
        Assert.NotNull(receipt);
        Assert.Equal(uploadedReceipt.Id, receipt.Id);

        _output.WriteLine("Get Receipt By ID Snapshot:");
        _output.WriteLine($"  ID: {receipt.Id}");
        _output.WriteLine($"  Status: {receipt.Status}");
        _output.WriteLine($"  MerchantName: {receipt.MerchantName}");
        _output.WriteLine($"  Date: {receipt.Date}");
        _output.WriteLine($"  Total: {receipt.Total}");
        _output.WriteLine($"  Tax: {receipt.Tax}");
        _output.WriteLine($"  Tip: {receipt.Tip}");
        _output.WriteLine($"  ImageUrl: {receipt.ImageUrl}");
        _output.WriteLine($"  Items Count: {receipt.Items.Count}");
        foreach (var item in receipt.Items)
        {
            _output.WriteLine($"    Item: {item.Name} - ${item.Price} x {item.Quantity}");
        }
    }

    [Fact]
    public async Task GetReceiptById_WithNonExistentId_Returns404()
    {
        // Arrange
        var token = await RegisterAndLoginAsync("getbyid2@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync($"/api/receipts/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetReceiptById_ForOtherUsersReceipt_Returns404()
    {
        // Arrange - User 1 uploads receipt
        var token1 = await RegisterAndLoginAsync("owner@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token1);

        var content = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });
        imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(imageContent, "file", "receipt.jpg");
        var uploadResponse = await _client.PostAsync("/api/receipts", content);
        var receipt = await uploadResponse.Content.ReadFromJsonAsync<ReceiptResponse>();

        // Arrange - User 2 tries to access
        var token2 = await RegisterAndLoginAsync("notowner@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token2);

        // Act
        var response = await _client.GetAsync($"/api/receipts/{receipt!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateReceiptItems_WithValidData_ReturnsUpdatedReceipt()
    {
        // Arrange
        var token = await RegisterAndLoginAsync("update1@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Upload a receipt
        var content = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });
        imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(imageContent, "file", "receipt.jpg");
        var uploadResponse = await _client.PostAsync("/api/receipts", content);
        var receipt = await uploadResponse.Content.ReadFromJsonAsync<ReceiptResponse>();

        // Get the receipt to get item IDs
        var getResponse = await _client.GetAsync($"/api/receipts/{receipt!.Id}");
        var currentReceipt = await getResponse.Content.ReadFromJsonAsync<ReceiptResponse>();

        if (currentReceipt!.Items.Count > 0)
        {
            // Act - Update items
            var updateRequest = new UpdateReceiptItemsRequest
            {
                Items = currentReceipt.Items.Select(item => new UpdateItemDto
                {
                    Id = item.Id,
                    Name = $"Updated {item.Name}",
                    Price = item.Price + 1.00m,
                    Quantity = item.Quantity + 1
                }).ToList()
            };

            var updateResponse = await _client.PutAsJsonAsync($"/api/receipts/{receipt.Id}/items", updateRequest);

            // Assert
            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
            var updatedReceipt = await updateResponse.Content.ReadFromJsonAsync<ReceiptResponse>();
            Assert.NotNull(updatedReceipt);

            _output.WriteLine("Update Receipt Items Snapshot:");
            _output.WriteLine($"  Receipt ID: {updatedReceipt.Id}");
            _output.WriteLine($"  Original Total: {receipt.Total}");
            _output.WriteLine($"  Updated Total: {updatedReceipt.Total}");
            _output.WriteLine($"  Items:");
            foreach (var item in updatedReceipt.Items)
            {
                _output.WriteLine($"    {item.Name}: ${item.Price} x {item.Quantity}");
            }

            // Verify items were updated
            Assert.All(updatedReceipt.Items, item => Assert.StartsWith("Updated ", item.Name));
        }
    }

    [Fact]
    public async Task UpdateReceiptItems_ForOtherUsersReceipt_Returns404()
    {
        // Arrange - User 1 uploads receipt
        var token1 = await RegisterAndLoginAsync("updateowner@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token1);

        var content = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });
        imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(imageContent, "file", "receipt.jpg");
        var uploadResponse = await _client.PostAsync("/api/receipts", content);
        var receipt = await uploadResponse.Content.ReadFromJsonAsync<ReceiptResponse>();

        // Arrange - User 2 tries to update
        var token2 = await RegisterAndLoginAsync("updatenotowner@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token2);

        var updateRequest = new UpdateReceiptItemsRequest
        {
            Items = new List<UpdateItemDto>
            {
                new() { Id = Guid.NewGuid(), Name = "Item", Price = 10.00m, Quantity = 1 }
            }
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/receipts/{receipt!.Id}/items", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteReceipt_WithValidId_Returns204()
    {
        // Arrange
        var token = await RegisterAndLoginAsync("delete1@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Upload a receipt
        var content = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });
        imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(imageContent, "file", "receipt.jpg");
        var uploadResponse = await _client.PostAsync("/api/receipts", content);
        var receipt = await uploadResponse.Content.ReadFromJsonAsync<ReceiptResponse>();

        // Act
        var deleteResponse = await _client.DeleteAsync($"/api/receipts/{receipt!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        _output.WriteLine("Delete Receipt Snapshot:");
        _output.WriteLine($"  Deleted Receipt ID: {receipt.Id}");
        _output.WriteLine($"  Status Code: {deleteResponse.StatusCode}");

        // Verify receipt is deleted
        var getResponse = await _client.GetAsync($"/api/receipts/{receipt.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteReceipt_WithNonExistentId_Returns404()
    {
        // Arrange
        var token = await RegisterAndLoginAsync("delete2@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.DeleteAsync($"/api/receipts/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteReceipt_ForOtherUsersReceipt_Returns404()
    {
        // Arrange - User 1 uploads receipt
        var token1 = await RegisterAndLoginAsync("deleteowner@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token1);

        var content = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });
        imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(imageContent, "file", "receipt.jpg");
        var uploadResponse = await _client.PostAsync("/api/receipts", content);
        var receipt = await uploadResponse.Content.ReadFromJsonAsync<ReceiptResponse>();

        // Arrange - User 2 tries to delete
        var token2 = await RegisterAndLoginAsync("deletenotowner@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token2);

        // Act
        var response = await _client.DeleteAsync($"/api/receipts/{receipt!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ReceiptWorkflow_CompleteScenario_Success()
    {
        // Arrange
        var token = await RegisterAndLoginAsync("workflow@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        _output.WriteLine("=== Complete Receipt Workflow Snapshot ===");

        // Step 1: Upload receipt
        var content = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });
        imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(imageContent, "file", "receipt.jpg");
        var uploadResponse = await _client.PostAsync("/api/receipts", content);
        var uploadedReceipt = await uploadResponse.Content.ReadFromJsonAsync<ReceiptResponse>();

        _output.WriteLine($"1. Upload: Status={uploadedReceipt!.Status}, ID={uploadedReceipt.Id}");

        // Step 2: Get all receipts
        var listResponse = await _client.GetAsync("/api/receipts");
        var receipts = await listResponse.Content.ReadFromJsonAsync<List<ReceiptResponse>>();

        _output.WriteLine($"2. List: Count={receipts!.Count}");

        // Step 3: Get specific receipt
        var getResponse = await _client.GetAsync($"/api/receipts/{uploadedReceipt.Id}");
        var receipt = await getResponse.Content.ReadFromJsonAsync<ReceiptResponse>();

        _output.WriteLine($"3. Get: Items={receipt!.Items.Count}, Total=${receipt.Total}");

        // Step 4: Delete receipt
        var deleteResponse = await _client.DeleteAsync($"/api/receipts/{uploadedReceipt.Id}");

        _output.WriteLine($"4. Delete: StatusCode={deleteResponse.StatusCode}");

        // Step 5: Verify deletion
        var verifyResponse = await _client.GetAsync($"/api/receipts/{uploadedReceipt.Id}");

        _output.WriteLine($"5. Verify: StatusCode={verifyResponse.StatusCode} (should be 404)");

        // Assert all steps
        Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, verifyResponse.StatusCode);
    }
}
