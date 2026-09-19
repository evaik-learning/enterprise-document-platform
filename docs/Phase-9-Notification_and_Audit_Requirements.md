# Phase 9 — Notification & Audit Services

**Project:** Enterprise Document Automation Platform  
**Repository:** `EnterpriseDocumentPlatform`  
**Phase:** 9  
**Status:** Implementation Ready  
**Primary Goal:** Implement reliable, event-driven Notification and Audit capabilities that integrate with the Workflow and Digital Signature phases and provide a complete, immutable history of business and security events.

---

## 1. Phase Objective

Phase 9 introduces two platform capabilities:

1. **Notification Service**
   - Email notifications.
   - In-app notifications.
   - Microsoft Teams notification abstraction.
   - Template-based notification messages.
   - Workflow and signature event notifications.
   - Retry, failure handling and dead-letter processing.
   - User notification preferences.
   - Correlation and idempotency.

2. **Audit Service**
   - Immutable audit trail for business, security and system events.
   - Actor, organization, document, template, workflow and signature context.
   - Before/after values where appropriate.
   - Correlation across distributed services.
   - Audit search and filtering.
   - Retention-aware storage.
   - Security-sensitive event tracking.

The implementation must be production-oriented and event-driven. Services must not directly depend on each other's databases.

---

# 2. Phase 9 Scope

## In Scope

### Notification

- Notification Service.
- Notification templates.
- Email provider abstraction.
- In-app notifications.
- Teams provider abstraction.
- Notification preferences.
- Event-driven notification processing.
- Notification status tracking.
- Retry policy.
- Dead-letter handling.
- Idempotent notification processing.
- Correlation IDs.
- Notification API.
- Notification worker.
- Integration with Azure Service Bus.
- Integration with Workflow Service.
- Integration with Digital Signature Service.
- Integration with Identity/Organization.
- Basic React notification center.

### Audit

- Audit Service.
- Audit event contract.
- Audit persistence.
- Immutable event model.
- Audit API.
- Audit search/filtering.
- Security audit events.
- Document/template/workflow/signature audit events.
- Correlation IDs.
- Actor information.
- Organization isolation.
- Audit retention metadata.
- Integration with Azure Service Bus.
- Basic React audit viewer.

---

# 3. Out of Scope

The following should not be implemented as part of Phase 9:

- Full enterprise SIEM implementation.
- Advanced analytics dashboards.
- AI-generated audit summaries.
- SMS provider implementation unless already available through an existing provider abstraction.
- WhatsApp integration.
- Complex marketing/campaign notifications.
- External customer notification portal.
- Legal compliance certification.
- Advanced notification orchestration designer.
- User-defined arbitrary code/actions.
- Direct database access between services.

These may be future phases.

---

# 4. Architecture

The target architecture is event-driven.

```text
                         ┌──────────────────────┐
                         │     React Web App     │
                         └──────────┬───────────┘
                                    │
                         ┌──────────▼───────────┐
                         │      API Gateway      │
                         └───────┬───────┬──────┘
                                 │       │
                    ┌────────────▼─┐   ┌▼─────────────┐
                    │ Notification │   │ Audit Service │
                    │    Service   │   │               │
                    └───────┬──────┘   └──────┬────────┘
                            │                   │
                            │                   │
                     ┌──────▼──────────────────▼──────┐
                     │        Azure Service Bus        │
                     │  Topics / Subscriptions / DLQ  │
                     └────────────────┬────────────────┘
                                      │
                 ┌────────────────────┼────────────────────┐
                 │                    │                    │
          ┌──────▼──────┐     ┌──────▼──────┐     ┌──────▼──────┐
          │  Workflow   │     │  Signature  │     │  Template / │
          │   Service   │     │   Service   │     │  Document    │
          └─────────────┘     └─────────────┘     └─────────────┘

Notification Providers:

Notification Service
       │
       ├── Email Provider
       │      └── SMTP / Azure Communication Services abstraction
       │
       ├── Teams Provider
       │      └── Microsoft Teams abstraction
       │
       └── In-App Notification Store

Audit Service
       │
       └── Audit Database
```

---

# 5. Design Principles

The implementation must follow these principles:

