# Phase 6 — Document Service Requirements

**Project:** Enterprise Document Automation Platform  
**Phase:** 6  
**Service:** Document Service  
**Status:** Planned  
**Technology:** .NET 10 / C# / ASP.NET Core / Azure  
**Database:** Azure SQL  
**Storage:** Azure Blob Storage  
**Messaging:** Azure Service Bus  

---

## 1. Purpose

The Document Service is responsible for taking an activated template from the Template Service, resolving placeholders using business data, generating final documents, converting them into supported formats such as DOCX and PDF, storing generated artifacts, and maintaining document metadata and lifecycle information.

The Document Service sits between the Template Service, Storage Service, Workflow Service, Notification Service, and Audit infrastructure.

### High-Level Flow

```text
User / React
      |
      v
API Gateway
      |
      v
Document Service
      |
      +----> Template Service
      |         |
      |         +---- Template Metadata
      |         +---- Template Version
      |         +---- Placeholder Definitions
      |
      +----> Placeholder Engine
      |
      +----> Document Generator
      |         |
      |         +---- DOCX
      |         +---- PDF
      |
      +----> Storage Service / Azure Blob
      |
      +----> Workflow Service
      |
      +----> Notification Service
      |
      +----> Audit Service
      |
      +----> Azure Service Bus
```

---

# 2. Phase 6 Objectives

The objectives of this phase are:

1. Create the Document Service.
2. Generate documents from activated templates.
3. Resolve template placeholders using supplied business data.
4. Validate required placeholder values.
5. Support DOCX generation.
6. Support PDF generation.
7. Store generated documents securely.
8. Maintain document metadata.
9. Maintain document versions.
10. Support document lifecycle management.
11. Support document download.
12. Support document regeneration.
13. Establish bulk document generation capability.
14. Publish document-related domain events.
15. Integrate with Workflow Service.
16. Integrate with Audit Service.
17. Integrate with Notification Service.
18. Provide asynchronous generation for long-running jobs.
19. Provide generation status tracking.
20. Implement tenant/organization-level isolation.
21. Implement authorization.
22. Implement observability and operational monitoring.

---

# 3. Scope

## 3.1 In Scope

- Document creation.
- Document generation.
- Template resolution.
- Placeholder resolution.
- Placeholder validation.
- DOCX generation.
- PDF conversion.
- Document metadata.
- Document versioning.
- Document status management.
- Document download/streaming.
- Document regeneration.
- Document cancellation.
- Document archival.
- Document deletion according to retention rules.
- Generation job management.
- Bulk-generation foundation.
- Storage integration.
- Workflow integration.
- Audit integration.
- Notification integration.
- Domain events.
- Tenant isolation.
- Authorization.
- API versioning.
- Structured logging.
- Metrics.
- Distributed tracing.

## 3.2 Out of Scope

The following remain outside Document Service ownership.

### Template Service

- Template upload.
- Template editing.
- Template version creation.
- Placeholder CRUD.
- Template activation/deactivation.
- Template authoring.

### Workflow Service

- Approval configuration.
- Approval assignment.
- Approval decisions.
- Digital-signature workflow.

### Storage Service

- Storage provider management.
- Blob container lifecycle.
- Storage lifecycle policies.
- Storage infrastructure management.

### Identity/Organization

- Login.
- User registration.
- Organization management.
- Role administration.

---

# 4. Document Service Responsibilities

```text
Document Service
|
+-- Document Management
+-- Document Generation
+-- Placeholder Resolution
+-- Document Versioning
+-- Format Conversion
+-- Generation Job Management
+-- Document Lifecycle
+-- Storage Coordination
+-- Workflow Coordination
+-- Event Publishing
+-- Audit Integration
+-- Observability
```

---

# 5. Document Lifecycle

A generated document should have a clearly defined lifecycle.

```text
Requested
   |
   v
Queued
   |
   v
Generating
   |
   +------> Failed
   |
   v
Generated
   |
   v
Stored
   |
   v
Draft
   |
   v
InReview
   |
   v
Approved
   |
   v
Signed
   |
   v
Completed
   |
   v
Archived
```

## 5.1 Document Statuses

Recommended statuses:

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

The exact workflow states should be coordinated with Workflow Service.

---

# 6. Core Document Concepts

## 6.1 Document

Represents the logical business document.

Examples:

```text
Contract
Invoice
Purchase Order
Engagement Letter
Offer Letter
Statement
Certificate
```

