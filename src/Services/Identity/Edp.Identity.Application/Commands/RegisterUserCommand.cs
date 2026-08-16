namespace Edp.Identity.Application.Commands;

public sealed record RegisterUserCommand(string Email, string FirstName, string LastName, string? Password = null);