1. **Event-driven communication**
2. **No cross-service database access**
3. **Multi-tenant isolation**
4. **Immutable audit records**
5. **Idempotent event processing**
6. **At-least-once message processing**
7. **Retry with exponential backoff**
8. **Dead-letter failed messages**
9. **Correlation across distributed operations**
10. **Secure handling of notification and audit data**
11. **Provider abstraction**
12. **API versioning**
13. **Observability**
14. **Testability**
15. **Clean Architecture / SOLID**
16. **Production-ready error handling**

---

# 6. Solution Structure

Add the following projects to the existing solution.

```text
src/
├── Services/
│   ├── Notification/
│   │   ├── Edp.Notification.Api/
│   │   ├── Edp.Notification.Application/
│   │   ├── Edp.Notification.Domain/
│   │   ├── Edp.Notification.Infrastructure/
│   │   └── Edp.Notification.Worker/
│   │
│   └── Audit/
│       ├── Edp.Audit.Api/
│       ├── Edp.Audit.Application/
│       ├── Edp.Audit.Domain/
│       ├── Edp.Audit.Infrastructure/
│       └── Edp.Audit.Worker/
│
└── Shared/
    ├── Edp.Shared.Contracts/
    ├── Edp.Shared.Messaging/
    ├── Edp.Shared.Observability/
    └── Edp.Shared.Security/
```

Follow the existing repository naming conventions from previous phases.

---

# 7. Notification Service

## 7.1 Responsibilities

The Notification Service is responsible for:

- Receiving notification requests/events.
- Resolving recipients.
- Resolving notification templates.
- Rendering notification content.
- Selecting notification channels.
- Sending notifications.
- Tracking delivery status.
- Retrying failures.
- Handling permanent failures.
- Creating in-app notifications.
- Respecting user notification preferences.
- Publishing notification outcome events.

---

# 8. Notification Domain Model

## 8.1 Notification

```text
Notification
-------------
Id
OrganizationId
NotificationType
Subject
Body
Priority
CreatedAt
ScheduledAt
Status
CorrelationId
CausationId
SourceEventId
CreatedBy
```

### Status

```text
Pending
Processing
Sent
PartiallySent
Failed
Cancelled
```

---

# 9. Notification Recipient

```text
NotificationRecipient
---------------------
Id
NotificationId
UserId
Email
DisplayName
Channel
Status
AttemptCount
LastAttemptAt
DeliveredAt
FailureReason
```

### Channel

```text
Email
InApp
Teams
```

The design must allow future channels without changing the core notification domain model.

---

# 10. Notification Template

Notification templates must be managed independently from application code.

```text
NotificationTemplate
--------------------
Id
OrganizationId
Name
Code
Description
Channel
SubjectTemplate
BodyTemplate
Version
IsActive
CreatedAt
UpdatedAt
CreatedBy
UpdatedBy
```

Examples:

```text
WORKFLOW_SUBMITTED
WORKFLOW_APPROVAL_REQUIRED
WORKFLOW_APPROVED
WORKFLOW_REJECTED

SIGNATURE_REQUESTED
SIGNATURE_COMPLETED
SIGNATURE_DECLINED
SIGNATURE_EXPIRED

DOCUMENT_COMPLETED
DOCUMENT_CANCELLED

USER_INVITED
USER_ADDED_TO_ORGANIZATION
```

---

# 11. Template Rendering

Notification templates must support strongly defined variables.

Example:

```text
Hello {{RecipientName}},

Document {{DocumentName}} requires your approval.

Submitted by: {{SubmittedBy}}
Due Date: {{DueDate}}

Open Document:
{{DocumentUrl}}
```

Variables must be validated before rendering.

The service must not execute arbitrary code contained inside templates.

Recommended implementation:

```text
Template
   ↓
Parse
   ↓
Validate placeholders
   ↓
Build rendering context
   ↓
Render
   ↓
Validate output
   ↓
Send
```

---

# 12. Notification Event Contracts

Create shared integration events.

## 12.1 WorkflowSubmittedEvent

```csharp
public sealed record WorkflowSubmittedEvent(
    Guid EventId,
    Guid OrganizationId,
    Guid WorkflowInstanceId,
    Guid DocumentId,
    string DocumentName,
    Guid SubmittedBy,
    DateTimeOffset OccurredAt,
    string CorrelationId);
```

## 12.2 ApprovalRequiredEvent

