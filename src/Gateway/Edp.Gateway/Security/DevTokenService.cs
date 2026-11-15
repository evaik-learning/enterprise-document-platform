using System.Security.Claims;
using Edp.Gateway.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Edp.Gateway.Security;

/// <summary>Built-in demo identities used when <c>Gateway:DevAuth:Users</c> is not configured.</summary>
public static class DevUsers
{
    private static readonly string[] WorkflowAdmin =
    [
        "Workflow.Read", "Workflow.Create", "Workflow.Update", "Workflow.Validate", "Workflow.Publish",
        "Workflow.Start", "Workflow.Cancel", "Workflow.Suspend", "Workflow.Resume",
        "Approval.Read", "Approval.Approve", "Approval.Reject", "Approval.Delegate"
    ];

    public static IReadOnlyList<DevUserOptions> Defaults { get; } =
    [
        new()
        {
            Id = Guid.Parse("00000000-0000-0000-0000-0000000000a1"),
            DisplayName = "Alex Admin",
            Email = "alex.admin@local.test",
            Description = "Organization owner: templates, documents, workflows, signatures, audit, administration",
            Roles = ["OrganizationAdmin", "SigningManager", "Signer", "Auditor", .. WorkflowAdmin]
        },
        new()
        {
            Id = Guid.Parse("00000000-0000-0000-0000-0000000000a2"),
            DisplayName = "Priya Approver",
            Email = "priya.approver@local.test",
            Description = "Reviews and approves documents (add as Administrator to see Approvals)",
            Roles = ["Signer", "Workflow.Read", "Workflow.Start", "Approval.Read", "Approval.Approve", "Approval.Reject", "Approval.Delegate"]
        },
        new()
        {
            Id = Guid.Parse("00000000-0000-0000-0000-0000000000a3"),
            DisplayName = "Sam Signer",
            Email = "sam.signer@local.test",
            Description = "External-style signer: opens and signs signature requests",
            Roles = ["Signer", "Workflow.Read", "Approval.Read"]
        }
    ];
}

/// <summary>Issues short-lived HS256 access tokens that the backend services accept through <c>Jwt:Key</c>.</summary>
public sealed class DevTokenService(Microsoft.Extensions.Options.IOptions<GatewayOptions> options)
{
    private readonly DevAuthOptions _options = options.Value.DevAuth;

    public IReadOnlyList<DevUserOptions> Users => _options.Users.Count > 0 ? _options.Users : DevUsers.Defaults;

    public DevUserOptions? Find(Guid id) => Users.FirstOrDefault(user => user.Id == id);

    public (string Token, DateTimeOffset ExpiresAt) Issue(DevUserOptions user)
    {
        var expires = DateTimeOffset.UtcNow.AddMinutes(_options.TokenLifetimeMinutes);
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Expires = expires.UtcDateTime,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)), SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>
            {
                ["sub"] = user.Id.ToString(),
                ["oid"] = user.Id.ToString(),
                ["name"] = user.DisplayName,
                ["email"] = user.Email,
                ["preferred_username"] = user.Email,
                ["roles"] = user.Roles
            }
        };
        return (new JsonWebTokenHandler().CreateToken(descriptor), expires);
    }

    public ClaimsPrincipal CreatePrincipal(DevUserOptions user, string authenticationType)
    {
        var identity = new ClaimsIdentity(
            [
                new Claim("oid", user.Id.ToString()),
                new Claim("sub", user.Id.ToString()),
                new Claim("name", user.DisplayName),
                new Claim("email", user.Email),
                new Claim("preferred_username", user.Email)
            ],
            authenticationType,
            "name",
            ClaimTypes.Role);
        return new ClaimsPrincipal(identity);
    }
}
