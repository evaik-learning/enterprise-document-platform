using Edp.Workflow.Application.Interfaces;
using Edp.Workflow.Domain;
using Edp.Shared.Security.CurrentUser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Edp.Workflow.Application.Contracts;
using Edp.Workflow.Api.Security;
using Edp.Workflow.Application;
using Microsoft.Extensions.Options;

namespace Edp.Workflow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/workflows")]
public sealed class WorkflowController : ControllerBase
{
    private readonly IWorkflowRepository _workflowRepository;
    private readonly IWorkflowExecutionService _executionService;
    private readonly IWorkflowDefinitionService _definitionService;
    private readonly IApprovalTaskRepository _approvalTaskRepository;
    private readonly IWorkflowHistoryRepository _historyRepository;
    private readonly IIdempotencyRepository _idempotencyRepository;
    private readonly ICurrentOrganization _currentOrganization;
    private readonly ICurrentUser _currentUser;
    private readonly WorkflowOptions _options;

    public WorkflowController(
        IWorkflowRepository workflowRepository,
        IWorkflowExecutionService executionService,
        IWorkflowDefinitionService definitionService,
        ICurrentOrganization currentOrganization,
        ICurrentUser currentUser,
        IApprovalTaskRepository approvalTaskRepository,
        IWorkflowHistoryRepository historyRepository,
        IIdempotencyRepository idempotencyRepository,
        IOptions<WorkflowOptions> options)
    {
        _workflowRepository = workflowRepository;
        _executionService = executionService;
        _definitionService = definitionService;
        _currentOrganization = currentOrganization;
        _currentUser = currentUser;
        _approvalTaskRepository = approvalTaskRepository;
        _historyRepository = historyRepository;
        _idempotencyRepository = idempotencyRepository;
        _options = options.Value;
    }

    [HttpPost]
    [Authorize(Policy = WorkflowAuthorizationPolicies.WorkflowCreate)]
    public async Task<ActionResult<WorkflowSummary>> Create(
        [FromBody] CreateWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        var workflow = await _definitionService.CreateWorkflowAsync(
            GetOrganizationId(), request.Code, request.Name, request.Description, _currentUser.UserId, cancellationToken);
        return CreatedAtAction(nameof(Get), new { workflowId = workflow.Id }, new WorkflowSummary(
            workflow.Id, workflow.Code, workflow.Name, workflow.Status, workflow.PublishedVersion));
    }

    [HttpGet("{workflowId:guid}")]
    [Authorize(Policy = WorkflowAuthorizationPolicies.WorkflowRead)]
    public async Task<ActionResult<WorkflowSummary>> Get(Guid workflowId, CancellationToken cancellationToken)
    {
        var workflow = await _workflowRepository.GetByIdAsync(GetOrganizationId(), workflowId, cancellationToken);
        return workflow is null
            ? NotFound()
            : Ok(new WorkflowSummary(workflow.Id, workflow.Code, workflow.Name, workflow.Status, workflow.PublishedVersion));
    }

    [HttpPost("{workflowId:guid}/versions")]
    [Authorize(Policy = WorkflowAuthorizationPolicies.WorkflowUpdate)]
    public async Task<ActionResult<WorkflowVersionResponse>> CreateVersion(
        Guid workflowId,
        CancellationToken cancellationToken)
    {
        var version = await _definitionService.CreateVersionAsync(
            GetOrganizationId(), workflowId, _currentUser.UserId, cancellationToken);
        return Ok(new WorkflowVersionResponse(version.Id, version.WorkflowId, version.Version, version.IsPublished));
    }

    [HttpGet("{workflowId:guid}/versions")]
    [Authorize(Policy = WorkflowAuthorizationPolicies.WorkflowRead)]
    public async Task<ActionResult<IReadOnlyList<WorkflowVersionResponse>>> GetVersions(
        Guid workflowId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = NormalizePage(page, pageSize);
        var versions = await _definitionService.GetVersionsPageAsync(
            GetOrganizationId(), workflowId, page, pageSize, cancellationToken);
        return Ok(versions.Select(version => new WorkflowVersionResponse(
            version.Id, version.WorkflowId, version.Version, version.IsPublished)));
    }