```csharp
public sealed record ApprovalRequiredEvent(
    Guid EventId,
    Guid OrganizationId,
    Guid WorkflowInstanceId,
    Guid DocumentId,
    Guid ApproverUserId,
    DateTimeOffset DueAt,
    DateTimeOffset OccurredAt,
    string CorrelationId);
```

## 12.3 WorkflowCompletedEvent

```csharp
public sealed record WorkflowCompletedEvent(
    Guid EventId,
    Guid OrganizationId,
    Guid WorkflowInstanceId,
    Guid DocumentId,
    DateTimeOffset OccurredAt,
    string CorrelationId);
```

## 12.4 SignatureRequestedEvent

```csharp
public sealed record SignatureRequestedEvent(
    Guid EventId,
    Guid OrganizationId,
    Guid DocumentId,
    Guid SignatureRequestId,
    Guid SignerUserId,
    DateTimeOffset ExpiresAt,
    DateTimeOffset OccurredAt,
    string CorrelationId);
```

## 12.5 SignatureCompletedEvent

```csharp
public sealed record SignatureCompletedEvent(
    Guid EventId,
    Guid OrganizationId,
    Guid DocumentId,
    Guid SignatureRequestId,
    DateTimeOffset OccurredAt,
    string CorrelationId);
```

Additional events should follow the same contract pattern.

---

# 13. Notification Processing

Use Azure Service Bus.

Recommended topic:

```text
edp.domain-events
```

Subscriptions:

```text
notification-service
audit-service
```

Notification processing:

```text
Domain Event
     ↓
Service Bus
     ↓
Notification Subscription
     ↓
Notification Worker
     ↓
Resolve Notification Rules
     ↓
Resolve Recipients
     ↓
Check Preferences
     ↓
Render Template
     ↓
Send
     ↓
Persist Delivery Result
     ↓
Publish NotificationSent/Failed
```

---

# 14. Idempotency

Messages may be delivered more than once.

The Notification Service must therefore maintain an idempotency record.

```text
ProcessedMessage
----------------
MessageId
EventId
HandlerName
ProcessedAt
```

Before processing:

```text
if already processed:
    acknowledge message
    do not send notification again
```

Do not depend solely on Service Bus duplicate detection.

---

# 15. Notification Retry

Use exponential backoff.

Example:

```text
Attempt 1 → immediate
Attempt 2 → 30 seconds
Attempt 3 → 2 minutes
Attempt 4 → 10 minutes
Attempt 5 → 30 minutes
```

After the configured maximum attempts:

```text
Notification → Failed
Message → Dead Letter
```

Failure must include:

```text
Provider
ErrorCode
ErrorMessage
AttemptCount
Timestamp
CorrelationId
```

---

# 16. Email Provider

Create abstraction:

```csharp
public interface IEmailProvider
{
    Task<EmailSendResult> SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken);
}
```

Do not couple the domain/application layer directly to SMTP or a specific vendor.

Possible infrastructure implementations:

```text
SmtpEmailProvider
AzureCommunicationEmailProvider
```

Configuration should determine the active provider.

---

# 17. Teams Provider

Create:

```csharp
public interface ITeamsNotificationProvider
{
    Task<NotificationSendResult> SendAsync(
        TeamsNotification message,
        CancellationToken cancellationToken);
}
```

The implementation must be isolated behind the provider interface.

---

# 18. In-App Notifications

Users must be able to see notifications inside the React application.

API:

```http
GET /api/v1/notifications
GET /api/v1/notifications/unread-count
POST /api/v1/notifications/{id}/read
POST /api/v1/notifications/read-all
```

Example UI:

```text
🔔 Notifications

Approval required
Contract ABC requires your approval.

Signature requested
You have a document waiting for signature.

Workflow completed
Contract ABC has completed successfully.
```

---

# 19. Notification Preferences

Users must be able to control supported notification channels.

Example:

```text
Notification Preferences

Workflow
[x] Email
[x] In-App
[ ] Teams

Approval
[x] Email
[x] In-App
[x] Teams

Signature
[x] Email
[x] In-App
[ ] Teams
```

Preferences must never override mandatory security/system notifications unless the notification type explicitly permits opt-out.

---

# 20. Notification APIs

### Get notifications

```http
GET /api/v1/notifications
```

Supports:

```text
page
pageSize
status
unreadOnly
from
to
```

### Get notification

