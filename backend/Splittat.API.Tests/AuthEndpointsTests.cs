using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Splittat.API.Data;
using Splittat.API.Models.Requests;
using Splittat.API.Models.Responses;

namespace Splittat.API.Tests;

public class AuthEndpointsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public AuthEndpointsTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();

        // Ensure database is created for each test
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Database.EnsureCreated();
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsCreated()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "newuser@example.com",
            Password = "password123",
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(authResponse);
        Assert.NotEmpty(authResponse.Token);
        Assert.Equal(request.Email, authResponse.Email);
        Assert.Equal(request.FirstName, authResponse.FirstName);
        Assert.Equal(request.LastName, authResponse.LastName);
        Assert.NotEqual(Guid.Empty, authResponse.UserId);
        Assert.True(authResponse.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "duplicate@example.com",
            Password = "password123",
            FirstName = "John",
            LastName = "Doe"
        };

        // Register first time
        await _client.PostAsJsonAsync("/api/auth/register", request);

        // Act - Try to register again with same email
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var errorResponse = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(errorResponse);
        Assert.True(errorResponse.ContainsKey("error"));
        Assert.Equal("Email already registered", errorResponse["error"]);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "notanemail",
            Password = "password123",
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithShortPassword_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "user@example.com",
            Password = "12345", // Too short
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithMissingFields_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "user@example.com",
            Password = "password123",
            FirstName = "", // Empty
            LastName = ""   // Empty
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOk()
    {
        // Arrange - First register a user
        var registerRequest = new RegisterRequest
        {
            Email = "logintest@example.com",
            Password = "password123",
            FirstName = "Test",
            LastName = "User"
        };
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginRequest
        {
            Email = "logintest@example.com",
            Password = "password123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(authResponse);
        Assert.NotEmpty(authResponse.Token);
        Assert.Equal(loginRequest.Email, authResponse.Email);
        Assert.Equal("Test", authResponse.FirstName);
        Assert.Equal("User", authResponse.LastName);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        // Arrange - First register a user
        var registerRequest = new RegisterRequest
        {
            Email = "wrongpwd@example.com",
            Password = "correctpassword",
            FirstName = "Test",
            LastName = "User"
        };
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginRequest
        {
            Email = "wrongpwd@example.com",
            Password = "wrongpassword"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "password123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithInvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "notanemail",
            Password = "password123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_EmailIsCaseInsensitive()
    {
        // Arrange - Register with lowercase email
        var registerRequest = new RegisterRequest
        {
            Email = "casetest@example.com",
            Password = "password123",
            FirstName = "Case",
            LastName = "Test"
        };
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Act - Login with uppercase email
        var loginRequest = new LoginRequest
        {
            Email = "CASETEST@EXAMPLE.COM",
            Password = "password123"
        };
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Register_MultipleTimes_GeneratesDifferentTokens()
    {
        // Arrange
        var request1 = new RegisterRequest
        {
            Email = "user1@example.com",
            Password = "password123",
            FirstName = "User",
            LastName = "One"
        };

        var request2 = new RegisterRequest
        {
            Email = "user2@example.com",
            Password = "password123",
            FirstName = "User",
            LastName = "Two"
        };

        // Act
        var response1 = await _client.PostAsJsonAsync("/api/auth/register", request1);
        var response2 = await _client.PostAsJsonAsync("/api/auth/register", request2);

        var auth1 = await response1.Content.ReadFromJsonAsync<AuthResponse>();
        var auth2 = await response2.Content.ReadFromJsonAsync<AuthResponse>();

        // Assert
        Assert.NotNull(auth1);
        Assert.NotNull(auth2);
        Assert.NotEqual(auth1.Token, auth2.Token);
        Assert.NotEqual(auth1.UserId, auth2.UserId);
    }

    [Fact]
    public async Task Login_MultipleTimes_GeneratesDifferentTokens()
    {
        // Arrange - Register a user
        var registerRequest = new RegisterRequest
        {
            Email = "multilogin@example.com",
            Password = "password123",
            FirstName = "Multi",
            LastName = "Login"
        };
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginRequest
        {
            Email = "multilogin@example.com",
            Password = "password123"
        };

        // Act - Login twice
        var response1 = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        await Task.Delay(100); // Small delay to ensure different JTI
        var response2 = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        var auth1 = await response1.Content.ReadFromJsonAsync<AuthResponse>();
        var auth2 = await response2.Content.ReadFromJsonAsync<AuthResponse>();

        // Assert
        Assert.NotNull(auth1);
        Assert.NotNull(auth2);
        Assert.NotEqual(auth1.Token, auth2.Token); // Different tokens due to different JTI
        Assert.Equal(auth1.UserId, auth2.UserId);  // Same user ID
    }
}
