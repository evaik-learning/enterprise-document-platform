# Phase 3 — Development Environment & Solution Bootstrap

**Project:** Enterprise Document Automation Platform  
**Repository:** `EnterpriseDocumentPlatform`  
**Phase:** 3 — Development Environment & Solution Bootstrap  
**Target Framework:** .NET 10  
**Frontend:** React + TypeScript  
**Cloud Target:** Microsoft Azure  
**Primary CI/CD:** Azure DevOps  
**Containerization:** Docker

---

# 1. Purpose

Phase 3 establishes the complete development foundation for the Enterprise Document Automation Platform.

The objective is to ensure that every developer can clone the repository and immediately:

- Restore dependencies.
- Build the complete solution.
- Run all backend projects.
- Run the React application.
- Run the platform using Docker.
- Execute unit tests.
- Execute integration tests.
- Validate code quality.
- Generate API documentation.
- Run the initial CI pipeline.

No business functionality is implemented in this phase.

This phase establishes the technical foundation on which all subsequent modules will be developed.

---

# 2. Phase Objectives

The following objectives must be completed.

## 2.1 Backend

Create the complete `.NET 10` solution containing:

- Gateway
- Identity Service
- Organization Service
- Template Service
- Document Service
- Workflow Service
- Storage Service
- Notification Service
- Audit Service
- Shared libraries
- Unit tests
- Integration tests

## 2.2 Frontend

Create the React application with:

- React
- TypeScript
- Vite
- React Router
- API client foundation
- Environment configuration
- Authentication foundation
- Shared UI structure

## 2.3 DevOps

Create:

- Dockerfiles
- Docker Compose
- Azure DevOps CI pipeline
- Build validation
- Unit test execution
- Frontend build validation
- Code quality foundation

## 2.4 Developer Experience

Establish:

- Naming conventions
- Folder conventions
- Project conventions
- Package management
- Environment configuration
- Git conventions
- Local development instructions

---

# 3. Target Repository Structure

The repository created in Phase 2 will evolve into the following structure.

```text
EnterpriseDocumentPlatform/
│
├── src/
│   │
│   ├── Gateway/
│   │   └── Edp.Gateway/
│   │
│   ├── Services/
│   │   ├── Identity/
│   │   │   └── Edp.IdentityService/
│   │   │
│   │   ├── Organization/
│   │   │   └── Edp.OrganizationService/
│   │   │
│   │   ├── Template/
│   │   │   └── Edp.TemplateService/
│   │   │
│   │   ├── Document/
│   │   │   └── Edp.DocumentService/
│   │   │
│   │   ├── Workflow/
│   │   │   └── Edp.WorkflowService/
│   │   │
│   │   ├── Storage/
│   │   │   └── Edp.StorageService/
│   │   │
│   │   ├── Notification/
│   │   │   └── Edp.NotificationService/
│   │   │
│   │   └── Audit/
│   │       └── Edp.AuditService/
│   │
│   ├── Shared/
│   │   ├── Edp.SharedKernel/
│   │   ├── Edp.Contracts/
│   │   ├── Edp.Infrastructure/
│   │   ├── Edp.Observability/
│   │   └── Edp.Security/
│   │
│   └── Web/
│       └── edp-web/
│
├── tests/
│   │
│   ├── Unit/
│   │   ├── Edp.Gateway.Tests/
│   │   ├── Edp.IdentityService.Tests/
│   │   ├── Edp.OrganizationService.Tests/
│   │   ├── Edp.TemplateService.Tests/
│   │   ├── Edp.DocumentService.Tests/
│   │   ├── Edp.WorkflowService.Tests/
│   │   ├── Edp.StorageService.Tests/
│   │   ├── Edp.NotificationService.Tests/
│   │   └── Edp.AuditService.Tests/
│   │
│   └── Integration/
│       └── Edp.IntegrationTests/
│
├── docs/
│   ├── architecture/
│   ├── api/
│   ├── database/
│   ├── events/
│   ├── development/
│   └── deployment/
│
├── infra/
│   ├── bicep/
│   ├── environments/
│   └── modules/
│
├── pipelines/
│   ├── azure-pipelines.yml
│   └── templates/
│
├── scripts/
│   ├── setup/
│   ├── database/
│   └── development/
│
├── docker/
│   ├── gateway/
│   ├── services/
│   └── web/
│
├── Directory.Build.props
├── Directory.Build.targets
├── Directory.Packages.props
├── global.json
├── .editorconfig
├── .gitignore
├── .dockerignore
├── docker-compose.yml
├── docker-compose.override.yml
├── EnterpriseDocumentPlatform.slnx
└── README.md
```