```http
GET /api/v1/notifications/{id}
```

### Mark read

```http
POST /api/v1/notifications/{id}/read
```

### Mark all read

```http
POST /api/v1/notifications/read-all
```

### Notification preferences

```http
GET /api/v1/notification-preferences
PUT /api/v1/notification-preferences
```

Administrative template APIs:

```http
GET    /api/v1/notification-templates
GET    /api/v1/notification-templates/{id}
POST   /api/v1/notification-templates
PUT    /api/v1/notification-templates/{id}
POST   /api/v1/notification-templates/{id}/activate
POST   /api/v1/notification-templates/{id}/deactivate
```

---

# 21. Audit Service

## 21.1 Responsibilities

Audit Service records:

- User actions.
- Authentication/security events.
- Organization changes.
- Template changes.
- Document changes.
- Workflow state transitions.
- Approval actions.
- Signature actions.
- Notification outcomes.
- Administrative actions.
- System failures where required.
- Integration events.

Audit records are append-only.

---

# 22. Audit Event Model

```text
AuditEvent
----------
Id
EventId
OrganizationId
ActorId
ActorType
Action
EntityType
EntityId
EntityName
Timestamp
CorrelationId
CausationId
SourceService
IpAddress
UserAgent
Outcome
Reason
Metadata
BeforeData
AfterData
```

### ActorType

```text
User
System
Service
Anonymous
```

### Outcome

```text
Success
Failure
Denied
```

---

# 23. Audit Actions

Standard action catalogue:

```text
Created
Updated
Deleted
Viewed
Downloaded
Uploaded
Activated
Deactivated

Submitted
Started
Paused
Resumed
Approved
Rejected
Cancelled
Completed

SignatureRequested
SignatureViewed
SignatureCompleted
SignatureDeclined
SignatureExpired

Login
Logout
LoginFailed
AccessDenied
RoleChanged
PermissionChanged

NotificationSent
NotificationFailed
```

The action catalogue must be extensible.

---

# 24. Audit Examples

### Document uploaded

```json
{
  "action": "Created",
  "entityType": "Document",
  "entityId": "document-id",
  "actorType": "User",
  "outcome": "Success"
}
```

### Workflow approved

```json
{
  "action": "Approved",
  "entityType": "WorkflowInstance",
  "entityId": "workflow-id",
  "actorType": "User",
  "outcome": "Success"
}
```

### Signature completed

```json
{
  "action": "SignatureCompleted",
  "entityType": "SignatureRequest",
  "entityId": "signature-id",
  "actorType": "User",
  "outcome": "Success"
}
```

---

# 25. Audit Immutability

Audit records must not expose normal update/delete APIs.

Allowed operations:

```text
Create
Read
Search
Export
```

Not allowed:

```text
Update
Delete
```

Corrections must be represented as new audit events rather than changing historical events.

---

# 26. Audit Storage

Use the platform's approved relational persistence strategy.

Recommended:

```text
Azure SQL
```

Suggested table:

```text
AuditEvents
```

Indexes:

```text
OrganizationId
Timestamp
ActorId
EntityType
EntityId
Action
CorrelationId
```

Composite indexes should support common queries:

```text
OrganizationId + Timestamp
OrganizationId + EntityType + EntityId
OrganizationId + ActorId + Timestamp
OrganizationId + CorrelationId
```

Audit metadata may use JSON storage where appropriate, but frequently queried properties should remain structured columns.

---

# 27. Audit API

### Search audit events

```http
GET /api/v1/audit-events
```

Filters:

```text
from
to
actorId
entityType
entityId
action
outcome
correlationId
page
pageSize
```

Example:

```http
GET /api/v1/audit-events?
    entityType=Document&
    entityId={documentId}&
    page=1&
    pageSize=50
```

### Get audit event

```http
GET /api/v1/audit-events/{id}
```

### Export

```http
POST /api/v1/audit-events/export
```

Export should be asynchronous for large datasets.

---

# 28. Audit Security

Audit APIs must enforce:

```text
Authenticated user
+
Organization membership
+
Audit permission
```

Example permissions:

```text
audit.read
audit.export
audit.admin
```

Users must never be able to access another organization's audit data.

---

# 29. Sensitive Data Rules

Never store:

```text
Passwords
Access tokens
Refresh tokens
Client secrets
Private keys
Authentication cookies
Complete payment card data
```

