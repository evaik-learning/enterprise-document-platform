# Template Service — Detailed Requirements

**Project:** Enterprise Intelligent Document Automation Platform  
**Service:** Template Service  
**Document Version:** 1.0  
**Status:** Draft / Baseline  
**Target Framework:** .NET 10  
**Primary Storage:** Azure SQL + Azure Blob Storage  
**Messaging:** Azure Service Bus  

---

## 1. Purpose

The Template Service is a core domain service responsible for managing the complete lifecycle of document templates used by the Enterprise Intelligent Document Automation Platform.

The service owns:

- Template creation
- Template metadata
- Template file upload
- Template versioning
- Placeholder detection and definitions
- Template validation
- Template activation and deactivation
- Template archival
- Template cloning
- Template search
- Template lifecycle audit events

The Template Service does **not** generate business documents. Document generation belongs to the Document/Generation Service.

### Core principle

> The Template Service defines and manages what a document template is; the Document Generation Service uses an approved template and business data to produce the final document.

---

# 2. Business Objective

The service must allow organizations to create and manage reusable document templates containing dynamic placeholders.

Example:

```text
Dear {{CustomerName}},

Your invoice dated {{InvoiceDate}} has been generated.

Invoice Number:
{{InvoiceNumber}}

Address:
{{Address}}

Contract Number:
{{ContractNumber}}
```

The platform must be able to:

1. Store the template securely.
2. Maintain template metadata.
3. Detect placeholders.
4. Allow placeholder definitions to be configured.
5. Validate the template.
6. Maintain immutable versions.
7. Activate a validated version.
8. Deactivate or archive templates.
9. Publish lifecycle events.
10. Maintain a complete audit trail.

---

# 3. Scope

## 3.1 In Scope

- Template creation
- Template metadata management
- DOCX template upload
- Template version management
- Placeholder extraction
- Placeholder definition management
- Template validation
- Template activation
- Template deactivation
- Template archival
- Template cloning
- Template search and filtering
- Organization/tenant isolation
- RBAC authorization
- Audit event generation
- Service Bus integration
- Azure Blob Storage integration
- Azure SQL persistence
- API documentation
- Observability
- Automated testing

## 3.2 Out of Scope for MVP

The following should be implemented in later releases:

- Visual drag-and-drop template designer
- PDF template editing
- HTML template designer
- AI template generation
- OCR
- Digital signatures
- Document generation
- Advanced workflow
- External e-signature provider integration
- Full-text document search
- Template marketplace

---

# 4. Template Lifecycle

The recommended lifecycle is:

```text
Draft
  |
  v
Uploaded
  |
  v
Validating
  |
  +--------------------+
  |                    |
  v                    v
Validated          Validation Failed
  |                    |
  v                    |
Active <---------------+
  |
  v
Inactive
  |
  v
Archived
```

For the MVP, the externally visible lifecycle can be simplified to:

```text
Draft
Validated
Active
Inactive
Archived
```

Validation is an internal processing state and does not necessarily need to be exposed as a long-lived business status.

---

# 5. Template Types

## MVP

Support:

```text
DOCX
```

## Future

```text
PDF
HTML
Markdown
```

DOCX should be implemented first because it provides a strong foundation for Open XML-based placeholder extraction and document generation.

---

# 6. Template Entity Requirements

The Template entity represents the logical document template.

### Required properties

```text
TemplateId
OrganizationId
TemplateCode
Name
Description
Category
DocumentType
Status
CurrentVersionId
CreatedBy
CreatedAt
UpdatedBy
UpdatedAt
PublishedBy
PublishedAt
```

### Example

```json
{
  "templateId": "tpl-001",
  "organizationId": "org-001",
  "templateCode": "EMP-OFFER",
  "name": "Employee Offer Letter",
  "description": "Standard employee offer letter",
  "category": "HR",
  "documentType": "DOCX",
  "currentVersion": 3,
  "status": "Active"
}
```

---

# 7. Template Metadata Requirements

The service must support metadata including:

- Template code
- Template name
- Description
- Category
- Document type
- Organization
- Business unit
- Department
- Tags
- Status
- Current version
- Owner
- Created by
- Created date
- Updated by
- Updated date
- Published by
- Published date

### Template code rules

Template code should:

- Be unique within an organization.
- Be case-insensitive.
- Be immutable after publication unless an explicit administrative operation is introduced.
- Follow a documented naming convention.

Example:

```text
EMP-OFFER
SUPPLIER-NDA
PURCHASE-ORDER
CUSTOMER-CONTRACT
INVOICE
```

---

# 8. Template Versioning

Template versions are immutable once published.

Example:

```text
Employee Offer Letter

Version 1
Version 2
Version 3
```

Each version must represent an independently stored template file.

## Version properties

```text
TemplateVersionId
TemplateId
VersionNumber
FileName
StoragePath
FileHash
FileSize
ContentType
ValidationStatus
Status
ChangeDescription
CreatedBy
CreatedAt
PublishedBy
PublishedAt
```

## Version rules

1. Version numbers must be sequential.
2. Published versions must be immutable.
3. A new change must create a new version.
4. Only one version may be Active at a time.
5. Previous active versions must become Inactive when a new version is activated.
6. Historical versions must remain available for audit purposes.
7. A generated document must retain the template version used for generation.

---

# 9. Placeholder Engine

The Template Service is responsible for defining the placeholders that a template version exposes.

Example:

```text
{{CustomerName}}
{{InvoiceDate}}
{{Address}}
{{ContractNumber}}
```

## Placeholder naming convention

Recommended syntax:

```text
{{PlaceholderName}}
```

Recommended validation rule:

```regex
^[A-Za-z][A-Za-z0-9_]*$
```

### Valid

```text
{{CustomerName}}
{{InvoiceDate}}
{{ContractNumber}}
{{Customer_Address}}
```

### Invalid

```text
{{ Customer Name }}
{{Customer-Name}}
{{123Customer}}
```

---

# 10. Placeholder Definition

Each placeholder must contain:

```text
PlaceholderId
TemplateVersionId
Name
DisplayName
DataType
IsRequired
DefaultValue
Format
Description
Source
ValidationRules
CreatedAt
UpdatedAt
```

### Example

```json
{
  "placeholderId": "ph-001",
  "name": "InvoiceDate",
  "displayName": "Invoice Date",
  "dataType": "Date",
  "isRequired": true,
  "defaultValue": null,
  "format": "dd-MMM-yyyy",
  "description": "Invoice issue date"
}
```

---

# 11. Supported Placeholder Data Types

## MVP

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
```

## Future

```text
Object
Array
Image
RichText
Signature
QRCode
Barcode
```

---

# 12. Placeholder Detection

When a DOCX file is uploaded, the service should detect placeholders automatically.

### Detection flow

```text
DOCX Upload
    |
    v
Open XML Parser
    |
    +--> Paragraphs
    |
    +--> Tables
    |
    +--> Headers
    |
    +--> Footers
    |
    v
Placeholder Extraction
    |
    v
Placeholder Definitions
```

The initial implementation should support placeholders found in:

- Paragraphs
- Tables
- Headers
- Footers

Future versions may support:

- Text boxes
- Shapes
- Content controls
- Advanced Word XML structures

---

# 13. Placeholder Extraction Requirements

The extractor must:

1. Detect all valid placeholders.
2. Ignore plain text.
3. Detect duplicate occurrences of the same placeholder.
4. Return unique placeholder definitions.
5. Preserve occurrence information where useful.
6. Detect malformed placeholder syntax.
7. Identify the document location where possible.

Example result:

```json
{
  "placeholders": [
    {
      "name": "CustomerName",
      "occurrences": 2
    },
    {
      "name": "InvoiceDate",
      "occurrences": 1
    },
    {
      "name": "Address",
      "occurrences": 1
    }
  ]
}
```

---

# 14. Template Validation

A template must be validated before activation.

Validation consists of:

## Structural validation

- File exists.
- File type is supported.
- File size is within limits.
- File is not corrupted.
- DOCX package is structurally valid.

## Placeholder validation

- Placeholder syntax is valid.
- Placeholder names are valid.
- No malformed placeholders exist.
- Placeholder definitions are valid.
- Data types are supported.
- Required definitions exist.

## Business validation

Examples:

```text
CustomerName is required.

