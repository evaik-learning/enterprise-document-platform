# Phase 8 — Digital Signature & Signing Service Implementation Roadmap

**Date:** 2026-09-19  
**Status:** Requirements Review & Task Breakdown Complete  
**Target:** Production-ready Digital Signature Service following platform conventions

---

## Executive Summary

Phase 8 implements a **Digital Signature & Signing Service** that:

- ✅ Manages complete electronic-signature lifecycle for workflow-driven documents
- ✅ Integrates seamlessly with Phase 7 Workflow/State-Machine Engine
- ✅ Supports sequential/parallel multi-signer workflows with role-based signing order
- ✅ Provides provider abstraction (LocalDemoSignatureProvider + future external providers like DocuSign/Adobe)
- ✅ Persists signing state, audit trails, and signed documents in Azure SQL + Blob Storage
- ✅ Publishes integration events via Service Bus for Workflow/Notification/Audit services
- ✅ Enforces tenant isolation, concurrency safety, and idempotency patterns
- ✅ Follows all platform conventions: authentication, authorization, logging, observability, error handling

---

## Key Design Decisions

### 1. Architecture — Layered with Service-Per-Bounded-Context

```
Edp.DigitalSignature.Domain/           Domain model, aggregates, business rules
    ↓
Edp.DigitalSignature.Application/      Use cases, services, DTOs
    ↓
Edp.DigitalSignature.Infrastructure/   EF Core, repositories, providers, persistence
    ↓
Edp.DigitalSignature.Api/              Controllers, endpoints, middleware
    ↓
Edp.DigitalSignature.Contracts/        Integration events (published to Service Bus)
```

**Pattern:** No MediatR/CQRS. Simple `ISigningRequestService` in Application layer with `SigningRequestService` implementation. Repositories in Infrastructure. One DbContext per service. All following Workflow/Document/Template pattern.

### 2. Domain Model — Three Core Aggregates

```
SigningRequest (aggregate root)
    ├── Signers (child entities)
    ├── SignatureFields (child entities)
    ├── SignatureActions (audit log)
    └── [Domain Events emitted on state change]

SigningProviderTransaction (idempotency + audit)
    └── Links to provider request IDs for external provider tracking
```

**State Machine:**
```
Draft → Pending → InProgress → PartiallySigned → Completed
                                             ↓
                                       (Sequential: next signer OR Parallel: check all)
                       ↓ (any state before completion)
                      Declined / Expired / Cancelled
```

### 3. Signing Modes

- **Sequential:** Only current signer (by order) can sign. Advances to next on completion.
- **Parallel:** All pending signers can sign independently. Completes when all required signers done.

### 4. Provider Abstraction

```csharp
public interface ISignatureProvider
{
    Task<SignatureEnvelopeResult> CreateEnvelopeAsync(SignatureEnvelopeRequest, CancellationToken);
    Task<SignatureStatusResult> GetStatusAsync(string providerRequestId, CancellationToken);
    Task<SignatureActionResult> SignAsync(string providerRequestId, string signerId, SignatureActionRequest, CancellationToken);
    Task CancelAsync(string providerRequestId, CancellationToken);
    Task<byte[]> DownloadCompletedDocumentAsync(string providerRequestId, CancellationToken);
}
```

**Implementations:**
- `LocalDemoSignatureProvider` — in-memory/simulated signing (dev, testing, portfolio demo)
- Future: `DocuSignAdapter`, `AdobeSignAdapter` (Phase 8.5+)

### 5. Document Integrity

- On signing request creation: SHA-256 hash of immutable document version stored
- On completion: final signed document hash calculated and stored separately
- Original document never overwritten
- All hashes and versions recorded in audit trail

### 6. Persistence Pattern — Transactional Outbox

**Atomic Transaction:**
```
BEGIN TRANSACTION
  UPDATE SigningRequest SET Status = 'Completed'
  UPDATE Signer SET Status = 'Signed'
  INSERT OutboxMessage (EventType, Payload, ...)
COMMIT
```

**Separate Background Worker:**
```
Poll OutboxMessages every N seconds
  → For each unpublished message:
    → Publish to Service Bus
    → Mark as published
```

**Benefits:** Event loss impossible (guarantees delivery), idempotency via EventId/CorrelationId.

