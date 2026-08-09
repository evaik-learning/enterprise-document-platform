# Phase 5 — Template Service Detailed Implementation

**Project:** Enterprise Document Automation Platform  
**Repository:** `EnterpriseDocumentPlatform`  
**Phase:** 5 — Template Service  
**Status:** Implementation Specification  
**Version:** 1.0  
**Target Framework:** .NET 10  
**Primary Database:** Azure SQL / SQL Server  
**Binary Storage:** Azure Blob Storage  
**Messaging:** Azure Service Bus  
**API Style:** REST  
**Frontend:** React + TypeScript  
**Architecture:** Modular Microservices / Domain-oriented Services

---

## 1. Purpose

Phase 5 implements the **Template Service**, the first major business service of the Enterprise Document Automation Platform.

The Template Service is responsible for managing document templates throughout their lifecycle:

- Template creation
- Template upload
- Template metadata
- Template versioning
- Template binary storage
- Placeholder definitions
- Placeholder validation
- Template validation
- Template activation/deactivation
- Template retrieval
- Template version retrieval
- Template lifecycle events
- Audit integration
- Tenant/organization isolation
- Authorization

The service must be implemented on top of the infrastructure, identity, organization, security, observability, persistence, messaging, and deployment foundations established in Phase 4.

---

# 2. Phase 5 Objectives

By the end of Phase 5, the platform must support:

1. Authenticated users can create templates.
2. Users can upload `.docx` templates.
3. Template metadata is stored in Azure SQL.
4. Template files are stored in Azure Blob Storage.
5. Each template supports immutable versions.
6. Users can define and retrieve placeholders.
7. The service can parse placeholders from uploaded Word documents.
8. Template validation detects malformed or unsupported placeholders.
9. A template can be activated or deactivated.
10. Only one version can be the active version for a template.
11. Template access is organization/tenant scoped.
12. Template changes generate domain/integration events.
13. All important state changes are auditable.
14. APIs return consistent ProblemDetails responses.
15. Optimistic concurrency protects metadata updates.
16. Automated unit, integration, API, contract, and architecture tests are implemented.
17. The service is ready to run locally through Docker and deploy through the existing CI/CD infrastructure.

---

# 3. Scope

## 3.1 In Scope

- Template CRUD lifecycle
- Template version lifecycle
- Word document upload
- Blob storage integration
- Placeholder extraction
- Placeholder metadata
- Template validation
- Template activation/deactivation
- Tenant isolation
- Authorization
- Audit events
- Service Bus integration
- OpenAPI/Scalar documentation
- Health checks
- Structured logging
- Distributed tracing
- Automated tests

## 3.2 Out of Scope

The following are intentionally deferred:

- Document generation
- Word-to-PDF conversion
- Workflow execution
- Approval management
- Digital signatures
- OCR
- AI template generation
- Bulk document generation
- Template visual designer
- External SharePoint integration
- Customer-specific document data binding

These belong to later phases.

---

# 4. Relationship With Phase 4

Phase 4 established the platform infrastructure and Identity/Organization foundations.

Template Service must consume those capabilities rather than recreate them.

## Phase 4 capabilities consumed

| Capability | Template Service Usage |
|---|---|
| Authentication | Identify current user |
| Organization/Tenant | Scope all templates |
| Authorization | RBAC and policy checks |
| User identity | CreatedBy/UpdatedBy |
| Organization membership | Access control |
| Azure SQL infrastructure | Template metadata |
| Blob Storage foundation | Template binaries |
| Service Bus foundation | Template events |
| Key Vault | Secrets/configuration |
| Observability | Logs/traces/metrics |
| Exception handling | ProblemDetails |
| API conventions | Versioning/OpenAPI |
| CI/CD | Build/test/deploy |
| Docker | Local containerization |

Template Service must not introduce a second identity or organization model.

---

# 5. Domain Boundary

The Template Service owns:

```text
Template
TemplateVersion
TemplatePlaceholder
TemplateValidationResult
TemplateStatus
TemplateVersionStatus
```

It does not own:

```text
User
Organization
Document
Workflow
Approval
Signature
Notification
AuditLog
```

Other services may reference Template IDs, but Template Service remains the source of truth for template lifecycle.

---

# 6. Service Location

Recommended structure:

```text
src/
└── Services/
    └── Template/
        ├── Edp.Template.Api/
        ├── Edp.Template.Application/
        ├── Edp.Template.Domain/
        └── Edp.Template.Infrastructure/
```

Tests:

```text
tests/
└── Services/
    └── Template/
        ├── Edp.Template.UnitTests/
        ├── Edp.Template.IntegrationTests/
        ├── Edp.Template.ApiTests/
        ├── Edp.Template.ContractTests/
        └── Edp.Template.ArchitectureTests/
```

Shared components remain under:

```text
src/Shared/
```

The Template Service must not place Template-specific business logic inside Shared projects.

