# Phase 8 — Digital Signature & Signing Service

## Enterprise Document Automation Platform

**Project:** EnterpriseDocumentPlatform  
**Phase:** 8  
**Service:** Digital Signature Service  
**Primary Responsibility:** Manage the complete electronic-signature lifecycle for documents produced by the platform and integrate with the Phase 7 Workflow/State-machine Engine.

---

# 1. Purpose

Phase 8 introduces the **Digital Signature & Signing Service**.

The service is responsible for taking a document that has reached the workflow's signing stage, creating a signing request, identifying the required signers, tracking signing order and status, collecting signatures, validating the completed signing transaction, and returning the workflow to the appropriate next state.

The implementation must be a **real production-style service**, not a mocked `SignDocument()` API.

The service must support:

- Signing request creation.
- Multiple signers.
- Sequential and parallel signing.
- Signer roles.
- Signing order.
- Signature placement metadata.
- Signature status tracking.
- Signing invitations.
- Signing reminders.
- Expiration.
- Cancellation.
- Decline/rejection.
- Re-send invitation.
- Audit trail.
- Document version integrity.
- Completed signed-document storage.
- Integration with the Phase 7 Workflow Engine.
- Provider abstraction for future external e-signature providers.
- A local/demo signing provider for development and portfolio demonstration.

> **Important:** The service must be provider-independent. Do not tightly couple the domain model to DocuSign, Adobe Sign, or any single external provider.

---

# 2. Phase 8 Position in the Platform

Phase 8 builds on all previous phases.

```text
Phase 4
Core Platform
    |
    v
Phase 5
Template Service
    |
    v
Phase 6
Document Service
    |
    v
Phase 7
Workflow / State Machine
    |
    v
Phase 8
Digital Signature & Signing
    |
    v
Completed Document
```

The expected business flow is:

```text
Draft
  |
Review
  |
Approval
  |
Signature Requested
  |
+--------------------------+
|                          |
|   Signing Participants   |
|                          |
+--------------------------+
  |
All Required Signers Complete
  |
Signature Validation
  |
Completed
```

---

# 3. Business Objective

A business user should be able to:

1. Generate a document from a template.
2. Start a workflow.
3. Send the document through review and approval.
4. Request signatures.
5. Define who needs to sign.
6. Specify signing order.
7. Send signing invitations.
8. Track signer progress.
9. Allow each signer to sign or decline.
10. Complete the signing process.
11. Store the final signed document.
12. Maintain a complete audit history.
13. Automatically transition the workflow to `Completed`.

---

# 4. Scope

## 4.1 In Scope

### Signing Request

- Create signing request.
- Link signing request to workflow instance.
- Link signing request to document version.
- Define expiration.
- Define signing mode.
- Define signers.
- Define signing order.
- Activate signing request.
- Cancel signing request.

### Signers

- Individual signer.
- Multiple signers.
- Sequential signing.
- Parallel signing.
- Required signer.
- Optional signer.
- Signer role.
- Signer status.

### Signature Fields

Support at least:

- Signature.
- Initials.
- Date.
- Text.
- Checkbox.

Future providers may support additional field types.

### Signing Lifecycle

```text
Draft
Pending
InProgress
PartiallySigned
Completed
Declined
Expired
Cancelled
Failed
```

### Notifications

Signing service must publish events for:

- Signing request created.
- Invitation sent.
- Reminder required.
- Signer completed.
- Signer declined.
- Request expired.
- Request cancelled.
- Signing completed.

Actual email/SMS/Teams delivery can be handled by the Notification Service introduced in a later phase.

### Audit

Record:

- Who created the request.
- Who sent it.
- Who viewed it.
- Who signed.
- Who declined.
- When each action occurred.
- IP address where available.
- User agent where available.
- Document hash.
- Signing request ID.
- Correlation ID.

---

# 5. Out of Scope

Do not implement these as part of the core Phase 8 domain:

- Full legal e-signature compliance certification.
- Qualified electronic signatures.
- Certificate authority implementation.
- Building a custom PKI.
- Biometric authentication.
- Payment processing.
- Identity-document verification.
- Production integration with every signature provider.
- Advanced OCR.
- AI-based signature detection.

These can be future enhancements.

---

# 6. Architecture

Follow the existing EnterpriseDocumentPlatform architecture.