    [HttpPost("{workflowId:guid}/archive")]
    [Authorize(Policy = WorkflowAuthorizationPolicies.WorkflowUpdate)]
    public async Task<IActionResult> Archive(Guid workflowId, CancellationToken cancellationToken)
    {
        await _definitionService.ArchiveWorkflowAsync(
            GetOrganizationId(), workflowId, _currentUser.UserId, cancellationToken);
        return NoContent();
    }

    [HttpPost("versions/{versionId:guid}/states")]
    [Authorize(Policy = WorkflowAuthorizationPolicies.WorkflowUpdate)]
    public async Task<ActionResult<WorkflowStateResponse>> AddState(
        Guid versionId,
        [FromBody] AddStateRequest request,
        CancellationToken cancellationToken)
    {
        var state = await _definitionService.AddStateAsync(
            GetOrganizationId(), versionId, request.Name, request.StateType, request.Configuration, cancellationToken);
        return Ok(new WorkflowStateResponse(state.Id, state.WorkflowVersionId, state.Name, state.StateType));
    }

    [HttpPost("versions/{versionId:guid}/transitions")]
    [Authorize(Policy = WorkflowAuthorizationPolicies.WorkflowUpdate)]
    public async Task<ActionResult<WorkflowTransitionResponse>> AddTransition(
        Guid versionId,
        [FromBody] AddTransitionRequest request,
        CancellationToken cancellationToken)
    {
        var transition = await _definitionService.AddTransitionAsync(
            GetOrganizationId(), versionId, request.FromStateId, request.ToStateId, request.Guard, request.Order, request.TriggerType, cancellationToken);
        return Ok(new WorkflowTransitionResponse(transition.Id, transition.WorkflowVersionId, transition.FromStateId, transition.ToStateId, transition.Order));
    }

    [HttpPost("{workflowId:guid}/versions/{version:int}/publish")]
    [Authorize(Policy = WorkflowAuthorizationPolicies.WorkflowPublish)]
    public async Task<IActionResult> Publish(
        Guid workflowId,
        int version,
        CancellationToken cancellationToken)
    {
        await _definitionService.PublishVersionAsync(
            GetOrganizationId(), workflowId, version, _currentUser.UserId, cancellationToken);
        return NoContent();
    }

    [HttpPost("versions/{versionId:guid}/validate")]
    [Authorize(Policy = WorkflowAuthorizationPolicies.WorkflowValidate)]
    public async Task<ActionResult<WorkflowValidationResponse>> Validate(Guid versionId, CancellationToken cancellationToken)
    {
        var errors = await _definitionService.ValidateVersionAsync(GetOrganizationId(), versionId, cancellationToken);
        return Ok(new WorkflowValidationResponse(errors.Count == 0, errors));
    }

