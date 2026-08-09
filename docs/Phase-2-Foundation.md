# Phase 2 — Foundation

## 1. Objective

The purpose of Phase 2 is to establish the technical foundation of the **Enterprise Document Automation Platform** before implementing business features.

In this phase, the project moves from architecture and planning into actual development.

The key objectives are:

- Create the central source code repository.
- Establish a scalable monorepo structure.
- Separate application code, infrastructure, documentation, tests, scripts, and deployment pipelines.
- Define the initial .NET solution and project boundaries.
- Establish Git branching and collaboration strategy.
- Create the base development environment.
- Introduce common engineering standards and coding conventions.
- Prepare the repository for CI/CD automation.
- Ensure the structure can grow from MVP to an enterprise-scale platform.

---

# 2. Repository Creation

## Repository Name

```text
EnterpriseDocumentPlatform
```

The repository should act as the central source code repository for the entire platform.

It will contain:

- Backend services
- API Gateway
- Frontend application
- Shared libraries
- Automated tests
- Infrastructure as Code
- CI/CD pipelines
- Docker configuration
- Technical documentation
- Development and maintenance scripts

The repository should use a **monorepo approach** during the initial development phase.

A monorepo is recommended because the platform is still evolving rapidly and multiple components will share:

- Domain models
- Contracts
- Event definitions
- Authentication concepts
- Common middleware
- Logging standards
- Configuration patterns
- Build and deployment pipelines

As the platform grows, individual services can still be independently deployed and scaled.

---

# 3. Top-Level Repository Structure

The initial repository structure should be:

```text
EnterpriseDocumentPlatform/
│
├── src/
├── docs/
├── infra/
├── pipelines/
├── tests/
├── scripts/
├── docker/
│
├── .gitignore
├── .editorconfig
├── README.md
├── LICENSE
├── Directory.Build.props
├── Directory.Build.targets
├── Directory.Packages.props
└── EnterpriseDocumentPlatform.sln
```

The main folders have the following responsibilities.

| Folder | Purpose |
|---|---|
| `src` | Production application source code |
| `docs` | Product, architecture, API, database, event and operational documentation |
| `infra` | Azure infrastructure and Infrastructure as Code |
| `pipelines` | CI/CD pipeline definitions |
| `tests` | Unit, integration, contract and end-to-end tests |
| `scripts` | Development, automation and maintenance scripts |
| `docker` | Dockerfiles, Docker Compose and local container configuration |

---

# 4. Source Code Structure

The main application source code will be located under:

```text
src/
```

The initial structure is:

```text
src/
│
├── Gateway/
├── Services/
├── Shared/
└── Web/
```

The purpose of each area is described below.

---

# 5. Gateway

The `Gateway` folder contains the entry point through which external clients communicate with backend services.

Recommended implementation:

```text
src/
└── Gateway/
    └── EnterpriseDocumentPlatform.Gateway/
        ├── Controllers/
        ├── Middleware/
        ├── Configuration/
        ├── Extensions/
        ├── Security/
        ├── Models/
        ├── appsettings.json
        ├── appsettings.Development.json
        └── Program.cs
```

The Gateway will be responsible for:

- External API entry point.
- Authentication and authorization integration.
- Request routing.
- API aggregation where required.
- Correlation ID propagation.
- Global exception handling.
- Request logging.
- Rate limiting.
- Security headers.
- API versioning.
- Health endpoints.
- OpenTelemetry instrumentation.
- Service-to-service communication policies.

For the MVP, the Gateway may act as a **Backend for Frontend (BFF)** for the React application.

A future architecture can evolve toward:

```text
Client
   │
   ▼
API Gateway / BFF
   │
   ├── Identity
   ├── Organization
   ├── Template
   ├── Document
   ├── Workflow
   ├── Storage
   ├── Notification
   └── Audit
```

The Gateway should not contain business logic.

Business rules must remain inside the relevant services.

---

# 6. Services

The `Services` folder contains the platform's business services.

Initial structure:

```text
src/
└── Services/
    │
    ├── Identity/
    ├── Organization/
    ├── Template/
    ├── Document/
    ├── Workflow/
    ├── Storage/
    ├── Notification/
    └── Audit/
```

Each service should be independently organized.

Example:

```text
src/Services/Template/

├── EnterpriseDocumentPlatform.Template.Api/
├── EnterpriseDocumentPlatform.Template.Application/
├── EnterpriseDocumentPlatform.Template.Domain/
└── EnterpriseDocumentPlatform.Template.Infrastructure/
```

