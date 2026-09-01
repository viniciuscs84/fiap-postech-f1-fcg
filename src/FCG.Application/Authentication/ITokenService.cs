using FCG.Domain.Users;

namespace FCG.Application.Authentication;

public interface ITokenService
{
    string CreateToken(Guid userId, string email, UserRole role);
}