ContractNumber is required.

Signature is required for Contract templates.
```

Business rules should eventually become configurable.

---

# 15. Validation Result

Example:

```json
{
  "isValid": false,
  "errors": [
    {
      "code": "TPL001",
      "severity": "Error",
      "message": "Invalid placeholder syntax: {{Customer Name}}"
    },
    {
      "code": "TPL002",
      "severity": "Error",
      "message": "Required placeholder ContractNumber is missing"
    }
  ],
  "warnings": [
    {
      "code": "TPL101",
      "severity": "Warning",
      "message": "InvoiceDate has no display format"
    }
  ]
}
```

---

# 16. Validation Status

Supported values:

```text
NotValidated
Validating
Valid
Invalid
```

A template version cannot become Active when:

```text
ValidationStatus != Valid
```

---

# 17. Template Activation

Only an authorized user can activate a validated template version.

### Activation flow

```text
Activate Request
      |
      v
Authenticate User
      |
      v
Authorize User
      |
      v
Check Organization
      |
      v
Check Template
      |
      v
Check Version
      |
      v
Check Validation
      |
      v
Deactivate Previous Active Version
      |
      v
Activate Requested Version
      |
      v
Publish TemplateActivated Event
```

### Business rules

- Version must exist.
- Version must belong to the template.
- Template must belong to the user's organization.
- User must have activation permission.
- Version must be valid.
- Template must not be archived.

---

# 18. Template Deactivation

Deactivation prevents new document generation from using the template.

Existing generated documents remain unaffected.

Example:

```text
Template
   |
   +--> Existing Documents
   |       |
   |       +--> Remain accessible
   |
   +--> New Generation
           |
           +--> Blocked
```

---

# 19. Template Archiving

Archive is preferred over physical deletion.

Archived templates:

- Cannot be edited.
- Cannot be activated.
- Cannot generate new documents.
- Remain available for historical reference.
- Remain available for audit.
- Retain their versions and metadata.

---

# 20. Template Cloning

Users with appropriate permissions should be able to clone a template.

Example:

```text
Employee Offer Letter
Version 5
      |
      v
Clone
      |
      v
Senior Employee Offer Letter
Version 1
```

The clone must have a new `TemplateId`.

The original template and its versions must remain unchanged.

---

# 21. Template Search

The service must support searching and filtering.

Supported filters:

```text
Name
TemplateCode
Category
DocumentType
Status
CreatedBy
CreatedDate
UpdatedDate
Tags
```

Pagination is mandatory.

Example:

```http
GET /api/v1/templates?page=1&pageSize=20&status=Active&category=HR
```

---

# 22. API Requirements

## Create Template

```http
POST /api/v1/templates
```

Purpose:

Creates the logical template.

---

## Get Templates

```http
GET /api/v1/templates
```

Purpose:

Returns paginated templates.

---

## Get Template

```http
GET /api/v1/templates/{templateId}
```

Purpose:

Returns template metadata and current status.

---

## Upload Template Version

```http
POST /api/v1/templates/{templateId}/versions
```

Purpose:

Uploads a new DOCX template version.

---

## Get Template Versions

```http
GET /api/v1/templates/{templateId}/versions
```

Purpose:

Returns version history.

---

## Get Template Version

```http
GET /api/v1/templates/{templateId}/versions/{versionId}
```

Purpose:

Returns version details.

---

## Validate Template

```http
POST /api/v1/templates/{templateId}/versions/{versionId}/validate
```

Purpose:

Validates the template version.

---

## Get Validation Result

```http
GET /api/v1/templates/{templateId}/versions/{versionId}/validation
```

---

## Get Placeholders

```http
GET /api/v1/templates/{templateId}/versions/{versionId}/placeholders
```

---

## Update Placeholder

```http
PUT /api/v1/templates/{templateId}/versions/{versionId}/placeholders/{placeholderId}
```

---

## Activate Version

```http
POST /api/v1/templates/{templateId}/versions/{versionId}/activate
```

---

## Deactivate Template

```http
POST /api/v1/templates/{templateId}/deactivate
```

---

## Archive Template

```http
POST /api/v1/templates/{templateId}/archive
```

---

## Clone Template

```http
POST /api/v1/templates/{templateId}/clone
```

---

# 23. Storage Requirements

Template files must not be stored directly in Azure SQL.

Use Azure Blob Storage.

Recommended structure:

```text
templates/
    {organizationId}/
        {templateId}/
            v1/
                template.docx
            v2/
                template.docx
            v3/
                template.docx
