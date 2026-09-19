using System.Security.Claims;
using System.Text.Json;
using Edp.Shared.Infrastructure.DependencyInjection;

namespace Edp.Workflow.Api.Security;

public static class WorkflowAuthorizationPolicies
{
    public const string WorkflowRead = "Workflow.Read";
    public const string WorkflowCreate = "Workflow.Create";
    public const string WorkflowUpdate = "Workflow.Update";
    public const string WorkflowValidate = "Workflow.Validate";
    public const string WorkflowPublish = "Workflow.Publish";
    public const string WorkflowStart = "Workflow.Start";
    public const string WorkflowCancel = "Workflow.Cancel";
    public const string WorkflowSuspend = "Workflow.Suspend";
    public const string WorkflowResume = "Workflow.Resume";
    public const string ApprovalRead = "Approval.Read";
    public const string ApprovalApprove = "Approval.Approve";
    public const string ApprovalReject = "Approval.Reject";
    public const string ApprovalDelegate = "Approval.Delegate";

    private static readonly string[] Policies =
    [
        WorkflowRead, WorkflowCreate, WorkflowUpdate, WorkflowValidate, WorkflowPublish,
        WorkflowStart, WorkflowCancel, WorkflowSuspend, WorkflowResume, ApprovalRead,
        ApprovalApprove, ApprovalReject, ApprovalDelegate
    ];

    public static IServiceCollection AddWorkflowAuthorization(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSharedJwtBearerAuthentication(configuration);
        services.AddSharedAuthorization(options =>
        {
            foreach (var policyName in Policies)
            {
                options.AddPolicy(policyName, policy => policy.RequireAssertion(context =>
                    HasPermission(context.User, policyName)));
            }
        });
        return services;
    }

    private static bool HasPermission(ClaimsPrincipal user, string permission)
    {
        if (user.Identity?.IsAuthenticated != true)
            return false;

        var values = user.Claims
            .Where(claim => claim.Type is "permission" or "permissions" or "scope" or ClaimTypes.Role or "roles")
            .SelectMany(claim => ParsePermissionValues(claim.Type, claim.Value));

        return values.Any(value => string.Equals(value, permission, StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<string> ParsePermissionValues(string claimType, string value)
    {
        if (claimType == "scope")
            return value.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (value.TrimStart().StartsWith("[", StringComparison.Ordinal))
        {
            try
            {
                return JsonSerializer.Deserialize<string[]>(value) ?? [];
            }
            catch (JsonException)
            {
                return [];
            }
        }

        return value.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}