## 6.2 Document Version

A document can contain multiple generated versions.

```text
Contract
 |
 +-- Version 1
 +-- Version 2
 +-- Version 3
```

Each version should retain:

- Version number.
- Generation timestamp.
- Generated-by user.
- Template ID.
- Template version ID.
- Input data reference/hash.
- Output files.
- Generation status.
- Document hash.
- Storage references.

---

# 7. Template-to-Document Relationship

Every generated document must retain the exact template version from which it was generated.

```text
Template
 |
 +-- Template Version 1
 +-- Template Version 2
 +-- Template Version 3
             |
             v
       Document Version
```

If Template Version 3 generated a document and Template Version 4 later becomes active, the existing document must continue referencing Version 3.

This is required for:

- Auditability.
- Legal traceability.
- Reproducibility.
- Compliance.
- Regeneration.

---

# 8. Placeholder Resolution

The Document Service consumes placeholder definitions managed by Template Service.

Example template:

```text
Dear {{CustomerName}},

Your contract number is {{ContractNumber}}.

The contract date is {{ContractDate}}.

Address:

{{Address}}
```

Input:

```json
{
  "CustomerName": "ABC Limited",
  "ContractNumber": "CNT-2026-00125",
  "ContractDate": "2026-08-17",
  "Address": "Mumbai, Maharashtra, India"
}
```

Generated output:

```text
Dear ABC Limited,

Your contract number is CNT-2026-00125.

The contract date is 17-Aug-2026.

Address:

Mumbai, Maharashtra, India
```

---

# 9. Placeholder Validation

Before generation, the service must validate supplied data against the placeholder definitions returned by Template Service.

Example:

```text
CustomerName
Type: String
Required: Yes

ContractNumber
Type: String
Required: Yes

ContractDate
Type: Date
Required: Yes

DiscountPercentage
Type: Decimal
Required: No
```

Invalid input:

```json
{
  "CustomerName": "ABC Limited"
}
```

Validation response:

```json
{
  "status": "ValidationFailed",
  "errors": [
    {
      "placeholder": "ContractNumber",
      "message": "Required placeholder value is missing."
    },
    {
      "placeholder": "ContractDate",
      "message": "Required placeholder value is missing."
    }
  ]
}
```

---

# 10. Supported Placeholder Types

Phase 6 should initially support:

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

Future types:

```text
Image
Table
Collection
Object
Conditional
Formula
Computed
QR Code
Barcode
Signature
```

---

# 11. Placeholder Formatting

The engine should support formatting instructions.

Example:

```text
{{InvoiceDate | format:"dd-MMM-yyyy"}}
```

Output:

```text
17-Aug-2026
```

Currency:

```text
{{Amount | currency:"INR"}}
```

Number:

```text
{{Amount | number:"2"}}
```

Formatting should be implemented through a controlled expression/formatting engine rather than arbitrary code execution.

---

# 12. Conditional Content

The architecture should support conditional blocks.

Example:

```text
{{#if IsCorporate}}

Corporate customer terms apply.

{{/if}}
```

Future expressions may include:

```text
{{#if DiscountPercentage > 0}}

Discount: {{DiscountPercentage}}%

{{/if}}
```

Conditional rendering may be limited in the first implementation, but the architecture must not prevent future expansion.

---

# 13. Repeating Data

The service should support collection-based placeholders.

Example:

```text
{{#each Items}}

{{ItemName}} | {{Quantity}} | {{Price}}

{{/each}}
```

Example input:

```json
{
  "Items": [
    {
      "ItemName": "Laptop",
      "Quantity": 2,
      "Price": 75000
    },
    {
      "ItemName": "Monitor",
      "Quantity": 2,
      "Price": 25000
    }
  ]
}
```

This is particularly important for:

- Invoices.
- Purchase orders.
- Statements.
- Reports.
- Quotes.

---

# 14. Document Generation Request

Recommended API:

```http
POST /api/v1/documents
```

Example request:

```json
{
  "templateId": "tpl_12345",
  "templateVersion": 3,
  "name": "Customer Contract - ABC Limited",
  "outputFormats": [
    "DOCX",
    "PDF"
  ],
  "data": {
    "CustomerName": "ABC Limited",
    "ContractNumber": "CNT-2026-00125",
    "ContractDate": "2026-08-17",
    "Address": "Mumbai, Maharashtra"
  }
}
```

Example response:

```json
{
  "documentId": "doc_12345",
  "status": "Queued",
  "message": "Document generation request accepted."
}
```