---

# 7. Project Responsibilities

## 7.1 Edp.Template.Api

Responsibilities:

- HTTP endpoints
- API versioning
- Authentication/authorization configuration
- Request/response models
- ProblemDetails
- OpenAPI/Scalar
- Middleware/pipeline integration
- Health endpoints
- Dependency injection composition

The API layer must remain thin.

It must not contain:

- EF Core queries
- Business rules
- Blob implementation
- Event publishing logic
- Placeholder parsing algorithms

---

## 7.2 Edp.Template.Application

Responsibilities:

- Use cases
- Commands
- Queries
- Handlers
- DTOs
- Validators
- Authorization checks
- Application interfaces
- Transaction orchestration

Recommended vertical slices:

```text
Application/
├── Templates/
│   ├── Create/
│   ├── Get/
│   ├── List/
│   ├── Update/
│   ├── Delete/
│   ├── UploadVersion/
│   ├── GetVersions/
│   ├── GetPlaceholders/
│   ├── Validate/
│   ├── Activate/
│   └── Deactivate/
└── Common/
```

---

## 7.3 Edp.Template.Domain

Responsibilities:

- Entities
- Value Objects
- Enums
- Domain events
- Business rules
- Domain exceptions
- Aggregates

The Domain project must have no dependency on:

- ASP.NET Core
- EF Core
- Azure SDKs
- Service Bus SDK
- Blob SDK
- Application project

---

## 7.4 Edp.Template.Infrastructure

Responsibilities:

- EF Core
- SQL persistence
- Blob Storage
- Word/Open XML parsing
- Service Bus publishing
- Configuration
- Repository implementations
- Outbox implementation if enabled
- Infrastructure observability

Recommended structure:

```text
Infrastructure/
├── Persistence/
│   ├── TemplateDbContext.cs
│   ├── Configurations/
│   ├── Migrations/
│   └── Repositories/
├── Storage/
│   ├── BlobTemplateStorage.cs
│   └── BlobPathBuilder.cs
├── Parsing/
│   ├── ITemplateParser.cs
│   └── OpenXmlTemplateParser.cs
├── Messaging/
│   ├── TemplateEventPublisher.cs
│   └── Contracts/
└── DependencyInjection.cs
```

---

# 8. Domain Model

## 8.1 Template Aggregate

```text
Template
 ├── TemplateId
 ├── OrganizationId
 ├── Name
 ├── Code
 ├── Description
 ├── Status
 ├── CurrentVersionId
 ├── CreatedBy
 ├── CreatedAtUtc
 ├── UpdatedBy
 ├── UpdatedAtUtc
 ├── RowVersion
 └── Versions
```

The Template is the aggregate root.

---

# 9. Template Entity

Recommended properties:

```csharp
TemplateId
OrganizationId
Name
Code
Description
Status
CurrentVersionId
CreatedBy
CreatedAtUtc
UpdatedBy
UpdatedAtUtc
RowVersion
```

## Rules

- `TemplateId` is a GUID.
- `OrganizationId` is mandatory.
- `Name` is mandatory.
- `Code` is mandatory and organization-unique.
- `Description` is optional.
- Status defaults to `Draft`.
- `CreatedAtUtc` and `UpdatedAtUtc` use UTC.
- CreatedBy and UpdatedBy come from authenticated identity.
- CurrentVersionId can be null until a version is activated.

---

# 10. Template Status

```text
Draft
Active
Inactive
Archived
```

Recommended lifecycle:

```text
Draft
  |
  v
Active
  |
  v
Inactive
  |
  v
Archived
```

A later implementation may allow reactivation of inactive templates according to business rules.

---

# 11. TemplateVersion Entity

```text
TemplateVersion
 ├── TemplateVersionId
 ├── TemplateId
 ├── VersionNumber
 ├── FileName
 ├── ContentType
 ├── FileSize
 ├── BlobContainer
 ├── BlobPath
 ├── FileHash
 ├── Status
 ├── ValidationStatus
 ├── CreatedBy
 ├── CreatedAtUtc
 └── RowVersion
```

## Version Rules

- Versions are immutable after creation.
- Version numbers are sequential per template.
- A version cannot be overwritten.
- Each version has exactly one stored binary.
- The SHA-256 hash should be persisted.
- The blob path must not depend on a mutable display name.
- Only a valid version may become active.

---

# 12. Version Numbering

Use integer version numbers:

```text
1
2
3
4
```

Do not store semantic versions such as:

```text
v1.0.0
v1.1.0
```

Semantic versioning can be introduced later if required.

---

# 13. Placeholder Model

A placeholder represents a dynamic value expected during document generation.

Examples:

```text
{{CustomerName}}
{{InvoiceDate}}
{{Address}}
{{ContractNumber}}
```

Recommended entity:

```text
TemplatePlaceholder
 ├── TemplatePlaceholderId
 ├── TemplateVersionId
 ├── Name
 ├── DataType
 ├── IsRequired
 ├── DefaultValue
 ├── Description
 ├── Format
 └── CreatedAtUtc
```

---

# 14. Supported Placeholder Data Types

Initial supported types:

```text
String
Integer
Decimal
Date
DateTime
Boolean
Currency
Email
Phone
```

The implementation must be extensible so additional types can be introduced without redesigning the aggregate.

---

# 15. Placeholder Naming Rules

Recommended regex:

```regex
^\{\{[A-Za-z][A-Za-z0-9_]*\}\}$
```

Valid:

```text
{{CustomerName}}
{{InvoiceDate}}
{{ContractNumber}}
{{Customer_Address}}
```

Invalid:

```text
{{ CustomerName }}
{{123Customer}}
{{Customer-Name}}
{CustomerName}
[[CustomerName]]
```

Placeholder matching should be case-sensitive unless the platform explicitly chooses otherwise.

---

# 16. Placeholder Parsing

Use the Open XML SDK for `.docx` parsing.

The parser must inspect relevant Word document content, including:

- Paragraph text
- Runs
- Tables
- Table cells
- Headers
- Footers

The parser should account for placeholders split across multiple Word runs.

Example:

```text
Run 1: {{Cust
Run 2: omerName}}
```

must be interpreted as:

```text
{{CustomerName}}
```

The parser must return normalized placeholder definitions.

---

# 17. Template File Rules

Initial supported file type:

```text
application/vnd.openxmlformats-officedocument.wordprocessingml.document
```

Extension:

```text
.docx
```

Recommended configurable maximum file size:

```text
10 MB
```

The maximum must be configuration-driven.

Reject:

- `.exe`
- `.zip`
- `.pdf`
- `.doc`
- unknown extensions
- invalid MIME types
- corrupted DOCX files

---

# 18. Blob Storage Design

Azure Blob Storage stores binary template files.

SQL stores metadata only.

Recommended blob path:

```text
organizations/{organizationId}/templates/{templateId}/versions/{versionId}/{fileName}
```

Example:

```text
organizations/
    2d9.../
        templates/
            8ab.../
                versions/
                    4f1.../
                        contract-template.docx
```

Never store the raw document binary in Azure SQL.

---

# 19. Blob Storage Interface

Application/domain code should depend on an abstraction:

```csharp
public interface ITemplateStorage
{
    Task<TemplateStorageResult> UploadAsync(
        TemplateStorageRequest request,
        CancellationToken cancellationToken);

    Task<Stream> DownloadAsync(
        string blobPath,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        string blobPath,
        CancellationToken cancellationToken);
}
```

Infrastructure implements the Azure Blob SDK integration.

---

# 20. Database Design

Recommended tables:

```text
Templates
TemplateVersions
TemplatePlaceholders
TemplateValidationResults
```

Optional future tables:

```text
TemplateTags
TemplatePermissions
TemplateDependencies
```

---

# 21. Templates Table

Suggested columns:

```text
TemplateId UNIQUEIDENTIFIER PK
OrganizationId UNIQUEIDENTIFIER NOT NULL
Name NVARCHAR(200) NOT NULL
Code NVARCHAR(100) NOT NULL
Description NVARCHAR(1000) NULL
Status INT NOT NULL
CurrentVersionId UNIQUEIDENTIFIER NULL
CreatedBy UNIQUEIDENTIFIER NOT NULL
CreatedAtUtc DATETIME2 NOT NULL
UpdatedBy UNIQUEIDENTIFIER NULL
UpdatedAtUtc DATETIME2 NULL
RowVersion ROWVERSION
```

Unique index:

```text
OrganizationId + Code
```

---

# 22. TemplateVersions Table

Suggested columns:

```text
TemplateVersionId UNIQUEIDENTIFIER PK
TemplateId UNIQUEIDENTIFIER NOT NULL
VersionNumber INT NOT NULL
FileName NVARCHAR(255) NOT NULL
ContentType NVARCHAR(200) NOT NULL
FileSize BIGINT NOT NULL
BlobContainer NVARCHAR(100) NOT NULL
BlobPath NVARCHAR(1000) NOT NULL
FileHash NVARCHAR(128) NOT NULL
Status INT NOT NULL
ValidationStatus INT NOT NULL
CreatedBy UNIQUEIDENTIFIER NOT NULL
CreatedAtUtc DATETIME2 NOT NULL
RowVersion ROWVERSION
```

Unique index:

```text
TemplateId + VersionNumber
```

---

# 23. TemplatePlaceholders Table

Suggested columns:

```text
TemplatePlaceholderId UNIQUEIDENTIFIER PK
TemplateVersionId UNIQUEIDENTIFIER NOT NULL
Name NVARCHAR(200) NOT NULL
DataType INT NOT NULL
IsRequired BIT NOT NULL
DefaultValue NVARCHAR(MAX) NULL
Description NVARCHAR(1000) NULL
Format NVARCHAR(200) NULL
CreatedAtUtc DATETIME2 NOT NULL
```