```text
React Web
    |
    v
Edp.Gateway
    |
    v
Digital Signature Service
    |
    +----------------------+
    |                      |
    v                      v
Azure SQL              Blob Storage
    |                      |
    v                      v
Signing Data          Signed Documents
    |
    v
Service Bus
    |
    +----------------------------+
    |                            |
    v                            v
Workflow Service          Notification Service
```

External provider integration:

```text
Digital Signature Service
        |
        v
ISignatureProvider
        |
        +---- LocalDemoSignatureProvider
        |
        +---- ExternalProviderAdapter
                    |
                    +---- DocuSign
                    +---- Adobe Acrobat Sign
                    +---- Other provider
```

---

# 7. Service Boundary

Recommended project structure:

```text
src/
  Services/
    DigitalSignature/
      Edp.DigitalSignature.Api/
      Edp.DigitalSignature.Application/
      Edp.DigitalSignature.Domain/
      Edp.DigitalSignature.Infrastructure/
      Edp.DigitalSignature.Contracts/
```

Tests:

```text
tests/
  Services/
    DigitalSignature/
      Edp.DigitalSignature.UnitTests/
      Edp.DigitalSignature.IntegrationTests/
      Edp.DigitalSignature.ContractTests/
```

Follow the naming, dependency, configuration, logging, exception handling, authentication, authorization, and observability conventions established in Phases 4–7.

---

# 8. Domain Model

## 8.1 SigningRequest

Represents a complete signing transaction.

Suggested properties:

```text
SigningRequestId
OrganizationId
WorkflowInstanceId
DocumentId
DocumentVersionId
Status
SigningMode
Title
Message
CreatedBy
CreatedAt
ActivatedAt
ExpiresAt
CompletedAt
CancelledAt
Provider
ProviderRequestId
DocumentHash
CorrelationId
RowVersion
```

---

# 9. SigningMode

Support:

```text
Sequential
Parallel
```

## Sequential

Example:

```text
Signer 1
   |
   v
Signer 2
   |
   v
Signer 3
```

Signer 2 cannot sign until signer 1 completes.

## Parallel

```text
        +--> Signer 1
        |
Request +--> Signer 2
        |
        +--> Signer 3
```

All required signers can sign independently.

---

# 10. Signer

Suggested entity:

```text
SignerId
SigningRequestId
UserId
Email
DisplayName
Role
SigningOrder
Status
IsRequired
InvitedAt
ViewedAt
SignedAt
DeclinedAt
DeclineReason
ProviderSignerId
CreatedAt
UpdatedAt
```

Statuses:

```text
Pending
Invited
Viewed
ReadyToSign
Signed
Declined
Expired
Cancelled
```

---

# 11. SignatureField

A signature field describes where and how a signer must interact with the document.

Suggested properties:

```text
SignatureFieldId
SigningRequestId
SignerId
DocumentPage
FieldType
X
Y
Width
Height
Required
Label
Value
CompletedAt
```

Field types:

```text
Signature
Initials
Date
Text
Checkbox
```

Coordinates should be stored independently of the rendering implementation.

---

# 12. Document Integrity

The service must protect document integrity.

When a signing request is created:

1. Resolve the immutable document version.
2. Calculate SHA-256 hash.
3. Store the hash.
4. Associate the hash with the signing request.

Example:

```text
DocumentVersion
       |
       v
SHA-256
       |
       v
SigningRequest.DocumentHash
```

At signing completion:

1. Calculate hash of the final signed document.
2. Store the final hash.
3. Preserve the original document hash.
4. Record both values in the audit trail.

---

# 13. Signature Provider Abstraction

Create:

```csharp
public interface ISignatureProvider
{
    Task<SignatureEnvelopeResult> CreateEnvelopeAsync(
        SignatureEnvelopeRequest request,
        CancellationToken cancellationToken);

    Task<SignatureStatusResult> GetStatusAsync(
        string providerRequestId,
        CancellationToken cancellationToken);

    Task<SignatureActionResult> SignAsync(
        string providerRequestId,
        string signerId,
        SignatureActionRequest request,
        CancellationToken cancellationToken);

    Task CancelAsync(
        string providerRequestId,
        CancellationToken cancellationToken);

    Task<byte[]> DownloadCompletedDocumentAsync(
        string providerRequestId,
        CancellationToken cancellationToken);
}
```

Do not expose provider-specific models outside Infrastructure.

