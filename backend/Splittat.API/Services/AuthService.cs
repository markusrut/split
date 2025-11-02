using Microsoft.EntityFrameworkCore;
using Splittat.API.Data;
using Splittat.API.Data.Entities;
using Splittat.API.Infrastructure;
using Splittat.API.Models.Requests;
using Splittat.API.Models.Responses;

namespace Splittat.API.Services;

public class AuthService
{
    private readonly AppDbContext _context;
    private readonly PasswordHasher _passwordHasher;
    private readonly JwtHelper _jwtHelper;
    private readonly IConfiguration _configuration;

    public AuthService(
        AppDbContext context,
        PasswordHasher passwordHasher,
        JwtHelper jwtHelper,
        IConfiguration configuration)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtHelper = jwtHelper;
        _configuration = configuration;
    }

    public async Task<(bool Success, string? Error, AuthResponse? Response)> RegisterAsync(RegisterRequest request)
    {
        // Validate email format (already done by data annotations, but double-check)
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
        {
            return (false, "Invalid email address", null);
        }

        // Check if user already exists
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

        if (existingUser != null)
        {
            return (false, "Email already registered", null);
        }

        // Validate password
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
        {
            return (false, "Password must be at least 6 characters", null);
        }

        // Create new user
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email.ToLower().Trim(),
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Generate JWT token
        var token = _jwtHelper.GenerateToken(user.Id, user.Email);
        var expirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "60");

        var response = new AuthResponse
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes)
        };

        return (true, null, response);
    }

    public async Task<(bool Success, string? Error, AuthResponse? Response)> LoginAsync(LoginRequest request)
    {
        // Find user by email
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

        if (user == null)
        {
            return (false, "Invalid email or password", null);
        }

        // Verify password
        if (!_passwordHasher.VerifyPassword(user.PasswordHash, request.Password))
        {
            return (false, "Invalid email or password", null);
        }

        // Generate JWT token
        var token = _jwtHelper.GenerateToken(user.Id, user.Email);
        var expirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "60");

        var response = new AuthResponse
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes)
        };

        return (true, null, response);
    }
}