Unique index:

```text
TemplateVersionId + Name
```

---

# 24. Validation Results

Validation results should capture:

```text
TemplateValidationResultId
TemplateVersionId
ValidationStatus
ErrorCode
Severity
Message
Location
CreatedAtUtc
```

Severity:

```text
Info
Warning
Error
```

Validation status:

```text
NotValidated
Valid
Invalid
```

---

# 25. API Design

Base route:

```text
/api/v1/templates
```

---

## 25.1 Create Template

```http
POST /api/v1/templates
```

Request:

```json
{
  "name": "Customer Contract",
  "code": "CUSTOMER_CONTRACT",
  "description": "Standard customer contract template"
}
```

Response:

```http
201 Created
```

```json
{
  "templateId": "guid",
  "name": "Customer Contract",
  "code": "CUSTOMER_CONTRACT",
  "status": "Draft"
}
```

---

# 26. Get Templates

```http
GET /api/v1/templates
```

Supported query parameters:

```text
page
pageSize
search
status
sortBy
sortDirection
```

Example:

```http
GET /api/v1/templates?page=1&pageSize=20&status=Active
```

Response must be paginated.

---

# 27. Get Template

```http
GET /api/v1/templates/{templateId}
```

Returns:

- metadata
- status
- current version
- timestamps
- creator/updater
- placeholder summary

---

# 28. Update Template

```http
PUT /api/v1/templates/{templateId}
```

Request:

```json
{
  "name": "Customer Contract Updated",
  "description": "Updated description",
  "rowVersion": "base64-value"
}
```

Only mutable metadata should be updated.

Do not update template binaries through this endpoint.

---

# 29. Upload Template Version

```http
POST /api/v1/templates/{templateId}/versions
```

Content type:

```text
multipart/form-data
```

Fields:

```text
file
```

Processing:

```text
Receive file
    |
Validate file
    |
Create version
    |
Upload Blob
    |
Parse placeholders
    |
Validate template
    |
Persist metadata
    |
Publish event
```

Transaction/outbox behavior must ensure metadata and event publication are reliable.

---

# 30. Get Versions

```http
GET /api/v1/templates/{templateId}/versions
```

Returns:

```json
[
  {
    "templateVersionId": "guid",
    "versionNumber": 2,
    "fileName": "contract.docx",
    "validationStatus": "Valid",
    "createdAtUtc": "2026-08-09T00:00:00Z"
  }
]
```

---

# 31. Get Version

```http
GET /api/v1/templates/{templateId}/versions/{versionId}
```

Returns version metadata and placeholder definitions.

---

# 32. Download Version

```http
GET /api/v1/templates/{templateId}/versions/{versionId}/download
```

The service should stream the file instead of loading the complete file into memory.

---

# 33. Get Placeholders

```http
GET /api/v1/templates/{templateId}/versions/{versionId}/placeholders
```

Example:

```json
[
  {
    "name": "CustomerName",
    "dataType": "String",
    "isRequired": true
  },
  {
    "name": "InvoiceDate",
    "dataType": "Date",
    "isRequired": true
  }
]
```

---

# 34. Validate Template

```http
POST /api/v1/templates/{templateId}/versions/{versionId}/validate
```

Response:

```json
{
  "status": "Valid",
  "errors": [],
  "warnings": []
}
```

---

# 35. Activate Template

```http
POST /api/v1/templates/{templateId}/versions/{versionId}/activate
```

Rules:

1. Version must exist.
2. Version must belong to the template.
3. Version must belong to current organization.
4. Version must pass validation.
5. Previous active version becomes inactive.
6. CurrentVersionId is updated.
7. Template status becomes Active.
8. Event is published.

---

# 36. Deactivate Template

```http
POST /api/v1/templates/{templateId}/deactivate
```

The template remains available historically but cannot be used for new document generation.

---

# 37. Delete/Archive Template

Prefer archive over hard delete:

```http
POST /api/v1/templates/{templateId}/archive
```

Historical template versions should remain available for audit and document traceability.

Hard deletion should not be part of MVP.

---

# 38. Authorization

Every Template Service request must enforce:

```text
Authenticated User
        |
        v
Organization Membership
        |
        v
Role/Permission
        |
        v
Template Organization Scope
```

Recommended permissions:

```text
Template.Read
Template.Create
Template.Update
Template.Upload
Template.Validate
Template.Activate
Template.Deactivate
Template.Archive
```

Example roles:

```text
OrganizationAdmin
TemplateManager
TemplateContributor
Viewer
```

Role definitions remain owned by the Identity/Organization foundation.

---

# 39. Tenant Isolation

Every query must include:

```text
OrganizationId == CurrentOrganizationId
```

Never accept organization ID from the client as the authoritative tenant boundary.

