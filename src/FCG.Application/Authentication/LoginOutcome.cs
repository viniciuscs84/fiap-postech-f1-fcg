namespace FCG.Application.Authentication;

public abstract record LoginOutcome
{
    private LoginOutcome()
    {
    }

    public sealed record Success(string AccessToken) : LoginOutcome;

    public sealed record Failure : LoginOutcome;
}