---

# 15. Synchronous vs Asynchronous Generation

## 15.1 Synchronous

Suitable for small/simple documents.

```text
HTTP Request
     |
     v
Document Service
     |
     v
Generate
     |
     v
Store
     |
     v
HTTP Response
```

## 15.2 Asynchronous

Preferred architecture for production document generation.

```text
POST /documents
       |
       v
Document Service
       |
       v
Service Bus
       |
       v
Document Generation Worker
       |
       +---- Template Service
       +---- Placeholder Engine
       +---- DOCX Generator
       +---- PDF Converter
       |
       v
Azure Blob Storage
       |
       v
DocumentGenerated Event
```

---

# 16. Generation Job

Every asynchronous request should create a generation job.

Recommended fields:

```text
JobId
DocumentId
DocumentVersionId
TemplateId
TemplateVersionId
OrganizationId
RequestedBy
Status
StartedAt
CompletedAt
ErrorCode
ErrorMessage
RetryCount
CorrelationId
```

Statuses:

```text
Queued
Processing
Completed
Failed
Cancelled
```

---

# 17. Generation Retry

Transient failures should be retried.

Recommended:

```text
Maximum retries: 3
Strategy: Exponential backoff
```

Validation failures must not be retried.

Examples of retryable errors:

```text
Temporary Blob Storage failure
Temporary Service Bus failure
Temporary Template Service timeout
Transient PDF conversion failure
```

---

# 18. DOCX Generation

The service should generate Word documents using the selected template.

Recommended technology:

```text
Open XML SDK
```

Responsibilities:

- Open template.
- Locate placeholders.
- Replace values.
- Preserve formatting.
- Preserve headers.
- Preserve footers.
- Preserve tables.
- Preserve styles.
- Preserve document properties.
- Support repeated sections where possible.
- Save generated DOCX.

The original template must never be modified.

---

# 19. PDF Generation

Recommended pipeline:

```text
Template
   |
   v
DOCX Generation
   |
   v
Generated DOCX
   |
   v
PDF Conversion
   |
   v
Generated PDF
```

PDF conversion must be abstracted:

```csharp
public interface IDocumentConverter
{
    Task<Stream> ConvertToPdfAsync(
        Stream source,
        CancellationToken cancellationToken);
}
```

This allows the conversion provider to be replaced without changing business logic.

---

# 20. Supported Output Formats

Phase 6:

```text
DOCX
PDF
```

Architecture should allow future:

```text
HTML
TXT
XLSX
PPTX
```

---

# 21. Document Storage

Generated files must not be stored directly inside Azure SQL.

Recommended:

```text
Azure Blob Storage
```

SQL stores metadata:

```text
DocumentId
FileId
FileName
ContentType
Size
BlobPath
Hash
```

Recommended storage structure:

```text
organizations/{organizationId}/
documents/{documentId}/
versions/{versionNumber}/
contract.docx
contract.pdf
```

---

# 22. Storage Security

Documents are enterprise data and must be protected.

Requirements:

- Private Blob containers.
- No public blob access.
- Managed Identity.
- Encryption at rest.
- HTTPS only.
- Short-lived download URLs where appropriate.
- Organization-level isolation.
- Authorization before download.
- Audit download operations.

---

# 23. Document Metadata

Recommended fields:

```text
DocumentId
OrganizationId
DocumentType
DocumentName
Description
TemplateId
CurrentTemplateVersionId
CurrentDocumentVersion
Status
CreatedBy
CreatedAt
UpdatedBy
UpdatedAt
ArchivedAt
DeletedAt
CorrelationId
ExternalReference
```

---

# 24. Document Version Metadata

```text
DocumentVersionId
DocumentId
VersionNumber
TemplateId
TemplateVersionId
GenerationJobId
GeneratedBy
GeneratedAt
Status
InputDataHash
DocumentHash
FileCount
CreatedAt
```

---

# 25. Document File Metadata

A document version may contain multiple output files.

```text
DocumentVersion
      |
      +-- DOCX
      +-- PDF
      +-- Preview
```

Recommended fields:

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

---

# 26. Document Hash

Every generated file should have a SHA-256 hash.

Example:

```text
SHA256:
9f8c4a................................
```

Hashing provides:

- Integrity verification.
- Auditability.
- Duplicate detection.
- Legal traceability.

---

# 27. Document APIs

## Create Document

```http
POST /api/v1/documents
```