Organization context must come from authenticated claims/context established by Phase 4.

Add architecture tests to prevent accidental cross-tenant queries.

---

# 40. Concurrency

Use optimistic concurrency through SQL `rowversion`.

Update behavior:

```text
Client RowVersion
       |
       v
Database RowVersion
       |
 mismatch
       |
       v
409 Conflict
```

ProblemDetails should communicate that the resource was modified by another user.

---

# 41. Events

Template Service publishes integration events.

Required events:

```text
TemplateCreated
TemplateUpdated
TemplateVersionCreated
TemplateValidated
TemplateActivated
TemplateDeactivated
TemplateArchived
```

---

# 42. TemplateCreated Event

Example:

```json
{
  "eventId": "guid",
  "eventType": "TemplateCreated",
  "occurredAtUtc": "2026-08-09T00:00:00Z",
  "organizationId": "guid",
  "templateId": "guid",
  "templateCode": "CUSTOMER_CONTRACT",
  "createdBy": "guid"
}
```

Events should include correlation and causation identifiers where supported by the platform messaging standard.

---

# 43. TemplateVersionCreated Event

```json
{
  "eventId": "guid",
  "eventType": "TemplateVersionCreated",
  "occurredAtUtc": "2026-08-09T00:00:00Z",
  "organizationId": "guid",
  "templateId": "guid",
  "templateVersionId": "guid",
  "versionNumber": 2,
  "createdBy": "guid"
}
```

Do not include sensitive document content in events.

---

# 44. Messaging Reliability

Preferred pattern:

```text
Database Transaction
       |
       +--> Template metadata
       |
       +--> Outbox Event
                  |
                  v
             Publisher
                  |
                  v
            Azure Service Bus
```

Avoid:

```text
SQL commit
   |
   v
Service Bus publish
```

without reliability handling.

If the shared Phase 4 platform already provides an Outbox implementation, Template Service must reuse it.

---

# 45. Audit

Template Service must emit audit information for:

```text
Create
Update
Upload Version
Validate
Activate
Deactivate
Archive
```

Audit information should include:

```text
OrganizationId
UserId
Action
EntityType
EntityId
Timestamp
CorrelationId
Result
```

The service should integrate with the shared audit mechanism rather than create an independent audit framework.

---

# 46. Validation Pipeline

Recommended processing pipeline:

```text
Upload
  |
  v
Request Validation
  |
  v
File Extension Validation
  |
  v
MIME Validation
  |
  v
File Size Validation
  |
  v
DOCX Integrity Validation
  |
  v
Placeholder Extraction
  |
  v
Placeholder Syntax Validation
  |
  v
Business Validation
  |
  v
Persist Validation Results
```

---

# 47. Validation Rules

At minimum validate:

- File exists
- File size is within limit
- Extension is `.docx`
- MIME type is supported
- File is a valid ZIP/Open XML package
- Document can be opened by Open XML SDK
- Placeholders are syntactically valid
- Placeholder names are unique
- Supported data types are used
- Required metadata exists
- No unsupported placeholder syntax exists

---

# 48. Error Handling

Use RFC 7807 ProblemDetails.

Example:

```json
{
  "type": "https://errors.enterprise-document-platform/template-invalid",
  "title": "Template validation failed",
  "status": 422,
  "detail": "The uploaded template contains invalid placeholders.",
  "instance": "/api/v1/templates/...",
  "traceId": "..."
}
```

Recommended status codes:

```text
400 Bad Request
401 Unauthorized
403 Forbidden
404 Not Found
409 Conflict
413 Payload Too Large
422 Unprocessable Entity
500 Internal Server Error
```

---

# 49. Security Requirements

## Upload Security

- Restrict extensions.
- Validate MIME type.
- Validate Open XML structure.
- Enforce file-size limits.
- Never execute uploaded content.
- Store files outside application filesystem.
- Use private Blob containers.
- Do not expose storage account keys to clients.
- Use Managed Identity where supported.
- Generate controlled download responses.

## Data Security

- Tenant isolation.
- Authorization on every endpoint.
- No sensitive content in logs.
- No document binary in application logs.
- No secrets in source control.

---

# 50. Configuration

Recommended configuration:

```json
{
  "TemplateService": {
    "MaxFileSizeBytes": 10485760,
    "AllowedExtensions": [".docx"],
    "AllowedContentTypes": [
      "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    ]
  }
}
```

Blob configuration should reference the shared platform configuration model.

Secrets must come from Azure Key Vault or the environment configuration mechanism established in Phase 4.

---

# 51. Dependency Injection

Create:

```csharp
services.AddTemplateApplication();
services.AddTemplateInfrastructure(configuration);
```

Infrastructure registration should include:

```text
DbContext
Repositories
Blob Storage
Template Parser
Event Publisher
Outbox
Validators
```

Do not register infrastructure implementations in API controllers.

---

# 52. EF Core