This follows a layered architecture approach.

## API

Responsible for:

- HTTP endpoints.
- Request/response models.
- Authentication policies.
- API validation.
- API versioning.
- Mapping between HTTP and application commands/queries.

Example:

```text
Template.Api
```

## Application

Responsible for:

- Use cases.
- Commands.
- Queries.
- Application services.
- Validation.
- Interfaces.
- DTOs.

Example:

```text
CreateTemplateCommand
UpdateTemplateCommand
GetTemplateQuery
UploadTemplateCommand
```

## Domain

Responsible for:

- Domain entities.
- Value objects.
- Domain services.
- Business rules.
- Domain events.
- Enumerations.

Example:

```text
Template
TemplateVersion
TemplatePlaceholder
TemplateStatus
TemplateCreatedEvent
```

## Infrastructure

Responsible for:

- Database implementation.
- Azure Storage integration.
- Messaging.
- External service integrations.
- Repository implementations.
- Event publishing.
- Cache implementation.

Example:

```text
Template.Infrastructure
```

---

# 7. Initial Service Boundaries

The initial service ownership should be defined as follows.

## Identity Service

Responsible for:

- User authentication integration.
- Identity information.
- User roles.
- Claims.
- Access control.

The MVP may use Microsoft Entra ID / Entra External ID depending on the application's identity model.

---

## Organization Service

Responsible for:

- Organizations / tenants.
- Organization membership.
- User-to-organization relationships.
- Organization settings.
- Tenant-level configuration.

---

## Template Service

Responsible for:

- Template upload.
- Template metadata.
- Template versioning.
- Placeholder definitions.
- Template validation.
- Template activation and deactivation.

Example placeholder:

```text
{{CustomerName}}
{{InvoiceDate}}
{{Address}}
{{ContractNumber}}
```

---

## Document Service

Responsible for:

- Document generation.
- Document metadata.
- Document lifecycle.
- Word document creation.
- PDF generation.
- Generated document versions.

Example workflow:

```text
Template
   ↓
Merge Data
   ↓
Generate DOCX
   ↓
Generate PDF
   ↓
Store Document
```

---

## Workflow Service

Responsible for:

- Workflow definitions.
- Workflow instances.
- Approval stages.
- Approval assignments.
- Approval actions.
- Rejection handling.
- Workflow state transitions.

Example:

```text
Draft
  ↓
Review
  ↓
Approval
  ↓
Approved
  ↓
Signing
  ↓
Completed
```

---

## Storage Service

Responsible for:

- Azure Blob Storage integration.
- Document file storage.
- Template file storage.
- Versioned file storage.
- Metadata about stored files.
- Secure file access.

Example storage structure:

```text
/{tenantId}/templates/{templateId}/versions/{versionId}/template.docx

/{tenantId}/documents/{documentId}/versions/{versionId}/document.docx

/{tenantId}/documents/{documentId}/versions/{versionId}/document.pdf
```

---

## Notification Service

Responsible for:

- Email notifications.
- Approval notifications.
- Document generation notifications.
- Workflow status notifications.
- Failure notifications.

Potential integrations:

- Email provider
- Azure Communication Services
- SendGrid
- Microsoft Graph

---

## Audit Service

Responsible for:

- User activity tracking.
- Security-related events.
- Document lifecycle events.
- Approval history.
- Configuration changes.
- Compliance records.

Example:

```text
UserCreated
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

# 8. Shared

The `Shared` folder contains reusable technical components that are common across services.

Structure:

```text
src/
└── Shared/
    │
    ├── EnterpriseDocumentPlatform.SharedKernel/
    ├── EnterpriseDocumentPlatform.Shared.Contracts/
    ├── EnterpriseDocumentPlatform.Shared.Messaging/
    ├── EnterpriseDocumentPlatform.Shared.Infrastructure/
    ├── EnterpriseDocumentPlatform.Shared.Security/
    ├── EnterpriseDocumentPlatform.Shared.Observability/
    └── EnterpriseDocumentPlatform.Shared.Storage/
