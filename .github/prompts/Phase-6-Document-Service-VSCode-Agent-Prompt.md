# VS Code Agent Implementation Prompt — Phase 6 Document Service

## 1. Role

You are the **Senior .NET 10 Solution Architect and Implementation Agent** for the **Enterprise Document Automation Platform**.

Your responsibility is to implement **Phase 6 — Document Service** in the existing repository according to the Phase 6 requirements and the architecture established in Phases 2–5.

You must work directly in the existing repository, inspect the current implementation before making changes, create an implementation plan/tasks, execute them in dependency order, and continuously validate the solution.

Do not redesign the platform unnecessarily. Reuse established project conventions, shared libraries, infrastructure abstractions, naming conventions, API patterns, error handling, observability, security, and CI/CD conventions from earlier phases.

---

# 2. Primary Objective

Implement a production-ready **Document Service** that can:

1. Create and manage logical documents.
2. Create and manage document versions.
3. Generate documents from an activated Template Service template/version.
4. Retrieve template metadata, template files, and placeholder definitions from Template Service.
5. Validate supplied placeholder/business data.
6. Resolve placeholders.
7. Generate DOCX documents.
8. Convert DOCX to PDF.
9. Store generated files securely in Azure Blob Storage.
10. Persist document metadata in Azure SQL.
11. Support asynchronous document generation using Azure Service Bus.
12. Track generation jobs and status.
13. Implement retry and idempotency.
14. Publish document domain/integration events.
15. Integrate with Workflow, Notification, and Audit services through contracts/events.
16. Enforce authentication, authorization, and organization/tenant isolation.
17. Implement structured logging, OpenTelemetry, metrics, health checks, and correlation IDs.
18. Provide comprehensive unit, integration, contract, and end-to-end tests.
19. Prepare the architecture for future bulk generation and advanced document capabilities.

---

# 3. Source of Truth

Before implementation, inspect and use these sources in this order:

1. Existing repository code.
2. Existing Phase 2 Foundation implementation.
3. Existing Phase 3 solution/bootstrap implementation.
4. Existing Phase 4 Identity/Organization implementation.
5. Existing Phase 5 Template Service implementation.
6. `Phase-6-Document-Service-Requirements.md`.
7. Existing documentation under `docs/`.
8. Existing tests and CI/CD configuration.

If the repository contains a Phase 6 requirements file, treat it as the primary functional requirement.

Do not assume that examples in this prompt exactly match the repository. The existing repository conventions take precedence unless they conflict with an explicit Phase 6 requirement.

---

# 4. Mandatory First Step — Repository Discovery

Before modifying any code, inspect the repository.

Determine:

- Current solution structure.
- `.sln` / `.slnx` structure.
- Target framework.
- Existing projects.
- Existing Shared projects.
- Existing Identity implementation.
- Existing Organization/Tenant implementation.
- Existing Template Service implementation.
- Existing API conventions.
- Existing database conventions.
- Existing EF Core setup.
- Existing authentication/authorization.
- Existing Service Bus abstraction.
- Existing Azure Blob abstraction.
- Existing OpenTelemetry configuration.
- Existing Serilog configuration.
- Existing API versioning.
- Existing Scalar/OpenAPI setup.
- Existing exception handling.
- Existing result/error model.
- Existing validation approach.
- Existing Docker setup.
- Existing test structure.
- Existing CI/CD pipeline.
- Existing configuration conventions.
- Existing dependency injection conventions.

Do not start coding before completing this discovery.

---

# 5. Create an Implementation Plan First

After repository discovery, create a detailed implementation plan.

The plan must be divided into executable tasks with:

- Task ID.
- Task title.
- Objective.
- Dependencies.
- Files/projects expected to change.
- Implementation details.
- Validation criteria.
- Test requirements.
- Completion status.

Use dependency order.

Recommended task hierarchy:

```text
DOC-001  Repository Discovery
DOC-002  Phase 6 Architecture Confirmation
DOC-003  Project/Directory Bootstrap
DOC-004  Domain Model
DOC-005  Domain Events
DOC-006  Database Model
DOC-007  EF Core Configuration
DOC-008  Document Application Commands
DOC-009  Document Application Queries
DOC-010  Validation
DOC-011  Template Service Integration
DOC-012  Placeholder Resolution Engine
DOC-013  DOCX Generation
DOC-014  PDF Conversion Abstraction
DOC-015  Blob Storage Integration
DOC-016  Generation Job Processing
DOC-017  Azure Service Bus Integration
DOC-018  Retry/Idempotency/Concurrency
DOC-019  Document APIs
DOC-020  Download APIs
DOC-021  Document Lifecycle
DOC-022  Workflow Integration Contracts
DOC-023  Notification Integration Contracts
DOC-024  Audit Integration
DOC-025  Authorization/Tenant Isolation
DOC-026  Observability
DOC-027  Health Checks
DOC-028  Unit Tests
DOC-029  Integration Tests
DOC-030  Contract Tests
DOC-031  End-to-End Tests
DOC-032  Performance Tests
DOC-033  Security Tests
DOC-034  Documentation
DOC-035  Docker/Local Development
DOC-036  CI/CD Validation
DOC-037  Final Verification
```