### 7. Tenant Isolation

- Every entity has `OrganizationId` (FK)
- DbContext has global query filter: `.HasQueryFilter(e => e.OrganizationId == currentOrgId)`
- All queries automatically scoped to current organization
- No API endpoint returns cross-tenant data

### 8. Concurrency Control

- All entities have `RowVersion` (SQL Server timestamp)
- Signing operations may race (two signers submitting simultaneously)
- Optimistic locking: `DbUpdateConcurrencyException` on version mismatch → retry with exponential backoff
- Completion logic is transaction-safe: only one `SigningRequestCompleted` event emitted even if multiple signers sign at same time

---

## Project Structure to Create

```
src/Services/DigitalSignature/
  ├── Edp.DigitalSignature.Domain/
  │   ├── Entities/
  │   │   ├── SigningRequest.cs           (aggregate root)
  │   │   ├── Signer.cs
  │   │   ├── SignatureField.cs
  │   │   ├── SignatureAction.cs
  │   │   └── SigningProviderTransaction.cs
  │   ├── Enums/
  │   │   ├── SigningRequestStatus.cs
  │   │   ├── SignerStatus.cs
  │   │   ├── SigningMode.cs
  │   │   ├── FieldType.cs
  │   │   └── SignatureActionType.cs
  │   ├── Exceptions/
  │   │   ├── SigningDomainException.cs   (base, inherits ProblemDetailsException)
  │   │   ├── SigningRequestNotFoundException.cs
  │   │   └── [other domain exceptions]
  │   ├── Events/
  │   │   └── DomainEvents.cs
  │   └── Edp.DigitalSignature.Domain.csproj
  │
  ├── Edp.DigitalSignature.Application/
  │   ├── Interfaces/
  │   │   ├── ISigningRequestService.cs
  │   │   ├── ISignatureProvider.cs
  │   │   ├── ISigningAuditService.cs
  │   │   └── [repositories]
  │   ├── Contracts/
  │   │   ├── CreateSigningRequestCommand.cs
  │   │   ├── SigningRequestDto.cs
  │   │   ├── SignerDto.cs
  │   │   ├── ListSigningRequestsFilter.cs
  │   │   └── [other DTOs]
  │   ├── Services/
  │   │   ├── SigningRequestService.cs    (implements ISigningRequestService)
  │   │   ├── ExpirationBackgroundWorker.cs
  │   │   └── ReminderBackgroundWorker.cs
  │   ├── DependencyInjection.cs
  │   └── Edp.DigitalSignature.Application.csproj
  │
  ├── Edp.DigitalSignature.Infrastructure/
  │   ├── Database/
  │   │   └── DigitalSignatureDbContext.cs
  │   ├── Persistence/
  │   │   ├── SigningRequestRepository.cs
  │   │   ├── SignerRepository.cs
  │   │   ├── SignatureFieldRepository.cs
  │   │   ├── SignatureActionRepository.cs
  │   │   └── SigningProviderTransactionRepository.cs
  │   ├── Migrations/
  │   │   └── [EF Core migration snapshots]
  │   ├── SignatureProviders/
  │   │   ├── LocalDemoSignatureProvider.cs
  │   │   └── ExternalProviderAdapter.cs (placeholder)
  │   ├── Storage/
  │   │   └── SignedDocumentStorage.cs    (Blob Storage integration)
  │   ├── Messaging/
  │   │   ├── OutboxBackgroundService.cs
  │   │   └── WorkflowIntegrationEventHandler.cs
  │   ├── DependencyInjection.cs
  │   └── Edp.DigitalSignature.Infrastructure.csproj
  │
  ├── Edp.DigitalSignature.Api/
  │   ├── Controllers/
  │   │   └── SigningRequestsController.cs
  │   ├── appsettings.json
  │   ├── appsettings.Development.json
  │   ├── appsettings.Production.json
  │   ├── Program.cs
  │   ├── DependencyInjection.cs
  │   └── Edp.DigitalSignature.Api.csproj
  │
  ├── Edp.DigitalSignature.Contracts/
  │   ├── Events/
  │   │   └── SigningEvents.cs            (integration event DTOs)
  │   └── Edp.DigitalSignature.Contracts.csproj
  │
  └── README.md

tests/
  ├── Edp.DigitalSignature.Tests/              (unit tests)
  ├── Edp.DigitalSignature.IntegrationTests/
  └── Edp.DigitalSignature.ContractTests/

docker/
  └── Dockerfile.digitalsignature.api          (multi-stage, mirrors other services)
```

