# Phase 4 — Core Platform Infrastructure & Identity/Organization Implementation

## 1. Phase Objective

Phase 4 establishes the foundational infrastructure required by every service in the Enterprise Document Platform.

By the end of this phase, the solution should have:

- Centralized configuration
- Structured logging
- Global exception handling
- Correlation ID / trace propagation
- Health checks
- OpenTelemetry foundations
- Database infrastructure
- EF Core conventions
- Azure Storage abstraction
- Azure Service Bus abstraction
- Redis/cache abstraction
- Secret/configuration management
- Identity management
- Authentication and authorization
- Organization/tenant management
- User-organization membership
- Role-based authorization
- Audit foundation
- Gateway authentication
- Service-to-service security foundation
- Docker support for local infrastructure
- Integration-test foundation
- CI validation for the new infrastructure

The architecture after this phase should look approximately like:

```text
                         ┌──────────────────────┐
                         │      React Web       │
                         │     Application      │
                         └──────────┬───────────┘
                                    │
                                    ▼
                         ┌──────────────────────┐
                         │    EDP Gateway       │
                         │ Authentication       │
                         │ Authorization        │
                         │ Rate Limiting        │
                         │ Correlation ID       │
                         └──────────┬───────────┘
                                    │
                    ┌───────────────┼────────────────┐
                    │               │                │
                    ▼               ▼                ▼
              ┌──────────┐   ┌──────────────┐  ┌────────────┐
              │ Identity │   │ Organization │  │   Future   │
              │ Service  │   │   Service    │  │ Services   │
              └────┬─────┘   └──────┬───────┘  └────────────┘
                   │                │
                   └────────┬───────┘
                            ▼
                     ┌─────────────┐
                     │  Azure SQL  │
                     └─────────────┘

        ┌────────────────────────────────────────────────┐
        │               Shared Platform                  │
        │                                                │
        │ Configuration │ Logging │ Telemetry            │
        │ Security      │ Storage │ Messaging            │
        │ Caching       │ Errors  │ Health Checks        │
        └────────────────────────────────────────────────┘

        ┌────────────────────────────────────────────────┐
        │             Infrastructure                     │
        │                                                │
        │ SQL Server │ Redis │ Service Bus │ Azurite     │
        └────────────────────────────────────────────────┘
```

---

## 2. Phase 4 Scope

| Area | Implementation |
|---|---|
| Platform Foundation | Configuration, middleware, observability |
| Persistence | EF Core, SQL Server/Azure SQL |
| Security | Authentication, JWT/OIDC, authorization |
| Identity | Users, roles, claims |
| Organization | Organizations, memberships, tenant context |
| Storage | Blob abstraction |
| Messaging | Service Bus abstraction |
| Caching | Redis abstraction |
| Audit | Audit infrastructure |
| Gateway | Authentication/authorization integration |
| Infrastructure | Docker local dependencies |
| Testing | Unit + integration foundation |
| CI/CD | Build/test/security validation |

---

## 3. Architectural Principles

### 3.1 Shared infrastructure must not contain business logic

`Edp.Shared.Infrastructure` can contain:

- EF Core base classes
- Logging
- Telemetry
- Storage clients
- Messaging clients
- Caching
- Authentication helpers
- Common middleware

It should **not** contain business rules for:

- Template
- Document
- Workflow
- Organization
- Identity

Business rules belong inside their respective services.

---

# 4. Target Solution Structure

Building on Phase 3, the solution should evolve toward:

```text
EnterpriseDocumentPlatform/
│
├── src/
│   │
│   ├── Gateway/
│   │   └── Edp.Gateway/
│   │
│   ├── Services/
│   │   │
│   │   ├── Identity/
│   │   │   ├── Edp.Identity.Api/
│   │   │   ├── Edp.Identity.Application/
│   │   │   ├── Edp.Identity.Domain/
│   │   │   └── Edp.Identity.Infrastructure/
│   │   │
│   │   └── Organization/
│   │       ├── Edp.Organization.Api/
│   │       ├── Edp.Organization.Application/
│   │       ├── Edp.Organization.Domain/
│   │       └── Edp.Organization.Infrastructure/
│   │
│   ├── Shared/
│   │   ├── Edp.Shared.Domain/
│   │   ├── Edp.Shared.Application/
│   │   ├── Edp.Shared.Infrastructure/
│   │   ├── Edp.Shared.Contracts/
│   │   └── Edp.Shared.Security/
│   │
│   └── Web/
│       └── edp-web/
│
├── tests/
│   ├── Unit/
│   │   ├── Edp.Identity.UnitTests/
│   │   ├── Edp.Organization.UnitTests/
│   │   └── Edp.Shared.UnitTests/
│   │
│   └── Integration/
│       ├── Edp.Identity.IntegrationTests/
│       ├── Edp.Organization.IntegrationTests/
│       └── Edp.Platform.IntegrationTests/
│
├── infra/
│   ├── bicep/
│   ├── environments/
│   └── modules/
│
├── docker/
│   ├── docker-compose.yml
│   ├── sql/
│   └── azurite/
│
├── pipelines/
├── scripts/
│
└── docs/
    ├── architecture/
    ├── api/
    ├── database/
    ├── security/
    └── phases/
```

---

# 5. Shared Platform Foundation

## 5.1 Edp.Shared.Domain

Create common domain primitives.

### Base Entity

```text
Entity
 ├── Id
 ├── CreatedAt
 ├── CreatedBy
 ├── ModifiedAt
 └── ModifiedBy
```

Prefer strongly typed identifiers where practical.

Examples:

```text
UserId
OrganizationId
DocumentId
TemplateId
WorkflowId
```

Initially, these may be backed by `Guid`.

---

## 5.2 Auditable Entity

Create:

```text
IAuditableEntity
```

with:

```text
CreatedAt
CreatedBy
ModifiedAt
ModifiedBy
```

EF Core should populate these automatically.

---

# 6. Shared Application Layer

Create common abstractions:

```text
ICurrentUser
ICurrentOrganization
IDateTimeProvider
IUnitOfWork
IPagedQuery
```

Example:

```text
ICurrentUser

UserId
Email
Name
Roles
Claims
IsAuthenticated
```

---

# 7. Configuration Management

Create strongly typed configuration classes:

```text
DatabaseOptions
JwtOptions
AzureStorageOptions
ServiceBusOptions
RedisOptions
OpenTelemetryOptions
```

Configuration should come from:

```text
appsettings.json
appsettings.Development.json
Environment Variables
Azure Key Vault
```

Never place secrets directly in:

```text
appsettings.json
Dockerfile
source code
Git
```

---

# 8. Global Exception Handling

Implement centralized exception handling.

Expected response:

```json
{
  "type": "https://api.enterprise-document-platform.com/errors/validation",
  "title": "Validation failed",
  "status": 400,
  "traceId": "00-abc...",
  "errors": {
    "email": [
      "Email is required."
    ]
  }
}
```

Use `ProblemDetails` and RFC 7807-compatible responses.

---

# 9. Correlation ID

Every incoming request must receive or propagate a correlation ID.

Example:

```text
X-Correlation-ID: 7f7f4e...
```

The correlation ID must flow through:

```text
React
   ↓
Gateway
   ↓
Identity Service
   ↓
Organization Service
   ↓
Service Bus
   ↓
Background Worker
```

The same identifier should be available in logs.

---

# 10. Logging

Use structured logging.

Recommended:

```text
Serilog
```

Log properties should include:

```text
Timestamp
Level
Message
TraceId
CorrelationId
UserId
OrganizationId
Service
Environment
RequestPath
RequestMethod
Duration
```

Never log:

```text
Password
JWT
Refresh Token
Client Secret
Connection String
Sensitive Document Content
```

---

# 11. OpenTelemetry

Establish the observability foundation.

## Metrics

Capture:

```text
HTTP request count
HTTP request duration
HTTP error count
Database calls
Service Bus messages
Cache hits/misses
```

## Traces

Capture:

```text
Gateway
API
SQL
Redis
Service Bus
HTTP clients
```

## Logs

Integrate structured logs with OpenTelemetry where appropriate.

Local development can export to:

```text
Console
```

Azure environments should use:

```text
Application Insights
```

---

# 12. Health Checks

Every service must expose:

```text
/health
/health/ready
/health/live
```

### Liveness

```text
GET /health/live
```

Checks only whether the application is alive.