You may split, merge, or rename tasks after inspecting the repository, but do not omit important dependencies.

---

# 6. Execution Rules

## Rule 1 — Inspect Before Editing

Never blindly create files or overwrite existing implementation.

For every major change:

1. Locate existing pattern.
2. Understand it.
3. Reuse it.
4. Extend it only where required.

## Rule 2 — Do Not Duplicate Existing Infrastructure

If the repository already contains:

- Shared Result types.
- Shared exception handling.
- Shared authentication.
- Shared tenant context.
- Shared Service Bus client.
- Shared Blob Storage abstraction.
- Shared telemetry.
- Shared database infrastructure.

reuse those components.

Do not create duplicate implementations.

## Rule 3 — Follow Existing Naming Conventions

If existing services use:

```text
Edp.Template.Api
Edp.Template.Application
Edp.Template.Domain
Edp.Template.Infrastructure
Edp.Template.Contracts
```

then Document Service must follow:

```text
Edp.Document.Api
Edp.Document.Application
Edp.Document.Domain
Edp.Document.Infrastructure
Edp.Document.Contracts
```

Use the repository's actual conventions if they differ.

## Rule 4 — Do Not Break Existing Services

After every significant phase:

```text
dotnet build
dotnet test
```

and any repository-specific validation commands.

The implementation must not break Template, Identity, Organization, Gateway, Shared, or Web projects.

---

# 7. Target Architecture

The expected service structure is:

```text
src/
└── Services/
    └── Document/
        ├── Edp.Document.Api/
        ├── Edp.Document.Application/
        ├── Edp.Document.Domain/
        ├── Edp.Document.Infrastructure/
        └── Edp.Document.Contracts/
```

Expected tests:

```text
tests/
├── Edp.Document.UnitTests/
├── Edp.Document.IntegrationTests/
├── Edp.Document.ContractTests/
└── Edp.Document.EndToEndTests/
```

Use existing repository naming conventions if they differ.

---

# 8. Domain Model

Implement the following core domain concepts.

## 8.1 Document

Minimum conceptual properties:

```text
Id
OrganizationId
DocumentType
Name
Description
TemplateId
CurrentTemplateVersionId
CurrentVersionNumber
Status
ExternalReference
CreatedBy
CreatedAt
UpdatedBy
UpdatedAt
ArchivedAt
DeletedAt
CorrelationId
RowVersion
```

## 8.2 DocumentVersion

```text
Id
DocumentId
VersionNumber
TemplateId
TemplateVersionId
GenerationJobId
InputDataHash
DocumentHash
Status
GeneratedBy
GeneratedAt
CreatedAt
```

## 8.3 DocumentFile

```text
Id
DocumentVersionId
FileType
FileName
ContentType
Size
StoragePath
Hash
CreatedAt
```

## 8.4 DocumentGenerationJob

```text
Id
DocumentId
DocumentVersionId
TemplateId
TemplateVersionId
OrganizationId
Status
RequestedBy
RequestedAt
StartedAt
CompletedAt
FailedAt
RetryCount
ErrorCode
ErrorMessage
CorrelationId
```

Implement proper aggregate boundaries and domain behavior rather than exposing setters everywhere.

---

# 9. Document Statuses

Implement the statuses required by the Phase 6 requirements.

Expected statuses include:

```text
Requested
Queued
Generating
Generated
Failed
Draft
InReview
PendingApproval
Approved
Rejected
PendingSignature
Signed
Completed
Archived
Cancelled
Deleted
```

Do not allow arbitrary status changes.

Define valid transitions.

For example:

```text
Requested -> Queued
Queued -> Generating
Generating -> Generated
Generating -> Failed
Generated -> Draft
Draft -> InReview
Draft -> Cancelled
Completed -> Archived
```

Coordinate workflow-owned states with Workflow Service.

---

# 10. Generation Job State Machine

Implement:

```text
Queued
   |
   v
Processing
   |
   +----> Failed
   |
   v
Completed
```

Optional:

```text
Queued -> Cancelled
Processing -> Cancelled
```

The state transition must be concurrency-safe.

---

# 11. Domain Events

Implement appropriate domain/integration event contracts.

At minimum:

```text
DocumentCreated
DocumentRequested
DocumentGenerationStarted
DocumentGenerated
DocumentGenerationFailed
DocumentVersionCreated
DocumentArchived
DocumentCancelled
```

Use the repository's existing event envelope if one exists.