---

# 14. Local Demo Provider

Implement:

```text
LocalDemoSignatureProvider
```

Purpose:

- Local development.
- Automated testing.
- Portfolio demonstration.
- End-to-end workflow demonstration.

The provider should simulate:

```text
Create Envelope
      |
      v
Invite Signer
      |
      v
Signer Opens
      |
      v
Signer Signs
      |
      v
Envelope Completed
```

The demo provider must still use the same `ISignatureProvider` abstraction used by future external providers.

---

# 15. Signing Request Lifecycle

```text
Draft
  |
  v
Pending
  |
  v
InProgress
  |
  +--------------------+
  |                    |
  v                    v
PartiallySigned      Declined
  |
  v
All Required Signers Signed
  |
  v
Completed
```

Alternative terminal states:

```text
Expired
Cancelled
Failed
```

---

# 16. State Transition Rules

Implement transitions explicitly.

| Current State | Action | Next State |
|---|---|---|
| Draft | Activate | Pending |
| Pending | First signer starts | InProgress |
| InProgress | One signer signs | PartiallySigned |
| PartiallySigned | Remaining signer signs | Completed |
| InProgress | Required signer declines | Declined |
| Pending | Expiry reached | Expired |
| InProgress | Expiry reached | Expired |
| Draft | Cancel | Cancelled |
| Pending | Cancel | Cancelled |
| InProgress | Cancel | Cancelled |

For parallel signing:

```text
All required signers signed
        |
        v
Completed
```

For sequential signing:

```text
Signer N completes
        |
        v
Activate next signer
        |
        v
All completed
        |
        v
Completed
```

---

# 17. Business Rules

## Rule 1 — Immutable Document Version

A signing request must reference a specific document version.

The source document cannot silently change after signing begins.

---

## Rule 2 — Required Signers

A signing request cannot reach `Completed` until every required signer has signed.

---

## Rule 3 — Optional Signers

Optional signers do not block completion.

---

## Rule 4 — Sequential Signing

Only the current signing-order participant can sign.

---

## Rule 5 — Parallel Signing

Any eligible pending signer may sign.

---

## Rule 6 — Decline

A required signer declining the request moves the request to:

```text
Declined
```

and publishes:

```text
SigningRequestDeclined
```

---

## Rule 7 — Expiration

Expired signing requests cannot be signed.

---

## Rule 8 — Cancellation

Cancelled requests cannot accept further signing actions.

---

## Rule 9 — Duplicate Signing

A signer who has already signed cannot sign again.

---

## Rule 10 — Tenant Isolation

Users must only access signing requests belonging to their organization.

---

# 18. API Design

Base route:

```text
/api/v1/signing-requests
```

## Create

```http
POST /api/v1/signing-requests
```

Request:

```json
{
  "workflowInstanceId": "guid",
  "documentId": "guid",
  "documentVersionId": "guid",
  "title": "Customer Agreement",
  "message": "Please review and sign.",
  "signingMode": "Sequential",
  "expiresAt": "2026-10-01T18:30:00Z",
  "signers": [
    {
      "email": "customer@example.com",
      "displayName": "Customer",
      "role": "Customer",
      "signingOrder": 1,
      "isRequired": true
    }
  ]
}
```

---

# 19. Activate Signing Request

```http
POST /api/v1/signing-requests/{id}/activate
```

Responsibilities:

- Validate request.
- Validate document version.
- Validate signers.
- Calculate document hash.
- Create provider envelope.
- Persist provider ID.
- Publish activation event.
- Trigger invitation workflow.

---

# 20. Get Signing Request

```http
GET /api/v1/signing-requests/{id}
```

Return:

```text
Signing request
Document
Workflow
Status
Expiration
Signers
Fields
Audit summary
```

---

# 21. List Signing Requests

```http
GET /api/v1/signing-requests
```

Support:

```text
status
documentId
workflowInstanceId
signer
createdFrom
createdTo
page
pageSize
sort
```

---

# 22. Sign

```http
POST /api/v1/signing-requests/{id}/sign
```

Request:

```json
{
  "signerId": "guid",
  "signature": {
    "type": "Drawn",
    "value": "signature-data"
  }
}
```

The API must:

1. Authenticate signer.
2. Validate signer access.
3. Validate signing request state.
4. Validate signing order.
5. Validate required fields.
6. Persist signature action.
7. Call provider.
8. Update signer status.
9. Evaluate signing completion.
10. Publish events.

---

# 23. Decline

```http
POST /api/v1/signing-requests/{id}/decline
```

Request:

```json
{
  "signerId": "guid",
  "reason": "Please update the commercial terms."
}
```

---

# 24. Cancel

```http
POST /api/v1/signing-requests/{id}/cancel
```

Only authorized users should be able to cancel a request.

---

# 25. Resend Invitation

```http
POST /api/v1/signing-requests/{id}/signers/{signerId}/resend
```

Apply throttling to prevent abuse.

---

# 26. Audit API

```http
GET /api/v1/signing-requests/{id}/audit
```

Return chronological events.

Example:

```text
RequestCreated
RequestActivated
InvitationSent
SignerViewed
SignerSigned
NextSignerActivated
SignerSigned
SigningCompleted
DocumentFinalized
WorkflowCompleted
```

---

# 27. Workflow Integration

Phase 8 must integrate directly with the Phase 7 Workflow Engine.

When workflow reaches:

```text
DigitalSignature
```

the workflow engine should create or request a signing transaction.

Example:

```text
Workflow Engine
      |
      | StartSigning
      v
Digital Signature Service
      |
      | SigningRequestCreated
      v
Workflow remains in Signing
```

When signing completes:

```text
Digital Signature Service
      |
      | SigningCompleted
      v
Service Bus
      |
      v
Workflow Engine
      |
      v
Next State
```

The Workflow Engine remains the owner of workflow state.

The Digital Signature Service owns signing state.

Do not duplicate workflow-state logic inside the signing service.

---

# 28. Integration Events

Publish events through Azure Service Bus.

Events:

```text
SigningRequestCreated
SigningRequestActivated
SigningInvitationSent
SignerViewedDocument
SignerSigned
SignerDeclined
SigningRequestExpired
SigningRequestCancelled
SigningRequestCompleted
SignedDocumentCreated
```

Event envelope:

```json
{
  "eventId": "guid",
  "eventType": "SigningRequestCompleted",
  "occurredAt": "2026-09-19T10:00:00Z",
  "organizationId": "guid",
  "correlationId": "guid",
  "causationId": "guid",
  "payload": {}
}
```

---

# 29. Idempotency

All externally triggered operations must support idempotency where applicable.

Example:

```text
SigningRequestCompleted
```

must not be processed twice.

Use:

```text
EventId
CorrelationId
ProviderRequestId
```

with unique constraints where appropriate.

---

# 30. Outbox Pattern

Use the transactional outbox pattern.

Transaction:

```text
Database Transaction
    |
    +-- Update SigningRequest
    |
    +-- Update Signer
    |
    +-- Insert OutboxEvent
```

Separate publisher:

```text
Outbox
  |
  v
Service Bus
```

This prevents:

```text
Database updated
BUT
event lost
```

---

# 31. Persistence

Use Azure SQL through EF Core.

Recommended tables:

```text
SigningRequests
SigningRequestSigners
SignatureFields
SignatureActions
SigningProviderTransactions
SigningAuditEntries
OutboxMessages
IdempotencyRecords
```

---

# 32. Database Constraints

Implement:

- Foreign keys.
- Unique constraints.
- Indexes.
- RowVersion concurrency.
- Organization-level filtering.
- Provider request ID uniqueness.
- Document version relationship.
- Signer ordering validation.

Recommended indexes:

```text
IX_SigningRequests_OrganizationId_Status
IX_SigningRequests_WorkflowInstanceId
IX_SigningRequests_DocumentVersionId
IX_SigningRequestSigners_SigningRequestId_Status
IX_SigningRequestSigners_Email
IX_SigningProviderTransactions_ProviderRequestId
IX_SigningAuditEntries_SigningRequestId_OccurredAt
```

---

# 33. Concurrency

Signing operations may occur simultaneously.

Example:

```text
Signer A signs
Signer B signs
```

at nearly the same time.

Use optimistic concurrency with `rowversion`.

The completion algorithm must be transaction-safe.

Do not allow:

```text
Signer A -> Completed
Signer B -> Completed
```

to create duplicate completion events.

Only one logical:

```text
SigningRequestCompleted
```

should be emitted.

---

# 34. Security

Follow the platform's Identity/Organization implementation.