---

## Key Workflows

### Workflow 1: Create and Activate Signing Request

```
Client (Workflow Service)
  │
  ├─→ POST /api/v1/signing-requests
  │       {
  │         workflowInstanceId, documentId, documentVersionId,
  │         title, message, signingMode, expiresAt,
  │         signers: [ { email, displayName, role, signingOrder, isRequired } ]
  │       }
  │
  └─→ Service
       1. Validate organization/tenant
       2. Fetch document version, calculate SHA-256 hash
       3. Create SigningRequest aggregate (status: Draft)
       4. Persist signers, signature fields
       5. Publish SigningRequestCreatedDomainEvent
       6. Return SigningRequestDto (201 Created)

POST /api/v1/signing-requests/{id}/activate
  │
  └─→ Service
       1. Validate state (Draft → Pending)
       2. Validate document hash unchanged
       3. Call _signatureProvider.CreateEnvelopeAsync()
       4. Store ProviderRequestId in SigningRequest
       5. Create SigningProviderTransaction (idempotency)
       6. Update status → Pending
       7. Within same transaction: INSERT OutboxEvent
       8. Publish SigningRequestActivatedDomainEvent
       9. Return 202 Accepted

Background: OutboxBackgroundService
  → Polls OutboxMessages every 10s
  → For each message: publish to Service Bus
  → Mark published
```

### Workflow 2: Sign Request (Sequential Mode)

```
Signer A
  │
  └─→ POST /api/v1/signing-requests/{id}/sign
       {
         signerId: "signer-a-id",
         signature: { type: "Drawn", value: "base64-signature-data" }
       }

Service
  1. Fetch signing request and signer
  2. Validate state (not Completed, Cancelled, Expired)
  3. Validate signing order (signer A is next in sequence)
  4. Validate signer hasn't already signed (duplicate check)
  5. Call _signatureProvider.SignAsync()
  6. Record SignatureAction (audit: IP, user agent, timestamp)
  7. Update Signer.Status → Signed, Signer.SignedAt = now
  8. Evaluate completion:
     - Sequential: Fetch next signer, activate (status: Invited, notify)
     - OR Parallel: Check all required signers signed?
  9. If all done:
     - Fetch final signed document from provider
     - Store to Blob Storage
     - Calculate new hash
     - Update SigningRequest.Status → Completed
     - Update SigningRequest.CompletedAt = now
     - Publish SigningRequestCompletedDomainEvent
  10. Within same transaction: INSERT OutboxEvent
  11. Publish SignerSignedDomainEvent
  12. Return 204 NoContent

Background: OutboxBackgroundService
  → Publishes SigningRequestCompletedEvent to Service Bus

Workflow Service (subscribes to topic "signing-events")
  → Receives SigningRequestCompletedEvent
  → Updates workflow state: Signing → Completed
  → Proceeds to next workflow state or ends
```

### Workflow 3: Expiration Processing

```
Background: ExpirationBackgroundWorker
  (runs every 30 seconds)
  
  1. Query: SigningRequests WHERE Status IN (Pending, InProgress)
            AND ExpiresAt < now
  2. For each expired request:
     a. Update SigningRequest.Status → Expired
     b. Update all Signer.Status → Expired (if still Invited/Pending)
     c. Call _signatureProvider.CancelAsync() if applicable
     d. Publish SigningRequestExpiredDomainEvent
     e. Within transaction: INSERT OutboxEvent
  3. Idempotent: track last run, skip already-processed
  
Background: OutboxBackgroundService
  → Publishes SigningRequestExpiredEvent to Service Bus
  
Workflow Service (subscribes)
  → Receives event
  → Updates workflow: Signing → Failed / Expired state
```

---

## API Endpoints Summary