```

Example:

```text
templates/
    org-001/
        tpl-employee-offer/
            v3/
                employee-offer.docx
```

The database stores the logical storage reference.

---

# 24. Storage Abstraction

The application layer should depend on an abstraction:

```csharp
public interface ITemplateStorage
{
    Task<TemplateStorageResult> UploadAsync(
        Stream content,
        string path,
        string contentType,
        CancellationToken cancellationToken);

    Task<Stream> DownloadAsync(
        string path,
        CancellationToken cancellationToken);

    Task<bool> ExistsAsync(
        string path,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        string path,
        CancellationToken cancellationToken);
}
```

The infrastructure layer implements Azure Blob Storage.

---

# 25. Database Requirements

Azure SQL should store template metadata and relational information.

## Templates

```text
Templates
---------
TemplateId PK
OrganizationId
TemplateCode
Name
Description
Category
DocumentType
Status
CurrentVersionId
CreatedBy
CreatedAt
UpdatedBy
UpdatedAt
PublishedBy
PublishedAt
```

## TemplateVersions

```text
TemplateVersions
----------------
TemplateVersionId PK
TemplateId FK
VersionNumber
FileName
StoragePath
FileHash
FileSize
ContentType
ValidationStatus
Status
ChangeDescription
CreatedBy
CreatedAt
PublishedBy
PublishedAt
```

## Placeholders

```text
Placeholders
------------
PlaceholderId PK
TemplateVersionId FK
Name
DisplayName
DataType
IsRequired
DefaultValue
Format
Description
Source
CreatedAt
UpdatedAt
```

## ValidationResults

```text
ValidationResults
-----------------
ValidationResultId PK
TemplateVersionId FK
Status
ErrorCount
WarningCount
ValidatedAt
ValidatedBy
```

## ValidationErrors

```text
ValidationErrors
----------------
ValidationErrorId PK
ValidationResultId FK
Code
Severity
Message
Location
PlaceholderName
```

---

# 26. Multi-Tenant Requirements

The platform is multi-tenant.

Every template must belong to exactly one organization.

All operations must enforce organization isolation.

Example:

```text
User A
Organization A
     |
     +--> Template A1
     +--> Template A2

User B
Organization B
     |
     +--> Template B1