---

# 4. .NET Solution

The solution must target:

```text
.NET 10
```

The preferred solution file is:

```text
EnterpriseDocumentPlatform.slnx
```

If the development environment/tooling requires the traditional solution format, `.sln` may be used temporarily, but `.slnx` should be the standard target.

---

# 5. Global SDK Configuration

Create:

```text
global.json
```

Purpose:

- Pin the .NET SDK.
- Ensure developer machines use a consistent SDK.
- Ensure CI uses the same SDK family.

Example:

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestPatch",
    "allowPrerelease": false
  }
}
```

The exact SDK patch version should be updated according to the approved project baseline.

---

# 6. Central Package Management

The solution must use centralized NuGet package management.

Create:

```text
Directory.Packages.props
```

Example:

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>

  <ItemGroup>

    <!-- ASP.NET Core / Microsoft -->

    <PackageVersion Include="Microsoft.AspNetCore.OpenApi"
                    Version="10.0.x" />

    <PackageVersion Include="Microsoft.AspNetCore.Authentication.JwtBearer"
                    Version="10.0.x" />

    <PackageVersion Include="Microsoft.EntityFrameworkCore"
                    Version="10.0.x" />

    <PackageVersion Include="Microsoft.EntityFrameworkCore.SqlServer"
                    Version="10.0.x" />

    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design"
                    Version="10.0.x" />

    <!-- API Versioning -->

    <PackageVersion Include="Asp.Versioning.Http"
                    Version="x.x.x" />

    <PackageVersion Include="Asp.Versioning.Mvc.ApiExplorer"
                    Version="x.x.x" />

    <!-- OpenAPI -->

    <PackageVersion Include="Scalar.AspNetCore"
                    Version="x.x.x" />

    <!-- Validation -->

    <PackageVersion Include="FluentValidation"
                    Version="x.x.x" />

    <!-- Resilience -->

    <PackageVersion Include="Microsoft.Extensions.Http.Resilience"
                    Version="10.0.x" />

    <!-- Azure -->

    <PackageVersion Include="Azure.Identity"
                    Version="x.x.x" />

    <PackageVersion Include="Azure.Storage.Blobs"
                    Version="x.x.x" />

    <PackageVersion Include="Azure.Messaging.ServiceBus"
                    Version="x.x.x" />

    <!-- Logging -->

    <PackageVersion Include="Serilog.AspNetCore"
                    Version="x.x.x" />

    <PackageVersion Include="Serilog.Sinks.Console"
                    Version="x.x.x" />

    <!-- OpenTelemetry -->

    <PackageVersion Include="OpenTelemetry.Extensions.Hosting"
                    Version="x.x.x" />

    <PackageVersion Include="OpenTelemetry.Exporter.OpenTelemetryProtocol"
                    Version="x.x.x" />

    <PackageVersion Include="OpenTelemetry.Instrumentation.AspNetCore"
                    Version="x.x.x" />

    <PackageVersion Include="OpenTelemetry.Instrumentation.Http"
                    Version="x.x.x" />

    <!-- Testing -->

    <PackageVersion Include="xunit"
                    Version="x.x.x" />

    <PackageVersion Include="xunit.runner.visualstudio"
                    Version="x.x.x" />

    <PackageVersion Include="Microsoft.NET.Test.Sdk"
                    Version="x.x.x" />

    <PackageVersion Include="FluentAssertions"
                    Version="x.x.x" />

    <PackageVersion Include="Moq"
                    Version="x.x.x" />

    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing"
                    Version="10.0.x" />

  </ItemGroup>
</Project>
```