```

## SharedKernel

Contains:

- Base entity.
- Entity ID abstractions.
- Result types.
- Domain event abstractions.
- Common value objects.

## Shared.Contracts

Contains:

- API contracts.
- Integration event contracts.
- Shared DTOs where necessary.

Care should be taken not to create excessive coupling between services.

## Shared.Messaging

Contains:

- Azure Service Bus abstractions.
- Message publishing.
- Message consumption.
- Retry policies.
- Dead-letter handling.
- Event envelope definitions.

## Shared.Infrastructure

Contains common:

- Dependency injection extensions.
- Configuration extensions.
- Database helpers.
- Resilience policies.

## Shared.Security

Contains:

- Authorization helpers.
- Claims handling.
- Tenant context.
- User context.

## Shared.Observability

Contains:

- Serilog configuration.
- OpenTelemetry configuration.
- Correlation ID handling.
- Trace context propagation.
- Standard logging extensions.

## Shared.Storage

Contains common storage abstractions and reusable Azure Blob Storage integration components.

---

# 9. Web

The `Web` folder contains the frontend application.

Recommended implementation:

```text
src/
└── Web/
    └── EnterpriseDocumentPlatform.Web/
        ├── src/
        │   ├── app/
        │   ├── components/
        │   ├── features/
        │   ├── layouts/
        │   ├── services/
        │   ├── hooks/
        │   ├── models/
        │   ├── routes/
        │   └── utils/
        │
        ├── public/
        ├── package.json
        ├── tsconfig.json
        └── vite.config.ts
```

The frontend will initially use:

- React
- TypeScript
- Tailwind CSS
- React Router
- API client layer
- Entra ID authentication

The UI should be organized by business feature rather than by generic technical components.

Example:

```text
features/
├── authentication/
├── organizations/
├── templates/
├── documents/
├── workflows/
├── approvals/
└── audit/
```

---

# 10. Documentation Structure

The `docs` folder should become the central technical documentation repository.

Recommended structure:

```text
docs/
│
├── 01-Product/
│   ├── Product-Vision.md
│   ├── MVP-Definition.md
│   ├── Product-Backlog.md
│   └── Roadmap.md
│
├── 02-Requirements/
│   ├── BRD.md
│   ├── SRS.md
│   └── Use-Cases.md
│
├── 03-Architecture/
│   ├── Solution-Architecture.md
│   ├── Component-Architecture.md
│   ├── Deployment-Architecture.md
│   └── Security-Architecture.md
│
├── 04-Design/
│   ├── Database-Design.md
│   ├── API-Design.md
│   ├── Event-Design.md
│   ├── Workflow-Design.md
│   └── Storage-Design.md
│
├── 05-Operations/
│   ├── Deployment.md
│   ├── Monitoring.md
│   ├── Disaster-Recovery.md
│   └── Troubleshooting.md
│
└── 06-ADRs/
    ├── ADR-001-Monorepo.md
    ├── ADR-002-Database-Strategy.md
    └── ADR-003-Messaging-Strategy.md
```

All major architecture decisions should be recorded as ADRs.

---

# 11. Infrastructure

The `infra` directory contains Azure Infrastructure as Code.

Recommended structure:

```text
infra/
│
├── bicep/
│   ├── main.bicep
│   ├── modules/
│   │   ├── networking/
│   │   ├── identity/
│   │   ├── storage/
│   │   ├── database/
│   │   ├── messaging/
│   │   ├── container-apps/
│   │   ├── key-vault/
│   │   └── monitoring/
│   │
│   └── environments/
│       ├── dev/
│       ├── test/
│       └── prod/
│
└── terraform/
    └── future/
```

For the initial implementation, **Bicep** can be the primary IaC technology.

The infrastructure should support:

```text
Azure
│
├── Microsoft Entra ID
├── Resource Group
├── Virtual Network
├── Subnets
├── Private Endpoints
├── Azure Container Apps
├── Azure SQL Database
├── Azure Storage Account
├── Azure Service Bus
├── Azure Key Vault
├── Application Insights
└── Log Analytics Workspace
```

Environment-specific configuration should be maintained separately from reusable infrastructure modules.

---

# 12. Pipelines

The `pipelines` folder contains CI/CD configuration.

Recommended structure:

```text
pipelines/
│
├── ci.yml
├── cd-dev.yml
├── cd-test.yml
├── cd-prod.yml
│
└── templates/
    ├── dotnet-build.yml
    ├── dotnet-test.yml
    ├── frontend-build.yml
    ├── docker-build.yml
    ├── security-scan.yml
    └── deploy-container-app.yml
```

The pipeline should eventually automate:

```text
Commit
   ↓
Build
   ↓
Unit Tests
   ↓