```

User A must never be able to retrieve or modify Template B1.

Tenant isolation must be enforced at the application and database query levels.

---

# 27. Security Requirements

## Authentication

Use Microsoft Entra ID.

## Authorization

Use role-based access control.

Example roles:

```text
PlatformAdmin
OrganizationAdmin
TemplateManager
TemplateReviewer
TemplateUser
Viewer
```

### Example permission matrix

| Operation | Admin | Template Manager | User | Viewer |
|---|---:|---:|---:|---:|
| Create | Yes | Yes | No | No |
| Upload | Yes | Yes | No | No |
| Edit | Yes | Yes | No | No |
| Validate | Yes | Yes | No | No |
| Activate | Yes | Yes | No | No |
| Deactivate | Yes | Yes | No | No |
| View | Yes | Yes | Yes | Yes |
| Clone | Yes | Yes | No | No |
| Archive | Yes | Yes | No | No |

---

# 28. Audit Requirements

The Template Service must generate audit information for important operations.

Audit actions include:

```text
TemplateCreated
TemplateUpdated
TemplateUploaded
TemplateVersionCreated
TemplateValidated
TemplateActivated
TemplateDeactivated
TemplateArchived
TemplateCloned
PlaceholderCreated
PlaceholderUpdated
```

Example audit event:

```json
{
  "action": "TemplateActivated",
  "entityType": "Template",
  "entityId": "tpl-001",
  "organizationId": "org-001",
  "userId": "user-001",
  "timestamp": "2026-08-08T12:00:00Z"
}
```

---

# 29. Event-Driven Requirements

The Template Service should publish integration events through Azure Service Bus.

Events:

```text
TemplateCreated
TemplateVersionCreated
TemplateValidated
TemplateActivated
TemplateDeactivated
TemplateArchived
```

Example:

```json
{
  "eventId": "evt-001",
  "eventType": "TemplateActivated",
  "templateId": "tpl-001",
  "templateVersionId": "ver-003",
  "organizationId": "org-001",
  "occurredAt": "2026-08-08T12:00:00Z",
  "correlationId": "cor-001"
}
```

Consumers may include:

```text
Audit Service
Document Service
Search Service
Notification Service
Reporting Service
```

---

# 30. Event Publishing Pattern

The service should avoid tightly coupling itself to downstream services.

Recommended:

```text
Template Service
      |
      v
Domain Event
      |
      v
Outbox
      |
      v
Azure Service Bus
      |
      +--> Audit Service
      +--> Search Service
      +--> Reporting Service
      +--> Notification Service