## Get Document

```http
GET /api/v1/documents/{documentId}
```

## List Documents

```http
GET /api/v1/documents
```

## Get Document Version

```http
GET /api/v1/documents/{documentId}/versions/{version}
```

## Generate Document

```http
POST /api/v1/documents/{documentId}/generate
```

## Regenerate Document

```http
POST /api/v1/documents/{documentId}/regenerate
```

## Get Generation Status

```http
GET /api/v1/documents/{documentId}/generation-status
```

## Download DOCX

```http
GET /api/v1/documents/{documentId}/files/docx
```

## Download PDF

```http
GET /api/v1/documents/{documentId}/files/pdf
```

## Download Specific Version

```http
GET /api/v1/documents/{documentId}/versions/{version}/files/{fileType}
```

## Cancel Generation

```http
POST /api/v1/documents/{documentId}/cancel
```

## Archive

```http
POST /api/v1/documents/{documentId}/archive
```

---

# 28. API Filtering

Document listing should support:

```http
GET /api/v1/documents?status=Completed&templateId=tpl_123&createdFrom=2026-08-01&createdTo=2026-08-17&page=1&pageSize=25
```

Filters:

```text
Document Type
Status
Template
Created By
Created Date
Updated Date
External Reference
```

---

# 29. Pagination

Collection APIs should use pagination.

Example:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 25,
  "totalCount": 250,
  "totalPages": 10
}
```

Maximum recommended page size:

```text
100
```

---

# 30. Bulk Document Generation

Architecture should support:

```http
POST /api/v1/documents/bulk
```

Example:

```json
{
  "templateId": "tpl_invoice",
  "outputFormats": ["PDF"],
  "documents": [
    {
      "externalReference": "INV-001",
      "data": {}
    },
    {
      "externalReference": "INV-002",
      "data": {}
    }
  ]
}
```

Architecture:

```text
Bulk Request
     |
     v
Bulk Job
     |
     +---- Job 1
     +---- Job 2
     +---- Job 3
     +---- Job 4
     |
     v
Service Bus
```

Phase 6 should establish the foundation even if advanced bulk optimization is deferred.

---

# 31. External Data Support

The initial version accepts JSON data directly.

Future versions may support:

```text
CRM
ERP
REST APIs
Database
Azure Functions
Event-driven data
```

Possible architecture:

```text
Document Service
      |
      v
Data Provider
      |
      +---- API
      +---- Database
      +---- CRM
      +---- ERP
```

---

# 32. Template Service Integration

Document Service must never directly access Template Service's database.

Communication should occur through:

```text
Document Service
       |
       v
Template Service API
```

Required information:

```text
Template metadata
Template version
Template file
Placeholder definitions
Template validation information
```

---

# 33. Template Snapshot

Generation must be tied to a specific template version.

Example:

```text
Template ID:
tpl-123

Template Version:
5

Template File:
template-v5.docx
```

If Version 6 becomes active while Version 5 is being used by a generation job, the job continues using Version 5.

---

# 34. Workflow Integration

After successful generation:

```text
DocumentGenerated
        |
        v
Workflow Service
        |
        v
Start Workflow
```

Example:

```text
Document Generated
       |
       v
Draft
       |
       v
Review
       |
       v
Approval
       |
       v
Signature
       |
       v
Completed
```

Document Service must not own approval rules.

---

# 35. Events

The service should publish events through Azure Service Bus.

## DocumentRequested

```json
{
  "eventType": "DocumentRequested",
  "documentId": "doc_123",
  "organizationId": "org_123",
  "templateId": "tpl_123",
  "requestedBy": "user_123",
  "timestamp": "2026-08-17T10:00:00Z"
}
```

## DocumentGenerationStarted

```json
{
  "eventType": "DocumentGenerationStarted",
  "documentId": "doc_123",
  "jobId": "job_123"
}
```

## DocumentGenerated

```json
{
  "eventType": "DocumentGenerated",
  "documentId": "doc_123",
  "version": 1,
  "formats": [
    "DOCX",
    "PDF"
  ]
}
```

## DocumentGenerationFailed

```json
{
  "eventType": "DocumentGenerationFailed",
  "documentId": "doc_123",
  "jobId": "job_123",
  "errorCode": "PDF_CONVERSION_FAILED"
}
```

## DocumentArchived

```json
{
  "eventType": "DocumentArchived",
  "documentId": "doc_123"
}
```

---

# 36. Event Consumers

Document Service may consume events such as:

```text
TemplateActivated
TemplateVersionCreated
WorkflowCompleted
DocumentArchivedRequested
```

Only implement consumers required by actual business flows.

---

# 37. Audit Requirements

The following actions must be auditable:

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

Audit information:

```text
User
Organization
Action
DocumentId
DocumentVersion
Timestamp
IP Address
CorrelationId
Result
```

---

# 38. Authorization

Authorization must be organization-aware.

Example:

```text
User
 |
 +-- Organization A
       |
       +-- Document A
       +-- Document B
