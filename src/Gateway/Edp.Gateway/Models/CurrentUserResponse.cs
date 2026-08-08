namespace Edp.Gateway.Models;

public sealed record CurrentUserResponse(
    bool IsAuthenticated,
    string? DisplayName,
    string? UserName,
    IReadOnlyCollection<UserClaimResponse> Claims);

public sealed record UserClaimResponse(string Type, string Value);