Do not invent a second event envelope.

---

# 12. Database Requirements

Use the repository's established EF Core/database conventions.

Primary database:

```text
Azure SQL
```

Core tables:

```text
Documents
DocumentVersions
DocumentFiles
DocumentGenerationJobs
DocumentGenerationErrors
```

Potential future tables:

```text
DocumentTags
DocumentExternalReferences
DocumentMetadata
```

Implement:

- Primary keys.
- Foreign keys.
- Required fields.
- Unique constraints.
- Indexes.
- RowVersion/concurrency.
- Organization/tenant filtering where appropriate.
- Created/updated audit fields where repository conventions require them.

Important indexes include:

```text
Documents.OrganizationId
Documents.Status
Documents.TemplateId
Documents.CreatedBy
Documents.CreatedAt
Documents.ExternalReference

DocumentVersions.DocumentId + VersionNumber

DocumentFiles.DocumentVersionId + FileType
```

---

# 13. Database Migration

Create EF Core migrations following existing repository conventions.

Do not modify the database manually if migrations are the established project approach.

Validate:

```text
dotnet ef migrations add ...
dotnet ef database update
```

or the repository's equivalent process.

---

# 14. Template Service Integration

Document Service must communicate with Template Service through its API/client abstraction.

It must obtain:

```text
Template metadata
Template version
Template file
Placeholder definitions
Template activation state
Template validation information
```

Never access Template Service database directly.

Create/reuse a typed client or service abstraction.

Example conceptual abstraction:

```csharp
public interface ITemplateServiceClient
{
    Task<TemplateVersionDto?> GetTemplateVersionAsync(
        Guid templateId,
        int version,
        CancellationToken cancellationToken);

    Task<Stream> GetTemplateFileAsync(
        Guid templateId,
        int version,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<PlaceholderDefinitionDto>>
        GetPlaceholderDefinitionsAsync(
            Guid templateId,
            int version,
            CancellationToken cancellationToken);
}
```

Adapt this to existing repository contracts.

---

# 15. Template Version Snapshot

Every generation job must use a deterministic template version.

The generation job must capture:

```text
TemplateId
TemplateVersionId
TemplateVersion
```

Never silently switch to a newer active template while a generation job is running.

---

# 16. Placeholder Engine

Implement the Document Service placeholder-resolution capability according to Phase 5's placeholder model.

Supported initial types:

```text
String
Integer
Decimal
Boolean
Date
DateTime
Currency
Email
Phone
URL
MultilineText
```

Required capabilities:

- Detect placeholders.
- Validate supplied values.
- Detect missing required values.
- Detect unknown placeholders.
- Type conversion.
- Null handling.
- Empty value handling.
- Formatting.
- Multiline text.
- Nested data where supported.

Do not duplicate the Template Service's placeholder CRUD functionality.

Template Service owns placeholder definitions.

Document Service consumes them and resolves values.

---

# 17. Placeholder Validation

Validation must occur before document generation.

Validation errors should be structured.

Example:

```json
{
  "code": "PLACEHOLDER_VALIDATION_FAILED",
  "message": "One or more placeholder values are invalid.",
  "errors": [
    {
      "placeholder": "ContractNumber",
      "code": "REQUIRED",
      "message": "Required placeholder value is missing."
    }
  ]
}
```

Do not send invalid generation requests to the expensive DOCX/PDF pipeline.

---

# 18. Formatting

Support controlled formatting.

Examples:

```text
{{InvoiceDate | format:"dd-MMM-yyyy"}}

{{Amount | currency:"INR"}}

{{Amount | number:"2"}}
```

Security requirement:

Never execute arbitrary C# or arbitrary scripts from a template.

The expression engine must be constrained to explicitly supported operations.

---

# 19. Conditional Rendering

Prepare the architecture for:

```text
{{#if IsCorporate}}
...
{{/if}}
```

and future expressions such as:

```text
{{#if DiscountPercentage > 0}}
...
{{/if}}
```

If full conditional processing is deferred, create the correct abstraction and document the limitation.

---

# 20. Collection/Repeating Data

Prepare for:

```text
{{#each Items}}
...
{{/each}}
```

This is especially important for:

- Invoices.
- Purchase orders.
- Statements.
- Quotes.
- Reports.

Do not implement a fragile solution that prevents future collection support.

---

# 21. DOCX Generation

Use:

```text
Open XML SDK
```

where consistent with repository technology decisions.

The generator must:

1. Read the selected template file.
2. Open it safely.
3. Resolve placeholders.
4. Replace values.
5. Preserve document formatting.
6. Preserve styles.
7. Preserve tables.
8. Preserve headers.
9. Preserve footers.
10. Preserve document properties where possible.
11. Generate a new DOCX.
12. Never modify the original template.

Create an abstraction such as:

```csharp
public interface IDocumentGenerator
{
    Task<DocumentGenerationResult> GenerateAsync(
        TemplateDocument template,
        DocumentData data,
        CancellationToken cancellationToken);
}
```

Adapt to existing patterns.

---

# 22. PDF Conversion

Create a conversion abstraction.

Example:

```csharp
public interface IDocumentConverter
{
    Task<Stream> ConvertToPdfAsync(
        Stream source,
        CancellationToken cancellationToken);
}
```

The concrete implementation may depend on the technology already selected/approved in the repository.

Do not hard-code the entire application around one PDF conversion provider.

---

# 23. Storage

Use Azure Blob Storage through an abstraction.

Example conceptual interface:

```csharp
public interface IDocumentStorage
{
    Task<StoredFile> UploadAsync(...);
    Task<Stream> DownloadAsync(...);
    Task DeleteAsync(...);
}
```

Adapt to existing Shared/Storage abstractions.

Recommended logical path:

```text
organizations/{organizationId}/documents/{documentId}/versions/{versionNumber}/file
```

Store:

```text
Blob path
Content type
Size
SHA-256 hash
```

Never expose public blob containers.

---

# 24. Download Security

Before returning a document:

1. Authenticate the caller.
2. Resolve organization context.
3. Authorize access.
4. Verify document ownership/tenant.
5. Retrieve file metadata.
6. Retrieve content or generate a short-lived access URL.
7. Audit the download.

Do not allow direct arbitrary blob path access.

---

# 25. Asynchronous Generation

Use Azure Service Bus following existing repository abstractions.

Flow:

```text
POST /documents
      |
      v
Create Document
      |
      v
Create Generation Job
      |
      v
Publish Generation Command
      |
      v
Azure Service Bus
      |
      v
Generation Worker
      |
      v
Template Retrieval
      |
      v
Placeholder Validation
      |
      v
DOCX
      |
      v
PDF
      |
      v
Blob Storage
      |
      v
Database Update
      |
      v
DocumentGenerated
```

---

# 26. Worker Requirements

The worker must:

1. Receive the generation command.
2. Validate message schema.
3. Load the generation job.
4. Verify idempotency.
5. Acquire processing ownership/concurrency lock.
6. Update job to `Processing`.
7. Retrieve template version.
8. Retrieve placeholder definitions.
9. Validate input data.
10. Generate DOCX.
11. Convert to PDF when requested.
12. Calculate SHA-256 hashes.
13. Upload output files.
14. Persist file metadata.
15. Mark document/version/job successful.
16. Publish `DocumentGenerated`.
17. Complete the Service Bus message.

---

# 27. Retry and Dead-Letter

Implement retry for transient failures.

Recommended:

```text
Maximum attempts: 3
Backoff: Exponential
```

Permanent failures must not endlessly retry.

Examples:

```text
Template validation failure -> no retry
Missing required placeholder -> no retry
Invalid template -> no retry

Temporary storage failure -> retry
Temporary network failure -> retry
Transient dependency timeout -> retry
```

Use the repository's existing Service Bus retry/dead-letter conventions.

---

# 28. Idempotency

Generation must be idempotent.

Use:

```text
GenerationJobId
MessageId
CorrelationId
```

If a duplicate message arrives:

```text
If job is already Completed:
    do not regenerate

If job is already Processing:
    do not create duplicate processing

If job is Failed and retry is allowed:
    process according to retry policy
```

Do not rely only on in-memory locks.

---

# 29. Concurrency

Use a durable concurrency strategy.

Preferred initial approach:

```text
SQL RowVersion + atomic job status transition
```

Example:

```text
Queued
  |
  v
Processing
```

Only one worker must successfully acquire the job.

---

# 30. API Requirements

Implement:

```http
POST   /api/v1/documents
GET    /api/v1/documents
GET    /api/v1/documents/{documentId}
GET    /api/v1/documents/{documentId}/versions/{version}
POST   /api/v1/documents/{documentId}/generate
POST   /api/v1/documents/{documentId}/regenerate
GET    /api/v1/documents/{documentId}/generation-status
GET    /api/v1/documents/{documentId}/files/docx
GET    /api/v1/documents/{documentId}/files/pdf
GET    /api/v1/documents/{documentId}/versions/{version}/files/{fileType}
POST   /api/v1/documents/{documentId}/cancel
POST   /api/v1/documents/{documentId}/archive
```

Implement bulk generation foundation:

```http
POST /api/v1/documents/bulk
```

if it is consistent with the current platform architecture.

---

# 31. API Behavior

## Create

Should:

1. Validate request.
2. Resolve organization/user.
3. Validate template reference.
4. Create document.
5. Create document version/generation job as appropriate.
6. Publish generation command.
7. Return document/job identifiers.

## Generate

Should:

1. Validate document access.
2. Create generation job/version as required.
3. Publish generation command.
4. Return asynchronous status.