Use EF Core migrations.

Recommended:

```text
dotnet ef migrations add InitialTemplateSchema
dotnet ef database update
```

Migration ownership:

```text
Edp.Template.Infrastructure
```

Do not allow runtime production applications to automatically modify the database schema.

---

# 53. Repository Strategy

Prefer aggregate-oriented repositories.

Example:

```csharp
ITemplateRepository
ITemplateVersionRepository
```

However, simple read-only queries may use optimized query handlers directly through an abstraction over the DbContext.

Avoid unnecessary generic repositories such as:

```csharp
IGenericRepository<T>
```

because they hide domain-specific behavior and often become an abstraction leak.

---

# 54. Application Use Cases

Implement these use cases first:

```text
CreateTemplate
GetTemplate
ListTemplates
UpdateTemplate
UploadTemplateVersion
GetTemplateVersions
GetTemplateVersion
GetPlaceholders
ValidateTemplate
ActivateTemplate
DeactivateTemplate
ArchiveTemplate
DownloadTemplateVersion
```

---

# 55. Recommended Implementation Order

## Step 1 — Domain

Create:

```text
Template
TemplateVersion
TemplatePlaceholder
TemplateStatus
TemplateVersionStatus
ValidationStatus
Domain Events
```

## Step 2 — Persistence

Create:

```text
TemplateDbContext
EF Configurations
Migrations
Repositories
```

## Step 3 — Blob Storage

Implement:

```text
ITemplateStorage
BlobTemplateStorage
BlobPathBuilder
```

## Step 4 — Parser

Implement:

```text
ITemplateParser
OpenXmlTemplateParser
PlaceholderExtractor
```

## Step 5 — Application

Implement commands, queries, validators, handlers.

## Step 6 — API

Expose REST endpoints.

## Step 7 — Messaging

Implement integration events and outbox/publisher integration.

## Step 8 — Audit

Integrate shared audit functionality.

## Step 9 — Tests

Implement all test categories.

## Step 10 — Docker/CI

Integrate the service with Phase 3/4 container and pipeline conventions.

---

# 56. Testing Strategy

## Unit Tests

Test:

- Template creation rules
- Template status transitions
- Version numbering
- Placeholder validation
- Placeholder normalization
- Activation rules
- Deactivation rules
- Domain events
- Validators

Target:

```text
80%+ business logic coverage
```

---

# 57. Integration Tests

Use a real or containerized SQL Server/Azure SQL-compatible test database where practical.

Test:

- EF mappings
- Tenant filters/scoping
- Unique indexes
- RowVersion concurrency
- Transactions
- Repository behavior
- Outbox persistence

Blob integration should be tested with an appropriate storage emulator/test infrastructure where available.

---

# 58. API Tests

Test:

```text
POST /templates
GET /templates
GET /templates/{id}
PUT /templates/{id}
POST /templates/{id}/versions
GET /templates/{id}/versions
GET /templates/{id}/versions/{versionId}
GET /templates/{id}/versions/{versionId}/placeholders
POST /templates/{id}/versions/{versionId}/validate
POST /templates/{id}/versions/{versionId}/activate
POST /templates/{id}/deactivate
POST /templates/{id}/archive
```

Verify:

- Authentication
- Authorization
- Tenant isolation
- Validation
- HTTP status codes
- ProblemDetails
- Concurrency

---

# 59. Contract Tests

Verify event contracts:

```text
TemplateCreated
TemplateUpdated
TemplateVersionCreated
TemplateValidated
TemplateActivated
TemplateDeactivated
TemplateArchived
```

Event schema changes must be backward-compatible.

---

# 60. Architecture Tests

Enforce:

```text
Domain -> no Infrastructure dependency
Domain -> no API dependency
Application -> no API dependency
API -> Application
Infrastructure -> Application/Domain
```

Also enforce:

- Controllers/endpoints do not access DbContext directly.
- Domain does not reference Azure SDK.
- Template Service does not reference unrelated service implementations.
- Organization scope is present in persistence operations.

---

# 61. Observability

Use the shared Phase 4 observability stack.

Required:

```text
Serilog
OpenTelemetry
Application Insights
Correlation ID
Trace ID
Structured Logging
Health Checks
```

Useful metrics:

```text
template.create.count
template.upload.count
template.validation.count
template.validation.failure.count
template.activation.count
template.upload.duration
template.validation.duration
template.download.count
```

---

# 62. Logging Rules

Log:

```text
TemplateId
TemplateVersionId
OrganizationId
UserId
CorrelationId
Operation
Duration
Result
```

Do not log:

```text
Document binary
Full document text
Sensitive placeholder values
Access tokens
Connection strings
Secrets
```

---

# 63. Health Checks

Required endpoints:

```text
/health
/health/ready
/health/live
```

Readiness should verify dependencies required for serving requests, such as:

```text
SQL
Blob Storage
Service Bus
```

