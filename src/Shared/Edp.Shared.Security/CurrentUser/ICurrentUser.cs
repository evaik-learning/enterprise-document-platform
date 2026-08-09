namespace Edp.Shared.Security.CurrentUser;

public interface ICurrentUser
{
    Guid UserId { get; }
    string? UserName { get; }
    bool IsAuthenticated { get; }
}
