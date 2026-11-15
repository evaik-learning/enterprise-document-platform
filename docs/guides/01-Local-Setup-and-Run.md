# 01 - Run the platform locally

Two ways to run everything on your machine. Both give you the full web app at a local URL and sign-in with built-in **demo users** (no Microsoft Entra ID, no Azure account needed).

| | A. Docker Compose (recommended) | B. Native (no Docker) |
|---|---|---|
| What runs | 9 services + Gateway + web in containers, SQL Server, Azurite, **Service Bus emulator** | The same .NET projects via `dotnet run`, Azurite, SQL LocalDB, Vite dev server |
| Cross-service events (notifications, audit trail from workflow/signing events) | Yes | No (messages are dropped by a no-op publisher) |
| Best for | Demos, end-to-end testing | Debugging code with hot reload |
| URL | http://localhost:8080 | http://localhost:5173 |

> Demo sign-in is enabled only when the Gateway runs with `ASPNETCORE_ENVIRONMENT=Development` **and** `Gateway:DevAuth:Enabled=true`. It is refused in every other environment, so it can never be active on Azure.

## Prerequisites

| Tool | Version | Needed for |
|---|---|---|
| Docker Desktop | recent (Compose v2) | Option A |
| .NET SDK | per `global.json` (10.x) | Option B, running tests |
| Node.js | 22+ | Option B, web tests |
| SQL Server LocalDB (installed with Visual Studio / SQL Express) | any | Option B |
| PowerShell | 5.1 or 7 | Option B scripts |

Allow about 8 GB of RAM for Docker Desktop. The first Docker build downloads images and restores NuGet packages and can take 10-15 minutes; later builds are cached.

## Option A - Docker Compose

```powershell
# from the repository root
docker compose up --build -d
docker compose ps          # wait until everything is "running" (migrator shows "exited (0)")
```

Open **http://localhost:8080** and pick a demo user.

What starts, in order: SQL Server -> `migrator` (applies EF migrations, then exits) -> Azurite + Service Bus emulator -> the 9 services -> Gateway -> web (nginx).

Useful commands:

```powershell
docker compose logs -f gateway          # follow one service (identity, template, document, workflow, ...)
docker compose restart workflow         # restart one service
docker compose down                     # stop, keep data
docker compose down -v                  # stop and DELETE all data (SQL, blobs)
$env:COMPOSE_PARALLEL_LIMIT = '2'       # if the first build fails with "SDK resolver ... worker node was shut down"
```

Ports: web 8080, Gateway 5200 (direct API access for tools), everything else stays inside the Docker network.
Override the local-only secrets with `SA_PASSWORD` and `JWT_DEV_KEY` environment variables if you like.

## Option B - Native, no Docker

```powershell
.\scripts\local\Start-Local.ps1        # starts Azurite, applies migrations, builds, starts all services + web
.\scripts\local\Stop-Local.ps1         # stops everything it started
```

Open **http://localhost:5173**. Logs: `.local\logs\<name>.log`. First run takes a few minutes (build + `npm ci`).

The script needs `dotnet`, `node`/`npm` and SQL LocalDB. To use a different SQL Server:

```powershell
.\scripts\local\Start-Local.ps1 -SqlConnection "Server=localhost,1433;Database=EdpDb;User Id=sa;Password=...;TrustServerCertificate=True;MultipleActiveResultSets=True" `
                                -SigningDbConnection "Server=localhost,1433;Database=EdpSigningDb;User Id=sa;Password=...;TrustServerCertificate=True"
```

Ports used: Gateway 5200, Identity 59038, Organization 59050, Template 53954, Document 59042, Workflow 59048, Storage 59047, Notification 59044, Audit 59040, DigitalSignature 54735, web 5173.

## Verify it works

```powershell
.\scripts\Smoke-Test.ps1 -BaseUrl http://localhost:8080     # Docker
.\scripts\Smoke-Test.ps1 -BaseUrl http://localhost:5173     # native
```

It signs in as a demo user, creates an organization and exercises the main read endpoints. Then do the manual walkthrough in [03 - Client demo script](03-Client-Demo-Script.md).

## Demo users

| User | Role summary | Use it to |
|---|---|---|
| **Alex Admin** | Organization admin, signing manager, auditor, full workflow rights | Run the whole demo; create the organization and invite the others |
| **Priya Approver** | Approval rights | Approve / reject documents |
| **Sam Signer** | Signer | Open and sign signature requests |

Each user is an identity, not yet a member of any organization. Sign in as Alex, **create an organization** (you become its Owner), then add Priya and Sam on *Administration -> Users* using their user ids:

| User | User id |
|---|---|
| Priya Approver | `00000000-0000-0000-0000-0000000000a2` |
| Sam Signer | `00000000-0000-0000-0000-0000000000a3` |

Test file for templates: `docs/test-data/sample-template.docx`.

## Run the tests

```powershell
dotnet test EnterpriseDocumentPlatform.sln                   # backend
cd src\Web\Edp.Web; npm ci; npm run lint; npm run typecheck; npm run test; npm run build   # web
```

## Troubleshooting

| Symptom | Fix |
|---|---|
| Login page shows only a Microsoft button | Gateway is not in Development or `DevAuth` is off. In Docker this is preconfigured; natively use `Start-Local.ps1`. |
| `401` on every call after changing the key | Gateway and services must share the same `Jwt__Key`. Restart everything (`Stop-Local` / `docker compose down`). |
| Docker build: "SDK resolver ... worker node was shut down" | Out of memory/CPU. Set `COMPOSE_PARALLEL_LIMIT=2` and retry; give Docker more RAM. |
| Service keeps restarting at start-up | `docker compose logs <service>`; most often the database is still migrating. It retries automatically. |
| Port already in use | Stop the other process or run `Stop-Local.ps1`; ports are listed above. |
| Notifications / audit entries from workflow or signing missing (native mode) | Expected: there is no Service Bus locally. Use Docker Compose. |

## Known limitations (be upfront with stakeholders)

- Document generation is currently a **stub** and does not merge the template content into the output file.
- The signing service reads the original PDF from a different storage path than the Document service writes, so end-to-end *sign a generated document* needs that path aligned before production use.
- "Request changes" in approvals is implemented as a rejection with a "Changes requested:" reason.
- Updates are by polling (no SignalR push yet); the document list has no workflow/signature status columns; template owner/date filters are not supported by the API yet.
- Signing events do not carry user ids, so signature notifications are not created (the audit trail still records them).