```

A user from Organization B must not access Organization A documents.

Recommended permissions:

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

---

# 39. Recommended Roles

## Organization Admin

```text
Create
Generate
Download
Regenerate
Archive
Delete
```

## Business User

```text
Create
Generate
Download
```

## Approver

```text
Read
Download
```

Approval/rejection remains owned by Workflow Service.

## Auditor

```text
Read
Download
Audit
```

---

# 40. Database Design

Recommended database:

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

---

# 41. Documents Table

```text
Documents
------------------------------------------------
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
RowVersion
```

Recommended indexes:

```text
OrganizationId
Status
TemplateId
CreatedBy
CreatedAt
ExternalReference
```

---

# 42. DocumentVersions Table

```text
DocumentVersions
------------------------------------------------
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

Unique constraint:

```text
DocumentId + VersionNumber
```

---

# 43. DocumentFiles Table

```text
DocumentFiles
------------------------------------------------
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

Unique constraint:

```text
DocumentVersionId + FileType
```

---

# 44. Generation Jobs Table

```text
DocumentGenerationJobs
------------------------------------------------
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

---

# 45. Recommended Project Structure

Based on the Phase 3–5 solution structure:

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

---

# 46. API Project

```text
Edp.Document.Api

Controllers/
    DocumentsController.cs
    DocumentGenerationController.cs

Middleware/

Configuration/

Extensions/

Program.cs
```

Responsibilities:

- HTTP APIs.
- Authentication.
- Authorization.
- API versioning.
- Exception handling.
- OpenAPI.
- Scalar.
- Health checks.

---

# 47. Application Project

```text
Edp.Document.Application

Commands/
    CreateDocument/
    GenerateDocument/
    RegenerateDocument/
    ArchiveDocument/
    CancelDocument/

Queries/
    GetDocument/
    GetDocuments/
    GetDocumentVersion/
    GetGenerationStatus/

Services/
    DocumentGenerationService
    PlaceholderResolutionService
    DocumentVersionService

Validators/
```

CQRS-style organization should remain consistent with earlier services.

---

# 48. Domain Project

```text
Edp.Document.Domain

Entities/
    Document
    DocumentVersion
    DocumentFile
    DocumentGenerationJob

Enums/
    DocumentStatus
    GenerationStatus
    DocumentFileType

ValueObjects/
    DocumentId
    DocumentVersionNumber

Events/
    DocumentCreated
    DocumentGenerated
    DocumentGenerationFailed
    DocumentArchived
```

The Domain project must not depend on:

```text
Azure SDK
Entity Framework
Blob Storage
Service Bus
HTTP
ASP.NET Core
```

---

# 49. Infrastructure Project

```text
Edp.Document.Infrastructure

Persistence/
    DocumentDbContext.cs
    Configurations/

Storage/
    BlobDocumentStorage.cs

Templates/
    TemplateServiceClient.cs

Generation/
    OpenXmlDocumentGenerator.cs
    PdfDocumentConverter.cs

Messaging/
    DocumentEventPublisher.cs

Jobs/
    DocumentGenerationWorker.cs

Services/
    HashService.cs
```

---

# 50. Contracts Project

```text
Edp.Document.Contracts

Requests/
Responses/
Events/
DTOs/
```

API and event contracts should not be coupled to domain entities.

---

# 51. Dependency Direction

```text
API
 |
 v
Application
 |
 v
Domain

Infrastructure
 |
 +----> Application
 +----> Domain
```

Never:

```text
Domain -> Infrastructure
Domain -> API
Domain -> EF Core
```

---

# 52. Background Worker

The service should include a background generation worker.

```text
Service Bus
    |
    v
DocumentGenerationWorker
    |
    v
GenerateDocumentHandler
```

Worker responsibilities:

