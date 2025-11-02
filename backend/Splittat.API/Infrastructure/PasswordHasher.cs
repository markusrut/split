using Microsoft.AspNetCore.Identity;

namespace Splittat.API.Infrastructure;

public class PasswordHasher
{
    private readonly PasswordHasher<object> _hasher;

    public PasswordHasher()
    {
        _hasher = new PasswordHasher<object>();
    }

    public string HashPassword(string password)
    {
        return _hasher.HashPassword(null!, password);
    }

    public bool VerifyPassword(string hashedPassword, string providedPassword)
    {
        var result = _hasher.VerifyHashedPassword(null!, hashedPassword, providedPassword);
        return result == PasswordVerificationResult.Success
            || result == PasswordVerificationResult.SuccessRehashNeeded;
    }
}