Actual package versions must be pinned to the approved versions during implementation rather than using floating versions.

---

# 7. Common Build Configuration

Create:

```text
Directory.Build.props
```

Recommended responsibilities:

- Target framework.
- Nullable reference types.
- Implicit usings.
- XML documentation.
- Treat warnings as errors where appropriate.
- Code analysis.
- Deterministic builds.

Example:

```xml
<Project>

  <PropertyGroup>

    <TargetFramework>net10.0</TargetFramework>

    <ImplicitUsings>enable</ImplicitUsings>

    <Nullable>enable</Nullable>

    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>

    <GenerateDocumentationFile>true</GenerateDocumentationFile>

    <Deterministic>true</Deterministic>

    <AnalysisLevel>latest</AnalysisLevel>

  </PropertyGroup>

</Project>
```

---

# 8. Build Targets

Create:

```text
Directory.Build.targets
```

This file may later contain:

- Static analysis.
- Generated source checks.
- Architecture validation.
- Build-time conventions.
- API contract validation.

Initially keep it lightweight.

---

# 9. Naming Conventions

All project naming must follow a consistent convention.

## 9.1 Root Namespace

```text
Edp
```

EDP means:

```text
Enterprise Document Platform
```

---

## 9.2 Services

Use:

```text
Edp.<ServiceName>Service
```

Examples:

```text
Edp.IdentityService
Edp.OrganizationService
Edp.TemplateService
Edp.DocumentService
Edp.WorkflowService
Edp.StorageService
Edp.NotificationService
Edp.AuditService
```

---

## 9.3 Gateway

```text
Edp.Gateway
```

---

## 9.4 Shared Projects

```text
Edp.SharedKernel
Edp.Contracts
Edp.Infrastructure
Edp.Observability
Edp.Security
```

---

# 10. Project Architecture

Each service must initially follow a modular architecture.

Example:

```text
Edp.TemplateService/
│
├── Api/
├── Application/
├── Domain/
├── Infrastructure/
├── Contracts/
├── Configuration/
├── Extensions/
├── Middleware/
├── Program.cs
└── Edp.TemplateService.csproj
```

Responsibilities:

### Api

HTTP endpoints/controllers.

### Application

Business use cases and application orchestration.

### Domain

Business entities, value objects, domain events and business rules.

### Infrastructure

Database, messaging, storage and external integrations.

### Contracts

Request/response DTOs and API contracts.

### Configuration

Options and configuration models.

### Extensions

Dependency injection and application registration.

### Middleware

HTTP middleware specific to the service.

---

# 11. Gateway Project

Create:

```text
src/Gateway/Edp.Gateway/
```

Responsibilities:

- API entry point.
- Authentication enforcement.
- Authorization.
- API versioning.
- Routing.
- Rate limiting.
- Correlation ID.
- Request logging.
- Global exception handling.
- OpenAPI/Scalar.
- Service discovery/configuration.
- Health checks.

Initial structure:

```text
Edp.Gateway/
│
├── Configuration/
├── Middleware/
├── Extensions/
├── Health/
├── Routing/
├── Security/
├── Program.cs
└── Edp.Gateway.csproj
```

The Gateway must not contain business logic.

---

# 12. Identity Service

Create:

```text
src/Services/Identity/Edp.IdentityService/
```

Initial responsibilities:

- User identity.
- Authentication integration.
- User profile.
- Roles.
- Permissions.
- Organization membership.

Initial project structure:

```text
Edp.IdentityService/
├── Api/
├── Application/
├── Domain/
├── Infrastructure/
├── Contracts/
├── Configuration/
├── Extensions/
├── Program.cs
└── Edp.IdentityService.csproj
```

Business implementation will be added in later phases.

---

# 13. Organization Service

Create:

```text
src/Services/Organization/Edp.OrganizationService/
```

Responsibilities:

- Organizations.
- Organization settings.
- Departments.
- Teams.
- Membership relationships.

---

# 14. Template Service

Create:

```text
src/Services/Template/Edp.TemplateService/
```

Responsibilities:

- Template upload.
- Template metadata.
- Template versioning.
- Placeholder definitions.
- Template validation.
- Template activation/deactivation.

Example placeholders:

```text
{{CustomerName}}
{{InvoiceDate}}
{{Address}}
{{ContractNumber}}
```

---

# 15. Document Service

Create:

```text
src/Services/Document/Edp.DocumentService/
```

Responsibilities:

- Document generation.
- Document metadata.
- Word generation.
- PDF generation.
- Document versions.
- Document lifecycle.

---

# 16. Workflow Service

Create:

```text
src/Services/Workflow/Edp.WorkflowService/
```

Responsibilities:

```text
Draft
  ↓
Review
  ↓
Approval
  ↓
Digital Signature
  ↓
Completed
```

The workflow engine will be implemented in a later phase.

---

# 17. Storage Service

Create:

```text
src/Services/Storage/Edp.StorageService/
```

Responsibilities:

- Blob storage.
- Document storage.
- Metadata.
- Upload/download.
- Secure access.
- Retention.

Target platform:

```text
Azure Blob Storage
```

---

# 18. Notification Service

Create:

```text
src/Services/Notification/Edp.NotificationService/
```

Responsibilities:

- Email.
- SMS.
- Teams notifications.
- Notification templates.
- Notification delivery tracking.

---

# 19. Audit Service

Create:

```text
src/Services/Audit/Edp.AuditService/
```

Responsibilities:

- Audit events.
- User activity.
- Document activity.
- Workflow activity.
- Security events.

Example:

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

# 20. Shared Kernel

Create:

```text
src/Shared/Edp.SharedKernel/
```

Initial contents:

```text
Common/
Entities/
ValueObjects/
Exceptions/
Results/
Constants/
```

Examples:

```text
Entity
AggregateRoot
ValueObject
DomainException
Result
PagedResult
```

The Shared Kernel must remain small.

Do not place business logic from individual services inside the Shared Kernel.

---

# 21. Contracts

Create:

```text
src/Shared/Edp.Contracts/
```

Responsibilities:

- Event contracts.
- Integration messages.
- Common API contracts.

Example:

```text
TemplateCreatedEvent
DocumentGeneratedEvent
WorkflowStartedEvent
ApprovalAssignedEvent
```

---

# 22. Infrastructure

Create:

```text
src/Shared/Edp.Infrastructure/
```

Responsibilities:

- Database abstractions.
- Messaging infrastructure.
- Azure integrations.
- Common persistence components.

Service-specific infrastructure should remain inside each service whenever possible.

---

# 23. Observability

Create:

```text
src/Shared/Edp.Observability/
```

Initial support:

- Structured logging.
- Correlation IDs.
- OpenTelemetry.
- Metrics.
- Distributed tracing.
- Application Insights integration.

Standard telemetry fields:

```text
TraceId
SpanId
CorrelationId
UserId
OrganizationId
ServiceName
Environment
```

---

# 24. Security Library

Create:

```text
src/Shared/Edp.Security/
```

Responsibilities:

- Authentication abstractions.
- Authorization policies.
- Claims handling.
- Permission constants.
- Security helpers.

---

# 25. Testing Projects

Create unit test projects for each major service.

Example:

```text
tests/Unit/Edp.TemplateService.Tests/
```

Test naming:

```text
CreateTemplateTests.cs
UpdateTemplateTests.cs
TemplateValidationTests.cs
```

Test method convention:

```text
MethodName_State_ExpectedResult
```

Example:

```text
CreateTemplate_WhenTemplateIsValid_ShouldCreateTemplate()
```

---

# 26. Integration Tests

Create:

```text
tests/Integration/Edp.IntegrationTests/
```

Initial infrastructure should support:

- ASP.NET Core integration testing.
- Test server.
- API endpoint testing.
- Database integration tests.
- Messaging integration tests.