### Readiness

```text
GET /health/ready
```

Checks dependencies such as:

```text
SQL Server
Redis
Service Bus
Blob Storage
```

---

# 13. Database Infrastructure

Use:

```text
Entity Framework Core
SQL Server / Azure SQL
```

The platform should establish database conventions before implementing business functionality.

---

# 14. Database Strategy

Use **database-per-service** logically.

Initial databases:

```text
IdentityDb
OrganizationDb
```

Future databases:

```text
TemplateDb
DocumentDb
WorkflowDb
AuditDb
```

Avoid a single giant shared database.

---

# 15. Identity Database

Initial tables:

```text
Users
UserCredentials / ExternalIdentities
Roles
UserRoles
RefreshTokens
UserSessions
```

Example:

```text
Users
--------------------------
Id
Email
NormalizedEmail
FirstName
LastName
DisplayName
IsActive
CreatedAt
ModifiedAt
```

---

# 16. Organization Database

Initial tables:

```text
Organizations
OrganizationMembers
OrganizationRoles
```

Organization:

```text
Organizations
--------------------------
Id
Name
Slug
Status
CreatedAt
CreatedBy
ModifiedAt
ModifiedBy
```

Membership:

```text
OrganizationMembers
--------------------------
Id
OrganizationId
UserId
Role
Status
JoinedAt
```

---

# 17. Multi-Tenant Foundation

The platform must be designed as multi-tenant from Phase 4.

Every business operation must eventually have an organization context.

Example:

```text
OrganizationId
```

should flow through:

```text
JWT
    ↓
CurrentOrganization
    ↓
Application Layer
    ↓
Repository
    ↓
Database
```

Do not trust a client-provided organization ID blindly.

---

# 18. Tenant Isolation

At minimum, `OrganizationId` must be included in tenant-owned tables.

Future examples:

```text
Templates
Documents
Workflows
AuditLogs
```

Repositories should enforce tenant filtering:

```text
WHERE OrganizationId = CurrentOrganizationId
```

---

# 19. Identity Service

Create:

```text
Edp.Identity.Api
Edp.Identity.Application
Edp.Identity.Domain
Edp.Identity.Infrastructure
```

Responsibilities:

- User registration
- User authentication
- User profile
- Password management if local authentication is supported
- External identity integration
- Token management
- Session management
- Role/claim management

---

# 20. Identity API

## Registration

```http
POST /api/v1/auth/register
```

## Login

```http
POST /api/v1/auth/login
```

## Refresh

```http
POST /api/v1/auth/refresh
```

## Logout

```http
POST /api/v1/auth/logout
```

## Current User

```http
GET /api/v1/users/me
```

---

# 21. Authentication Strategy

The preferred enterprise architecture should support an external Identity Provider.

For example:

```text
Microsoft Entra ID
```

or another OIDC-compliant provider.

The application should not tightly couple business services to a specific identity provider.

Use:

```text
OIDC
OAuth 2.0
JWT
```

The Gateway validates the access token.

Services validate authorization requirements.

---

# 22. JWT Claims

Standard claims:

```text
sub
name
email
iss
aud
exp
iat
```

Platform claims:

```text
organization_id
roles
permissions
```

Example:

```json
{
  "sub": "user-id",
  "email": "user@example.com",
  "organization_id": "organization-id",
  "roles": [
    "OrganizationAdmin"
  ]
}
```

---

# 23. Authorization

Implement policy-based authorization.

Instead of relying only on:

```csharp
[Authorize(Roles = "Admin")]
```

prefer permission-based policies:

```text
Template.Read
Template.Write
Document.Generate
Document.Read
Workflow.Approve
Organization.Manage
User.Manage
```

This gives the platform room to evolve.

---

# 24. Initial Platform Roles

Create:

```text
PlatformAdmin
OrganizationOwner
OrganizationAdmin
User
Approver
```

The initial MVP can keep the model simple.

Later:

```text
Custom Roles
Custom Permissions
Role Templates
Delegation
```

---

# 25. Organization Service

Create:

```text
Edp.Organization.Api
Edp.Organization.Application
Edp.Organization.Domain
Edp.Organization.Infrastructure
```

Responsibilities:

- Organization creation
- Organization retrieval
- Organization update
- Organization membership
- Invitations
- User membership
- Organization roles
- Tenant context

---

# 26. Organization API

Initial endpoints:

```http
POST /api/v1/organizations
GET /api/v1/organizations
GET /api/v1/organizations/{id}
PUT /api/v1/organizations/{id}
```

Membership:

```http
GET /api/v1/organizations/{id}/members
POST /api/v1/organizations/{id}/members
PUT /api/v1/organizations/{id}/members/{userId}
DELETE /api/v1/organizations/{id}/members/{userId}
```

Current organization:

```http
GET /api/v1/me/organizations
```

---

# 27. Organization Creation Flow

Example:

```text
User registers
      ↓
Identity created
      ↓
Organization created
      ↓
User added as OrganizationOwner
      ↓
Organization context established
      ↓
Audit event generated
```

---

# 28. Organization Invitation

Implement the foundation for:

```text
OrganizationAdmin
      ↓
Invite User
      ↓
Email Notification
      ↓
User Accepts
      ↓
Membership Created
```

For Phase 4, email sending can be abstracted. Actual notification implementation can be completed in the Notification phase.

---

# 29. Current User Context

Implement:

```text
ICurrentUser
ICurrentOrganization
```

Example:

```csharp
_currentUser.UserId
_currentUser.Email
_currentOrganization.OrganizationId
```

This avoids repeatedly reading claims throughout application code.

---

# 30. Storage Abstraction

Create:

```text
IBlobStorage
```

Example operations:

```text
UploadAsync
DownloadAsync
DeleteAsync
ExistsAsync
GetMetadataAsync
```

Implementation:

```text
AzureBlobStorage
```

Local development:

```text
Azurite
```

This becomes essential in Phase 5 when Template Service starts storing templates.

---

# 31. Blob Storage Container Strategy

Prepare containers:

```text
templates
documents
attachments
exports
```

Phase 4 only establishes the infrastructure abstraction.

Template/document usage comes later.

---

# 32. Messaging Abstraction

Create:

```text
IMessagePublisher
IMessageConsumer
```

Use:

```text
Azure Service Bus
```

Local development should use a supported local messaging/emulator approach.

Events should use contracts from:

```text
Edp.Shared.Contracts
```

---

# 33. Initial Events

Prepare event infrastructure for:

```text
UserRegistered
OrganizationCreated
OrganizationMemberAdded
OrganizationMemberRemoved
```

Future events:

```text
TemplateCreated
TemplateUpdated
DocumentGenerated
WorkflowStarted
ApprovalAssigned
Approved
Rejected
Signed
Archived
```

---

# 34. Event Envelope

All events should use a common envelope.

Example:

```json
{
  "eventId": "guid",
  "eventType": "OrganizationCreated",
  "occurredAt": "2026-08-09T10:00:00Z",
  "organizationId": "guid",
  "userId": "guid",
  "correlationId": "guid",
  "version": 1,
  "data": {}
}
```

This creates consistency across the platform.

---

# 35. Redis Infrastructure

Create:

```text
ICacheService
```

Capabilities:

```text
GetAsync
SetAsync
RemoveAsync
ExistsAsync
```

Use Redis for:

```text
Session-related caching
Distributed locks
Reference data
Rate limiting
Short-lived application cache
```

Do not use Redis as the system of record.

---

# 36. Audit Infrastructure

Create the foundation for:

```text
AuditLogs
```

Example:

```text
AuditLogs
--------------------------------
Id
OrganizationId
UserId
Action
EntityType
EntityId
Timestamp
CorrelationId
IpAddress
Metadata
```

Initial events:

```text
UserRegistered
UserLoggedIn
OrganizationCreated
MemberAdded
MemberRemoved
RoleChanged
```

---

# 37. Gateway Implementation

The Gateway created in Phase 3 now becomes security-aware.

Pipeline:

```text
Request
   ↓
Correlation ID
   ↓
Exception Handler
   ↓
HTTPS
   ↓
Authentication
   ↓
Authorization
   ↓
Rate Limiting
   ↓
Routing
   ↓
Service
```

Gateway must reject:

```text
Missing token
Expired token
Invalid issuer
Invalid audience
Invalid signature
```

---

# 38. Service-to-Service Security

Do not assume that an internal network automatically makes services trusted.