All endpoints require `[Authorize]`; use Entra ID + platform JWT bearer tokens.

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/api/v1/signing-requests` | Create signing request | SigningManager |
| POST | `/api/v1/signing-requests/{id}/activate` | Activate request → Pending | SigningManager |
| GET | `/api/v1/signing-requests/{id}` | Get signing request details | User |
| GET | `/api/v1/signing-requests` | List requests (paginated, filterable) | User |
| POST | `/api/v1/signing-requests/{id}/sign` | Sign document | Signer |
| POST | `/api/v1/signing-requests/{id}/decline` | Decline signing | Signer |
| POST | `/api/v1/signing-requests/{id}/cancel` | Cancel request | SigningManager |
| POST | `/api/v1/signing-requests/{id}/signers/{signerId}/resend` | Resend invitation | SigningManager |
| GET | `/api/v1/signing-requests/{id}/audit` | Get audit trail | Auditor |

---

## Database Schema Overview

**Tables:**
- `SigningRequests` — Main aggregate
- `SigningRequestSigners` — Signers (one per signer per request)
- `SignatureFields` — Signature field definitions
- `SignatureActions` — Audit log (viewed, signed, declined, etc.)
- `SigningProviderTransactions` — Track calls to external provider (idempotency)
- `OutboxMessages` — Transactional outbox (domain events to publish)
- `InboxMessages` — Inbox for consumed events (idempotency for subscribers)

**Key Indexes (for performance):**
```sql
IX_SigningRequests_OrganizationId_Status
IX_SigningRequests_WorkflowInstanceId
IX_SigningRequests_DocumentVersionId
IX_SigningRequestSigners_SigningRequestId_Status
IX_SigningRequestSigners_Email
IX_SigningProviderTransactions_ProviderRequestId
IX_SigningAuditEntries_SigningRequestId_OccurredAt
```

**Concurrency:**
- All entities use `RowVersion` (SQL Server timestamp) for optimistic locking
- Global query filter on `OrganizationId` ensures tenant isolation

---

## Integration Events (Published to Service Bus)

```
Topic: "signing-events"
Subscription: "workflow" → WorkflowService consumes
Subscription: "notification" → NotificationService consumes (Phase 9)
Subscription: "audit" → AuditService consumes

Events:
  1. SigningRequestCreated
  2. SigningRequestActivated
  3. SigningInvitationSent
  4. SignerViewedDocument
  5. SignerSigned
  6. SignerDeclined
  7. SigningRequestExpired
  8. SigningRequestCancelled
  9. SigningRequestCompleted
  10. SignedDocumentCreated

Envelope:
{
  "eventId": "guid",
  "eventType": "SigningRequestCompleted",
  "occurredAt": "2026-09-19T10:00:00Z",
  "organizationId": "guid",
  "correlationId": "guid",
  "causationId": "guid",
  "payload": { ... }
}
```

---

## Security & Authorization

### Authentication
- Entra ID + platform JWT bearer token (from gateway)
- All endpoints require `[Authorize]`

### Authorization (Recommended Roles)
```csharp
// Granular policies (not yet implemented platform-wide, but recommended):
[Authorize(Policy = "SigningManager")]     // Create, activate, cancel signing requests
[Authorize(Policy = "Signer")]             // Sign, decline, view own requests
[Authorize(Policy = "Auditor")]            // Access audit trail
[Authorize(Policy = "OrganizationAdmin")]  // Override any action
```

### Data Protection
- No signature image data in logs
- No authentication tokens in logs
- Signing tokens are short-lived, single-purpose, revocable
- Blob Storage URLs are signed/short-lived (not public)
- Secrets (provider credentials) stored in Key Vault
- Document content not logged

### External Signer Access
- Do NOT expose `/sign` endpoint without authentication
- Instead: create short-lived signing session token
  - Token: cryptographically random, short TTL (1 hour)
  - Token: stored hashed in DB
  - Token: revocable
  - Signing session: must include signing request ID, signer ID, token

---

## Observability

### Logging (Serilog)
- Structured logs with correlation ID
- No sensitive data (signature, tokens, full docs)
- Log levels: Info (key events), Debug (flow), Warn (unusual), Error (failures)

### Application Insights Metrics
```
signing_requests_created_total        (counter)
signing_requests_completed_total      (counter)
signing_requests_declined_total       (counter)
signing_requests_expired_total        (counter)
signing_requests_cancelled_total      (counter)
signing_requests_failed_total         (counter)