Required:

- Entra ID authentication.
- Organization/tenant authorization.
- Role-based authorization.
- Resource ownership validation.
- Secure document access.
- No public Blob URLs.
- Short-lived signed download URLs where necessary.
- Encryption at rest.
- Secrets stored in Key Vault.
- No signature data in application logs.
- No document content in normal logs.

Recommended roles:

```text
OrganizationAdmin
TemplateManager
DocumentManager
WorkflowManager
Signer
Auditor
```

---

# 35. External Signer Access

Support a secure signing-session model for external users who do not have platform accounts.

Do not expose:

```text
/api/v1/signing-requests/{id}/sign
```

without authentication.

Instead use a short-lived signing session:

```text
Signing Request
      |
      v
Secure Signing Token
      |
      v
Signing Session
```

The token should:

- Be cryptographically random.
- Be short-lived.
- Be single-purpose.
- Be revocable.
- Never contain sensitive information directly.
- Be stored hashed when persistence is required.

---

# 36. Signed Document Storage

Completed documents must be stored in Blob Storage.

Suggested structure:

```text
organizations/{organizationId}/
documents/{documentId}/
versions/{versionId}/
signing/{signingRequestId}/
original/
signed/
audit/
```

Store:

```text
Original document
Signed document
Final hash
Provider metadata
Completion metadata
```

---

# 37. Document Versioning Rule

Never overwrite the original document.

Example:

```text
Document v1
    |
    +-- Original
    |
    +-- Signed Artifact
```

If the business requires another signing cycle:

```text
Document v2
    |
    +-- New Signing Request
```

Do not modify an already-completed signed artifact.

---

# 38. Notifications

Phase 8 should expose events for the future Notification Service.

Required notifications:

### Invitation

```text
You have been requested to sign a document.
```

### Reminder

```text
Your signature is still required.
```

### Completed

```text
All required signatures have been completed.
```

### Declined

```text
A signer has declined the document.
```

### Expired

```text
The signing request has expired.
```

Email/SMS/Teams delivery should be delegated to the platform Notification Service rather than embedded inside the signing domain.

---

# 39. Reminder Processing

Support:

```text
ReminderInterval
MaximumReminderCount
LastReminderAt
NextReminderAt
```

Example:

```text
Invitation
   |
   v
Wait 24 hours
   |
   v
Reminder 1
   |
   v
Wait 24 hours
   |
   v
Reminder 2
```

Use scheduled/background processing rather than a long-running API request.

---

# 40. Expiration Processing

A background worker must detect expired requests.

Example:

```text
Pending/InProgress
        |
        v
ExpiresAt reached
        |
        v
Expired
        |
        v
Publish SigningRequestExpired
```

The worker must be idempotent.

---

# 41. Observability

Use the platform's standard:

- Application Insights.
- Structured logging.
- Distributed tracing.
- Metrics.
- Correlation IDs.

Metrics:

```text
signing_requests_created_total
signing_requests_completed_total
signing_requests_declined_total
signing_requests_expired_total
signing_requests_cancelled_total
signing_requests_failed_total

signing_completion_duration
signing_provider_latency
signing_invitation_delivery_duration
```

Never log:

- Signature image/data.
- Authentication tokens.
- Signing tokens.
- Full document content.
- Sensitive personal data unnecessarily.

---

# 42. Health Checks

Implement:

```http
GET /health
GET /health/ready
GET /health/live
```

Readiness must validate:

- SQL connectivity.
- Blob Storage availability.
- Service Bus availability.
- Provider configuration.

---

# 43. API Error Model

Use the platform standard Problem Details response.

Example:

```json
{
  "type": "https://errors.enterprise-document-platform.com/signing/invalid-state",
  "title": "Invalid signing request state",
  "status": 409,
  "detail": "The signing request has already been completed.",
  "traceId": "..."
}
```

Important HTTP responses:

```text
400 Invalid Request
401 Unauthorized
403 Forbidden
404 Not Found
409 Conflict
422 Business Validation Error
429 Rate Limited
500 Internal Server Error
```

---

# 44. Validation

Validate:

### Signing Request

- Document exists.
- Document version exists.
- Document version is active/valid for signing.
- Workflow instance exists.
- User has organization access.
- At least one signer exists.
- At least one required signer exists.
- Expiry is in the future.

### Signer