1. Receive generation message.
2. Validate message.
3. Load generation job.
4. Retrieve template.
5. Retrieve placeholder definitions.
6. Validate input data.
7. Generate DOCX.
8. Convert to PDF.
9. Calculate hashes.
10. Upload files.
11. Update database.
12. Publish DocumentGenerated.
13. Handle failures.
14. Retry transient failures.

---

# 53. Idempotency

Generation must be idempotent.

Duplicate Service Bus messages must not unintentionally create duplicate document versions.

Use:

```text
MessageId
GenerationJobId
CorrelationId
```

Before processing:

```text
if Job.Status == Completed
    return
```

---

# 54. Concurrency

The service must prevent multiple workers from processing the same job simultaneously.

Recommended initial approach:

```text
SQL RowVersion + Job Status transition
```

Example:

```text
Queued
  |
  v
Processing
```

Only one worker should successfully transition the job.

---

# 55. Error Handling

Errors should be categorized.

## Validation Errors

```text
PLACEHOLDER_REQUIRED
INVALID_DATE
INVALID_EMAIL
INVALID_NUMBER
UNKNOWN_PLACEHOLDER
```

## Template Errors

```text
TEMPLATE_NOT_FOUND
TEMPLATE_VERSION_NOT_FOUND
TEMPLATE_INVALID
TEMPLATE_DOWNLOAD_FAILED
```

## Generation Errors

```text
DOCX_GENERATION_FAILED
PDF_CONVERSION_FAILED
```

## Infrastructure Errors

```text
STORAGE_UNAVAILABLE
SERVICE_BUS_UNAVAILABLE
DATABASE_UNAVAILABLE
```

---

# 56. API Error Response

Standard response:

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
Azure credentials
Internal infrastructure details
```

---

# 57. Observability

Use the platform observability standards established in earlier phases:

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
Storage upload duration
Generation failure rate
Queue processing time
Retry count
Documents generated per minute
```

---

# 58. Correlation ID

Every request must have a Correlation ID.

```text
HTTP Request
     |
     +-- CorrelationId: CORR-123
             |
             +-- Document Service
             +-- Template Service
             +-- Storage
             +-- Service Bus
             +-- Generation Worker
```

This is required for production troubleshooting.

---

# 59. Security Requirements

The service must implement:

- JWT/OIDC authentication.
- Organization isolation.
- Role-based authorization.
- Resource-level authorization.
- Input validation.
- File-type validation.
- Maximum file size limits.
- Secure storage.
- No public blobs.
- Audit logging.
- Secure service-to-service communication.
- Managed Identity where possible.

---

# 60. File Security

Generated file names must not be trusted as storage paths.

Prevent path traversal such as:

```text
../../../file.docx
```

Storage paths must be generated internally using controlled identifiers.

---

# 61. Performance Requirements

Initial target:

```text
Simple DOCX generation:
< 5 seconds

Simple DOCX + PDF:
< 10 seconds
```

These are target SLOs and depend on template complexity and conversion infrastructure.

Asynchronous processing should support:

```text
100+ generation jobs
```

with horizontal scaling.

---

# 62. Scalability

Document generation workers should scale independently from APIs.

```text
             API
              |
        +-----+-----+
        |           |
      Worker      Worker
        |           |
        +-----+-----+
              |
         Azure Blob
```

Example deployment:

```text
API instances = 3
Workers = 10
```

based on workload.

---

# 63. Caching

Potential caching:

```text
Template metadata
Placeholder definitions
```

Recommended technology:

```text
Redis
```

Generated document content should normally remain in Blob Storage.

---

# 64. Health Checks

Endpoints:

```http
GET /health
GET /health/live
GET /health/ready
```

Readiness should validate critical dependencies:

```text
Azure SQL
Azure Blob Storage
Azure Service Bus
```

---

# 65. Testing Strategy

## Unit Tests

Test:

```text
Placeholder resolution
Placeholder validation
Formatting
Document state transitions
Version calculation
Hash generation
Business rules
```

## Integration Tests

Test:

```text
SQL
Blob Storage
Template Service API
Service Bus
Document generation
```

## End-to-End Tests

Example:

```text
Create Template
      |
Activate Template
      |
Create Document
      |
Generate Document
      |
Generate DOCX
      |
Generate PDF
      |
Store Files
      |
Download PDF
```

---

# 66. Minimum Test Cases

## Placeholder

```text
Required value present
Required value missing
Optional value missing
Invalid type
Unknown placeholder
Null value
Empty value
Special characters
Multiline text
```

## Document

