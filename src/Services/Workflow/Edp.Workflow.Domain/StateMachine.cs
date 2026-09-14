using Edp.SharedKernel.Domain;

namespace Edp.Workflow.Domain;

public sealed record TransitionResult(
    bool Success,
    string Message,
    Guid? ToStateId = null,
    IReadOnlyList<DomainEvent>? Events = null,
    string? ErrorCode = null)
{
    public static TransitionResult Allowed(Guid toStateId) =>
        new(true, "Transition is allowed", toStateId);

    public static TransitionResult Rejected(string message, string errorCode) =>
        new(false, message, ErrorCode: errorCode);
}

public interface IWorkflowStateMachine
{
    TransitionResult Evaluate(
        WorkflowInstance instance,
        WorkflowVersion version,
        WorkflowTransition transition,
        WorkflowExecutionContext context);

    TransitionResult Execute(
        WorkflowInstance instance,
        WorkflowVersion version,
        WorkflowTransition transition,
        WorkflowExecutionContext context);
}

public sealed class WorkflowStateMachine : IWorkflowStateMachine
{
    private readonly IGuardEvaluator _guardEvaluator;

    public WorkflowStateMachine(IGuardEvaluator guardEvaluator)
    {
        _guardEvaluator = guardEvaluator;
    }

    public TransitionResult Evaluate(
        WorkflowInstance instance,
        WorkflowVersion version,
        WorkflowTransition transition,
        WorkflowExecutionContext context)
    {
        if (instance.OrganizationId != context.OrganizationId || version.OrganizationId != context.OrganizationId)
            return TransitionResult.Rejected("Organization context does not match workflow data", "organization_mismatch");

        if (instance.CurrentStateId != transition.FromStateId)
            return TransitionResult.Rejected("Transition does not originate from the current state", "invalid_source_state");

        if (version.GetState(transition.ToStateId) is null)
            return TransitionResult.Rejected("Transition destination state does not exist", "invalid_destination_state");

        if (!_guardEvaluator.Evaluate(transition.GetGuard(), context.Variables))
            return TransitionResult.Rejected("Transition guard evaluated to false", "guard_failed");

        return TransitionResult.Allowed(transition.ToStateId);
    }

    public TransitionResult Execute(
        WorkflowInstance instance,
        WorkflowVersion version,
        WorkflowTransition transition,
        WorkflowExecutionContext context)
    {
        var evaluation = Evaluate(instance, version, transition, context);
        if (!evaluation.Success)
            return evaluation;

        instance.TransitionToState(transition.ToStateId, context.ActorUserId);
        return evaluation with { Events = instance.DomainEvents.ToList() };
    }
}
