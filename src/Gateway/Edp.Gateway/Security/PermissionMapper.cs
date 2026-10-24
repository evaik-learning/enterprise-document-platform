namespace Edp.Gateway.Security;

/// <summary>
/// Translates the Organization service permission catalog into the stable permission names
/// that the web application consumes. Backend services remain the authority for enforcement.
/// </summary>
public static class PermissionMapper
{
    private static readonly Dictionary<string, string[]> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Organization.Read"] = ["organization.read"],
        ["Organization.Manage"] = ["organization.admin"],
        ["Members.Manage"] = ["organization.admin"],
        ["Templates.Read"] = ["template.read"],
        ["Templates.Write"] = ["template.write"],
        ["Documents.Read"] = ["document.read"],
        ["Documents.Write"] = ["document.generate"],
        ["Workflow.Read"] = ["workflow.read"],
        ["Workflow.Start"] = ["workflow.execute"],
        ["Approval.Approve"] = ["workflow.approve"],
        ["Signing.Read"] = ["signature.read"],
        ["Signing.Manage"] = ["signature.execute"],
        ["Notifications.Read"] = ["notification.read"],
        ["Audit.Read"] = ["audit.read"]
    };

    public static IReadOnlyList<string> ToFrontend(IEnumerable<string> backendPermissions) =>
        backendPermissions
            .SelectMany(permission => Map.TryGetValue(permission, out var mapped) ? mapped : [])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.Ordinal)
            .ToArray();
}
