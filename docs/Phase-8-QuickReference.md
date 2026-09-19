# Phase 8 Quick Reference — Digital Signature Service

## What You Need to Build

A **production-grade Digital Signature Service** that:
- ✅ Manages the complete signing lifecycle (request → sign → complete)
- ✅ Integrates with Phase 7 Workflow engine
- ✅ Supports sequential and parallel multi-signer workflows
- ✅ Provides abstraction for signature providers (local demo + future external)
- ✅ Stores signed documents + full audit trail
- ✅ Publishes events to notify Workflow/Notification services
- ✅ Enforces tenant isolation, concurrency safety, idempotency

---

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Gateway (authenticated requests)          │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ↓
         ┌──────────────────────────────────┐
         │  Edp.DigitalSignature.Api        │
         │  (Controllers, error handling)   │
         └────────────────┬─────────────────┘
                          │
                ┌─────────┴──────────┐
                ↓                    ↓
     ┌──────────────────────┐  ┌────────────────────┐
     │Application Layer     │  │Infrastructure Layer│
     │(Services, DTOs)      │  │(DbContext, Repos)  │
     └─────────┬────────────┘  └────────┬───────────┘
               │                        │
               └────────────┬───────────┘
                            ↓
                   ┌────────────────────┐
                   │   Domain Layer     │
                   │ (Aggregates, Rules)│
                   └──────┬─────────────┘
                          │
        ┌─────────────────┼─────────────────┐
        ↓                 ↓                 ↓
    ┌────────┐      ┌─────────┐      ┌──────────────┐
    │  SQL   │      │  Blob   │      │ Service Bus  │
    │ Server │      │ Storage │      │   (Events)   │
    └────────┘      └─────────┘      └──────────────┘
```

---

## Project Files to Create

```
5 main projects:
  1. Edp.DigitalSignature.Domain       (business rules, aggregates)
  2. Edp.DigitalSignature.Application  (services, use cases)
  3. Edp.DigitalSignature.Infrastructure (database, providers)
  4. Edp.DigitalSignature.Api          (controllers, endpoints)
  5. Edp.DigitalSignature.Contracts    (integration events)

3 test projects:
  1. Edp.DigitalSignature.Tests        (unit tests)
  2. Edp.DigitalSignature.IntegrationTests
  3. Edp.DigitalSignature.ContractTests
```

---

## Domain Model (Simplified)

```csharp
// Main aggregate
public class SigningRequest : AuditableEntity<Guid>
{
    public Guid OrganizationId { get; set; }           // Tenant isolation
    public Guid WorkflowInstanceId { get; set; }       // Link to workflow
    public Guid DocumentId { get; set; }
    public Guid DocumentVersionId { get; set; }        // Immutable
    public SigningRequestStatus Status { get; set; }   // Draft, Pending, InProgress, Completed, Declined, Expired, Cancelled
    public SigningMode SigningMode { get; set; }       // Sequential or Parallel
    public DateTime? ExpiresAt { get; set; }
    public string DocumentHash { get; set; }           // SHA-256 for integrity
    public string ProviderRequestId { get; set; }      // Idempotency
    public byte[] RowVersion { get; set; }             // Concurrency token
    
    // Children
    public ICollection<Signer> Signers { get; set; }
    public ICollection<SignatureField> Fields { get; set; }
}

// Child entity
public class Signer
{
    public Guid SignerId { get; set; }
    public Guid SigningRequestId { get; set; }
    public string Email { get; set; }
    public string DisplayName { get; set; }
    public SignerStatus Status { get; set; }           // Pending, Invited, Viewed, Signed, Declined, Expired, Cancelled
    public int SigningOrder { get; set; }              // For sequential mode
    public bool IsRequired { get; set; }
    public DateTime? SignedAt { get; set; }
    public DateTime? DeclinedAt { get; set; }
    public byte[] RowVersion { get; set; }
}

// State enumeration
public enum SigningRequestStatus
{
    Draft, Pending, InProgress, PartiallySigned, Completed, Declined, Expired, Cancelled, Failed
}