Sensitive document content should not automatically be copied into audit records.

Instead store references:

```text
DocumentId
DocumentVersionId
BlobVersionId
```

For before/after values, only store fields approved for audit.

---

# 30. Correlation and Distributed Tracing

Every command/event should carry:

```text
CorrelationId
CausationId
EventId
```

Example:

```text
User submits document
       ↓
CorrelationId = C123
       ↓
WorkflowStarted
       ↓
ApprovalRequired
       ↓
NotificationCreated
       ↓
SignatureRequested
       ↓
SignatureCompleted
       ↓
WorkflowCompleted
```

The same correlation ID must allow the complete business journey to be reconstructed.

---

# 31. Audit + Notification Event Flow

Example:

```text
Workflow Service
      │
      ├── WorkflowSubmitted
      │
      ▼
Azure Service Bus
      │
      ├──────────────────────┐
      ▼                      ▼
Audit Service          Notification Service
      │                      │
      ▼                      ▼
Audit Event             Notification
                            │
                     ┌──────┼──────┐
                     ▼      ▼      ▼
                   Email   InApp  Teams
```

This ensures that Audit and Notification remain independently deployable.

---

# 32. Transactional Outbox

Where a service creates a business record and publishes an event, use the platform's transactional outbox pattern where appropriate.

Example:

```text
Business Transaction
        │
        ├── Business Data
        │
        └── Outbox Event
                 ↓
             Dispatcher
                 ↓
          Azure Service Bus
```

This prevents:

```text
Database committed
BUT
event lost
```

The same pattern should be reusable by Workflow and Signature services.

---

# 33. Service Bus Topology

Recommended:

```text
Topic:
edp.domain-events
```

Subscriptions:

```text
notification-service
audit-service
```

Optional notification-specific topic:

```text
edp.notification-events
```

Dead-letter queues must be monitored.

Messages should include:

```text
MessageId
EventId
EventType
CorrelationId
CausationId
OccurredAt
OrganizationId
SchemaVersion
Payload
```

---

# 34. API Gateway Integration

Add routes:

```text
/api/v1/notifications/*
/api/v1/notification-preferences/*
/api/v1/notification-templates/*
/api/v1/audit-events/*
```

The Gateway must:

- Validate authentication.
- Propagate user identity.
- Propagate organization context.
- Propagate correlation ID.
- Apply rate limits where appropriate.
- Forward authorization context.

---

# 35. React UI

Add:

```text
src/features/notifications/
src/features/audit/
```

Notification UI:

```text
Notification Bell
Notification Drawer
Notification List
Notification Details
Notification Preferences
```

Audit UI:

```text
Audit Explorer
Audit Filters
Audit Details
Entity Timeline
```

Example document timeline:

```text
19 Sep 2026 15:30
Document Created

19 Sep 2026 15:42
Workflow Submitted

19 Sep 2026 15:45
Approval Completed

19 Sep 2026 16:02
Signature Requested

19 Sep 2026 16:15
Signature Completed

19 Sep 2026 16:16
Workflow Completed
```

---

# 36. Document Timeline

The Audit Service should support a document-centric timeline.

Query:

```text
GET /api/v1/audit-events?
entityType=Document&
entityId={documentId}
```

The UI should display chronological business events.

Do not create a second source of truth for document history.

The timeline must be derived from audit events.

---

# 37. Notification Rules

Introduce a simple rule model.

```text
NotificationRule
----------------
Id
OrganizationId
EventType
NotificationTemplateId
Channel
RecipientType
IsActive
```

Recipient types:

```text
Actor
DocumentOwner
WorkflowInitiator
CurrentApprover
Signer
OrganizationAdmin
ExplicitUser
```

The implementation must avoid arbitrary executable rules.

---

# 38. Authorization

Permissions:

```text
notification.read
notification.manage
notification.template.read
notification.template.manage
notification.preference.manage

audit.read
audit.export
audit.admin
```

Organization administrators may manage organization-level templates.

Regular users can manage their own notification preferences.

Audit administration must be restricted.

---

# 39. Multi-Tenancy

Every persisted Notification and Audit record must contain:

```text
OrganizationId
```

Every query must apply organization filtering.

Never trust `OrganizationId` supplied directly by the client.

Resolve organization context from the authenticated identity and authorization context.

---