Future service calls should support:

```text
Managed Identity
OAuth2 Client Credentials
```

For local development:

```text
Development credentials
```

For Azure:

```text
Managed Identity
```

---

# 39. Docker Infrastructure

Extend the Phase 3 Docker setup:

```text
docker/
├── docker-compose.yml
├── sql/
├── redis/
└── azurite/
```

Local platform:

```text
Gateway
Identity API
Organization API
SQL Server
Redis
Azurite
```

React can run separately with Vite/Node.

---

# 40. Local Developer Experience

The target should eventually be:

```bash
docker compose up -d
```

followed by:

```bash
dotnet run
```

or a single development orchestration command.

Developers should not manually configure:

```text
SQL Server
Redis
Blob Storage
```

---

# 41. Database Migrations

Each service owns its migrations.

Example:

```text
Edp.Identity.Infrastructure
    └── Migrations

Edp.Organization.Infrastructure
    └── Migrations
```

Example migration commands:

```bash
dotnet ef migrations add InitialIdentity
dotnet ef database update
```

---

# 42. API Versioning

Continue the Phase 3 API versioning strategy.

Use:

```text
/api/v1/...
```

Examples:

```text
/api/v1/auth/login
/api/v1/users/me
/api/v1/organizations
```

Do not expose unversioned production APIs.

---

# 43. OpenAPI

Each API should expose OpenAPI documentation.

For example:

```text
Identity API
Organization API
Gateway API
```

Scalar can be used as the API documentation UI based on the Phase 3 setup.

---

# 44. Validation

Use a consistent validation approach.

Examples:

```text
RegisterUserCommandValidator
CreateOrganizationCommandValidator
AddOrganizationMemberCommandValidator
```

Validation should happen before domain processing.

---

# 45. Result/Error Pattern

Establish a consistent application response/error strategy:

```text
Success
ValidationError
NotFound
Conflict
Unauthorized
Forbidden
BusinessRuleViolation
```

Avoid every service inventing its own error response.

---

# 46. Testing Strategy

Phase 4 must establish testing standards.

## Unit Tests

Test:

```text
Domain entities
Domain rules
Application handlers
Validators
Authorization policies
Tenant filtering
```

## Integration Tests

Test:

```text
API
Database
Authentication
Organization creation
Membership
Health checks
```

---

# 47. Identity Test Cases

Minimum tests:

```text
Register user
Duplicate email rejected
Login succeeds
Invalid password rejected
Expired token rejected
Refresh token works
Logout invalidates session
Current user returned
```

---

# 48. Organization Test Cases

Minimum tests:

```text
Create organization
Owner automatically assigned
Get organization
Update organization
Add member
Remove member
Duplicate membership rejected
Unauthorized user rejected
Cross-organization access rejected
```

The final test is especially important for multi-tenancy.

---

# 49. Security Tests

Implement tests for:

```text
401 Unauthorized
403 Forbidden
Tenant isolation
Invalid JWT
Expired JWT
Invalid audience
Invalid issuer
Missing organization context
Privilege escalation
```

---

# 50. CI Pipeline

Extend Phase 3 CI.

Pipeline:

```text
Checkout
   ↓
Restore
   ↓
Build
   ↓
Unit Tests
   ↓
Integration Tests
   ↓
Code Coverage
   ↓
Format Check
   ↓
Static Analysis
   ↓
Security Scan
   ↓
Publish Artifacts
```

---

# 51. Code Quality Gates

Recommended:

```text
dotnet format
Roslyn analyzers
SonarCloud
NuGet vulnerability scanning
Secret scanning
```

Build should fail for:

```text
Compilation errors
Test failures
Critical vulnerabilities
Formatting violations
```

---

# 52. Documentation Deliverables

Create:

```text
docs/
├── architecture/
│   ├── phase-4-platform-foundation.md
│   ├── identity-architecture.md
│   ├── organization-architecture.md
│   └── multi-tenancy.md
│
├── security/
│   ├── authentication.md
│   ├── authorization.md
│   └── tenant-isolation.md
│
├── database/
│   ├── identity-database.md
│   └── organization-database.md
│
└── infrastructure/
    ├── local-development.md
    ├── storage.md
    ├── messaging.md
    └── caching.md
```