signing_completion_duration           (histogram)
signing_provider_latency              (histogram)
signing_invitation_delivery_duration  (histogram)
```

### Health Checks
```
GET /health          → Overall status
GET /health/live     → Liveness (basic checks)
GET /health/ready    → Readiness (SQL, Blob, Service Bus, Provider config)
```

---

## Implementation Phases (Step-by-Step)

### Phase 1: Project Setup
- Create 5 project files (.csproj)
- Add to solution
- Configure project dependencies (references)
- Verify build

**Time: ~30 minutes**

### Phase 2: Domain Layer (Business Rules)
- Define entities: SigningRequest, Signer, SignatureField, SignatureAction, SigningProviderTransaction
- Define enums: Status, Modes, Field Types
- Define exceptions (inherit ProblemDetailsException)
- Define domain events
- Implement state transitions + business rules
- Implement aggregate factory methods

**Time: ~2-3 hours**

### Phase 3: Application Layer (Use Cases)
- Define service interfaces
- Define DTOs/commands/filters
- Implement SigningRequestService (main business logic)
- Implement background workers (expiration, reminders)
- Configure dependency injection

**Time: ~2-3 hours**

### Phase 4: Infrastructure Layer (Persistence)
- Create DbContext
- Configure entity mappings, indexes, constraints, concurrency
- Implement repositories (5 repos)
- Create EF Core migration
- Implement LocalDemoSignatureProvider
- Implement SignedDocumentStorage
- Configure dependency injection

**Time: ~3-4 hours**

### Phase 5: API Layer (Endpoints)
- Implement SigningRequestsController (8 endpoints)
- Configure health checks
- Configure Program.cs (DI, logging, auth, OpenAPI)
- Configure appsettings.json (all environments)

**Time: ~2 hours**

### Phase 6: Service Bus & Integration
- Define integration events (Contracts project)
- Implement OutboxBackgroundService
- Implement WorkflowIntegrationEventHandler
- Wire up Workflow→Signing integration

**Time: ~2 hours**

### Phase 7: Testing
- Unit tests (domain logic, state transitions)
- Integration tests (database, repositories, provider)
- API contract tests (endpoints, auth, error codes)

**Time: ~3-4 hours**

### Phase 8: Documentation & Deployment
- Write README
- Update docker-compose
- Create Dockerfile
- Add health checks
- Verify end-to-end (Template→Document→Workflow→Signing→Workflow completion)

**Time: ~1-2 hours**

**Total estimated time: 16-22 hours** (including testing)

---

## Commonly Overlooked Details

1. **Idempotency:** SigningProviderTransaction table tracks provider request IDs with unique constraint
2. **Tenant Isolation:** Global query filter on OrganizationId prevents cross-tenant data leaks
3. **Concurrency:** RowVersion + optimistic locking handles simultaneous signing
4. **Outbox Pattern:** Events inserted in same DB transaction; separate worker publishes → Service Bus
5. **Document Hash:** SHA-256 of original + final signed document stored; enables integrity validation
6. **State Transitions:** Explicit validation rules (e.g., can't sign an expired request)
7. **Sequential vs Parallel:** Different completion logic depending on SigningMode
8. **External Signers:** Require secure short-lived signing session tokens, not direct API exposure
9. **Provider Abstraction:** Domain should NOT reference specific providers (DocuSign, Adobe); use ISignatureProvider
10. **Error Codes:** Domain exceptions must inherit ProblemDetailsException with proper HTTP status codes (404, 409, 403, etc.)

---

## Next Steps

1. **Start with Phase 1 (Project Setup):** Create the 5 .csproj files and add to solution
2. **Phase 2 (Domain Layer):** Build the aggregate, enums, exceptions, and business rules
3. **Iteratively test each layer** as you build (unit → integration → API)
4. **Integrate with Workflow Service** once signing logic is solid
5. **End-to-end test:** Document creation → Workflow → Signing → Completion

---

## References

- Phase-8 Requirements: `docs/Phase-8-Digital_Signature_Service_Requirement.md`
- Workflow Service (example): `src/Services/Workflow/`
- Document Service (example): `src/Services/Document/`
- Shared Infrastructure: `src/Shared/`
- Detailed task breakdown: See session notes (Phase-8-DigitalSignature-TaskList.md)