# 40. Observability

Use the existing platform observability implementation.

Metrics:

### Notification

```text
notifications.created
notifications.sent
notifications.failed
notifications.retried
notifications.deadlettered
notification.delivery.duration
```

### Audit

```text
audit.events.created
audit.events.failed
audit.query.duration
audit.export.started
audit.export.completed
```

Logs must include:

```text
CorrelationId
OrganizationId
EventId
ServiceName
OperationName
```

Do not log:

```text
passwords
tokens
secrets
private document contents
```

---

# 41. Health Checks

Notification API:

```http
GET /health
GET /alive
```

Notification Worker must verify:

```text
Database
Service Bus
Provider configuration
```

Audit API:

```http
GET /health
GET /alive
```

Audit Worker must verify:

```text
Database
Service Bus
```

---

# 42. Configuration

Use configuration abstraction.

Example:

```json
{
  "ServiceBus": {
    "Connection": "",
    "DomainEventsTopic": "edp.domain-events"
  },
  "Notifications": {
    "DefaultRetryCount": 5,
    "EnableEmail": true,
    "EnableTeams": true,
    "EnableInApp": true
  },
  "Email": {
    "Provider": "AzureCommunicationServices"
  },
  "Audit": {
    "RetentionDays": 2555
  }
}
```

Secrets must never be committed to source control.

Use:

```text
Azure Key Vault
Managed Identity
```

for production secret retrieval.

---

# 43. Database Migration

Create migrations for:

### Notification

```text
Notifications
NotificationRecipients
NotificationTemplates
NotificationPreferences
NotificationDeliveries
ProcessedMessages
```

### Audit

```text
AuditEvents
AuditExportJobs
ProcessedMessages
```

Indexes must be explicitly defined.

All migrations must be idempotent and deployable through the existing CI/CD process.

---

# 44. API Versioning

Use the existing API versioning mechanism.

Example:

```text
/api/v1/notifications
/api/v1/audit-events
```

Future breaking changes should use:

```text
/api/v2/
```

---

# 45. Error Handling

Use the existing Problem Details implementation.

Example:

```json
{
  "type": "https://errors.enterprise-document-platform.com/notification/template-not-found",
  "title": "Notification template not found",
  "status": 404,
  "traceId": "..."
}
```

Do not expose provider secrets or internal exception details.

---

# 46. Testing Strategy

## Unit Tests

Notification:

```text
TemplateRenderingTests
RecipientResolutionTests
PreferenceTests
NotificationStatusTests
RetryPolicyTests
IdempotencyTests
```

Audit:

```text
AuditEventCreationTests
AuditAuthorizationTests
AuditFilteringTests
AuditImmutabilityTests
CorrelationTests
```

---

# 47. Integration Tests

Test:

```text
API → Database
API → Authorization
Worker → Service Bus
Worker → Database
Worker → Provider
Domain Event → Notification
Domain Event → Audit
```

---

# 48. Contract Tests

Validate shared events.

For every event verify:

```text
EventId
EventType
SchemaVersion
OrganizationId
CorrelationId
OccurredAt
Payload
```

Breaking contract changes must be detected automatically.

---

# 49. End-to-End Scenarios

## Scenario 1 — Workflow Approval

```text
User submits document
        ↓
Workflow enters Approval
        ↓
ApprovalRequired event
        ↓
Audit event created
        ↓
Notification generated
        ↓
Approver receives Email/In-App
        ↓
Approver approves
        ↓
Audit event created
        ↓
Next workflow step triggered
```

---

## Scenario 2 — Digital Signature

```text
Workflow reaches Signature
        ↓
SignatureRequested event
        ↓
Audit event
        ↓
Signer notification
        ↓
Signer completes signature
        ↓
SignatureCompleted event
        ↓
Audit event
        ↓
Completion notification
```

---

## Scenario 3 — Notification Failure

```text
Event received
     ↓
Email provider fails
     ↓
Retry
     ↓
Retry
     ↓
Maximum attempts reached
     ↓
Notification marked Failed
     ↓
Message dead-lettered
     ↓
Audit event generated
```

---

# 50. Audit Retention

Audit records must support retention metadata.

Minimum fields:

```text
CreatedAt
RetentionUntil
```

Retention policy should be configurable.

Example:

```text
Default: 7 years
```