```text
Valid template
Invalid template
Template version unavailable
Template inactive
Duplicate request
Concurrent generation
Generation retry
Generation failure
PDF conversion failure
Storage failure
```

## Security

```text
User accesses own organization document
User accesses another organization document
Unauthorized download
Unauthorized generation
Unauthorized archive
Unauthorized delete
```

---

# 67. Acceptance Criteria

Phase 6 is complete when:

- [ ] Document Service project structure is implemented.
- [ ] Document APIs are implemented.
- [ ] Document entity is implemented.
- [ ] Document versioning is implemented.
- [ ] Generation job management is implemented.
- [ ] Template Service integration is implemented.
- [ ] Placeholder validation is implemented.
- [ ] Placeholder resolution is implemented.
- [ ] DOCX generation is implemented.
- [ ] PDF conversion is implemented.
- [ ] Azure Blob Storage integration is implemented.
- [ ] Document metadata is persisted.
- [ ] Document files are persisted.
- [ ] Document hashes are generated.
- [ ] Async generation is implemented.
- [ ] Service Bus integration is implemented.
- [ ] Retry mechanism is implemented.
- [ ] Idempotency is implemented.
- [ ] Organization isolation is implemented.
- [ ] Authorization is implemented.
- [ ] Audit events are published.
- [ ] Document events are published.
- [ ] Workflow integration contract is implemented.
- [ ] Health checks are implemented.
- [ ] OpenTelemetry is implemented.
- [ ] Unit tests are implemented.
- [ ] Integration tests are implemented.
- [ ] End-to-end generation succeeds.
- [ ] DOCX can be downloaded.
- [ ] PDF can be downloaded.
- [ ] Failed generation can be diagnosed using correlation ID.

---

# 68. Phase 6 Implementation Breakdown

## Phase 6.1 — Document Domain

```text
Document
DocumentVersion
DocumentFile
GenerationJob
Statuses
Domain Events
Business Rules
```

## Phase 6.2 — Database

```text
Azure SQL
EF Core
Migrations
Indexes
Concurrency
```

## Phase 6.3 — Document APIs

```text
Create
Get
List
Version
Generate
Status
Download
Archive
Cancel
```

## Phase 6.4 — Template Integration

```text
Template Service Client
Template Version Retrieval
Placeholder Definitions
Template File Retrieval
```

## Phase 6.5 — Placeholder Engine

```text
String
Date
Number
Currency
Boolean
Required Validation
Formatting
Nested Data
```

## Phase 6.6 — DOCX Generator

```text
Open XML
Placeholder Replacement
Formatting Preservation
Tables
Headers
Footers
```

## Phase 6.7 — PDF Generator

```text
DOCX -> PDF
Conversion abstraction
Error handling
```

## Phase 6.8 — Storage

```text
Blob Storage
Upload
Download
Metadata
Hash
Secure access
```

## Phase 6.9 — Async Processing

```text
Service Bus
Generation Queue
Worker
Retry
Dead-letter
Idempotency
```

## Phase 6.10 — Platform Integration

```text
Workflow
Notification
Audit
Observability
```

## Phase 6.11 — Testing

```text
Unit
Integration
Contract
E2E
Performance
Security
```

---

# 69. End-to-End Target Flow

```text
                    +------------------+
                    |      React       |
                    +--------+---------+
                             |
                             v
                    +------------------+
                    |   API Gateway    |
                    +--------+---------+
                             |
                             v
                 +-----------------------+
                 |    Document Service   |
                 +-----------+-----------+
                             |
                   Create Generation Job
                             |
                             v
                    +------------------+
                    | Azure Service Bus |
                    +--------+---------+
                             |
                             v
                 +-----------------------+
                 | Generation Worker     |
                 +-----------+-----------+
                             |
             +---------------+----------------+
             |               |                |
             v               v                v
      Template Service  Placeholder      Configuration
                           Engine
             |               |
             +---------------+
                     |
                     v
              +--------------+
              | DOCX         |
              | Generator    |
              +------+-------+
                     |
                     v
              +--------------+
              | PDF          |
              | Converter    |
              +------+-------+
                     |
                     v
              +--------------+
              | Azure Blob   |
              | Storage      |
              +------+-------+
                     |
                     v
             DocumentGenerated
                     |
          +----------+----------+
          |          |          |
          v          v          v
      Workflow  Notification  Audit
       Service     Service    Service
```

---

# 70. Example Complete Scenario

A business user generates an engagement letter.

## Step 1 — Select Template

