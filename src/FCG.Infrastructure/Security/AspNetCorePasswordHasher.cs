using FCG.Application.Users;
using Microsoft.AspNetCore.Identity;

namespace FCG.Infrastructure.Security;

public sealed class AspNetCorePasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<object> hasher = new();

    public string HashPassword(string password)
    {
        return hasher.HashPassword(new object(), password);
    }

    public bool VerifyPassword(string passwordHash, string providedPassword)
    {
        return hasher.VerifyHashedPassword(new object(), passwordHash, providedPassword) is PasswordVerificationResult.Success
            or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