- Email valid.
- Role provided.
- Signing order valid.
- No duplicate signer/order combination where prohibited.

### Signature Fields

- Page number valid.
- Coordinates valid.
- Dimensions valid.
- Required field belongs to a signer.

---

# 45. API Versioning

Use the same versioning strategy as the Gateway and existing services.

Recommended:

```text
/api/v1/signing-requests
```

OpenAPI documentation must be available.

Scalar should be integrated consistently with the existing platform configuration.

---

# 46. Configuration

Example:

```json
{
  "DigitalSignature": {
    "Provider": "LocalDemo",
    "DefaultExpirationDays": 7,
    "ReminderIntervalHours": 24,
    "MaximumReminders": 3,
    "SigningSessionLifetimeMinutes": 30
  }
}
```

Secrets:

```text
Azure Key Vault
```

Never commit provider credentials to source control.

---

# 47. Background Jobs

Implement background processing for:

```text
SigningExpirationWorker
SigningReminderWorker
ProviderStatusSynchronizationWorker
OutboxPublisher
```

Workers must be:

- Retryable.
- Idempotent.
- Observable.
- Cancellation-aware.

---

# 48. Retry Strategy

Use exponential backoff for transient provider failures.

Example:

```text
Attempt 1
  |
  +-- 2 seconds
Attempt 2
  |
  +-- 10 seconds
Attempt 3
  |
  +-- 30 seconds
Attempt 4
```

Do not retry:

- Invalid signer.
- Invalid signing request.
- Unauthorized request.
- Declined request.
- Expired request.

---

# 49. Resilience

Implement:

- Timeout.
- Retry.
- Circuit breaker where appropriate.
- Dead-letter handling.
- Provider failure isolation.

External provider failure must not corrupt the signing domain state.

---

# 50. Testing Strategy

## Unit Tests

Test:

- State transitions.
- Signing rules.
- Sequential signing.
- Parallel signing.
- Required/optional signers.
- Expiration.
- Cancellation.
- Decline.
- Duplicate signing.
- Document hash.
- Completion rules.

Minimum target:

```text
80%+ domain/application coverage
```

---

# 51. Integration Tests

Test:

```text
API
 |
 v
SQL
 |
 v
Blob
 |
 v
Service Bus
 |
 v
Workflow
```

Test:

- Create signing request.
- Activate.
- Sign.
- Complete.
- Decline.
- Expire.
- Cancel.
- Concurrent signing.

---

# 52. Contract Tests

Validate events:

```text
SigningRequestCreated
SignerSigned
SignerDeclined
SigningRequestCompleted
SigningRequestExpired
```

against shared event contracts.

---

# 53. End-to-End Test

The following scenario must pass:

```text
1. User creates document.
2. User starts workflow.
3. Workflow moves through Review.
4. Approver approves.
5. Workflow enters Signature stage.
6. Signing request is created.
7. Signer 1 receives invitation.
8. Signer 1 signs.
9. Signer 2 becomes active.
10. Signer 2 signs.
11. Signing request becomes Completed.
12. Signed document is stored.
13. SigningCompleted event is published.
14. Workflow receives event.
15. Workflow moves to Completed.
16. Audit trail contains every action.
```

---

# 54. UI Requirements

React UI must include:

## Signing Request List

Columns:

```text
Request
Document
Status
Signers
Created
Expires
Actions
```

## Signing Request Details

Display:

- Document.
- Workflow.
- Status.
- Expiry.
- Signers.
- Signing order.
- Progress.
- Audit.
- Actions.

## Signing Experience

Display:

```text
Document Viewer
       |
       v
Required Fields
       |
       v
Signature Input
       |
       v
Review
       |
       v
Sign
```

---

# 55. Signature UI

For demo provider support:

- Draw signature.
- Type signature.
- Clear signature.
- Apply signature.
- Confirm signing.

Example:

```text
+----------------------------------+
| Document                         |
|                                  |
| ...............................  |
|                                  |
|       [ Signature Field ]        |
|                                  |
+----------------------------------+

[Draw Signature]

[Clear]                 [Sign]
```

---

# 56. Audit Trail UI

Example:

```text
19 Sep 10:00  Request Created
19 Sep 10:01  Request Activated
19 Sep 10:02  Invitation Sent
19 Sep 10:10  Document Viewed
19 Sep 10:12  Signer Signed
19 Sep 10:13  Next Signer Activated
19 Sep 10:20  Signer Signed
19 Sep 10:21  Signing Completed
19 Sep 10:21  Document Finalized
```

