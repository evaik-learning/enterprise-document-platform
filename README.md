# Enterprise Document Platform

Central source code repository for the Enterprise Document Platform.

This repository uses a monorepo structure during the initial development phase so backend services, API gateway, frontend application, shared libraries, tests, infrastructure, CI/CD, Docker configuration, documentation, and maintenance scripts can evolve together.

## Repository Structure

```text
enterprise-document-platform/
â”œâ”€â”€ src/
â”‚   â”œâ”€â”€ Gateway/
â”‚   â”œâ”€â”€ Services/
â”‚   â”œâ”€â”€ Shared/
â”‚   â””â”€â”€ Web/
â”œâ”€â”€ docs/
â”œâ”€â”€ infra/
â”œâ”€â”€ pipelines/
â”œâ”€â”€ tests/
â”œâ”€â”€ scripts/
â”œâ”€â”€ docker/
â”œâ”€â”€ .gitignore
â”œâ”€â”€ .editorconfig
â”œâ”€â”€ README.md
â”œâ”€â”€ LICENSE
â”œâ”€â”€ Directory.Build.props
â”œâ”€â”€ Directory.Build.targets
â”œâ”€â”€ Directory.Packages.props
â””â”€â”€ EnterpriseDocumentPlatform.sln
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

## Quick start

```powershell
docker compose up --build -d        # then open http://localhost:8080 and pick a demo user
.\scripts\Smoke-Test.ps1 -BaseUrl http://localhost:8080
```

No Docker? `.\scripts\local\Start-Local.ps1` runs everything natively (needs .NET SDK, Node 22, SQL LocalDB).

## Guides

| Guide | Purpose |
| --- | --- |
| [01 - Local setup and run](docs/guides/01-Local-Setup-and-Run.md) | Run the full platform locally (Docker or native), demo users, tests, troubleshooting, known limitations |
| [02 - Azure deployment](docs/guides/02-Azure-Deployment-Guide.md) | Dev and Prod on Azure with Bicep + Azure DevOps pipelines, Entra ID setup, promotion, rollback, hardening |
| [03 - Client demo script](docs/guides/03-Client-Demo-Script.md) | Step-by-step feature showcase with talking points and Q&A |

Component docs: [infra](infra/README.md), [pipelines](pipelines/README.md), [docker](docker/README.md), [scripts](scripts/README.md).

## Build and test

```bash
dotnet test EnterpriseDocumentPlatform.sln
npm --prefix src/Web/Edp.Web ci
npm --prefix src/Web/Edp.Web run lint
npm --prefix src/Web/Edp.Web run typecheck
npm --prefix src/Web/Edp.Web run test
npm --prefix src/Web/Edp.Web run build
```

## Architecture in one paragraph

A React single-page app (`src/Web`) talks only to the Gateway (`src/Gateway`), a backend-for-frontend that owns the Microsoft Entra ID sign-in (cookie session; tokens never reach the browser), aggregates dashboard/search/approval data and proxies to nine services (Identity, Organization, Template, Document, Workflow, Storage, Notification, Audit, DigitalSignature). Services share one SQL database (`EdpDb`, the signing service has its own), store files in Blob Storage and exchange events over Service Bus topics.