```

The Outbox Pattern should be considered mandatory before production deployment to prevent database/event inconsistencies.

---

# 31. Error Handling

Use ASP.NET Core `ProblemDetails` for API errors.

Recommended error codes:

```text
TPL001 TemplateNotFound
TPL002 TemplateVersionNotFound
TPL003 UnsupportedFileType
TPL004 FileTooLarge
TPL005 InvalidTemplate
TPL006 InvalidPlaceholder
TPL007 DuplicatePlaceholder
TPL008 TemplateNotValidated
TPL009 TemplateAlreadyActive
TPL010 TemplateAlreadyArchived
TPL011 UnauthorizedTemplateAccess
TPL012 TemplateVersionConflict
TPL013 TemplateCodeAlreadyExists
TPL014 TemplateCannotBeModified
TPL015 ValidationFailed
```

---

# 32. Concurrency Requirements

The service must support optimistic concurrency.

Example:

```text
User A -> edits Template Version
User B -> edits same Template Version
```

The system must detect stale updates.

Recommended approaches:

- SQL `rowversion`
- ETag
- Version token

Published versions must never be modified.

---

# 33. File Upload Requirements

The upload API must validate:

- File extension
- MIME type
- File size
- File signature where applicable
- DOCX structure
- Malware scanning integration point
- Organization quota
- Template ownership

Recommended initial maximum size:

```text
10 MB
```

The limit should be configurable.

---

# 34. Security for Uploaded Files

Uploaded documents should be treated as untrusted input.

The service should provide an extension point for:

```text
Malware Scanning
Content Validation
File Signature Validation
ZIP/XML Security Validation
```

DOCX is an Open XML package and should not be assumed safe merely because its extension is `.docx`.

---

# 35. Observability Requirements

Every request should carry or generate:

```text
CorrelationId
TraceId
OrganizationId
UserId
TemplateId
TemplateVersionId
```

Logs should capture:

```text
Template upload started
Template upload completed
Placeholder extraction started
Placeholder extraction completed
Validation started
Validation completed
Template activation started
Template activation completed
```

Recommended technology:

```text
Serilog
OpenTelemetry
Application Insights
Azure Monitor
Log Analytics
```

---

# 36. Health Checks

The service should expose:

```http
GET /health
```

and:

```http
GET /health/ready
GET /health/live
```

Readiness should verify critical dependencies such as:

- Azure SQL
- Azure Blob Storage
- Service Bus

Liveness should not depend on external services.

---

# 37. API Versioning

The API should use URL versioning:

```text
/api/v1/templates
```

Future versions:

```text
/api/v2/templates
```

The API contract should remain backward compatible within a major version.

---

# 38. Idempotency

Operations that may be retried should support idempotency.

Especially:

```text
Template creation
Template version upload
Activation
Event publishing
```

An idempotency key can be accepted through:

```http
Idempotency-Key: <unique-request-id>
```

This is especially important for cloud environments where network retries are common.

---

# 39. Transaction Requirements

Template metadata and version metadata must be persisted transactionally.

However, Azure Blob Storage and Azure SQL are separate systems.

Recommended upload strategy:

```text
1. Validate request
2. Create template/version record
3. Upload blob
4. Update storage metadata
5. Commit database state
6. Publish event through Outbox
```

The exact transaction/orchestration strategy should be documented and tested for failure scenarios.

---

# 40. Domain Model

Recommended entities:

```text
Template
TemplateVersion
Placeholder
ValidationResult
ValidationError
```

Recommended value objects:

```text
TemplateCode
TemplateName
PlaceholderName
FileMetadata
StorageReference
```

Recommended domain events:

```text
TemplateCreated
TemplateVersionCreated
TemplateValidated
TemplateActivated
TemplateDeactivated
TemplateArchived
```

---

# 41. Recommended .NET Project Structure

```text
Edp.TemplateService
│
├── Api
│   ├── Endpoints
│   ├── Contracts
│   ├── Filters
│   └── Middleware
│
├── Application
│   ├── Templates
│   │   ├── Create
│   │   ├── Get
│   │   ├── Search
│   │   ├── Upload
│   │   ├── Activate
│   │   ├── Deactivate
│   │   ├── Archive
│   │   └── Clone
│   │
│   ├── Versions
│   │   ├── Create
│   │   ├── Get
│   │   └── Validate
│   │
│   └── Placeholders
│       ├── Detect
│       ├── Get
│       └── Update
│
├── Domain
│   ├── Entities
│   ├── ValueObjects
│   ├── Enums
│   ├── Events
│   ├── Rules
│   └── Exceptions
│
├── Infrastructure
│   ├── Persistence
│   ├── BlobStorage
│   ├── DocumentParser
│   ├── ServiceBus
│   └── Observability
│
└── Tests
    ├── UnitTests
    ├── IntegrationTests
    ├── ApiTests
    └── ArchitectureTests
```

---

# 42. Core Interfaces

Recommended abstractions:

```csharp
ITemplateRepository

ITemplateVersionRepository

IPlaceholderRepository

ITemplateStorage

IPlaceholderExtractor

ITemplateValidator

IEventPublisher

IAuditEventPublisher

ICurrentUser

ITenantContext
```

The Application layer should depend on interfaces rather than Azure-specific implementations.

---

# 43. Template Upload Flow

```text
Client
  |
  | POST /templates/{id}/versions
  v
API Gateway
  |
  v
Template Service
  |
  +--> Authenticate
  |
  +--> Authorize
  |
  +--> Validate file
  |
  +--> Create Version
  |
  +--> Upload Blob
  |
  +--> Extract Placeholders
  |
  +--> Validate Template
  |
  +--> Save Metadata
  |
  +--> Write Outbox Event
  |
  v
Response
```

---

# 44. Template Activation Flow

```text
Client
  |
  v
API Gateway
  |
  v
Template Service
  |
  +--> Authenticate
  |
  +--> Authorize
  |
  +--> Verify Organization
  |
  +--> Verify Template
  |
  +--> Verify Version
  |
  +--> Verify Validation
  |
  +--> Deactivate Current Version
  |
  +--> Activate New Version
  |
  +--> Write Audit Event
  |
  +--> Write Outbox Event
  |
  v