---

# 57. Authorization Matrix

| Operation | Admin | Workflow Manager | Document Manager | Signer | Auditor |
|---|---:|---:|---:|---:|---:|
| Create Request | Yes | Yes | Yes | No | No |
| Activate | Yes | Yes | Yes | No | No |
| View Request | Yes | Yes | Yes | Own | Yes |
| Sign | No | No | No | Own | No |
| Decline | No | No | No | Own | No |
| Cancel | Yes | Yes | Yes | No | No |
| Resend | Yes | Yes | Yes | No | No |
| View Audit | Yes | Yes | Yes | Own | Yes |

---

# 58. Definition of Done

Phase 8 is complete only when:

- [ ] Digital Signature Service exists.
- [ ] Domain model implemented.
- [ ] EF Core persistence implemented.
- [ ] Signing request lifecycle implemented.
- [ ] Sequential signing implemented.
- [ ] Parallel signing implemented.
- [ ] Required/optional signers implemented.
- [ ] Signature fields implemented.
- [ ] Local demo provider implemented.
- [ ] Provider abstraction implemented.
- [ ] Secure signing session implemented.
- [ ] Document hashing implemented.
- [ ] Signed artifact storage implemented.
- [ ] Audit trail implemented.
- [ ] Outbox pattern implemented.
- [ ] Service Bus events implemented.
- [ ] Workflow integration implemented.
- [ ] Expiration worker implemented.
- [ ] Reminder worker implemented.
- [ ] Retry/resilience implemented.
- [ ] Authentication implemented.
- [ ] Authorization implemented.
- [ ] Tenant isolation implemented.
- [ ] API documentation implemented.
- [ ] Health checks implemented.
- [ ] Application Insights telemetry implemented.
- [ ] Unit tests implemented.
- [ ] Integration tests implemented.
- [ ] Contract tests implemented.
- [ ] End-to-end workflow test passes.
- [ ] React signing UI implemented.
- [ ] React audit UI implemented.
- [ ] Docker configuration implemented.
- [ ] CI pipeline updated.
- [ ] Infrastructure configuration updated.
- [ ] Documentation completed.

---

# 59. Recommended Implementation Tasks

## Task 1 — Service Bootstrap

Create:

```text
Edp.DigitalSignature.Api
Edp.DigitalSignature.Application
Edp.DigitalSignature.Domain
Edp.DigitalSignature.Infrastructure
Edp.DigitalSignature.Contracts
```

---

## Task 2 — Domain

Implement:

```text
SigningRequest
Signer
SignatureField
SignatureAction
SigningAuditEntry
```

and strongly typed enums/value objects.

---

## Task 3 — Persistence

Create EF Core configurations and migrations.

---

## Task 4 — Application Commands

Implement:

```text
CreateSigningRequest
ActivateSigningRequest
SignDocument
DeclineSigningRequest
CancelSigningRequest
ResendInvitation
```

---

## Task 5 — Queries

Implement:

```text
GetSigningRequest
ListSigningRequests
GetSigningAudit
GetSignerStatus
```

---

## Task 6 — Provider Abstraction

Implement:

```text
ISignatureProvider
LocalDemoSignatureProvider
```

---

## Task 7 — Signing Engine

Implement:

```text
Sequential signing
Parallel signing
Required signer evaluation
Optional signer evaluation
Completion detection
```

---

## Task 8 — Document Integrity

Implement:

```text
SHA-256 hashing
Original hash
Final hash
Signed artifact
```

---

## Task 9 — Service Bus

Implement:

```text
Outbox
Publisher
Event contracts
Consumers
```

---

## Task 10 — Workflow Integration

Implement:

```text
StartSigning
SigningCompleted
SigningDeclined
SigningExpired
```

integration with Phase 7.

---

## Task 11 — Background Workers

Implement:

```text
Expiration
Reminder
Provider synchronization
Outbox publishing
```

---

## Task 12 — Security

Implement:

```text
RBAC
Tenant isolation
External signer session
Token security
```

---

## Task 13 — React UI

Implement:

```text
Signing Requests
Signing Details
Signer Management
Signature Experience
Audit Timeline
```

---

## Task 14 — Tests

