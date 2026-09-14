using Edp.Workflow.Domain;

namespace Edp.Workflow.Application.Interfaces;

public interface IAssignmentResolver
{
    Task<IReadOnlyList<Guid>> ResolveAsync(
        IReadOnlyList<AssignmentRule> rules,
        Guid actorUserId,
        IReadOnlyCollection<string> actorRoles,
        CancellationToken cancellationToken = default);
}

public sealed class AssignmentResolver : IAssignmentResolver
{
    public Task<IReadOnlyList<Guid>> ResolveAsync(
        IReadOnlyList<AssignmentRule> rules,
        Guid actorUserId,
        IReadOnlyCollection<string> actorRoles,
        CancellationToken cancellationToken = default)
    {
        var users = rules
            .Where(rule => rule.Type == AssignmentRuleType.FixedUsers)
            .SelectMany(rule => rule.UserIds ?? [])
            .Where(userId => userId != Guid.Empty)
            .Distinct()
            .ToList();

        // Role membership is verified by the Identity integration at the boundary.
        // The actor is eligible when the request carries a matching role claim.
        foreach (var rule in rules.Where(rule => rule.Type == AssignmentRuleType.OrganizationRole))
        {
            if (!string.IsNullOrWhiteSpace(rule.RoleCode) && actorRoles.Contains(rule.RoleCode, StringComparer.OrdinalIgnoreCase))
                users.Add(actorUserId);
        }

        return Task.FromResult<IReadOnlyList<Guid>>(users.Distinct().ToList());
    }
}