External Azure services should initially be abstracted behind interfaces.

---

# 27. React Application

Create:

```text
src/Web/edp-web/
```

Technology:

```text
React
TypeScript
Vite
```

Recommended structure:

```text
edp-web/
│
├── public/
│
├── src/
│   ├── app/
│   │   ├── App.tsx
│   │   ├── routes.tsx
│   │   └── providers.tsx
│   │
│   ├── components/
│   │   ├── common/
│   │   ├── layout/
│   │   └── forms/
│   │
│   ├── features/
│   │   ├── identity/
│   │   ├── organization/
│   │   ├── templates/
│   │   ├── documents/
│   │   ├── workflows/
│   │   └── approvals/
│   │
│   ├── services/
│   │   ├── api/
│   │   └── auth/
│   │
│   ├── hooks/
│   ├── models/
│   ├── utils/
│   ├── config/
│   ├── styles/
│   └── main.tsx
│
├── .env.development
├── .env.production
├── package.json
├── tsconfig.json
├── vite.config.ts
└── Dockerfile
```

---

# 28. React Dependencies

Initial frontend dependencies:

```text
react
react-dom
react-router-dom
typescript
vite
```

Recommended future dependencies:

```text
axios
@tanstack/react-query
react-hook-form
zod
```

UI framework selection should be finalized before implementing application screens.

---

# 29. Frontend Environment Configuration

Development:

```text
VITE_API_BASE_URL=https://localhost:7000
```

Production configuration must be injected through the deployment environment.

Do not commit:

```text
passwords
client secrets
API keys
connection strings
tokens
```

---

# 30. Docker Strategy

Every deployable application must have a Dockerfile.

Initial container targets:

```text
Gateway
Identity Service
Organization Service
Template Service
Document Service
Workflow Service
Storage Service
Notification Service
Audit Service
React Web
```

---

# 31. Backend Dockerfile Standard

Use a multi-stage build.

Conceptually:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY . .

RUN dotnet restore
RUN dotnet publish \
    src/Gateway/Edp.Gateway/Edp.Gateway.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Edp.Gateway.dll"]
```

Each service should eventually have its own optimized Dockerfile.

---

# 32. React Dockerfile

Use a two-stage build.

Stage 1:

```text
Node
```

Build React application.

Stage 2:

```text
Nginx
```

Serve static content.

Conceptual flow:

```text
Node
 ↓
npm ci
 ↓
npm run build
 ↓
dist/
 ↓