The implementation must not physically delete records automatically unless an explicit retention job is introduced and approved.

Retention jobs must be auditable.

---

# 51. Export

Audit export must be asynchronous for large result sets.

Flow:

```text
POST /audit-events/export
        ↓
AuditExportJob
        ↓
Background Worker
        ↓
Generate CSV/JSON
        ↓
Store in Blob Storage
        ↓
Create AuditExportCompleted
        ↓
Notify requester
```

Export must respect the requester's authorization and organization boundary.

---

# 52. Security Requirements

Implement:

- Authentication.
- Authorization.
- Organization isolation.
- Secure provider credentials.
- Input validation.
- Output encoding.
- Rate limiting where required.
- Audit access logging.
- Secure export URLs.
- No sensitive data in logs.
- No secrets in configuration files.
- No client-controlled tenant switching.

---

# 53. Performance Requirements

Initial targets:

### Notification API

```text
P95 < 500 ms
```

for normal database-backed operations.

### Audit Query

```text
P95 < 1 second
```

for indexed queries over normal result sets.

### Event Processing

Worker should process messages asynchronously and support horizontal scaling.

The system must not block the Workflow or Signature service while waiting for email/Teams delivery.

---

# 54. Reliability Requirements

The system must tolerate:

```text
Service restart
Worker restart
Temporary Service Bus failure
Temporary provider failure
Duplicate events
Out-of-order events
Transient database failure
```

Processing must be restart-safe.

---

# 55. Deployment

Add containers:

```text
edp-notification-api
edp-notification-worker
edp-audit-api
edp-audit-worker
```

Deploy using the project's existing Azure infrastructure conventions.

Recommended runtime:

```text
Azure Container Apps
```

Use:

```text
Managed Identity
Azure Service Bus
Azure SQL
Azure Key Vault
Application Insights
Blob Storage
```

---

# 56. CI/CD

Pipeline must include:

```text
Restore
Build
Unit Tests
Integration Tests
Security Scan
Container Build
Container Push
Database Migration Validation
Deploy DEV
Smoke Tests
Deploy TST
Approval
Deploy UAT
Approval
Deploy PROD
```

Use the repository's existing Azure DevOps pipeline conventions.

---

# 57. Infrastructure

Add infrastructure definitions for:

```text
Notification Container App
Notification Worker Container App
Audit Container App
Audit Worker Container App

Service Bus subscriptions
Database objects
Managed identities
Key Vault permissions
Application Insights configuration
```

Infrastructure must be implemented using the existing Bicep strategy.

Do not hard-code environment-specific values.

---

# 58. Definition of Done

Phase 9 is complete only when:

- [ ] Notification API implemented.
- [ ] Notification Worker implemented.
- [ ] Audit API implemented.
- [ ] Audit Worker implemented.
- [ ] Notification templates implemented.
- [ ] Notification preferences implemented.
- [ ] Email provider abstraction implemented.
- [ ] Teams provider abstraction implemented.
- [ ] In-app notifications implemented.
- [ ] Notification retry implemented.
- [ ] Dead-letter handling implemented.
- [ ] Idempotency implemented.
- [ ] Audit event model implemented.
- [ ] Audit persistence implemented.
- [ ] Audit immutability enforced.
- [ ] Audit filtering implemented.
- [ ] Audit export implemented.
- [ ] Organization isolation implemented.
- [ ] Correlation IDs implemented.
- [ ] Workflow integration implemented.
- [ ] Digital Signature integration implemented.
- [ ] Service Bus subscriptions implemented.
- [ ] Gateway routes implemented.
- [ ] React notification center implemented.
- [ ] React audit viewer implemented.
- [ ] Unit tests implemented.
- [ ] Integration tests implemented.
- [ ] Contract tests implemented.
- [ ] E2E scenarios implemented.
- [ ] Health checks implemented.
- [ ] Metrics implemented.
- [ ] Logging implemented.
- [ ] Bicep infrastructure implemented.
- [ ] Docker images implemented.
- [ ] Azure DevOps pipeline updated.
- [ ] DEV deployment completed.
- [ ] Smoke tests passed.
- [ ] Documentation updated.

---

# 59. Expected Phase 9 Outcome

At the end of Phase 9, the platform must support the following complete business journey:

```text
Template
   ↓
Document
   ↓
Workflow
   ↓
Approval
   ↓
Digital Signature
   ↓
Completion
   ↓
Notifications
   ↓
Complete Audit Timeline
```

A user should be able to:

1. Submit a document.
2. Trigger a workflow.
3. Receive approval notifications.
4. Complete approval.
5. Trigger digital signature.
6. Notify the signer.
7. Complete the signature.
8. Receive completion notification.
9. Open the document timeline.
10. See the complete chronological audit history.

---

# 60. Implementation Order

Implement Phase 9 in the following order.

## Step 1 — Shared Contracts

Create:

```text
Domain events
Notification events
Audit events
Common metadata
Correlation models
```

## Step 2 — Audit Domain

Implement:

```text
AuditEvent
AuditAction
AuditOutcome
Audit repositories
```

## Step 3 — Audit Infrastructure

Implement:

```text
EF Core
Database
Migrations
Indexes
Service Bus consumer
```

## Step 4 — Audit Worker

Implement:

```text
Event consumer
Idempotency
Audit persistence
Retry
Dead-letter handling
```

## Step 5 — Audit API

Implement:

```text
Search
Get
Export
Authorization
```

## Step 6 — Notification Domain

Implement:

```text
Notification
Recipient
Template
Preference
Delivery
```

## Step 7 — Notification Infrastructure

Implement:

```text
Email provider
Teams provider
In-app persistence
Service Bus
```

## Step 8 — Notification Worker

Implement:

```text
Event consumption
Recipient resolution
Template rendering
Preference evaluation
Provider dispatch
Retry
Idempotency
```

## Step 9 — Notification API

Implement:

```text
Notifications
Preferences
Templates
```

## Step 10 — Workflow Integration

Connect:

```text
Workflow events
Approval events
Completion events
```

## Step 11 — Digital Signature Integration

Connect:

```text
SignatureRequested
SignatureCompleted
SignatureDeclined
SignatureExpired
```

## Step 12 — React UI

Implement:

```text
Notification Bell
Notification Center
Preferences
Audit Explorer
Document Timeline
```

## Step 13 — Observability

Implement:

```text
Metrics
Tracing
Structured logs
Health checks
```

## Step 14 — Infrastructure

Implement:

```text
Bicep
Container Apps
Service Bus
Managed Identity
Key Vault
```

## Step 15 — Testing

Execute:

```text
Unit
Integration
Contract
E2E
Performance
Failure/retry
Security
```

## Step 16 — CI/CD

Update Azure DevOps pipelines and deploy through:

```text
DEV → TST → UAT → PROD
```

---

# 61. Phase 9 Acceptance Criteria

The implementation is accepted when the following scenario succeeds without manual database intervention:

```text
1. Create document
2. Start workflow
3. Submit for approval
4. Approval notification generated
5. Notification delivered
6. Approval action performed
7. Signature request generated
8. Signature notification delivered
9. Signature completed
10. Completion notification delivered
11. Audit timeline contains all major events
12. CorrelationId connects the complete journey
13. Duplicate events do not create duplicate notifications
14. Provider failures are retried
15. Permanently failed messages reach DLQ
16. Tenant A cannot read Tenant B data
17. Audit records cannot be updated through the API
18. Audit export respects authorization
19. Worker restart does not lose events
20. Application metrics and traces are visible
```

---

# 62. Future Extension Points

The design should allow future phases to add:

```text
SMS
WhatsApp
Push Notifications
Slack
Advanced Teams integration
Webhook notifications
Scheduled notifications
Notification escalation
Reminder engine
SLA breach notifications
Advanced audit analytics
Compliance reporting
AI-assisted audit investigation
```

These should be added through interfaces and new providers rather than modifying the core domain unnecessarily.

---

# 63. Final Architecture Principle

Phase 9 must establish:

```text
Business Services
       │
       │ Domain Events
       ▼
Azure Service Bus
       │
       ├───────────────┐
       ▼               ▼
 Audit Service   Notification Service
       │               │
       ▼               ├── Email
   Audit Store         ├── In-App
                       └── Teams
```

The key architectural rule is:

> **Workflow and Digital Signature services publish facts. Audit records those facts. Notification reacts to those facts. Neither Audit nor Notification becomes a dependency that blocks the core business workflow.**

This keeps the Enterprise Document Automation Platform loosely coupled, observable, resilient, multi-tenant and extensible.