Integration Tests
   ↓
Code Quality
   ↓
Security Scan
   ↓
Docker Build
   ↓
Container Registry
   ↓
Infrastructure Validation
   ↓
Deployment
   ↓
Smoke Test
```

---

# 13. Tests

The `tests` folder should be organized by testing level.

```text
tests/
│
├── Unit/
├── Integration/
├── Contract/
├── EndToEnd/
└── Performance/
```

Example:

```text
tests/
└── Unit/
    ├── Template.Tests/
    ├── Document.Tests/
    └── Workflow.Tests/
```

## Unit Tests

Validate:

- Domain logic.
- Application use cases.
- Business rules.
- Validation.

## Integration Tests

Validate:

- SQL integration.
- Blob Storage integration.
- Service Bus integration.
- External dependencies.

## Contract Tests

Validate:

- API contracts.
- Event contracts.
- Service integration compatibility.

## End-to-End Tests

Validate complete business flows.

Example:

```text
Login
  ↓
Create Template
  ↓
Upload Template
  ↓
Generate Document
  ↓
Start Workflow
  ↓
Approve Document
  ↓
Store Final Document
```

## Performance Tests

Focus on high-volume scenarios such as:

- Bulk document generation.
- Concurrent document generation.
- Large template processing.
- Workflow processing.

---

# 14. Scripts

The `scripts` folder contains automation utilities.

Recommended structure:

```text
scripts/
│
├── setup/
├── database/
├── local/
├── migration/
├── deployment/
└── utilities/
```

Examples:

```text
scripts/setup/setup-local.ps1
scripts/database/apply-migrations.ps1
scripts/local/start-dependencies.ps1
scripts/deployment/deploy-dev.ps1
```

Scripts should support developer productivity and reduce manual deployment steps.

---

# 15. Docker

The `docker` folder contains container-related configuration.

Recommended structure:

```text
docker/
│
├── gateway/
│   └── Dockerfile
│
├── services/
│   ├── identity/
│   │   └── Dockerfile
│   ├── template/
│   │   └── Dockerfile
│   ├── document/
│   │   └── Dockerfile
│   └── workflow/
│       └── Dockerfile
│
├── web/
│   └── Dockerfile
│
└── docker-compose.yml
```

For local development, Docker Compose may provide local dependencies such as:

- SQL Server
- Redis
- Azurite
- Service Bus emulator or equivalent local messaging option

The architecture should avoid making local development dependent on a full Azure environment.

---

# 16. .NET Solution Structure

The root solution should be:

```text
EnterpriseDocumentPlatform.sln
```

The initial solution should contain projects similar to:

```text
EnterpriseDocumentPlatform.sln

├── Gateway
│   └── EnterpriseDocumentPlatform.Gateway
│
├── Services
│   ├── Identity
│   ├── Organization
│   ├── Template
│   ├── Document
│   ├── Workflow
│   ├── Storage
│   ├── Notification
│   └── Audit
│
└── Shared
    ├── SharedKernel
    ├── Shared.Contracts
    ├── Shared.Messaging
    ├── Shared.Infrastructure
    ├── Shared.Security
    └── Shared.Observability
