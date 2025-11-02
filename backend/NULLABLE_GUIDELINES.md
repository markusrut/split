# Nullable Reference Types Guidelines

## Project Status
✅ Nullable context is **ENABLED** in `Splittat.API.csproj` (`<Nullable>enable</Nullable>`)
✅ Current build: **0 warnings, 0 errors**
✅ All 24 tests passing (12 auth + 12 file storage)

## Recent Fixes (2025-11-02)
- **FileStorageService.cs**: Removed redundant null check on non-nullable `IFormFile file` parameter
  - Changed from: `if (file == null || file.Length == 0)`
  - Changed to: `if (file.Length == 0)`
  - Rationale: If parameter is non-nullable, don't check for null - respect the type system!

## Golden Rule
**Always consider: Can this value be null?**
- If YES → Mark with `?` (e.g., `string?`)
- If NO → Leave unmarked (e.g., `string`) or use `required` keyword for properties

## Current Code Review (2025-11-02)

### ✅ Correctly Annotated Files

#### JwtHelper.cs
- ✅ `ClaimsPrincipal? ValidateToken(string token)` - Returns null on validation failure
- ✅ `Guid? GetUserIdFromToken(string token)` - Returns null if token invalid or claim missing
- ✅ Local variables properly use null-coalescing operators (`??`)

#### Entity Classes (Data/Entities/)
- ✅ **User.cs**: Required properties marked with `required` keyword
- ✅ **Receipt.cs**: Nullable fields properly marked:
  - `string? MerchantName` - Can be null if not extracted
  - `DateTime? Date` - Can be null if not extracted
  - `decimal? Tax` - Optional
  - `decimal? Tip` - Optional
- ✅ **Split.cs**: `Guid? GroupId` and `Group? Group` - Optional group association
- ✅ Navigation properties use `= null!;` pattern correctly

#### DTOs (Models/)
- ✅ All request/response models initialize non-nullable strings with `= string.Empty`
- ✅ Properly use `[Required]` attributes for validation

#### Services
- ✅ **FileStorageService.cs**: `string?` for error messages that can be null
- ✅ **AuthService.cs**: Returns tuples with nullable error/response fields

### 🔍 Areas to Monitor (Currently Correct, But Watch for Future Changes)

#### 1. IConfiguration Access
**Pattern Used**: Null-coalescing with exception
```csharp
var secretKey = _configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException("JWT SecretKey not configured");
```
✅ This is correct - fails fast if config is missing.

**Alternative Pattern** (if default is acceptable):
```csharp
var expirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "60");
```
✅ Also correct - provides sensible default.

⚠️ **Watch out for**: Direct access without null handling
```csharp
// ❌ BAD - could throw NullReferenceException
var value = _configuration["SomeKey"].ToString();

// ✅ GOOD
var value = _configuration["SomeKey"] ?? "default";
```

#### 2. Navigation Properties in Entity Framework
**Pattern Used**: `= null!;`
```csharp
public User User { get; set; } = null!;
```
✅ This is correct for EF Core navigation properties - tells compiler "trust me, EF will populate this".

⚠️ **Only use `null!` for**:
- EF Core navigation properties that are required by foreign key
- NOT for optional relationships (use `Type?` instead)

#### 3. Method Parameters
✅ All current methods use non-nullable parameters appropriately.

⚠️ **Future rule**: If a parameter can legitimately be null, mark it:
```csharp
// ✅ GOOD - parameter can be null
public void ProcessData(string? optionalFilter)
{
    if (optionalFilter != null)
    {
        // Use filter
    }
}

// ❌ BAD - caller might pass null, causing issues
public void ProcessData(string filter)
{
    var length = filter.Length; // NullReferenceException if null passed!
}
```

## Best Practices for Future Development

### 1. DTOs and Request/Response Models
```csharp
// ✅ GOOD - Required fields
public class CreateItemRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public decimal Price { get; set; }

    // Optional field - explicitly nullable
    public string? Description { get; set; }
}
```

### 2. Service Methods Returning Data
```csharp
// ✅ GOOD - Return null when not found
public async Task<Receipt?> GetReceiptByIdAsync(Guid id)
{
    return await _context.Receipts.FindAsync(id);
}

// ✅ GOOD - Return tuple with nullable error
public async Task<(bool Success, string? Error, AuthResponse? Response)> LoginAsync(LoginRequest request)
{
    // Implementation
}
```

### 3. String Initialization
```csharp
// ✅ GOOD - Non-nullable property with default
public string Name { get; set; } = string.Empty;

// ✅ GOOD - Nullable property
public string? OptionalField { get; set; }

// ❌ BAD - Non-nullable without initialization (compiler warning)
public string Name { get; set; }
```