Implement:

```text
Unit
Integration
Contract
End-to-End
Concurrency
```

---

## Task 15 — Deployment

Update:

```text
Docker
Bicep
Azure SQL
Blob Storage
Service Bus
Key Vault
Application Insights
CI/CD
```

---

# 60. Acceptance Criteria

### AC-01 — Create

Given a valid document version and workflow instance, when a user creates a signing request, then the request is persisted with its signers and fields.

### AC-02 — Sequential

Given three sequential signers, when signer 1 signs, then signer 2 becomes eligible and signer 3 remains blocked.

### AC-03 — Parallel

Given three parallel signers, all eligible signers can sign independently.

### AC-04 — Completion

When every required signer has signed, the request becomes `Completed`.

### AC-05 — Decline

When a required signer declines, the request becomes `Declined`.

### AC-06 — Expiration

When the expiration time is reached, the request becomes `Expired` and cannot be signed.

### AC-07 — Cancellation

When an authorized user cancels the request, no further signing action is accepted.

### AC-08 — Integrity

The original document hash is preserved and the final signed document has its own hash.

### AC-09 — Workflow

When signing completes, the workflow engine receives `SigningRequestCompleted` and advances the workflow.

### AC-10 — Audit

Every material signing action is auditable.

### AC-11 — Tenant Isolation

A user from Organization A cannot access signing requests belonging to Organization B.

### AC-12 — Idempotency

Repeated delivery of the same completion event does not create duplicate workflow completion.

### AC-13 — Concurrency

Concurrent signing operations do not result in inconsistent signer or request states.

### AC-14 — Provider Independence

Replacing `LocalDemoSignatureProvider` with an external provider does not require domain changes.

---

# 61. Deliverables

At the end of Phase 8 the repository should contain:

```text
src/
  Services/
    DigitalSignature/
      Edp.DigitalSignature.Api/
      Edp.DigitalSignature.Application/
      Edp.DigitalSignature.Domain/
      Edp.DigitalSignature.Infrastructure/
      Edp.DigitalSignature.Contracts/

tests/
  Services/
    DigitalSignature/
      Edp.DigitalSignature.UnitTests/
      Edp.DigitalSignature.IntegrationTests/
      Edp.DigitalSignature.ContractTests/

docs/
  phase-8-digital-signature.md
```

Additionally:

```text
EF Core migrations
OpenAPI documentation
Docker configuration
Bicep updates
CI/CD updates
Event contracts
Architecture documentation
API examples
Test data
```

---

# 62. Phase 8 Final Architecture

```text
                         React Web
                            |
                            v
                       Edp.Gateway
                            |
            +---------------+---------------+
            |                               |
            v                               v
     Workflow Service              Digital Signature
            |                           Service
            |                               |
            |                    +----------+----------+
            |                    |                     |
            |                    v                     v
            |                Azure SQL            Blob Storage
            |                    |                     |
            |                    +----------+----------+
            |                               |
            |                               v
            |                         Signed Artifact
            |
            +------------- Service Bus <-------------+
                          |
             +------------+------------+
             |                         |
             v                         v
       Notification              Other Services
          Service
```

---

# 63. Phase 8 Architectural Principle

The most important architectural boundary is:

```text
Workflow Service
    owns:
    "What state is the business process in?"

Digital Signature Service
    owns:
    "What is the signing transaction's state?"

Document Service
    owns:
    "What document/version/artifact exists?"

Notification Service
    owns:
    "How is the user notified?"
```

Do not combine these responsibilities.

This separation is required so the platform remains extensible and can later support multiple signing providers, notification channels, and workflow types without rewriting the core domain.

---

# 64. Expected Result

After Phase 8, the Enterprise Document Automation Platform must support a complete executable business journey:

```text
Template
   |
   v
Document Generation
   |
   v
Workflow
   |
   v
Review
   |
   v
Approval
   |
   v
Digital Signature Request
   |
   +------> Signer 1
   |
   +------> Signer 2
   |
   +------> Signer N
   |
   v
Signed Document
   |
   v
Audit Trail
   |
   v
Workflow Completed
```

This creates the first complete end-to-end document lifecycle across Phases 5–8 and provides the foundation for subsequent integrations such as enterprise notifications, external signature providers, OCR, bulk processing, SharePoint integration, analytics, and AI-assisted document operations.