## Regenerate

Must create a new document version.

It must not overwrite the existing generated version.

Example:

```text
Document
 |
 +-- Version 1
 +-- Version 2
 +-- Version 3
```

---

# 32. API Filtering and Pagination

Support:

```text
status
templateId
createdFrom
createdTo
createdBy
documentType
externalReference
```

Pagination:

```text
page
pageSize
```

Maximum page size:

```text
100
```

Follow repository conventions for pagination response format.

---

# 33. Bulk Generation Foundation

Support the architectural foundation for:

```text
Bulk Job
 |
 +-- Generation Job 1
 +-- Generation Job 2
 +-- Generation Job 3
 ...
```

Each generated document must have its own:

```text
DocumentId
DocumentVersionId
GenerationJobId
```

Bulk processing must use Service Bus rather than attempting to generate every document in one HTTP request.

---

# 34. Authorization

Reuse existing Identity/Organization authorization infrastructure.

Permissions:

```text
Document.Read
Document.Create
Document.Generate
Document.Download
Document.Regenerate
Document.Archive
Document.Delete
Document.Manage
```

At minimum:

- Authentication required.
- Organization isolation required.
- Resource authorization required.
- No cross-tenant access.
- Download must be authorized.
- Archive/delete must be restricted.

---

# 35. Audit

Audit at least:

```text
DocumentCreated
DocumentGenerationRequested
DocumentGenerationStarted
DocumentGenerated
DocumentGenerationFailed
DocumentDownloaded
DocumentRegenerated
DocumentVersionCreated
DocumentArchived
DocumentDeleted
DocumentCancelled
```

Reuse the existing Audit abstraction/event contract if already implemented.

Do not create a separate Audit database inside Document Service unless the architecture explicitly requires it.

---

# 36. Workflow Integration

Document Service should publish the event/contract required by Workflow Service.

Example:

```text
DocumentGenerated
      |
      v
Workflow Service
      |
      v
Start Workflow
```

Do not implement approval logic inside Document Service.

Document Service owns document generation and document lifecycle; Workflow Service owns approval/signature state.

---

# 37. Notification Integration

Document Service should publish events required for notifications.

Examples:

```text
DocumentGenerated
DocumentGenerationFailed
DocumentCompleted
```

Notification Service should determine how notifications are delivered.

Do not directly implement email/SMS/Teams delivery inside Document Service.

---

# 38. Error Model

Reuse the repository's standard error response.

If none exists, use a structured format similar to:

```json
{
  "code": "DOCUMENT_GENERATION_FAILED",
  "message": "The document could not be generated.",
  "correlationId": "corr-123",
  "errors": []
}
```

Never expose:

```text
Stack traces
Connection strings
Secrets
Access tokens
Internal infrastructure details
```

---

# 39. Observability

Reuse existing platform standards.

Expected:

```text
Serilog
OpenTelemetry
Application Insights
```

Track:

```text
Document generation duration
DOCX generation duration
PDF conversion duration
Template retrieval duration
Storage upload duration
Queue processing duration
Generation failure rate
Retry count
Documents generated per minute
```

Include dimensions:

```text
OrganizationId
DocumentType
TemplateId
TemplateVersion
OutputFormat
GenerationStatus
```

Do not log sensitive document payloads or full customer data.

---

# 40. Correlation ID

Correlation ID must flow through:

```text
API
 |
 v
Document Service
 |
 +--> Template Service
 |
 +--> Service Bus
 |
 +--> Worker
 |
 +--> Blob Storage
 |
 +--> Workflow
 |
 +--> Audit
```

Use existing correlation middleware/header conventions.

---

# 41. Health Checks

Implement:

```http
GET /health
GET /health/live
GET /health/ready
```

Readiness should validate critical dependencies according to repository conventions:

```text
Azure SQL
Azure Blob Storage
Azure Service Bus
Template Service
```

Do not make liveness depend on external services.

---

# 42. Security Requirements

Implement:

- JWT/OIDC authentication.
- Organization isolation.
- RBAC.
- Resource-level authorization.
- Input validation.
- File type validation.
- Maximum file size limits.
- Secure Blob Storage.
- HTTPS.
- Managed Identity.
- Secretless service-to-service authentication where supported.
- Audit logging.
- Safe file names.
- Path traversal protection.
- No arbitrary code execution in templates.

---

# 43. File Security

Never trust user-controlled file names or storage paths.

Protect against:

```text
../../../file.docx
..\..\file.docx
```

Generate internal storage paths using:

```text
OrganizationId
DocumentId
VersionNumber
Generated File ID
```

Validate MIME/content type where required.

---

# 44. Performance Targets

Initial target:

```text
Simple DOCX generation:
< 5 seconds

Simple DOCX + PDF:
< 10 seconds
```