Liveness should not depend on every external service.

---

# 64. API Documentation

Scalar/OpenAPI must document:

- All endpoints
- Request schemas
- Response schemas
- Authorization
- Error responses
- Multipart upload
- Pagination
- Example requests/responses

The service must follow the .NET 10 OpenAPI/Scalar conventions already established in the solution.

---

# 65. Docker

Create:

```text
src/Services/Template/Dockerfile
```

The image must:

- Build in Release mode.
- Run as a non-root user where supported.
- Use environment-based configuration.
- Expose the configured HTTP port.
- Include health check integration.
- Not contain secrets.

Local Docker Compose should integrate:

```text
Template API
SQL Server
Azurite
Service Bus emulator/test equivalent where applicable
```

Reuse Phase 3/4 infrastructure rather than creating duplicated platform containers.

---

# 66. CI/CD

Pipeline stages:

```text
Restore
  |
Build
  |
Unit Test
  |
Integration Test
  |
API Test
  |
Architecture Test
  |
Code Coverage
  |
Static Analysis
  |
Docker Build
  |
Security Scan
  |
Publish Artifact
```

Deployment environments:

```text
Development
Test
Staging
Production
```

Deployment should use the existing Phase 4 infrastructure and IaC conventions.

---

# 67. Database Indexes

Recommended indexes:

```text
IX_Templates_OrganizationId
UX_Templates_OrganizationId_Code

IX_TemplateVersions_TemplateId
UX_TemplateVersions_TemplateId_VersionNumber

IX_TemplatePlaceholders_TemplateVersionId
UX_TemplatePlaceholders_TemplateVersionId_Name
```

Query plans should be reviewed after implementation.

---

# 68. Pagination

Default:

```text
page = 1
pageSize = 20
```

Maximum:

```text
pageSize = 100
```

Response:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalCount": 0,
  "totalPages": 0
}
```

---

# 69. Idempotency

For operations that may be retried, support idempotency where required.

Particularly:

```text
UploadVersion
ActivateVersion
```

Do not create duplicate versions because of network retries.

A future platform-wide idempotency mechanism should be reused when available.

---

# 70. Transaction Boundaries

Template creation:

```text
Validate
  |
Create SQL record
  |
Create audit/outbox records
  |
Commit
```

Version upload:

```text
Validate file
  |
Upload blob
  |
Create SQL metadata
  |
Create placeholders
  |
Create validation results
  |
Create outbox event
  |