    [HttpGet]
    [Authorize(Policy = WorkflowAuthorizationPolicies.WorkflowRead)]
    public async Task<ActionResult<IReadOnlyList<WorkflowSummary>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var organizationId = GetOrganizationId();
        (page, pageSize) = NormalizePage(page, pageSize);
        var workflows = await _workflowRepository.ListPageAsync(organizationId, page, pageSize, cancellationToken);
        return Ok(workflows.Select(workflow => new WorkflowSummary(
            workflow.Id,
            workflow.Code,
            workflow.Name,
            workflow.Status,
            workflow.PublishedVersion)));
    }

    [HttpPost("{workflowId:guid}/instances")]
    [Authorize(Policy = WorkflowAuthorizationPolicies.WorkflowStart)]
    public async Task<ActionResult<WorkflowInstanceResponse>> Start(
        Guid workflowId,
        [FromBody] StartWorkflowRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var organizationId = GetOrganizationId();
        var requestHash = HashRequest(request);
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existing = await _idempotencyRepository.GetAsync(organizationId, idempotencyKey, cancellationToken);
            if (existing is not null)
            {
                if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
                    return Conflict("The idempotency key was already used with a different request.");
                return new ContentResult
                {
                    StatusCode = existing.ResponseStatusCode,
                    ContentType = "application/json",
                    Content = existing.ResponseBody
                };
            }
        }
        var instance = await _executionService.StartAsync(
            organizationId,
            workflowId,
            request.DocumentId,
            _currentUser.UserId,
            request.CorrelationId ?? HttpContext.TraceIdentifier,
            cancellationToken);

        var response = ToResponse(instance);
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            await _idempotencyRepository.AddAsync(new IdempotencyRecord(
                organizationId, idempotencyKey, requestHash, StatusCodes.Status201Created,
                JsonSerializer.Serialize(response), DateTimeOffset.UtcNow.AddHours(24)), cancellationToken);
        }
        return CreatedAtAction(nameof(GetInstance), new { instanceId = instance.Id }, response);
    }

    [HttpGet("instances/{instanceId:guid}")]
    [Authorize(Policy = WorkflowAuthorizationPolicies.WorkflowRead)]
    public async Task<ActionResult<WorkflowInstanceResponse>> GetInstance(
        Guid instanceId,
        [FromServices] IWorkflowInstanceRepository instanceRepository,
        CancellationToken cancellationToken)
    {
        var instance = await instanceRepository.GetByIdAsync(GetOrganizationId(), instanceId, cancellationToken);
        return instance is null ? NotFound() : Ok(ToResponse(instance));
    }

    [HttpPost("instances/{instanceId:guid}/cancel")]
    [Authorize(Policy = WorkflowAuthorizationPolicies.WorkflowCancel)]
    public async Task<ActionResult<WorkflowInstanceResponse>> Cancel(Guid instanceId, [FromBody] WorkflowReasonRequest request, CancellationToken cancellationToken)
    {
        var instance = await _executionService.CancelAsync(GetOrganizationId(), instanceId, _currentUser.UserId, request.Reason, cancellationToken);
        return Ok(ToResponse(instance));
    }

    [HttpPost("instances/{instanceId:guid}/suspend")]
    [Authorize(Policy = WorkflowAuthorizationPolicies.WorkflowSuspend)]
    public async Task<ActionResult<WorkflowInstanceResponse>> Suspend(Guid instanceId, [FromBody] WorkflowReasonRequest request, CancellationToken cancellationToken)
    {
        var instance = await _executionService.SuspendAsync(GetOrganizationId(), instanceId, _currentUser.UserId, request.Reason, cancellationToken);
        return Ok(ToResponse(instance));
    }

    [HttpPost("instances/{instanceId:guid}/resume")]
    [Authorize(Policy = WorkflowAuthorizationPolicies.WorkflowResume)]
    public async Task<ActionResult<WorkflowInstanceResponse>> Resume(Guid instanceId, CancellationToken cancellationToken)
    {
        var instance = await _executionService.ResumeAsync(GetOrganizationId(), instanceId, _currentUser.UserId, cancellationToken);
        return Ok(ToResponse(instance));
    }

    [HttpPost("instances/{instanceId:guid}/transitions/{transitionId:guid}")]
    [Authorize(Policy = WorkflowAuthorizationPolicies.WorkflowStart)]
    public async Task<ActionResult<WorkflowInstanceResponse>> ExecuteTransition(Guid instanceId, Guid transitionId, CancellationToken cancellationToken)
    {
        var instance = await _executionService.ExecuteTransitionAsync(
            GetOrganizationId(), instanceId, transitionId, _currentUser.UserId,
            HttpContext.TraceIdentifier, cancellationToken);
        return Ok(ToResponse(instance));
    }

    [HttpGet("instances/{instanceId:guid}/history")]
    [Authorize(Policy = WorkflowAuthorizationPolicies.WorkflowRead)]
    public async Task<ActionResult<IReadOnlyList<WorkflowHistoryResponse>>> History(
        Guid instanceId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = NormalizePage(page, pageSize);
        var history = await _historyRepository.ListPageAsync(GetOrganizationId(), instanceId, page, pageSize, cancellationToken);
        return Ok(history.Select(entry => new WorkflowHistoryResponse(
            entry.Id, entry.EventType, entry.StateId, entry.ApprovalTaskId, entry.UserId,
            entry.Description, entry.EventAt)));
    }

    [HttpGet("approval-tasks/my")]
    [Authorize(Policy = WorkflowAuthorizationPolicies.ApprovalRead)]
    public async Task<ActionResult<IReadOnlyList<ApprovalTaskResponse>>> MyApprovals(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = NormalizePage(page, pageSize);
        var tasks = await _approvalTaskRepository.ListForUserPageAsync(
            GetOrganizationId(), _currentUser.UserId, page, pageSize, cancellationToken);
        return Ok(tasks.Select(ToResponse));
    }

    private (int Page, int PageSize) NormalizePage(int page, int pageSize)
    {
        var maximum = Math.Max(1, _options.MaxHistoryPageSize);
        return (Math.Max(1, page), Math.Clamp(pageSize, 1, maximum));
    }

    [HttpPost("approval-tasks/{taskId:guid}/approve")]
    [Authorize(Policy = WorkflowAuthorizationPolicies.ApprovalApprove)]
    public async Task<ActionResult<ApprovalTaskResponse>> Approve(
        Guid taskId,
        [FromBody] ApprovalCommentRequest? request,
        CancellationToken cancellationToken)
    {
        var task = await _executionService.ApproveAsync(
            GetOrganizationId(), taskId, _currentUser.UserId, request?.Comment, cancellationToken);
        return Ok(ToResponse(task));
    }

    [HttpPost("approval-tasks/{taskId:guid}/reject")]
    [Authorize(Policy = WorkflowAuthorizationPolicies.ApprovalReject)]
    public async Task<ActionResult<ApprovalTaskResponse>> Reject(
        Guid taskId,
        [FromBody] ApprovalCommentRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Comment))
            return BadRequest("A rejection reason is required.");

        var task = await _executionService.RejectAsync(
            GetOrganizationId(), taskId, _currentUser.UserId, request.Comment, cancellationToken);
        return Ok(ToResponse(task));
    }

    [HttpPost("approval-tasks/{taskId:guid}/delegate")]
    [Authorize(Policy = WorkflowAuthorizationPolicies.ApprovalDelegate)]
    public async Task<ActionResult<ApprovalTaskResponse>> Delegate(Guid taskId, [FromBody] DelegateApprovalRequest request, CancellationToken cancellationToken)
    {
        if (request.DelegateToUserId == Guid.Empty || string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest("A delegate user and reason are required.");

        var task = await _executionService.DelegateAsync(
            GetOrganizationId(), taskId, request.DelegateToUserId, _currentUser.UserId, request.Reason, cancellationToken);
        return Ok(ToResponse(task));
    }

    private Guid GetOrganizationId() =>
        _currentOrganization.OrganizationId
        ?? throw new UnauthorizedAccessException("An organization context is required.");

    private static WorkflowInstanceResponse ToResponse(WorkflowInstance instance) =>
        new(instance.Id, instance.WorkflowId, instance.WorkflowVersion, instance.DocumentId, instance.Status, instance.CurrentStateId);

    private static ApprovalTaskResponse ToResponse(ApprovalTask task) =>
        new(task.Id, task.WorkflowInstanceId, task.StateId, task.Status, task.AssignedToUserId, task.DeadlineAt);

    private static string HashRequest<T>(T request) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request))));
}