These are target SLOs rather than guaranteed execution limits.

The API must not remain blocked while large documents are generated.

Prefer asynchronous generation for production workloads.

---

# 45. Scalability

API and generation workers must scale independently.

Expected architecture:

```text
API Instances
     |
     v
Service Bus
     |
     +---- Worker 1
     +---- Worker 2
     +---- Worker 3
     +---- Worker N
     |
     v
Azure Blob
```

Workers should be horizontally scalable.

---

# 46. Caching

If the platform already provides Redis:

Potentially cache:

```text
Template metadata
Placeholder definitions
```

Do not cache sensitive generated document content in Redis by default.

Respect template versioning and invalidation.

---

# 47. Testing Strategy

Implement four primary test layers.

## Unit Tests

Cover:

```text
Document aggregate
Document versioning
State transitions
Placeholder resolution
Placeholder validation
Formatting
Hash generation
Idempotency rules
```

## Integration Tests

Cover:

```text
Azure SQL
Blob Storage
Template Service client
Service Bus
Document generation
PDF conversion
```

Use test containers/emulators/mocks according to existing repository standards.

## Contract Tests

Validate:

```text
Template Service contract
Service Bus event contract
Workflow event contract
Audit event contract
```

## End-to-End Tests

At minimum:

```text
Template
  |
  v
Activate
  |
  v
Create Document
  |
  v
Generate
  |
  v
DOCX
  |
  v
PDF
  |
  v
Blob Storage
  |
  v
Download
```

---

# 48. Mandatory Test Cases

## Placeholder

- Required value present.
- Required value missing.
- Optional value missing.
- Invalid type.
- Unknown placeholder.
- Null value.
- Empty value.
- Special characters.
- Multiline text.
- Date formatting.
- Currency formatting.

## Document

- Valid template.
- Invalid template.
- Template version unavailable.
- Template inactive.
- Duplicate generation request.
- Concurrent generation.
- Generation retry.
- Permanent generation failure.
- PDF conversion failure.
- Storage failure.
- Regeneration creates new version.
- Existing versions remain unchanged.

## Security

- User accesses own organization document.
- User cannot access another organization document.
- Unauthorized download.
- Unauthorized generation.
- Unauthorized archive.
- Unauthorized delete.
- Invalid organization context.

---

# 49. Documentation Requirements

Create/update:

```text
docs/phase-6-document-service/
```

Recommended files:

```text
README.md
requirements.md
architecture.md
api-design.md
database-design.md
event-design.md
generation-engine.md
storage-design.md
security.md
testing.md
```

Documentation must reflect the actual implementation, not an aspirational design.

---

# 50. Local Development

Inspect existing Docker/infra configuration.

Add only what is required for Document Service.

Potential local dependencies:

```text
SQL Server / Azure SQL compatible database
Azurite
Service Bus emulator or repository-supported substitute
Redis if required
PDF conversion dependency
```

Do not introduce local infrastructure that duplicates existing services.

Update:

```text
docker-compose
Dockerfiles
environment configuration
appsettings
development configuration
```

only where necessary.

Never commit secrets.

---

# 51. Configuration

Configuration should support:

```text
ConnectionStrings
TemplateService
AzureBlobStorage
ServiceBus
PdfConversion
DocumentGeneration
Retry
Observability
```

Example conceptual structure:

```json
{
  "DocumentService": {
    "Generation": {
      "MaxFileSizeMb": 25,
      "MaxRetryCount": 3
    }
  },
  "TemplateService": {
    "BaseUrl": "..."
  },
  "Storage": {
    "ContainerName": "..."
  }
}
```

Use Options pattern and existing configuration conventions.

---

# 52. Dependency Injection

Use interfaces for external dependencies:

```text
ITemplateServiceClient
IDocumentStorage
IDocumentGenerator
IDocumentConverter
IDocumentRepository
IGenerationJobRepository
IEventPublisher
IHashService
```

Avoid service locator patterns.

Keep dependency direction clean.

---

# 53. EF Core Rules

Use:

```text
DbContext
EntityTypeConfiguration
Migrations
```

Avoid putting EF Core attributes throughout Domain entities if the existing architecture favors fluent configuration.

Do not expose `IQueryable` outside repository/application boundaries unless the existing architecture explicitly uses that pattern.

---

# 54. API Design Rules

Follow existing API conventions for:

- Versioning.
- Route naming.
- DTOs.
- Validation.
- HTTP status codes.
- Problem Details.
- Pagination.
- Sorting.
- Filtering.
- OpenAPI.
- Scalar.

Do not introduce a second API framework or documentation mechanism.

---

# 55. Expected HTTP Status Codes

Use repository conventions, with expected semantics:

```text
201 Created
200 OK
202 Accepted
204 No Content
400 Bad Request
401 Unauthorized
403 Forbidden
404 Not Found
409 Conflict
422 Unprocessable Entity
500 Internal Server Error
503 Service Unavailable
```