Commit
```

Failure handling must avoid orphaned blobs. The infrastructure should provide cleanup/reconciliation behavior for failures occurring between blob upload and database commit.

---

# 71. API Security Example

Conceptually:

```csharp
[Authorize(Policy = "Template.Read")]
```

For mutation:

```csharp
[Authorize(Policy = "Template.Upload")]
```

Policies should be defined centrally according to the Identity/Organization foundation established in Phase 4.

---

# 72. Definition of Done

Phase 5 is complete when:

### Domain

- [ ] Template aggregate implemented.
- [ ] Version aggregate behavior implemented.
- [ ] Placeholder model implemented.
- [ ] Lifecycle rules implemented.
- [ ] Domain events implemented.

### Database

- [ ] Tables created.
- [ ] Relationships configured.
- [ ] Indexes configured.
- [ ] RowVersion configured.
- [ ] Migrations created.

### Storage

- [ ] Blob upload implemented.
- [ ] Blob download implemented.
- [ ] Blob path convention implemented.
- [ ] Hash calculation implemented.
- [ ] Private storage enforced.

### Parsing

- [ ] DOCX validation implemented.
- [ ] Placeholder extraction implemented.
- [ ] Cross-run placeholder handling implemented.
- [ ] Header/footer/table parsing implemented.

### API

- [ ] Create endpoint.
- [ ] List endpoint.
- [ ] Get endpoint.
- [ ] Update endpoint.
- [ ] Version upload endpoint.
- [ ] Version list endpoint.
- [ ] Version detail endpoint.
- [ ] Download endpoint.
- [ ] Placeholder endpoint.
- [ ] Validation endpoint.
- [ ] Activation endpoint.
- [ ] Deactivation endpoint.
- [ ] Archive endpoint.

### Security

- [ ] Authentication.
- [ ] Authorization.
- [ ] Organization isolation.
- [ ] Permission checks.
- [ ] Secure upload.

### Messaging

- [ ] Events defined.
- [ ] Event contracts implemented.
- [ ] Outbox integrated.
- [ ] Service Bus publisher integrated.

### Audit

- [ ] Create audit.
- [ ] Update audit.
- [ ] Upload audit.
- [ ] Validation audit.
- [ ] Activation audit.
- [ ] Deactivation audit.
- [ ] Archive audit.

### Observability

- [ ] Structured logging.
- [ ] Metrics.
- [ ] Tracing.
- [ ] Health checks.
- [ ] Correlation IDs.

### Testing

- [ ] Unit tests.
- [ ] Integration tests.
- [ ] API tests.
- [ ] Contract tests.
- [ ] Architecture tests.
- [ ] Tenant isolation tests.
- [ ] Concurrency tests.

### DevOps

- [ ] Docker image.
- [ ] CI pipeline.
- [ ] Test pipeline.
- [ ] Security scan.
- [ ] Deployment configuration.
- [ ] Environment configuration.

---

# 73. Expected Repository Structure After Phase 5

```text
EnterpriseDocumentPlatform/
│
├── src/
│   ├── Gateway/
│   │
│   ├── Services/
│   │   └── Template/
│   │       ├── Edp.Template.Api/
│   │       │   ├── Endpoints/
│   │       │   ├── Contracts/
│   │       │   ├── Extensions/
│   │       │   └── Program.cs
│   │       │
│   │       ├── Edp.Template.Application/
│   │       │   ├── Templates/
│   │       │   │   ├── Create/
│   │       │   │   ├── Get/
│   │       │   │   ├── List/
│   │       │   │   ├── Update/
│   │       │   │   ├── UploadVersion/
│   │       │   │   ├── GetVersions/
│   │       │   │   ├── GetPlaceholders/
│   │       │   │   ├── Validate/
│   │       │   │   ├── Activate/
│   │       │   │   ├── Deactivate/
│   │       │   │   └── Archive/
│   │       │   └── Common/
│   │       │
│   │       ├── Edp.Template.Domain/
│   │       │   ├── Entities/
│   │       │   ├── ValueObjects/
│   │       │   ├── Enums/
│   │       │   ├── Events/
│   │       │   └── Exceptions/
│   │       │
│   │       └── Edp.Template.Infrastructure/
│   │           ├── Persistence/
│   │           ├── Storage/
│   │           ├── Parsing/
│   │           ├── Messaging/
│   │           └── DependencyInjection.cs
│   │
│   ├── Shared/
│   │
│   └── Web/
│
├── tests/
│   └── Services/
│       └── Template/
│           ├── Edp.Template.UnitTests/
│           ├── Edp.Template.IntegrationTests/
│           ├── Edp.Template.ApiTests/
│           ├── Edp.Template.ContractTests/
│           └── Edp.Template.ArchitectureTests/
│
├── docs/
│   └── phases/
│       └── phase-05-template-service.md
│
├── infra/
├── pipelines/
├── scripts/
└── docker/
```

---

# 74. Phase 5 Deliverables

The following artifacts must exist after implementation:

```text
1. Template Service projects
2. Template domain model
3. SQL schema and EF migrations
4. Blob Storage integration
5. DOCX parser
6. Placeholder engine
7. Template validation engine
8. REST APIs
9. Authorization policies
10. Integration events
11. Outbox integration
12. Audit integration
13. Unit tests
14. Integration tests
15. API tests
16. Contract tests
17. Architecture tests
18. Docker configuration
19. CI/CD pipeline integration
20. API/OpenAPI documentation
21. Phase 5 technical documentation
```

---

# 75. Suggested Implementation Sequence

Implement Phase 5 in the following order:

```text
Phase 5.1
Domain Model

        ↓

Phase 5.2
Database + EF Core

        ↓

Phase 5.3
Blob Storage

        ↓

Phase 5.4
DOCX/Open XML Parser

        ↓

Phase 5.5
Placeholder Engine

        ↓

Phase 5.6
Template Validation

        ↓

Phase 5.7
Application Use Cases

        ↓

Phase 5.8
REST API

        ↓

Phase 5.9
Authorization + Tenant Isolation

        ↓

Phase 5.10
Audit + Events + Outbox

        ↓

Phase 5.11
Observability

        ↓

Phase 5.12
Automated Tests

        ↓

Phase 5.13
Docker + CI/CD

        ↓

Phase 5.14
End-to-End Verification
```

---

# 76. Phase 5 Success Criteria

A business user should be able to perform this complete flow:

```text
Login
  |
  v
Select Organization
  |
  v
Create Template
  |
  v
Upload Word Template
  |
  v
System Parses DOCX
  |
  v
System Detects Placeholders
  |
  v
System Validates Template
  |
  v
User Reviews Version
  |
  v
User Activates Version
  |
  v
Template Becomes Active
  |
  v
TemplateActivated Event
  |
  v
Audit Record Created
```

The resulting template must be available to the future **Document Service** as a trusted, versioned template definition.

---

# 77. Next Phase

After Phase 5, proceed to:

**Phase 6 — Document Service**

Phase 6 will consume the Template Service to implement:

- Document creation
- Placeholder data binding
- Word document generation
- PDF generation
- Document metadata
- Document versioning
- Document storage
- Document lifecycle
- Document events
- Integration with Workflow

The Template Service should therefore expose stable APIs and event contracts that Phase 6 can consume without coupling to Template Service internals.