Nginx
```

---

# 33. Docker Compose

Create:

```text
docker-compose.yml
```

Initial local infrastructure:

```text
edp-gateway
edp-identity
edp-organization
edp-template
edp-document
edp-workflow
edp-storage
edp-notification
edp-audit
edp-web
```

Infrastructure containers may later include:

```text
SQL Server
Redis
Azurite
Service Bus emulator/local replacement where appropriate
```

The first Docker Compose version should focus on application startup and local development.

---

# 34. Local Development Ports

Reserve predictable ports.

Example:

```text
Gateway             7000
Identity Service    7001
Organization        7002
Template            7003
Document            7004
Workflow            7005
Storage             7006
Notification        7007
Audit               7008
React               5173
```

The actual ports should be centralized in development configuration rather than duplicated throughout the repository.

---

# 35. Health Checks

Every backend service must expose:

```text
/health
```

and preferably:

```text
/health/live
/health/ready
```

Example semantics:

### Liveness

Indicates that the process is running.

### Readiness

Indicates that required dependencies are available.

Dependencies may include:

```text
Database
Service Bus
Blob Storage
Redis
External APIs
```

---

# 36. API Documentation

All APIs must support OpenAPI documentation.

The Gateway should expose the API documentation UI.

Target:

```text
Scalar
```

OpenAPI should be generated using the supported .NET 10 OpenAPI approach.

The API documentation must include:

- API version.
- Endpoint description.
- Request schema.
- Response schema.
- Error responses.
- Authentication requirements.

---

# 37. API Versioning

All public APIs should use versioning.

Recommended initial convention:

```text
/api/v1/templates
/api/v1/documents
/api/v1/workflows
```

Avoid breaking API changes within the same major API version.

---

# 38. Configuration

Use the standard .NET configuration hierarchy:

```text
appsettings.json
appsettings.Development.json
Environment Variables
Azure Key Vault
```

Configuration classes should use strongly typed options.

Example:

```text
DatabaseOptions
StorageOptions
ServiceBusOptions
AuthenticationOptions
OpenTelemetryOptions
```

Secrets must never be stored in:

```text
appsettings.json
appsettings.Development.json
source code
Git
Dockerfiles
```

For local development, use:

```text
dotnet user-secrets
```

where appropriate.

---

# 39. Git Configuration

Branches:

```text
main
develop
feature/*
bugfix/*
hotfix/*
release/*
```

Primary development branch:

```text
develop
```

Production branch:

```text
main
```

Feature example:

```text
feature/template-service
feature/document-generation
feature/workflow-engine
```

---

# 40. Commit Convention

Use conventional commit-style messages.

Examples:

```text
feat: add template service bootstrap
feat: add document API foundation
fix: correct gateway health check
test: add template validation tests
docs: update API documentation
refactor: improve service registration
build: update dotnet SDK
ci: add Azure DevOps build pipeline
```

---

# 41. .editorconfig

Create a repository-level:

```text
.editorconfig
```

The configuration should standardize:

- UTF-8.
- Indentation.
- New lines.
- C# formatting.
- Nullable-related conventions.
- Naming rules.
- TypeScript formatting.

All developers and CI agents should use the repository configuration.

---

# 42. .gitignore

The `.gitignore` must exclude:

```text
bin/
obj/
.vs/
.idea/
node_modules/
dist/
coverage/
TestResults/
*.user
*.suo
.env
.env.local
```

Never ignore files required for reproducible builds.

---

# 43. .dockerignore

Create:

```text
.dockerignore
```

Exclude:

```text
.git
.gitignore
.vs
.idea
bin
obj
node_modules
dist
TestResults
coverage
docs
*.user
*.suo
```

Only required build artifacts and source files should enter the Docker build context.

---

# 44. Initial CI Pipeline

Create:

```text
pipelines/azure-pipelines.yml
```

The first CI pipeline must perform:

```text
Checkout
   ↓
Install .NET 10 SDK
   ↓
Restore
   ↓
Build
   ↓
Unit Tests
   ↓
Integration Tests
   ↓
Frontend npm install
   ↓
Frontend Build
   ↓
Publish Test Results
   ↓
Publish Code Coverage
```

---

# 45. Azure DevOps Pipeline Structure

Recommended pipeline:

```yaml
trigger:
  branches:
    include:
      - main
      - develop
      - feature/*

pr:
  branches:
    include:
      - main
      - develop

pool:
  vmImage: ubuntu-latest

stages:

- stage: Build
  displayName: Build

  jobs:

  - job: Backend
    displayName: Build Backend

    steps:

    - task: UseDotNet@2
      inputs:
        packageType: sdk
        version: '10.x'

    - script: dotnet restore
      displayName: Restore

    - script: dotnet build --configuration Release --no-restore
      displayName: Build

  - job: Frontend
    displayName: Build Frontend

    steps:

    - task: NodeTool@0
      inputs:
        versionSpec: '22.x'

    - script: |
        cd src/Web/edp-web
        npm ci
        npm run build
      displayName: Build React Application
```

The exact Node version should be pinned to the project's approved frontend runtime version.

---

# 46. Unit Test Pipeline

Add:

```yaml
- script: |
    dotnet test \
      --configuration Release \
      --no-build \
      --logger trx \
      --collect:"XPlat Code Coverage"
  displayName: Run Unit Tests
```

Publish:

```text
Test Results
Code Coverage
```

---

# 47. Frontend Validation

The frontend pipeline should eventually include:

```text
npm ci
npm run lint
npm run test
npm run build
```

A failed lint or test must fail the CI build.

---

# 48. Code Quality

Initial quality gates:

```text
Build must succeed
Unit tests must pass
Frontend build must succeed
No compilation warnings
No secrets committed
```

Future quality gates:

```text
SonarCloud
Code coverage threshold
Dependency vulnerability scanning
Container scanning
License scanning
Architecture tests
API contract validation
```

---

# 49. Local Development Commands

Backend:

```bash
dotnet restore
dotnet build
dotnet test
```

Run Gateway:

```bash
dotnet run --project src/Gateway/Edp.Gateway
```

Run React:

```bash
cd src/Web/edp-web
npm install
npm run dev
```

Docker:

```bash
docker compose up --build
```

Stop:

```bash
docker compose down
```

---

# 50. Developer Setup Requirements

Each developer machine should have:

```text
.NET 10 SDK
Node.js
npm
Git
Docker Desktop
Visual Studio / VS Code / Rider
Azure CLI
```

Optional:

```text
Azure Storage Explorer
SQL Server Management Studio
Postman
Azure CLI extensions
```

---

# 51. Initial README

The root README must explain:

```text
Project Overview
Architecture
Technology Stack
Repository Structure
Prerequisites
Local Setup
Running Backend
Running Frontend
Running Docker
Running Tests
API Documentation
Branch Strategy
Contribution Guidelines
```

Example startup sequence:

```text
git clone <repository>

cd EnterpriseDocumentPlatform

dotnet restore

dotnet build

dotnet test

cd src/Web/edp-web

npm install

npm run dev
```

---

# 52. Architecture Rules

The following rules are mandatory from Phase 3 onward.

## Rule 1

Gateway must not contain business logic.

## Rule 2

Services must own their business domains.

## Rule 3

Services should not directly access another service's database.

## Rule 4

Cross-service communication should use:

```text
HTTP APIs
Events
Messaging
```

## Rule 5

Shared libraries must not become a dumping ground.

## Rule 6

Secrets must never be committed.

## Rule 7

All services must support health checks.

## Rule 8

All public APIs must be versioned.

## Rule 9

All services must support structured logging.

## Rule 10

All deployable applications must be containerizable.

---

# 53. Initial Service Communication

The initial architecture should follow:

```text
                    React
                      │
                      ▼
                 API Gateway
                      │
       ┌──────────────┼──────────────┐
       ▼              ▼              ▼
   Template       Document       Workflow
    Service        Service        Service
       │              │              │
       └──────────────┼──────────────┘
                      │
                 Azure Service Bus
                      │
       ┌──────────────┼──────────────┐
       ▼              ▼              ▼
   Notification     Storage          Audit
```

Identity and Organization services provide cross-cutting business capabilities.

---

# 54. Initial Event Infrastructure

The messaging infrastructure must be prepared for events such as:

```text
TemplateCreated
TemplateUpdated

DocumentGenerated
DocumentUpdated

WorkflowStarted
ApprovalAssigned

Approved
Rejected

Signed
Archived
```

No complex event processing is required in Phase 3.

Only the infrastructure foundation should be established.

---

# 55. Initial Database Strategy

The platform will ultimately use:

```text
Azure SQL
Azure Cosmos DB
Azure Blob Storage
Redis
```

Phase 3 should only establish the technical integration boundaries.

Do not implement the complete database schema in this phase.

Database design from the earlier architecture phase will be implemented incrementally with each service.

---

# 56. Environment Strategy

Supported environments:

```text
Local
Development
QA
UAT
Production
```

Configuration must be environment-specific.

Example:

```text
appsettings.json
appsettings.Development.json
appsettings.QA.json
appsettings.UAT.json
appsettings.Production.json
```

Production secrets must come from Azure Key Vault or equivalent secure secret management.

---

# 57. Definition of Done

Phase 3 is complete when:

### Solution

- [ ] `.NET 10` solution created.
- [ ] All initial projects created.
- [ ] Projects compile successfully.
- [ ] Project references follow architecture rules.

### Backend

- [ ] Gateway created.
- [ ] Identity Service created.
- [ ] Organization Service created.
- [ ] Template Service created.
- [ ] Document Service created.
- [ ] Workflow Service created.
- [ ] Storage Service created.
- [ ] Notification Service created.
- [ ] Audit Service created.
- [ ] Shared libraries created.

### Frontend

- [ ] React application created.
- [ ] TypeScript configured.
- [ ] Vite configured.
- [ ] Routing foundation created.
- [ ] API client foundation created.
- [ ] Environment configuration created.

### Testing

- [ ] Unit test projects created.
- [ ] Integration test project created.
- [ ] Test execution verified.

### Docker

- [ ] Backend Dockerfile created.
- [ ] Frontend Dockerfile created.
- [ ] Docker Compose created.
- [ ] Containers start successfully.

### API

- [ ] OpenAPI enabled.
- [ ] Scalar configured.
- [ ] API versioning configured.
- [ ] Health checks configured.

### DevOps

- [ ] Azure DevOps pipeline created.
- [ ] Backend restore succeeds.
- [ ] Backend build succeeds.
- [ ] Unit tests execute.
- [ ] Frontend build succeeds.
- [ ] Test results published.

### Developer Experience

- [ ] `.editorconfig` created.
- [ ] `.gitignore` created.
- [ ] `.dockerignore` created.
- [ ] `README.md` updated.
- [ ] Naming conventions documented.
- [ ] Local development documented.

---

# 58. Phase 3 Deliverables

At the end of Phase 3, the repository should contain:

```text
EnterpriseDocumentPlatform.slnx
global.json
Directory.Build.props
Directory.Build.targets
Directory.Packages.props
.editorconfig
.gitignore
.dockerignore
docker-compose.yml

src/
tests/
docs/
infra/
pipelines/
scripts/
docker/
```

The platform should be able to execute:

```bash
dotnet build
```

```bash
dotnet test
```

```bash
npm run build
```

and:

```bash
docker compose up --build
```

successfully.

---

# 59. Phase 3 Exit Criteria

The most important outcome of Phase 3 is:

> **A clean developer machine or CI agent can clone the repository, restore the solution, build every backend project, build the React application, execute tests, and start the platform using Docker without manually modifying source code.**

At this point the platform is technically ready for **Phase 4 — Core Platform Infrastructure & Identity/Organization implementation**.

---

# 60. Recommended Phase 3 Execution Order

Implementation should happen in this order:

```text
1. Install/pin .NET 10 SDK
        ↓
2. Create solution
        ↓
3. Configure Directory.Build.props
        ↓
4. Configure central NuGet management
        ↓
5. Create Shared projects
        ↓
6. Create Gateway
        ↓
7. Create service projects
        ↓
8. Create test projects
        ↓
9. Configure OpenAPI + Scalar
        ↓
10. Configure health checks
        ↓
11. Configure logging/observability foundation
        ↓
12. Create React application
        ↓
13. Configure Dockerfiles
        ↓
14. Create Docker Compose
        ↓
15. Create Azure DevOps pipeline
        ↓
16. Verify local build
        ↓
17. Verify CI build
        ↓
18. Freeze Phase 3 baseline
```

---

# 61. Phase 3 Baseline

The repository baseline after this phase should be tagged:

```text
phase-3-foundation
```

Suggested Git sequence:

```bash
git checkout develop

git checkout -b feature/phase-3-solution-bootstrap

# Implement Phase 3

git add .

git commit -m "build: bootstrap dotnet 10 solution and react application"

git push origin feature/phase-3-solution-bootstrap
```

After CI validation:

```text
Pull Request
      ↓
Code Review
      ↓
CI Validation
      ↓
Merge → develop
      ↓
Tag Phase 3 Baseline
```

---

# 62. Important Implementation Principle

Do **not** implement Template, Document, Workflow, Approval, Signature, or other business functionality during this phase.

Phase 3 is strictly about:

```text
Structure
+
Standards
+
Tooling
+
Build
+
Testing
+
Containers
+
CI
```

The objective is to create a stable engineering platform before feature development begins.