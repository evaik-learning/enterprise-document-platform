using Edp.Notification.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Edp.Shared.Infrastructure.Exceptions;
using Edp.Shared.Security.CurrentUser;
using Microsoft.AspNetCore.Authorization;

namespace Edp.Notification.Api.Controllers;

[ApiController]
[Authorize(Policy = "notification.read")]
[Route("api/v1/notifications")]
public sealed class NotificationsController : ControllerBase
{
    private readonly INotificationService _service;
    private readonly ICurrentOrganization _organization;
    private readonly ICurrentUser _user;

    public NotificationsController(INotificationService service, ICurrentOrganization organization, ICurrentUser user)
    {
        _service = service;
        _organization = organization;
        _user = user;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? organizationId,
        [FromQuery] Guid? userId,
        [FromQuery] bool unreadOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var context = ResolveContext(organizationId, userId);
        var notifications = await _service.ListAsync(context.OrganizationId, context.UserId, unreadOnly, page, pageSize, cancellationToken);
        return Ok(notifications);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, [FromQuery] Guid? organizationId, [FromQuery] Guid? userId, CancellationToken cancellationToken)
    {
        var context = ResolveContext(organizationId, userId);
        var notification = await _service.GetAsync(context.OrganizationId, context.UserId, id, cancellationToken);
        return notification is null ? NotFound() : Ok(notification);
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, [FromQuery] Guid? organizationId, [FromQuery] Guid? userId, CancellationToken cancellationToken)
    {
        var context = ResolveContext(organizationId, userId);
        await _service.MarkReadAsync(context.OrganizationId, context.UserId, id, cancellationToken);
        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead([FromQuery] Guid? organizationId, [FromQuery] Guid? userId, CancellationToken cancellationToken)
    {
        var context = ResolveContext(organizationId, userId);
        await _service.MarkAllReadAsync(context.OrganizationId, context.UserId, cancellationToken);
        return NoContent();
    }

    private (Guid OrganizationId, Guid UserId) ResolveContext(Guid? requestedOrganizationId, Guid? requestedUserId)
    {
        var organizationId = _organization.OrganizationId;
        var userId = _user.UserId;
        if (!organizationId.HasValue || userId == Guid.Empty
            || (requestedOrganizationId.HasValue && requestedOrganizationId != organizationId)
            || (requestedUserId.HasValue && requestedUserId != userId))
            throw new ForbiddenProblemDetailsException("The requested notification context is not accessible.", "NOTIFICATION_CONTEXT_FORBIDDEN");
        return (organizationId.Value, userId);
    }
}
