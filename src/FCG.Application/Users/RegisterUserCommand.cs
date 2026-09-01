namespace FCG.Application.Users;

public sealed record RegisterUserCommand(string Name, string Email, string Password);