```text
Engagement Letter
Template Version: 4
```

## Step 2 — Provide Data

```json
{
  "CustomerName": "ABC Limited",
  "ContactPerson": "John Smith",
  "ContractNumber": "EL-2026-001",
  "EffectiveDate": "2026-08-17",
  "Address": "Mumbai, Maharashtra",
  "Fee": 250000
}
```

## Step 3 — Request

```http
POST /api/v1/documents
```

## Step 4 — Queue

```text
GenerationJob = JOB-123
Status = Queued
```

## Step 5 — Worker

Worker receives:

```text
JOB-123
```

## Step 6 — Retrieve Template

```text
Template = Engagement Letter
Version = 4
```

## Step 7 — Validate

```text
CustomerName       OK
ContactPerson      OK
ContractNumber     OK
EffectiveDate      OK
Address            OK
Fee                OK
```

## Step 8 — Generate DOCX

```text
engagement-letter.docx
```

## Step 9 — Generate PDF

```text
engagement-letter.pdf
```

## Step 10 — Store

```text
Blob Storage
   |
   +-- engagement-letter.docx
   +-- engagement-letter.pdf
```

## Step 11 — Update Database

```text
Document Status = Generated
Version = 1
```

## Step 12 — Publish Event

```text
DocumentGenerated
```

## Step 13 — Workflow

```text
Draft
  |
  v
Review
  |
  v
Approval
```

## Step 14 — Audit

```text
User generated Document DOC-123
Template: Engagement Letter v4
Timestamp: 17-Aug-2026
```

---

# 71. Phase 6 Deliverables

At the end of Phase 6, the repository should contain:

```text
src/
├── Gateway/
├── Services/
│   ├── Identity/
│   ├── Organization/
│   ├── Template/
│   └── Document/
│       ├── Edp.Document.Api
│       ├── Edp.Document.Application
│       ├── Edp.Document.Domain
│       ├── Edp.Document.Infrastructure
│       └── Edp.Document.Contracts
│
├── Shared/
└── Web/

tests/
├── Edp.Document.UnitTests
├── Edp.Document.IntegrationTests
├── Edp.Document.ContractTests
└── Edp.Document.EndToEndTests
```

Documentation:

```text
docs/
└── phase-6-document-service/
    ├── README.md
    ├── requirements.md
    ├── architecture.md
    ├── api-design.md
    ├── database-design.md
    ├── event-design.md
    ├── generation-engine.md
    ├── storage-design.md
    ├── security.md
    └── testing.md
```

---

# 72. Relationship With Other Phases

```text
Phase 1
Architecture & Product Definition
        |
        v
Phase 2
Foundation
        |
        v
Phase 3
Development Environment & Solution Bootstrap
        |
        v
Phase 4
Core Platform Infrastructure
Identity + Organization
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
Workflow Service
        |
        v
Phase 8
Storage Service
        |
        v
Phase 9
Notification Service
        |
        v
Phase 10
Audit & Compliance
        |
        v
Phase 11+
AI / OCR / Integrations / SaaS
```

---

# 73. Key Architectural Principle

The Document Service must **consume** template definitions and versions from Phase 5 rather than duplicating template-management responsibilities.

The separation is:

```text
Template Service
    |
    | Owns what a template is
    |
    +-- Template
    +-- Template Version
    +-- Placeholder Definition
    +-- Template Validation
    |
    v
Document Service
    |
    | Owns how a template becomes a document
    |
    +-- Placeholder Resolution
    +-- DOCX Generation
    +-- PDF Generation
    +-- Document Version
    +-- Document Storage Coordination
    +-- Document Lifecycle
```

This separation is important for future Workflow, Digital Signature, AI Template Generator, OCR, bulk generation, and enterprise integration capabilities.

---

# 74. Phase 6 Definition of Done

Phase 6 should be considered complete only when a real end-to-end business scenario works:

```text
User
 |
 v
Select Active Template
 |
 v
Provide Business Data
 |
 v
Document Service
 |
 v
Validate Placeholder Data
 |
 v
Generate DOCX
 |
 v
Generate PDF
 |
 v
Store Files in Azure Blob
 |
 v
Persist Document Metadata
 |
 v
Publish DocumentGenerated
 |
 v
Start Workflow
 |
 v
Audit Event
 |
 v
User Downloads Document
```

The implementation should be production-oriented, tenant-aware, secure, observable, asynchronous where appropriate, and consistent with the architecture established in Phases 2–5.
