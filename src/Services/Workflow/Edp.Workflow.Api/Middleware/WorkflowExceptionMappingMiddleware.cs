using System.Net;
using Edp.Shared.Infrastructure.Exceptions;
using Edp.Workflow.Domain;

namespace Edp.Workflow.Api.Middleware;

public sealed class WorkflowExceptionMappingMiddleware
{
    private readonly RequestDelegate _next;

    public WorkflowExceptionMappingMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (WorkflowDomainException exception)
        {
            throw Map(exception);
        }
    }

    private static ProblemDetailsException Map(WorkflowDomainException exception) => exception switch
    {
        WorkflowNotFoundException => new NotFoundProblemDetailsException(exception.Message, "WORKFLOW_NOT_FOUND"),
        WorkflowInstanceNotFoundException => new NotFoundProblemDetailsException(exception.Message, "WORKFLOW_INSTANCE_NOT_FOUND"),
        ApprovalTaskNotFoundException => new NotFoundProblemDetailsException(exception.Message, "APPROVAL_NOT_FOUND"),
        InvalidWorkflowVersionException => new NotFoundProblemDetailsException(exception.Message, "WORKFLOW_VERSION_NOT_FOUND"),
        WorkflowStateNotFoundException => new NotFoundProblemDetailsException(exception.Message, "WORKFLOW_STATE_NOT_FOUND"),
        ApprovalNotPermittedException => new ForbiddenProblemDetailsException(exception.Message, "UNAUTHORIZED_APPROVAL"),
        InvalidWorkflowDefinitionException => new UnprocessableEntityProblemDetailsException(exception.Message, "WORKFLOW_INVALID"),
        CannotPublishWorkflowException => new UnprocessableEntityProblemDetailsException(exception.Message, "WORKFLOW_INVALID"),
        InvalidTransitionException => new ConflictProblemDetailsException(exception.Message, "INVALID_TRANSITION"),
        InvalidWorkflowStateException => new ConflictProblemDetailsException(exception.Message, "INVALID_STATE"),
        ApprovalDeadlineExpiredException => new ConflictProblemDetailsException(exception.Message, "APPROVAL_EXPIRED"),
        CircularDependencyException => new UnprocessableEntityProblemDetailsException(exception.Message, "WORKFLOW_INVALID"),
        MissingRequiredVariableException => new UnprocessableEntityProblemDetailsException(exception.Message, "MISSING_REQUIRED_VARIABLE"),
        GuardEvaluationException => new UnprocessableEntityProblemDetailsException(exception.Message, "GUARD_EVALUATION_FAILED"),
        _ => new ProblemDetailsExceptionAdapter(exception)
    };

    private sealed class ProblemDetailsExceptionAdapter : ProblemDetailsException
    {
        public ProblemDetailsExceptionAdapter(Exception exception)
            : base("Workflow request failed", exception.Message, HttpStatusCode.BadRequest, "WORKFLOW_ERROR")
        {
        }
    }
}