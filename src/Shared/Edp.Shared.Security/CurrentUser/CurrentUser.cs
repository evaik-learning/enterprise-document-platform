using System.Security.Claims;

namespace Edp.Shared.Security.CurrentUser;

public sealed class CurrentUser : ICurrentUser
{
    public Guid UserId { get; init; }
    public string? UserName { get; init; }
    public bool IsAuthenticated { get; init; }
    public string? Email { get; init; }
    public string[] Roles { get; init; } = [];
    public IReadOnlyDictionary<string, string> Claims { get; init; } = new Dictionary<string, string>();

    public static CurrentUser Anonymous { get; } = new() { UserId = Guid.Empty, UserName = null, IsAuthenticated = false };

    public static CurrentUser FromClaimsPrincipal(ClaimsPrincipal principal)
    {
        if (principal?.Identity is null || !principal.Identity.IsAuthenticated)
        {
            return Anonymous;
        }

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)
            ?? principal.FindFirst("oid")
            ?? principal.FindFirst("sub");

        var userId = userIdClaim is not null && Guid.TryParse(userIdClaim.Value, out var parsedUserId)
            ? parsedUserId
            : Guid.Empty;

        var userName = principal.FindFirst(ClaimTypes.Name)?.Value
            ?? principal.FindFirst(ClaimTypes.Email)?.Value
            ?? principal.FindFirst("preferred_username")?.Value
            ?? principal.Identity.Name;

        var roles = principal.FindAll(ClaimTypes.Role)
            .Select(x => x.Value)
            .Concat(principal.FindAll("roles").Select(x => x.Value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var claims = principal.Claims
            .GroupBy(x => x.Type)
            .ToDictionary(
                g => g.Key,
                g => g.Last().Value,
                StringComparer.OrdinalIgnoreCase);

        return new CurrentUser
        {
            UserId = userId,
            UserName = userName,
            IsAuthenticated = true,
            Email = principal.FindFirst(ClaimTypes.Email)?.Value ?? principal.FindFirst("email")?.Value,
            Roles = roles,
            Claims = claims
        };
    }
}