Service Bus
```

---

# 45. Non-Functional Requirements

## Performance

Target:

- Template metadata GET: < 300 ms under normal load.
- Search API: < 500 ms under normal load.
- Metadata CRUD: < 500 ms.
- Upload performance should primarily depend on file size and network.
- Placeholder extraction should complete asynchronously for larger templates in future releases.

## Availability

Target:

```text
99.9%
```

for production deployment.

## Scalability

The service must be horizontally scalable.

Avoid:

- In-memory template state.
- Local file persistence.
- Local session state.

## Security

- Entra ID authentication.
- RBAC.
- Tenant isolation.
- Managed Identity.
- Key Vault.
- Private networking where applicable.
- HTTPS only.
- Secure Blob access.

## Reliability

- Retry transient Azure failures.
- Circuit breaker where appropriate.
- Idempotent operations.
- Outbox pattern.
- Dead-letter queue handling.
- Distributed tracing.

---

# 46. Testing Requirements

## Unit Tests

Test:

- Domain rules
- Placeholder parser
- Placeholder validation
- Template lifecycle
- Version rules
- Business rules

## Integration Tests

Test:

- Azure SQL
- Blob Storage
- Service Bus
- Repository behavior

Testcontainers may be used for local infrastructure where appropriate.

## API Tests

Test:

- Authentication
- Authorization
- Tenant isolation
- CRUD
- Upload
- Validation
- Activation
- Deactivation
- Archive

## Architecture Tests

Verify:

```text
API -> Application
Application -> Domain
Infrastructure -> Application/Domain
Domain -> no Infrastructure dependency
```

---

# 47. MVP Acceptance Criteria

The Template Service MVP is complete when the following are working:

### Template Management

- [ ] Create template
- [ ] View template
- [ ] Update draft template
- [ ] Search templates
- [ ] Archive template

### Upload

- [ ] Upload DOCX
- [ ] Store file in Azure Blob Storage
- [ ] Store metadata in Azure SQL
- [ ] Validate file type
- [ ] Validate file size

### Versioning

- [ ] Create version
- [ ] View version history
- [ ] Maintain immutable published versions
- [ ] Activate one version
- [ ] Deactivate previous active version

### Placeholder Engine

- [ ] Detect placeholders
- [ ] Support paragraphs
- [ ] Support tables
- [ ] Support headers
- [ ] Support footers
- [ ] Create placeholder definitions
- [ ] Validate placeholder syntax

### Validation

- [ ] Structural validation
- [ ] Placeholder validation
- [ ] Validation errors
- [ ] Validation warnings
- [ ] Validation status

### Security

- [ ] Entra ID
- [ ] RBAC
- [ ] Organization isolation
- [ ] Secure Blob access

### Events

- [ ] TemplateCreated
- [ ] TemplateVersionCreated
- [ ] TemplateValidated
- [ ] TemplateActivated
- [ ] TemplateDeactivated
- [ ] TemplateArchived

### Observability

- [ ] Structured logging
- [ ] Distributed tracing
- [ ] Application Insights
- [ ] Health checks

### Testing

- [ ] Unit tests
- [ ] Integration tests
- [ ] API tests
- [ ] Architecture tests

---

# 48. Implementation Roadmap

## Sprint 1 — Service Foundation

```text
Create Edp.TemplateService
Configure .NET 10
Configure OpenAPI / Scalar
Configure Serilog
Configure OpenTelemetry
Configure Health Checks
Configure Entra ID
Configure tenant context
```

## Sprint 2 — Domain + Database

```text
Template entity
TemplateVersion entity
Placeholder entity
Validation entities
EF Core
Azure SQL
Migrations
Repositories
```

## Sprint 3 — Blob Storage

```text
Azure Blob Storage
TemplateStorage abstraction
Upload
Download
Metadata
Hashing
```

## Sprint 4 — Template APIs

```text
Create
Get
List
Search
Upload Version
Version History
```

## Sprint 5 — Placeholder Engine

```text
Open XML parser
Placeholder extraction
Placeholder definitions
Placeholder validation
```

## Sprint 6 — Validation

```text
Structural validation
Placeholder validation
Business validation
Validation results
Validation errors
```

## Sprint 7 — Lifecycle

```text
Activate
Deactivate
Archive
Clone
Optimistic concurrency
```

## Sprint 8 — Messaging + Audit

```text
Azure Service Bus
Outbox
Integration events
Audit events
Retry
Dead-letter handling
```

## Sprint 9 — Testing

```text
Unit tests
Integration tests
API tests
Architecture tests
Security tests
```

## Sprint 10 — UI

```text
Template List
Create Template
Upload Template
Version History
Placeholder Editor
Validation Results
Activate / Deactivate
```

---

# 49. Future Enhancements

After the MVP, the Template Service can evolve to support:

- Visual template designer
- Drag-and-drop fields
- Conditional sections
- Repeating sections
- Dynamic tables
- Nested objects
- Images
- QR codes
- Barcodes
- Signature fields
- Rich text
- Template approval workflow
- Template marketplace
- AI template generation
- AI placeholder suggestions
- Template recommendations
- Template comparison
- PDF templates
- HTML templates
- Localization
- Multi-language templates
- Template analytics

---

# 50. Architectural Boundary

The most important boundary is:

```text
                 TEMPLATE SERVICE
                       |
          +------------+------------+
          |            |            |
       Template     Version      Placeholder
       Metadata     Management    Definitions
          |            |            |
          +------------+------------+
                       |
                       v
                 Blob Storage
                       |
                       v
                  Service Bus
                       |
                       v
              DOCUMENT SERVICE
                       |
                       v
             GENERATION ENGINE
                       |
              +--------+--------+
              |                 |
             DOCX              PDF
