namespace FCG.Application.Users;

public interface IUserRegistrationService
{
    Task<RegistrationOutcome> RegisterAsync(RegisterUserCommand command, CancellationToken cancellationToken);
}