---

# 53. Recommended Implementation Order

Do not implement everything simultaneously.

## Step 1 — Shared Domain

```text
Edp.Shared.Domain
Edp.Shared.Application
Edp.Shared.Contracts
```

## Step 2 — Infrastructure

```text
Edp.Shared.Infrastructure
Edp.Shared.Security
```

## Step 3 — Persistence

```text
EF Core
SQL Server
DbContext
Migrations
Repositories
```

## Step 4 — Observability

```text
Serilog
OpenTelemetry
Correlation ID
Health checks
Exception handling
```

## Step 5 — Identity

```text
Identity Domain
Identity Application
Identity Infrastructure
Identity API
```

## Step 6 — Authentication

```text
JWT/OIDC
Claims
Policies
Current User
```

## Step 7 — Organization

```text
Organization Domain
Organization Application
Organization Infrastructure
Organization API
```

## Step 8 — Tenant Context

```text
Current Organization
Tenant validation
Tenant-aware repositories
```

## Step 9 — Storage

```text
IBlobStorage
Azure Blob
Azurite
```

## Step 10 — Messaging

```text
IMessagePublisher
Service Bus
Event Envelope
```

## Step 11 — Caching

```text
Redis
ICacheService
```

## Step 12 — Audit

```text
Audit abstraction
Audit persistence
Initial audit events
```

## Step 13 — Gateway Security

```text
Authentication
Authorization
Service routing
```

## Step 14 — Testing

```text
Unit
Integration
Security
Tenant isolation
```

## Step 15 — CI

```text
Build
Test
Scan
Coverage
Package
```

---

# 54. Phase 4 Definition of Done

Phase 4 is complete only when the following works.

## Application

```text
✓ Gateway starts
✓ Identity API starts
✓ Organization API starts
✓ React application starts
```

## Database

```text
✓ Identity database created
✓ Organization database created
✓ EF migrations work
✓ Database health checks work
```

## Authentication

```text
✓ User can authenticate
✓ JWT/OIDC token validated
✓ Current user available
✓ Unauthorized requests rejected
✓ Forbidden requests rejected
```

## Organization

```text
✓ Organization can be created
✓ Creator becomes owner
✓ Members can be managed
✓ Organization context available
✓ Cross-tenant access prevented
```

## Infrastructure

```text
✓ Redis available
✓ Blob storage abstraction works
✓ Messaging abstraction works
✓ Health checks work
✓ Correlation IDs work
✓ Structured logging works
✓ OpenTelemetry works
```

## Quality

```text
✓ Unit tests pass
✓ Integration tests pass
✓ Security tests pass
✓ CI passes
✓ No secrets committed
✓ Documentation updated
```

---

# 55. Phase 4 API Surface

At the end of the phase:

```text
                    EDP Gateway
                         │
        ┌────────────────┼─────────────────┐
        │                │                 │
        ▼                ▼                 ▼
    Identity        Organization        Health
        │                │
        │                │
        ▼                ▼
 /auth/register     /organizations
 /auth/login        /organizations/{id}
 /auth/refresh      /organizations/{id}/members
 /auth/logout       /me/organizations
 /users/me
```

---

# 56. Phase 4 Event Surface

Initial events:

```text
UserRegistered
UserLoggedIn
OrganizationCreated
OrganizationUpdated
OrganizationMemberAdded
OrganizationMemberRemoved
OrganizationRoleChanged
```

These events should already use the common event envelope so that later services can consume them without redesigning the messaging architecture.

---

# 57. Phase 4 Security Boundary

```text
                         Internet
                            │
                            ▼
                    ┌──────────────┐
                    │    Gateway   │
                    └──────┬───────┘
                           │
                     JWT/OIDC
                           │
              ┌────────────┴────────────┐
              │                         │
              ▼                         ▼
       Identity Service         Organization Service
              │                         │
              │                         │
              └────────────┬────────────┘
                           │
                    OrganizationId
                           │
                           ▼
                    Tenant Boundary
```

This is important because Template, Document, Workflow, Storage and Audit will all depend on this foundation.

---

# 58. What We Deliberately Do NOT Build in Phase 4

Avoid scope creep.