```

### Template Service

Owns:

```text
What is the template?
Which version is active?
What placeholders exist?
Is the template valid?
Who can use it?
```

### Document Generation Service

Owns:

```text
How do we populate the template?
How do we generate DOCX?
How do we generate PDF?
How do we produce the final document?
```

This separation must be preserved as the platform grows.

---

# 51. Definition of Done

The Template Service should be considered production-ready only when:

- [ ] Requirements implemented
- [ ] API contracts documented
- [ ] Database schema implemented
- [ ] Azure Blob integration implemented
- [ ] Placeholder extraction implemented
- [ ] Validation engine implemented
- [ ] Versioning implemented
- [ ] Lifecycle rules implemented
- [ ] RBAC implemented
- [ ] Tenant isolation verified
- [ ] Audit events implemented
- [ ] Service Bus integration implemented
- [ ] Outbox implemented
- [ ] Retry policies implemented
- [ ] Health checks implemented
- [ ] Structured logging implemented
- [ ] Distributed tracing implemented
- [ ] Unit test coverage established
- [ ] Integration tests established
- [ ] API tests established
- [ ] Architecture tests established
- [ ] CI pipeline configured
- [ ] CD pipeline configured
- [ ] Infrastructure deployed through Bicep
- [ ] Security review completed
- [ ] Documentation completed

---

# 52. Recommended First Implementation Target

Do not implement the complete specification in the first coding session.

Build this vertical slice first:

```text
Create Template
      |
      v
Upload DOCX
      |
      v
Store in Blob
      |
      v
Store Metadata in SQL
      |
      v
Extract
{{CustomerName}}
{{InvoiceDate}}
{{Address}}
      |
      v
Save Placeholder Definitions
      |
      v
Validate
      |
      v
Activate Version
      |
      v
Publish TemplateActivated
```

Once this vertical slice works end-to-end, expand the service feature by feature.

---

## Related Platform Services

The Template Service will eventually integrate with:

```text
                +----------------------+
                |    API Gateway       |
                +----------+-----------+
                           |
                           v
                +----------------------+
                |   Template Service   |
                +----------+-----------+
                           |
          +----------------+----------------+
          |                |                |
          v                v                v
     Azure SQL       Blob Storage      Service Bus
                                           |
                         +-----------------+----------------+
                         |                 |                |
                         v                 v                v
                   Audit Service    Document Service   Notification
```

The Template Service should therefore be implemented as an independently deployable bounded context with clear ownership of template data and lifecycle.