public enum SigningMode
{
    Sequential,  // Signer 1 → Signer 2 → Signer 3 (one at a time)
    Parallel     // All signers can sign simultaneously
}
```

---

## Key Business Rules

| Rule | Description | Implementation |
|------|-------------|-----------------|
| Immutable Document | Document version cannot change after signing begins | SHA-256 hash stored, validated throughout |
| Required Signers | Request cannot complete until ALL required signers sign | Aggregate validates before transitioning to Completed |
| Sequential Order | Only current signer in order can sign | Fetch next signer by order, validate before allowing sign |
| Parallel Eligibility | Any eligible signer can sign simultaneously | No order validation needed; check all required signed |
| No Duplicate Sign | A signer cannot sign twice | Validate signer.Status ≠ Signed before accepting |
| Expiration Blocks | Expired requests cannot be signed | Check ExpiresAt < now; transition to Expired |
| Tenant Isolation | Users see only their org's data | Global query filter on OrganizationId |
| Concurrency Safety | Two signers signing simultaneously don't create duplicate completion events | Use RowVersion + optimistic locking; retry on conflict |

---

## State Transitions (Visual)

```
       ┌─ CREATE ─┐
       │          ↓
      [Draft]
       │
       │ Activate
       ↓
      [Pending]
       │
       │ First signer starts
       ↓
    [InProgress]
       │
       ├─ One signer completes (Sequential: advance to next)
       │ OR check if all required signed (Parallel)
       ↓
  [PartiallySigned]
       │
       │ All required signers complete
       ↓
    [Completed] ←─ FINAL STATE ─→ Store signed doc + events

Alternative terminal states:
  [InProgress] → [Expired]     (timer reached)
  [Pending]    → [Expired]     (timer reached)
  [InProgress] → [Declined]    (required signer declined)
  [Any]        → [Cancelled]   (user action)
  [Any]        → [Failed]      (provider error)
```

---

## Main Service Operations

```csharp
public interface ISigningRequestService
{
    // Create request (status: Draft)
    Task<SigningRequestDto> CreateSigningRequestAsync(CreateSigningRequestCommand cmd, CancellationToken ct);
    
    // Activate request (Draft → Pending, call provider, emit event)
    Task ActivateSigningRequestAsync(Guid id, CancellationToken ct);
    
    // Signer signs document
    Task SignAsync(Guid requestId, Guid signerId, SignRequest sig, CancellationToken ct);
    
    // Signer declines request
    Task DeclineAsync(Guid requestId, Guid signerId, string reason, CancellationToken ct);
    
    // Manager cancels request
    Task CancelAsync(Guid requestId, CancellationToken ct);
    
    // Resend invitation to signer
    Task ResendInvitationAsync(Guid requestId, Guid signerId, CancellationToken ct);
    
    // Get full request details
    Task<SigningRequestDetailDto> GetSigningRequestAsync(Guid id, CancellationToken ct);
    
    // List with filters & pagination
    Task<PagedResult<SigningRequestDto>> ListSigningRequestsAsync(ListSigningRequestsFilter filter, CancellationToken ct);
    
    // Get audit trail
    Task<List<AuditEntryDto>> GetAuditTrailAsync(Guid requestId, CancellationToken ct);
    
    // Background: process expired requests
    Task ProcessExpiredRequestsAsync(CancellationToken ct);
    
    // Background: send reminders
    Task ProcessRemindersAsync(CancellationToken ct);
}
```

---

## Data Flow: Create & Sign

```
SCENARIO: Sequential signing (2 signers)

1. CLIENT (Workflow Service)
   POST /api/v1/signing-requests
   {
     workflowInstanceId: "workflow-123",
     documentId: "doc-456",
     documentVersionId: "v1",
     signingMode: "Sequential",
     signers: [
       { email: "alice@example.com", signingOrder: 1, isRequired: true },
       { email: "bob@example.com", signingOrder: 2, isRequired: true }
     ]
   }

2. SIGNING SERVICE (Application Layer)
   a. Fetch document version, calculate SHA-256 hash
   b. Create SigningRequest (status: Draft)
   c. Add 2 Signer entities (order 1, order 2)
   d. Emit SigningRequestCreatedDomainEvent
   e. Save to DB (includes OutboxMessage)
   f. Return DTO

3. CLIENT
   POST /api/v1/signing-requests/{id}/activate

4. SIGNING SERVICE
   a. Validate state (Draft → Pending)
   b. Call _signatureProvider.CreateEnvelopeAsync() 
   c. Store ProviderRequestId
   d. Update status → Pending
   e. Emit SigningRequestActivatedDomainEvent
   f. Save to DB (includes OutboxMessage)
   g. Return 202 Accepted