```

Not every planned service needs to be fully implemented on day one.

For the MVP, development can start with:

```text
Gateway
Organization
Template
Document
Workflow
Storage
Notification
Audit
```

Identity can initially be implemented through Microsoft Entra ID integration rather than building a custom authentication system.

---

# 17. Git Strategy

The project will use Git for source control.

The repository should follow a controlled branch strategy.

```text
main
│
└── develop
    │
    ├── feature/*
    ├── bugfix/*
    ├── hotfix/*
    └── release/*
```

The primary branches are:

```text
main
develop
feature/*
```

---

# 18. Main Branch

Branch:

```text
main
```

Purpose:

- Contains production-ready code.
- Represents the currently released version.
- Must always remain stable.
- Direct commits are prohibited.

All changes must come through Pull Requests.

Recommended protection rules:

- Pull Request required.
- At least 1–2 reviewers.
- CI pipeline must pass.
- No unresolved merge conflicts.
- Security checks must pass.
- Required status checks enabled.

---

# 19. Develop Branch

Branch:

```text
develop
```

Purpose:

- Main integration branch for active development.
- Contains the latest completed development features.
- Used for integration testing.
- Acts as the source for release preparation.

Developers should create feature branches from `develop`.

Example:

```text
develop
   │
   ├── feature/template-management
   ├── feature/document-generation
   ├── feature/workflow-engine
   └── feature/audit-logging
```

---

# 20. Feature Branches

Feature branches follow this naming convention:

```text
feature/<short-description>
```

Examples:

```text
feature/template-upload
feature/template-versioning
feature/document-generation
feature/pdf-generation
feature/workflow-approval
feature/audit-logging
feature/blob-storage
```

For larger features, use a ticket or user story identifier.

Example:

```text
feature/EDP-102-template-upload
feature/EDP-125-document-generation
feature/EDP-140-workflow-approval
```

Development process:

```text
develop
    │
    └── feature/EDP-102-template-upload
                │
                ├── Commit 1
                ├── Commit 2
                └── Commit 3
                        │
                        ▼
                  Pull Request
                        │
                        ▼
                     develop
```

---

# 21. Bugfix Branches

For non-production bugs:

```text
bugfix/<description>
```

Example:

```text
bugfix/template-placeholder-validation
```

These branches should normally be created from:

```text
develop
```

After completion:

```text
bugfix/*
    ↓
Pull Request
    ↓
develop
```

---

# 22. Hotfix Branches

Production-critical fixes should use:

```text
hotfix/<description>
```

Example:

```text
hotfix/document-download-failure
```

A hotfix should be created from:

```text
main
```

After successful validation, it should be merged into:

```text
main
develop
```

This ensures the production fix is also included in ongoing development.

---

# 23. Release Branches

As the project matures, release branches can be introduced:

```text
release/v1.0.0
```

Typical flow:

```text
develop
   ↓
release/v1.0.0
   ↓
QA / UAT
   ↓
main
   ↓
Production
```

The MVP can initially use only:

```text
main
develop
feature/*
```

and introduce `release/*`, `bugfix/*`, and `hotfix/*` as deployment maturity increases.

---

# 24. Commit Strategy

Commit messages should follow a consistent convention.

Recommended format:

```text
<type>: <description>
```

Examples:

```text
feat: add template upload API
feat: implement document generation service
fix: handle missing template placeholders
refactor: simplify workflow state transition
test: add template service unit tests
docs: update API design
chore: upgrade .NET dependencies
```

Recommended commit types:

```text
feat
fix
refactor
test
docs
chore
build
ci
perf
```

Commit messages should be:

- Short.
- Meaningful.
- Action-oriented.
- Related to one logical change.

Avoid commits such as:

```text
changes
update
final
test
new code
```

---

# 25. Pull Request Strategy

Every feature should be integrated through a Pull Request.

Recommended process:

```text
Create Feature Branch
        ↓
Implement Feature
        ↓
Run Local Tests
        ↓
Push Branch
        ↓
Create Pull Request
        ↓
CI Validation
        ↓
Code Review
        ↓
Resolve Comments
        ↓
Approval
        ↓
Merge to develop
        ↓
Delete Feature Branch
```

Pull Requests should contain:

- Business requirement / user story.
- Technical summary.
- Changes made.
- Testing performed.
- Database changes.
- API changes.
- Infrastructure changes.
- Screenshots for UI changes.
- Deployment considerations.

---

# 26. Definition of Done for Foundation

Phase 2 Foundation is considered complete when:

## Repository

- Git repository is created.
- `main` and `develop` branches exist.
- Branch protection is configured.
- Pull Request process is defined.

## Solution

- .NET solution is created.
- Projects are organized by service boundaries.
- Shared libraries are established.
- React application is created.

## Development Standards

- `.editorconfig` is configured.
- `.gitignore` is configured.
- Code formatting rules are established.
- Dependency version management is established.
- Logging and error handling patterns are defined.

## Infrastructure

- Initial Bicep structure is created.
- Environment structure is defined.
- Docker setup is available for local development.

## CI/CD

- CI pipeline is created.
- Build pipeline validates backend and frontend.
- Unit tests run automatically.
- Pull Requests require successful pipeline validation.

## Documentation

- Repository README is created.
- Architecture documentation is committed.
- API, database, event and workflow design documents are added.

---

# 27. Recommended Initial Repository

The final initial repository should look approximately like this:

```text
EnterpriseDocumentPlatform/
│
├── src/
│   ├── Gateway/
│   │   └── EnterpriseDocumentPlatform.Gateway/
│   │
│   ├── Services/
│   │   ├── Identity/
│   │   ├── Organization/
│   │   ├── Template/
│   │   ├── Document/
│   │   ├── Workflow/
│   │   ├── Storage/
│   │   ├── Notification/
│   │   └── Audit/
│   │
│   ├── Shared/
│   │   ├── SharedKernel/
│   │   ├── Shared.Contracts/
│   │   ├── Shared.Messaging/
│   │   ├── Shared.Infrastructure/
│   │   ├── Shared.Security/
│   │   ├── Shared.Observability/
│   │   └── Shared.Storage/
│   │
│   └── Web/
│       └── EnterpriseDocumentPlatform.Web/
│
├── tests/
│   ├── Unit/
│   ├── Integration/
│   ├── Contract/
│   ├── EndToEnd/
│   └── Performance/
│
├── docs/
│   ├── 01-Product/
│   ├── 02-Requirements/
│   ├── 03-Architecture/
│   ├── 04-Design/
│   ├── 05-Operations/
│   └── 06-ADRs/
│
├── infra/
│   ├── bicep/
│   └── terraform/
│
├── pipelines/
│   ├── ci.yml
│   ├── cd-dev.yml
│   └── templates/
│
├── scripts/
│   ├── setup/
│   ├── database/
│   ├── deployment/
│   └── utilities/
│
├── docker/
│   ├── gateway/
│   ├── services/
│   ├── web/
│   └── docker-compose.yml
│
├── .editorconfig
├── .gitignore
├── Directory.Build.props
├── Directory.Build.targets
├── Directory.Packages.props
├── README.md
└── EnterpriseDocumentPlatform.sln
```

---

# 28. Phase 2 Execution Sequence

The implementation should proceed in the following order:

```text
1. Create GitHub Repository
        ↓
2. Create main branch
        ↓
3. Create develop branch
        ↓
4. Create repository folder structure
        ↓
5. Create .NET Solution
        ↓
6. Create Gateway
        ↓
7. Create Shared Libraries
        ↓
8. Create Initial Service Projects
        ↓
9. Create React Web Application
        ↓
10. Configure .editorconfig and coding standards
        ↓
11. Configure Docker
        ↓
12. Create Initial Bicep Structure
        ↓
13. Create CI Pipeline
        ↓
14. Configure Branch Policies
        ↓
15. Add Unit Test Projects
        ↓
16. Add README and Architecture Documentation
        ↓
17. Create First Feature Branch
        ↓
18. Implement First MVP User Story
```

The first actual business feature after completing the foundation should be **Template Management**, starting with:

```text
Create Organization
        ↓
Login
        ↓
Upload Word Template
        ↓
Store Template in Blob Storage
        ↓
Save Template Metadata
        ↓
Extract / Validate Placeholders
        ↓
Publish TemplateCreated Event
        ↓
Record Audit Event
```

---

# 29. Phase 2 Deliverables

At the end of Phase 2, the following deliverables should exist:

| # | Deliverable | Status |
|---|---|---|
| 1 | EnterpriseDocumentPlatform Git repository | Planned |
| 2 | Main/develop branch strategy | Planned |
| 3 | Repository folder structure | Planned |
| 4 | .NET 10 solution | Planned |
| 5 | API Gateway project | Planned |
| 6 | Initial service projects | Planned |
| 7 | Shared libraries | Planned |
| 8 | React frontend project | Planned |
| 9 | Unit test foundation | Planned |
| 10 | Docker development setup | Planned |
| 11 | Bicep infrastructure structure | Planned |
| 12 | CI pipeline foundation | Planned |
| 13 | Branch protection rules | Planned |
| 14 | Development standards | Planned |
| 15 | Technical documentation structure | Planned |

---

# 30. Phase Completion Criteria

Phase 2 is complete when a developer can clone the repository, build the solution, run the frontend, execute automated tests, and understand the architecture and development workflow without requiring undocumented manual setup.

The target state is:

```text
Developer
   │
   ├── Clone Repository
   │
   ├── Checkout develop
   │
   ├── Run Setup Script
   │
   ├── Start Local Dependencies
   │
   ├── Build .NET Solution
   │
   ├── Run Tests
   │
   ├── Start Gateway
   │
   ├── Start Web Application
   │
   └── Begin Feature Development
```

This foundation establishes the baseline for **Phase 3 — Development Environment & Solution Bootstrap**, where the actual .NET 10 projects, React application, local infrastructure, NuGet packages, configuration, Docker setup, and first CI pipeline will be implemented.