```text
❌ Template Designer
❌ Word Template Processing
❌ Placeholder Engine
❌ Document Generation
❌ PDF Generation
❌ Workflow Engine
❌ Approval UI
❌ Digital Signature
❌ OCR
❌ AI Template Generator
❌ Bulk Document Generation
❌ SharePoint Integration
❌ DocuSign Integration
```

Those belong to subsequent phases.

---

# 59. Phase 4 Milestones

| Milestone | Deliverable |
|---|---|
| M1 | Shared platform foundation |
| M2 | Database & persistence |
| M3 | Observability |
| M4 | Authentication |
| M5 | Identity Service |
| M6 | Organization Service |
| M7 | Multi-tenancy |
| M8 | Storage abstraction |
| M9 | Messaging abstraction |
| M10 | Redis |
| M11 | Audit foundation |
| M12 | Gateway security |
| M13 | Integration tests |
| M14 | CI/CD |
| M15 | Documentation |

---

# 60. Git Strategy

Continue the Phase 3 strategy:

```text
main
 │
 └── develop
       │
       ├── feature/phase4-shared-platform
       ├── feature/phase4-persistence
       ├── feature/phase4-observability
       ├── feature/phase4-identity
       ├── feature/phase4-organization
       ├── feature/phase4-multitenancy
       ├── feature/phase4-storage
       ├── feature/phase4-messaging
       ├── feature/phase4-caching
       ├── feature/phase4-audit
       └── feature/phase4-testing
```

Each feature should follow:

```text
Code
 ↓
Unit Tests
 ↓
Integration Tests where applicable
 ↓
Pull Request
 ↓
Code Review
 ↓
Merge → develop
```

---

# 61. Phase 4 Final Architecture

```text
                    ┌───────────────────────┐
                    │      React Web        │
                    └───────────┬───────────┘
                                │
                                ▼
                    ┌───────────────────────┐
                    │     EDP Gateway       │
                    │                       │
                    │ Auth                  │
                    │ Authorization         │
                    │ Rate Limiting         │
                    │ Correlation            │
                    │ Routing               │
                    └───────────┬───────────┘
                                │
              ┌─────────────────┼──────────────────┐
              │                 │                  │
              ▼                 ▼                  ▼
       ┌─────────────┐  ┌───────────────┐  ┌─────────────┐
       │  Identity   │  │ Organization  │  │   Future    │
       │   Service   │  │    Service    │  │  Services   │
       └──────┬──────┘  └───────┬───────┘  └─────────────┘
              │                 │
              ▼                 ▼
       ┌─────────────┐  ┌───────────────┐
       │ Identity DB │  │ Organization  │
       │             │  │     DB       │
       └─────────────┘  └───────────────┘

 ┌──────────────────────────────────────────────────────┐
 │                Shared Platform                       │
 │                                                      │
 │ Security │ Persistence │ Logging │ Telemetry         │
 │ Storage  │ Messaging   │ Cache   │ Audit             │
 │ Errors   │ Health      │ Config  │ Tenant Context    │
 └──────────────────────────────────────────────────────┘

 ┌──────────────────────────────────────────────────────┐
 │             Local / Azure Infrastructure             │
 │                                                      │
 │ SQL │ Redis │ Blob Storage │ Service Bus │ Key Vault │
 └──────────────────────────────────────────────────────┘
```

---

# 62. Phase 4 Success Criteria

> **After Phase 4, every future business service can be created on top of a production-grade platform foundation without reinventing authentication, authorization, tenant isolation, persistence, logging, telemetry, storage, messaging, caching, error handling, or audit infrastructure.**

The next phase should therefore be:

```text
Phase 5 — Template Service
```

with:

```text
Template Upload
Template Metadata
Template Versioning
Placeholder Definitions
Template Validation
Template Activation/Deactivation
```

The overall progression is:

```text
Phase 1 → Product/MVP Definition
Phase 2 → Repository & Engineering Foundation
Phase 3 → Solution Bootstrap
Phase 4 → Platform Infrastructure + Identity + Organization
Phase 5 → Template Service
Phase 6 → Document Service
Phase 7 → Workflow & Approval
Phase 8 → Notification
Phase 9 → Audit & Advanced Security
Phase 10 → AI / OCR / Advanced Automation
Phase 11 → Production Cloud Deployment
```
