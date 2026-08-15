# Enterprise Document Platform

Central source code repository for the Enterprise Document Platform.

This repository uses a monorepo structure during the initial development phase so backend services, API gateway, frontend application, shared libraries, tests, infrastructure, CI/CD, Docker configuration, documentation, and maintenance scripts can evolve together.

## Repository Structure

```text
enterprise-document-platform/
├── src/
│   ├── Gateway/
│   ├── Services/
│   ├── Shared/
│   └── Web/
├── docs/
├── infra/
├── pipelines/
├── tests/
├── scripts/
├── docker/
├── .gitignore
├── .editorconfig
├── README.md
├── LICENSE
├── Directory.Build.props
├── Directory.Build.targets
├── Directory.Packages.props
└── EnterpriseDocumentPlatform.sln
```

## Folder Responsibilities

| Folder | Purpose |
| --- | --- |
| `src` | Production application source code |
| `src/Gateway` | API Gateway source code |
| `src/Services` | Backend services |
| `src/Shared` | Shared libraries, domain models, contracts, event definitions, middleware, and common patterns |
| `src/Web` | Frontend application |
| `docs` | Product, architecture, API, database, event, and operational documentation |
| `infra` | Azure infrastructure and Infrastructure as Code |
| `pipelines` | CI/CD pipeline definitions |
| `tests` | Unit, integration, contract, and end-to-end tests |
| `scripts` | Development, automation, and maintenance scripts |
| `docker` | Dockerfiles, Docker Compose, and local container configuration |

## Local build and validation

### .NET solution

```bash
dotnet restore EnterpriseDocumentPlatform.sln
dotnet build EnterpriseDocumentPlatform.sln --nologo
dotnet test EnterpriseDocumentPlatform.sln --configuration Debug
```

### Frontend app

```bash
npm --prefix src/Web/Edp.Web install
npm --prefix src/Web/Edp.Web run build
```

### Run the gateway locally

```bash
dotnet run --project src/Gateway/Edp.Gateway --launch-profile https
```

The Gateway exposes health and OpenAPI endpoints, and the middleware pipeline includes correlation IDs, security headers, request logging, and global exception handling.

### Docker and local compose

```bash
docker compose up --build
```

### CI validation

```bash
dotnet restore EnterpriseDocumentPlatform.sln
dotnet build EnterpriseDocumentPlatform.sln --configuration Release --no-restore
dotnet test EnterpriseDocumentPlatform.sln --configuration Release --no-build
npm --prefix src/Web/Edp.Web ci
npm --prefix src/Web/Edp.Web run build
```

## Current Phase 3 status

The repository is now aligned to the Phase 3 development bootstrap baseline:

- the .NET 10 SDK is pinned via global.json,
- centralized package management is enabled,
- the gateway and all service APIs expose OpenAPI and health endpoints,
- the frontend app builds with Vite and TypeScript,
- Docker bootstrap files and a CI pipeline are in place,
- and the solution is ready for the next platform implementation phase.
