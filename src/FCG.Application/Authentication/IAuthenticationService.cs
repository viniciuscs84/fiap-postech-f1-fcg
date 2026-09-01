namespace FCG.Application.Authentication;

public interface IAuthenticationService
{
    Task<LoginOutcome> LoginAsync(LoginCommand command, CancellationToken cancellationToken);
}