Generation requests that are queued asynchronously should normally return:

```text
202 Accepted
```

---

# 56. Implementation Sequence

Execute tasks in this dependency order:

```text
1. Repository Discovery
2. Architecture Confirmation
3. Project Bootstrap
4. Domain
5. Domain Events
6. Database
7. Application Commands/Queries
8. Validation
9. Template Integration
10. Placeholder Engine
11. DOCX Generator
12. PDF Converter
13. Blob Storage
14. Generation Jobs
15. Service Bus
16. Worker
17. Retry/Idempotency/Concurrency
18. APIs
19. Download
20. Lifecycle
21. Workflow/Notification/Audit contracts
22. Security
23. Observability
24. Health Checks
25. Unit Tests
26. Integration Tests
27. Contract Tests
28. E2E Tests
29. Documentation
30. Docker/CI/CD
31. Full Verification
```

Do not jump directly to API implementation before the domain and infrastructure contracts are understood.

---

# 57. Agent Task Execution Format

For each task, report internally/use the following structure:

```text
Task: DOC-XXX
Status: NOT_STARTED | IN_PROGRESS | BLOCKED | COMPLETED

Objective:
...

Dependencies:
...

Files:
...

Implementation:
...

Validation:
...

Tests:
...

Result:
...
```

After completing a task:

```text
Task DOC-XXX = COMPLETED
```

If blocked:

```text
Task DOC-XXX = BLOCKED

Reason:
...

Impact:
...

Recommended action:
...
```

Do not silently skip blocked tasks.

---

# 58. Validation After Each Major Milestone

After each milestone execute appropriate validation.

## After Project Bootstrap

```bash
dotnet restore
dotnet build
```

## After Domain/Database

```bash
dotnet build
dotnet test
```

## After APIs

```bash
dotnet build
dotnet test
```

## After Worker/Infrastructure

```bash
dotnet build
dotnet test
```

## Final

Run:

```bash
dotnet restore
dotnet build
dotnet test
```

plus repository-specific:

```text
lint
format
security scan
integration tests
container build
```

if already configured.

---

# 59. Build and Test Quality Gates

Do not consider Phase 6 complete if:

- Build fails.
- Existing services fail to compile.
- Existing tests fail because of unintended changes.
- Document tests fail.
- API contracts are inconsistent.
- Tenant isolation is missing.
- Generation is not idempotent.
- Generated files cannot be retrieved.
- DOCX generation is broken.
- PDF generation is broken when PDF is requested.
- Service Bus generation workflow is broken.
- Critical security checks fail.

---

# 60. Existing Code Compatibility

Before adding NuGet packages:

1. Inspect existing package versions.
2. Reuse existing versions where compatible.
3. Check .NET 10 compatibility.
4. Avoid unnecessary package additions.
5. Keep package versions consistent across the solution.

Do not upgrade unrelated packages simply to implement Document Service.

---

# 61. Important Implementation Constraint — Phase 5 Placeholder Ownership

The Phase 5 Template Service owns:

```text
Placeholder Definition CRUD
Placeholder Metadata
Placeholder Type
Required/Optional Definition
Template Placeholder Association
```

Phase 6 owns:

```text
Runtime Placeholder Data
Placeholder Validation
Placeholder Resolution
Placeholder Formatting
Document Rendering
```

Do not duplicate Placeholder Management CRUD in Document Service.

---

# 62. Important Implementation Constraint — Document Versioning

Never overwrite a generated document version during regeneration.

Correct:

```text
Document DOC-001
 |
 +-- Version 1
 +-- Version 2
```

Incorrect:

```text
Document DOC-001
 |
 +-- Overwrite Version 1
```

Versioning is required for auditability and legal traceability.

---

# 63. Important Implementation Constraint — Template Immutability

Never modify the source template.

Correct:

```text
Template v5
     |
     v
Read
     |
     v
Generate new DOCX
```

Incorrect:

```text
Template v5
     |
     v
Modify template
```

---

# 64. Important Implementation Constraint — Tenant Isolation

Every document query must be organization-aware.

Never implement:

```csharp
_context.Documents.FindAsync(documentId);
```

without applying the appropriate organization/resource authorization strategy.

A document ID must never be sufficient by itself to bypass tenant authorization.

---

# 65. Important Implementation Constraint — Sensitive Data

Do not log:

- Full placeholder payload.
- Customer personal information.
- Full document content.
- Access tokens.
- Connection strings.
- SAS tokens.
- Secrets.

Use identifiers and safe metadata:

```text
DocumentId
TemplateId
OrganizationId
CorrelationId
GenerationStatus
Duration
```

---

# 66. Important Implementation Constraint — PDF Converter

Before selecting/adding a PDF converter:

1. Inspect existing project dependencies.
2. Check current approved architecture.
3. Check licensing implications.
4. Check Linux/container compatibility.
5. Check .NET 10 compatibility.
6. Check production deployment requirements.

Do not silently introduce a converter that cannot run in the target Azure environment.

If a provider decision is genuinely missing, create a clear implementation task/blocker rather than making an unverified architectural assumption.

---

# 67. Important Implementation Constraint — Cloud Abstraction

Business logic must not depend directly on Azure SDK types.

Prefer:

```text
Application
   |
   v
Interface
   |
   v
Infrastructure
   |
   v
Azure SDK
```

This is required for testability and future provider flexibility.

---

# 68. Final End-to-End Acceptance Scenario

The agent must demonstrate the following flow before declaring Phase 6 complete:

```text
1. User authenticates.
2. User selects an activated template.
3. User supplies business data.
4. Document Service validates organization access.
5. Document Service validates template/version.
6. Document Service creates Document.
7. Document Service creates DocumentVersion.
8. Document Service creates GenerationJob.
9. Generation command is published to Service Bus.
10. Worker receives the command.
11. Worker acquires the job using concurrency control.
12. Worker retrieves exact Template Version.
13. Worker retrieves Placeholder Definitions.
14. Worker validates runtime data.
15. Worker generates DOCX.
16. Worker generates PDF when requested.
17. Worker calculates SHA-256 hashes.
18. Worker uploads files to Blob Storage.
19. Worker persists DocumentFile records.
20. Worker marks generation successful.
21. Worker publishes DocumentGenerated.
22. Workflow integration can consume the event.
23. Audit integration records the action.
24. User can query document status.
25. User can download DOCX.
26. User can download PDF.
27. Regeneration creates a new document version.
28. Previous document version remains unchanged.
29. Duplicate generation messages do not create duplicate versions.
30. Unauthorized users cannot access the document.
31. Cross-organization access is rejected.
32. Logs contain CorrelationId without sensitive payloads.
33. Health endpoints work.
34. Unit/integration/E2E tests pass.
35. Entire solution builds successfully.
```

---

# 69. Final Definition of Done

Do not report Phase 6 as complete until all applicable items are satisfied.

```text
[ ] Repository inspected.
[ ] Phase 2–5 conventions reused.
[ ] Phase 6 implementation plan created.
[ ] Document Service projects created/updated.
[ ] Domain model implemented.
[ ] Domain events implemented.
[ ] EF Core model implemented.
[ ] Database migration implemented.
[ ] Document APIs implemented.
[ ] Template Service integration implemented.
[ ] Placeholder validation implemented.
[ ] Placeholder resolution implemented.
[ ] DOCX generation implemented.
[ ] PDF conversion implemented.
[ ] Blob Storage integration implemented.
[ ] Document metadata implemented.
[ ] Document versioning implemented.
[ ] Generation jobs implemented.
[ ] Service Bus integration implemented.
[ ] Background worker implemented.
[ ] Retry implemented.
[ ] Idempotency implemented.
[ ] Concurrency protection implemented.
[ ] Download security implemented.
[ ] Tenant isolation implemented.
[ ] Authorization implemented.
[ ] Workflow integration implemented.
[ ] Notification integration contract implemented.
[ ] Audit integration implemented.
[ ] Structured logging implemented.
[ ] OpenTelemetry implemented.
[ ] Metrics implemented.
[ ] Health checks implemented.
[ ] Unit tests implemented.
[ ] Integration tests implemented.
[ ] Contract tests implemented.
[ ] E2E tests implemented.
[ ] Security tests implemented.
[ ] Performance validation completed.
[ ] Docker/local environment updated.
[ ] CI/CD validation completed.
[ ] Documentation updated.
[ ] Full solution build passes.
[ ] Full relevant test suite passes.
[ ] Final end-to-end scenario passes.
```

---

# 70. Final Agent Behavior

Throughout implementation:

- Be proactive.
- Inspect before changing.
- Create tasks before execution.
- Execute tasks in dependency order.
- Keep changes focused on Phase 6.
- Reuse existing platform abstractions.
- Avoid unnecessary refactoring.
- Do not duplicate Phase 5 functionality.
- Do not bypass tenant authorization.
- Do not introduce secrets.
- Do not silently make major architecture decisions.
- Stop and report a blocker when a required dependency or architectural decision is genuinely unavailable.
- Fix compilation/test failures caused by your implementation.
- Keep documentation synchronized with actual code.
- Validate continuously.
- At the end, provide a concise implementation summary, completed task list, remaining blockers, tests executed, and files/projects changed.

The final implementation must be **production-oriented, secure, tenant-aware, observable, testable, asynchronous where appropriate, and consistent with the Enterprise Document Automation Platform architecture established in Phases 2–5.**
