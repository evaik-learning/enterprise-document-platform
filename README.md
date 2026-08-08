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
