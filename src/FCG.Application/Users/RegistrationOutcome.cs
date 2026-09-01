using FCG.Domain.Users;

namespace FCG.Application.Users;

public abstract record RegistrationOutcome
{
    private RegistrationOutcome()
    {
    }

    public sealed record Success(RegisteredUserResponse User) : RegistrationOutcome;

    public sealed record ValidationFailure(IReadOnlyDictionary<string, string[]> Errors) : RegistrationOutcome;

    public sealed record Conflict : RegistrationOutcome;
}

public sealed record RegisteredUserResponse(
    Guid Id,
    string Name,
    string Email,
    UserRole Role,
    DateTime CreatedAtUtc);