### 4. Collection Properties
```csharp
// ✅ GOOD - Initialize collection properties
public ICollection<Receipt> Receipts { get; set; } = new List<Receipt>();

// ✅ ALSO GOOD - Required collection (EF Core 8+)
public required ICollection<Receipt> Receipts { get; set; }

// ❌ BAD - Could be null
public ICollection<Receipt> Receipts { get; set; }
```

### 5. Method Return Values
```csharp
// ✅ GOOD - Clearly indicates "might not find"
public User? FindUserByEmail(string email);

// ✅ GOOD - Never returns null
public List<User> GetAllUsers();

// ✅ GOOD - Async version
public async Task<User?> FindUserByEmailAsync(string email);
```

### 6. Null Checking Patterns
```csharp
// ✅ GOOD - Null-coalescing operator
var name = user.FirstName ?? "Unknown";

// ✅ GOOD - Null-coalescing assignment (C# 8.0+)
_cache ??= new Dictionary<string, object>();

// ✅ GOOD - Null-conditional operator
var length = user?.Email?.Length;

// ✅ GOOD - Pattern matching
if (user is not null)
{
    // Use user
}
```

### 7. Database Queries
```csharp
// ✅ GOOD - SingleOrDefaultAsync returns null if not found
var user = await _context.Users
    .SingleOrDefaultAsync(u => u.Id == id);

if (user == null)
{
    return Results.NotFound();
}

// ✅ GOOD - FirstOrDefaultAsync
var firstReceipt = await _context.Receipts
    .Where(r => r.UserId == userId)
    .OrderByDescending(r => r.CreatedAt)
    .FirstOrDefaultAsync();
```

## Common Pitfalls to Avoid

### ❌ Pitfall 1: Forgetting to Check for Null
```csharp
// ❌ BAD
public void ProcessReceipt(Receipt? receipt)
{
    Console.WriteLine(receipt.Total); // NullReferenceException if null!
}

// ✅ GOOD
public void ProcessReceipt(Receipt? receipt)
{
    if (receipt == null)
    {
        throw new ArgumentNullException(nameof(receipt));
    }
    Console.WriteLine(receipt.Total);
}
```

### ❌ Pitfall 2: Using `null!` Inappropriately
```csharp
// ❌ BAD - Hiding potential nulls
public string GetUserName(User user)
{
    return user.FirstName!; // Don't use ! unless you're 100% sure
}

// ✅ GOOD
public string GetUserName(User user)
{
    return user.FirstName ?? "Unknown";
}
```

### ❌ Pitfall 3: Inconsistent Nullable Annotations
```csharp
// ❌ BAD - Method says "never null" but can return null
public Receipt GetReceipt(Guid id)
{
    return _context.Receipts.Find(id); // This can return null!
}

// ✅ GOOD - Honest about nullability
public Receipt? GetReceipt(Guid id)
{
    return _context.Receipts.Find(id);
}
```

## Checklist for New Code

When writing new code, ask yourself:

- [ ] Can any parameter be null? If yes, mark with `?`
- [ ] Can this method return null? If yes, return type should be `T?`
- [ ] Are all non-nullable properties initialized?
- [ ] Are configuration values accessed safely with null-coalescing?
- [ ] Do database queries that might not find data return nullable types?
- [ ] Are nullable values checked before use?
- [ ] Is `null!` only used for EF Core navigation properties?

## IDE Support

Visual Studio/Rider will show warnings for:
- Possible null reference assignments
- Dereferencing a possibly null reference
- Converting null literal to non-nullable type

**Always fix these warnings!** They prevent runtime NullReferenceExceptions.

## Testing Nullable Behavior

```csharp
[Fact]
public void Method_WithNullInput_ReturnsNull()
{
    // Arrange
    var service = new MyService();

    // Act
    var result = service.FindUser(null);

    // Assert
    Assert.Null(result);
}

[Fact]
public void Method_WithNullInput_ThrowsException()
{
    // Arrange
    var service = new MyService();

    // Act & Assert
    Assert.Throws<ArgumentNullException>(() => service.ProcessUser(null!));
}
```

## Summary

✅ **Current Status**: All backend code properly uses nullable annotations
✅ **Build Status**: 0 warnings, 0 errors
✅ **Entity Framework**: Navigation properties correctly marked
✅ **Services**: Return types accurately reflect nullability
✅ **DTOs**: Properties properly initialized

**Going Forward**: Always apply the "Can this be null?" test when writing new code.