5. BACKGROUND WORKER (OutboxBackgroundService, runs every 10s)
   a. Poll OutboxMessages table
   b. Serialize: SigningRequestActivatedDomainEvent → SigningRequestActivatedEvent
   c. Publish to Service Bus (topic: "signing-events", subscription: "workflow")
   d. Mark OutboxMessage as published

6. WORKFLOW SERVICE (subscribes to "signing-events" topic)
   a. Receive SigningRequestActivatedEvent
   b. Remain in "DigitalSignature" state (waiting for completion)

7. ALICE (Signer #1) signs via web UI:
   POST /api/v1/signing-requests/{id}/sign
   {
     signerId: "alice-signer-id",
     signature: { type: "Drawn", value: "base64-signature-data" }
   }

8. SIGNING SERVICE
   a. Fetch request & signer (Alice)
   b. Validate: signer is next in order, status ≠ Signed, request not expired
   c. Call _signatureProvider.SignAsync()
   d. Record SignatureAction (audit)
   e. Update Signer: status → Signed, signedAt → now
   f. Evaluate completion:
      - Sequential: fetch Signer #2, update status → Invited (notify)
   g. NOT complete yet (Bob hasn't signed)
   h. Emit SignerSignedDomainEvent
   i. Save to DB (includes OutboxMessage)
   j. Return 204 NoContent

9. BOB (Signer #2) signs:
   POST /api/v1/signing-requests/{id}/sign
   {
     signerId: "bob-signer-id",
     signature: { type: "Drawn", value: "..." }
   }

10. SIGNING SERVICE
    a. Fetch request & signer (Bob)
    b. Validate: signer is next in order, status ≠ Signed, not expired
    c. Call _signatureProvider.SignAsync()
    d. Record SignatureAction
    e. Update Signer: status → Signed
    f. Evaluate completion:
       - Sequential: check if any more signers → NO
       - Both required signers signed → ALL DONE
    g. Fetch final signed document from provider
    h. Store to Blob Storage: /organizations/{orgId}/documents/{docId}/versions/{verId}/signing/{sigReqId}/signed/document.pdf
    i. Calculate new hash (final signed document)
    j. Update SigningRequest: status → Completed, completedAt → now
    k. Emit SigningRequestCompletedDomainEvent
    l. Save to DB (includes OutboxMessage)
    m. Return 204 NoContent

11. BACKGROUND WORKER
    a. Publish SigningRequestCompletedEvent to Service Bus
    b. Emit SignerSignedEvent for Bob

12. WORKFLOW SERVICE (subscribes)
    a. Receive SigningRequestCompletedEvent
    b. Transition workflow: "DigitalSignature" → "Completed" (or next state)
    c. Update workflow instance status
    d. Continue workflow...

13. AUDIT SERVICE (also subscribes)
    a. Receive events
    b. Log audit trail: request created, activated, alice signed, bob signed, completed
```

---

## Concurrency Handling

**Problem:** Two signers submit signatures within milliseconds of each other.

**Solution:** Optimistic locking with RowVersion

```csharp
// Signing Service when completing:
try
{
    request.RowVersion = rowVersion;  // Track current version
    request.Status = SigningRequestStatus.Completed;
    _repo.Update(request);
    await _unitOfWork.SaveChangesAsync();
}
catch (DbUpdateConcurrencyException ex)
{
    // Signer B's request arrived before Signer A's changes were saved
    // Retry: Signer B fetches updated request, sees it's already Completed
    // Returns 409 Conflict (already completed)
}
```

**Result:** Only ONE `SigningRequestCompleted` event emitted, even if both sign simultaneously.

---

## Testing Strategy

**Unit Tests** (Domain logic, no DB):
- State transitions (can Draft→Pending? can PartiallySigned→Completed?)
- Business rules (immutable document, required signers, signing order)
- Domain events emitted correctly

**Integration Tests** (with real DB, no HTTP):
- Create signing request
- Fetch from DB
- Verify all entities persisted
- Test repository queries
- Test migrations

**API Contract Tests** (HTTP endpoints, auth, error codes):
- POST /create → 201 Created
- POST /sign → 204 NoContent
- GET /nonexistent → 404 NotFound
- Sign invalid signer → 403 Forbidden
- Sign already-signed signer → 409 Conflict

---

## Integration with Workflow Service

```
Phase 7: Workflow Engine
Phase 8: Digital Signature Service

┌─────────────────┐
│ Workflow State  │
│    Machine      │
└────────┬────────┘
         │
         │ Workflow reaches state: "DigitalSignature"
         │
         ↓
    ┌─────────────────────────────────┐
    │ Publish WorkflowStateChangedEvent│
    └────────┬────────────────────────┘
             │
             ↓
    ┌─────────────────────────────────┐
    │ Digital Signature Service       │
    │ (WorkflowIntegrationEventHandler)
    │                                 │
    │ CREATE signing request          │
    │ Activate                        │
    └────────┬────────────────────────┘
             │
             │ Emit SigningRequestActivatedEvent
             │
             ↓
    ┌──────────────────────────────┐
    │ Service Bus: signing-events  │
    │ (OutboxBackgroundService)    │
    └────────┬─────────────────────┘
             │
             │ (Signers sign document)
             │
             ↓
    ┌──────────────────────────────┐
    │ Workflow subscribes to topic │
    │ Receive: SigningRequestCompleted
    │                              │
    │ Transition to next state     │
    └──────────────────────────────┘
```

---

## 8 API Endpoints

| # | Method | Path | Purpose |
|---|--------|------|---------|
| 1 | POST | `/api/v1/signing-requests` | Create signing request (status: Draft) |
| 2 | POST | `/api/v1/signing-requests/{id}/activate` | Activate request (Draft → Pending) |
| 3 | GET | `/api/v1/signing-requests/{id}` | Get signing request details |
| 4 | GET | `/api/v1/signing-requests` | List signing requests (paginated, filterable) |
| 5 | POST | `/api/v1/signing-requests/{id}/sign` | Sign document (signer action) |
| 6 | POST | `/api/v1/signing-requests/{id}/decline` | Decline signing |
| 7 | POST | `/api/v1/signing-requests/{id}/cancel` | Cancel request (manager action) |
| 8 | POST | `/api/v1/signing-requests/{id}/signers/{signerId}/resend` | Resend invitation |
| 9 | GET | `/api/v1/signing-requests/{id}/audit` | Get audit trail |

---

## Database Tables (SQL Server)

```sql
-- Main entities
SigningRequests             (OrganizationId, WorkflowInstanceId, DocumentVersionId, Status, ExpiresAt, DocumentHash, ProviderRequestId, RowVersion)
SigningRequestSigners       (SigningRequestId, Email, SigningOrder, Status, RowVersion)
SignatureFields             (SigningRequestId, SignerId, DocumentPage, FieldType, X, Y, Value)
SignatureActions            (SigningRequestId, SignerId, ActionType, OccurredAt, IpAddress, UserAgent)
SigningProviderTransactions (ProviderRequestId, SigningRequestId, Provider, RequestPayload, ResponsePayload, Status, AttemptCount)

-- Messaging & idempotency
OutboxMessages              (EventId, EventType, Payload, Published)
InboxMessages               (EventId, EventType, Payload, ProcessedAt)

-- Key indexes:
IX_SigningRequests_OrganizationId_Status
IX_SigningRequests_WorkflowInstanceId
IX_SigningRequestSigners_SigningRequestId_Status
IX_SigningProviderTransactions_ProviderRequestId
IX_SigningAuditEntries_SigningRequestId_OccurredAt
```

---

## 10 Integration Events (Published to Service Bus)

```
Topic: "signing-events"

Subscriptions:
  - "workflow"      → Workflow service (SigningRequestCompleted → advance workflow)
  - "notification"  → Notification service (Phase 9) (invitations, reminders, declined, expired)
  - "audit"         → Audit service (all events)

Events:
  1. SigningRequestCreated        (request, signers, fields)
  2. SigningRequestActivated      (provider envelope created)
  3. SigningInvitationSent        (signer invited)
  4. SignerViewedDocument         (signer opened document)
  5. SignerSigned                 (signer submitted signature)
  6. SignerDeclined               (signer declined)
  7. SigningRequestExpired        (timer reached)
  8. SigningRequestCancelled      (manager cancelled)
  9. SigningRequestCompleted      (all required signers done)
  10. SignedDocumentCreated       (final document stored)
```

---

## Provider Abstraction Pattern

```csharp
// Domain should NOT reference DocuSign/Adobe/specific providers

public interface ISignatureProvider
{
    Task<SignatureEnvelopeResult> CreateEnvelopeAsync(SignatureEnvelopeRequest, CancellationToken);
    Task<SignatureActionResult> SignAsync(string providerRequestId, string signerId, SignatureActionRequest, CancellationToken);
    Task<SignatureStatusResult> GetStatusAsync(string providerRequestId, CancellationToken);
    Task CancelAsync(string providerRequestId, CancellationToken);
    Task<byte[]> DownloadCompletedDocumentAsync(string providerRequestId, CancellationToken);
}

// Implementations:
LocalDemoSignatureProvider     // In-memory, for dev/testing
ExternalProviderAdapter         // Base for DocuSign, Adobe, etc. (Phase 8.5+)

// DI: resolve by configuration
var provider = config["SignatureProvider"] switch
{
    "LocalDemo" => new LocalDemoSignatureProvider(...),
    "DocuSign" => new DocuSignAdapter(...),
    _ => throw new InvalidOperationException(...)
};
```

---

## Common Gotchas (What You'll Forget)

| Gotcha | Why It Matters | Solution |
|--------|---|---|
| Tenant isolation silently fails | Query filter doesn't throw; queries just return empty | Test explicitly: fetch by org ID in test, verify empty from other org |
| RowVersion not incremented | Concurrency check passes when it shouldn't | Use EF Core `IsRowVersion()` configuration |
| Outbox event not published | Domain event emitted but DB not queried | Verify background worker is running; check OutboxMessages table |
| Idempotency skipped | Provider called twice for same request | Store ProviderRequestId with unique constraint; check before calling |
| External signers can sign directly | API exposed to unauthenticated users | Don't use [Authorize] only; require short-lived signing session token |
| Document overwritten | Original lost after signing | Never update doc blob; create new versioned path |
| Duplicate completion event | Workflow notified twice | Use RowVersion + catch concurrency exception; emit once |
| Sequential order ignored | Wrong signer signs first | Validate signingOrder in domain; fetch next signer by order |
| Parallel signing blocks | Waits for first signer | Check ALL required signed, not "any required signed" |
| Error codes wrong HTTP status | 409 Conflict returned as 500 | Domain exceptions inherit `ProblemDetailsException` with proper status code |

---

## Deliverables Checklist

- [ ] **Phase-8-Implementation-Roadmap.md** — Complete roadmap (this folder, docs/)
- [ ] **Phase-8-DigitalSignature-TaskList.md** — Detailed 10-phase task breakdown (session notes)
- [ ] **5 .csproj files** — Domain, Application, Infrastructure, Api, Contracts
- [ ] **Domain layer** — Entities, enums, exceptions, business rules, state machine
- [ ] **Application layer** — Services, DTOs, background workers
- [ ] **Infrastructure layer** — DbContext, repositories, migrations, provider implementations
- [ ] **Api layer** — Controllers, endpoints, Program.cs, health checks
- [ ] **Service Bus integration** — Outbox pattern, event contracts, workflow event handler
- [ ] **Tests** — Unit, integration, API contract tests
- [ ] **Documentation** — README, API docs (Swagger/Scalar), architecture diagrams
- [ ] **Docker** — Dockerfile + docker-compose update
- [ ] **End-to-end test** — Template → Document → Workflow → Signing → Completion

---

## Estimated Timeline

| Phase | Task | Hours |
|-------|------|-------|
| 1 | Project setup (create .csproj, add to solution) | 0.5 |
| 2 | Domain layer (entities, state machine, rules) | 2.5 |
| 3 | Application layer (services, DTOs, workers) | 2.5 |
| 4 | Infrastructure (DbContext, repos, migration, provider) | 3.5 |
| 5 | API layer (controllers, Program.cs, health checks) | 2 |
| 6 | Service Bus & integration (outbox, events, workflow handler) | 2 |
| 7 | Testing (unit, integration, API contract) | 3.5 |
| 8 | Documentation, Docker, end-to-end | 1.5 |
| | **TOTAL** | **18 hours** |

---

## Resources

- Full requirements: `docs/Phase-8-Digital_Signature_Service_Requirement.md`
- Implementation roadmap: `docs/Phase-8-Implementation-Roadmap.md` (this file, in project)
- Detailed task list: Session notes (`/memories/session/Phase-8-DigitalSignature-TaskList.md`)
- Example services: `src/Services/Workflow/`, `src/Services/Document/`
- Shared libraries: `src/Shared/`
- Patterns used: Transactional outbox, optimistic concurrency, tenant isolation, provider abstraction
