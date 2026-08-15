# Phase 2 Foundation Implementation Tasks

## Overview
This task list is intended to complete the Phase 2 Foundation for the Enterprise Document Platform repository.
It is based on the current repository state and the expected foundation items from `docs/Phase-2-Foundation.md`.

## 1. Repository & structure completion
- [ ] Verify top-level repository folders exist and contain actual artifacts beyond placeholders:
  - [ ] `infra/`
  - [ ] `pipelines/`
  - [ ] `scripts/`
  - [ ] `docker/`
  - [ ] `tests/`
- [ ] Add standard root files if missing:
  - [ ] `.editorconfig`
  - [ ] `.gitignore`
  - [ ] `README.md`
  - [ ] `LICENSE`
  - [ ] `Directory.Build.props`
  - [ ] `Directory.Build.targets`
  - [ ] `Directory.Packages.props`
- [ ] Confirm `EnterpriseDocumentPlatform.sln` includes all intended projects and folders.

## 2. Gateway foundation
- [ ] Implement the API gateway middleware pipeline in `src/Gateway/Edp.Gateway/Program.cs`:
  - [ ] `CorrelationIdMiddleware`
  - [ ] `GlobalExceptionHandlingMiddleware`
  - [ ] `SecurityHeadersMiddleware`
  - [ ] `RequestLoggingMiddleware`
- [ ] Validate gateway responsibilities:
  - [ ] External API entry point
  - [ ] Authentication and authorization integration
  - [ ] Request routing and aggregation
  - [ ] Correlation ID propagation
  - [ ] Global exception handling
  - [ ] Request logging
  - [ ] Rate limiting
  - [ ] Security headers
  - [ ] Health endpoints
  - [ ] OpenTelemetry / observability
- [ ] Confirm the gateway contains no business logic; route business concerns to service layers.

## 3. Service scaffolding
- [ ] Expand service scaffolding beyond `Template`:
  - [ ] `Identity`
  - [ ] `Organization`
  - [ ] `Document`
  - [ ] `Workflow`
  - [ ] `Storage`
  - [ ] `Notification`
  - [ ] `Audit`
- [ ] For each service, create the basic layered structure:
  - `*.Api`
  - `*.Application`
  - `*.Domain`
  - `*.Infrastructure`
- [ ] Ensure each service follows the shared layered architecture pattern.

## 4. Shared libraries and platform infrastructure
- [ ] Confirm shared projects are present and meaningful:
  - `Edp.SharedKernel`
  - `Edp.Shared.Contracts`
  - `Edp.Shared.Messaging`
  - `Edp.Shared.Infrastructure`
  - `Edp.Shared.Security`
  - `Edp.Shared.Observability`
  - `Edp.Shared.Storage`
- [ ] Add or document common platform concerns:
  - Domain models and contracts
  - Messaging/event definitions
  - Security/auth abstractions
  - Observability and tracing
  - Storage abstractions

## 5. Infrastructure and deployment
- [ ] Add Azure infrastructure IaC under `infra/`:
  - [ ] Networking
  - [ ] Storage
  - [ ] SQL / persistence
  - [ ] Service Bus or messaging infra
  - [ ] App service / container app definitions
- [ ] Add Docker artifacts under `docker/`:
  - [ ] Dockerfiles for gateway, services, frontend
  - [ ] Docker Compose for local development
- [ ] Add CI/CD pipeline definitions under `pipelines/`:
  - [ ] Build pipeline
  - [ ] Test pipeline
  - [ ] Publish/deploy pipeline
- [ ] Add local development scripts under `scripts/`:
  - [ ] Build/run helpers
  - [ ] Test execution helpers
  - [ ] Clean/reset scripts

## 6. Tests and quality
- [ ] Create real test projects under `tests/`:
  - [ ] Gateway / integration tests
  - [ ] Template service unit tests
  - [ ] Shared library tests
  - [ ] Future service unit and integration test scaffolding
- [ ] Add a test execution workflow:
  - [ ] `dotnet test` setup
  - [ ] local integration test guidance
  - [ ] CI test stage
- [ ] Add coding standards and enforcement guidance:
  - [ ] C# formatting rules
  - [ ] folder/project naming conventions
  - [ ] API design and versioning rules
  - [ ] branch and collaboration strategy

## 7. Documentation and developer onboarding
- [ ] Update `docs/Phase-2-Foundation.md` with:
  - [ ] A task checklist for Phase 2 implementation
  - [ ] Status of each foundation area
- [ ] Add onboarding documentation for developers:
  - [ ] How to run locally
  - [ ] How to build the solution
  - [ ] How to run tests
  - [ ] Repository conventions and architecture overview

## 8. Validation and signoff
- [ ] Confirm the repo can build successfully from root
- [ ] Confirm solution tests run successfully
- [ ] Confirm the platform can start locally with gateway and frontend
- [ ] Confirm placeholder folders now contain real implementation or scaffolding