public sealed record WorkflowSummary(Guid Id, string Code, string Name, WorkflowStatus Status, int? PublishedVersion);
public sealed record CreateWorkflowRequest(string Code, string Name, string? Description);
public sealed record WorkflowVersionResponse(Guid Id, Guid WorkflowId, int Version, bool IsPublished);
public sealed record AddStateRequest(string Name, StateType StateType, Dictionary<string, string>? Configuration);
public sealed record AddTransitionRequest(Guid FromStateId, Guid ToStateId, TransitionGuard? Guard, int Order = 0, string TriggerType = "complete");
public sealed record WorkflowStateResponse(Guid Id, Guid WorkflowVersionId, string Name, StateType StateType);
public sealed record WorkflowTransitionResponse(Guid Id, Guid WorkflowVersionId, Guid FromStateId, Guid ToStateId, int Order);
public sealed record StartWorkflowRequest(Guid DocumentId, string? CorrelationId);
public sealed record ApprovalCommentRequest(string? Comment);
public sealed record DelegateApprovalRequest(Guid DelegateToUserId, string Reason);
public sealed record WorkflowReasonRequest(string Reason);
public sealed record WorkflowInstanceResponse(Guid Id, Guid WorkflowId, int WorkflowVersion, Guid DocumentId, InstanceStatus Status, Guid? CurrentStateId);
public sealed record ApprovalTaskResponse(Guid Id, Guid WorkflowInstanceId, Guid StateId, ApprovalStatus Status, Guid AssignedToUserId, DateTime? DeadlineAt);
public sealed record WorkflowHistoryResponse(Guid Id, HistoryEventType EventType, Guid? StateId, Guid? ApprovalTaskId, Guid? UserId, string Description, DateTime EventAt);
public sealed record WorkflowValidationResponse(bool IsValid, IReadOnlyList<string> Errors);
