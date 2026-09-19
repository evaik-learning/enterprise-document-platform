# Local Services and Frontend Testing Guide

This guide describes how to run and test the complete Enterprise Document Platform locally in September 2026. It covers the Gateway, React front end, all backend services, local dependencies, automated tests, and one end-to-end document workflow.

The primary local mode is:

```text
React/Vite frontend -> Gateway -> backend APIs -> SQL Server/LocalDB and Azurite
                                              -> optional Azure Service Bus
```

The guide uses PowerShell commands and the HTTP ports from the projects' launch profiles.

## 1. What is covered

| Component | Responsibility | Local HTTP endpoint |
| --- | --- | --- |
| Web | React/Vite user interface | http://localhost:5173 |
| Gateway | Browser-facing BFF/API gateway | http://localhost:5200 or https://localhost:7067 |
| Identity API | User registration and identity operations | http://localhost:59038 |
| Organization API | Organizations and tenant context | http://localhost:59050 |
| Template API | Templates, versions, validation, and placeholders | http://localhost:53954 |
| Document API | Documents and document generation | http://localhost:59042 |
| Workflow API | Workflow definitions, instances, and approvals | http://localhost:59048 |
| Storage API | Storage-facing API operations | http://localhost:59047 |
| Notification API | Notification API and service boundary | http://localhost:59044 |
| Audit API | Audit log queries and recording | http://localhost:59040 |
| Digital Signature API | Signing requests and local demo signing provider | Check its launch profile or run on a free port |

The HTTPS Gateway port is important for the front end: `src/Web/Edp.Web/vite.config.ts` proxies `/api`, `/bff`, and `/health` to `https://localhost:7067`.

## 2. Current local-test boundaries

The following behavior is expected in the current codebase:

- Entra ID authentication is configured for protected flows. A browser test needs a working Entra app registration and a user who can sign in.
- Template and Document use Azurite for Blob Storage when `UseDevelopmentStorage=true` is configured.
- Azurite does not emulate Azure Service Bus. Without a real Service Bus connection, local publishers fall back to the local null publisher and subscriber-driven workflows do not run across processes.
- Notification is currently an API/service shell. Its health endpoint can be tested, but a complete notification delivery assertion requires the Phase 9 implementation and messaging infrastructure.
- Workflow messaging infrastructure exists, but consuming a document event does not yet start or resolve a workflow business process automatically. Test workflow endpoints directly when validating workflow behavior.
- The Docker Compose files are useful as deployment references, but the supported developer path in this guide runs the .NET projects directly. Use Compose only after validating its Dockerfiles and database dependencies in your environment.

## 3. Prerequisites

Install and verify:

1. .NET 10 SDK selected by `global.json`.
2. Node.js and npm.
3. SQL Server LocalDB or another SQL Server instance.
4. Azurite.
5. Azure CLI if using Entra ID tokens or Azure Service Bus.
6. A browser with the local HTTPS development certificate trusted.

```powershell
dotnet --version
node --version
npm --version
az --version
```

Trust the .NET HTTPS certificate if required:

```powershell
dotnet dev-certs https --trust
```

Install and start Azurite from the repository root. Keep this terminal running:

```powershell
npm install -g azurite
azurite --silent --location .azurite --debug .azurite/debug.log
```

Confirm the sample template exists:

```powershell
Test-Path .\docs\test-data\sample-template.docx
```

## 4. Build and automated tests first

Run these checks before starting the full stack:

```powershell
dotnet restore .\EnterpriseDocumentPlatform.sln
dotnet build .\EnterpriseDocumentPlatform.sln --configuration Debug --nologo
dotnet test .\EnterpriseDocumentPlatform.sln --configuration Debug --nologo --verbosity minimal
npm --prefix .\src\Web\Edp.Web install
npm --prefix .\src\Web\Edp.Web run build
```

To run one focused test project:

```powershell
dotnet test .\tests\Edp.Template.Tests\Edp.Template.Tests.csproj --nologo --verbosity minimal
dotnet test .\tests\Edp.Document.Tests\Edp.Document.Tests.csproj --nologo --verbosity minimal
dotnet test .\tests\Edp.Workflow.Tests\Edp.Workflow.Tests.csproj --nologo --verbosity minimal
dotnet test .\tests\Edp.DigitalSignature.Tests\Edp.DigitalSignature.Tests.csproj --nologo --verbosity minimal
```

The remaining test projects are `Edp.Identity.Tests`, `Edp.Organization.Tests`, `Edp.Gateway.Tests`, `Edp.Notification.Tests`, and `Edp.Audit.Tests`.

## 5. Configure local dependencies

The Development appsettings files contain the repository defaults. Override values in the current PowerShell session only when your machine uses different services:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ConnectionStrings__BlobStorage = 'UseDevelopmentStorage=true'
$env:ConnectionStrings__ServiceBus = ''
```

Use a real Service Bus connection only when testing publishers/subscribers:

```powershell
$env:ConnectionStrings__ServiceBus = '<service-bus-connection-string>'
```

Do not commit connection strings, client secrets, or tokens. Each bounded context has its own connection-string names and database configuration; check that service's `appsettings.Development.json` before overriding a database value.

## 6. Start the backend services

Open one PowerShell terminal per process. Run each command from the repository root. The `--launch-profile` option preserves the HTTPS and HTTP ports listed above.

```powershell
dotnet run --project .\src\Services\Identity\Edp.Identity.Api --launch-profile Edp.Identity.Api
dotnet run --project .\src\Services\Organization\Edp.Organization.Api --launch-profile Edp.Organization.Api
dotnet run --project .\src\Services\Storage\Edp.Storage.Api --launch-profile Edp.Storage.Api
dotnet run --project .\src\Services\Template\Edp.Template.Api --launch-profile Edp.Template.Api
dotnet run --project .\src\Services\Document\Edp.Document.Api --launch-profile Edp.Document.Api
dotnet run --project .\src\Services\Workflow\Edp.Workflow.Api --launch-profile Edp.Workflow.Api
dotnet run --project .\src\Services\Notification\Edp.Notification.Api --launch-profile Edp.Notification.Api
dotnet run --project .\src\Services\Audit\Edp.Audit.Api --launch-profile Edp.Audit.Api
```

Start the Gateway with HTTPS because the Vite proxy targets port `7067`:

```powershell
dotnet run --project .\src\Gateway\Edp.Gateway\Edp.Gateway.csproj --launch-profile https
```

Digital Signature is part of the solution but does not have a launch profile in the same matrix. Start it with an available local port when testing its API:

```powershell
dotnet run --project .\src\Services\DigitalSignature\Edp.DigitalSignature.Api --urls http://localhost:59051
```

If a database is not created automatically, apply the service migrations using the service's Infrastructure project and API startup project. Example:

```powershell
dotnet ef database update --project .\src\Services\Template\Edp.Template.Infrastructure\Edp.Template.Infrastructure.csproj --startup-project .\src\Services\Template\Edp.Template.Api\Edp.Template.Api.csproj
```

Repeat that command for the service that reports a database readiness failure.

## 7. Verify every service is alive

Run this from a separate PowerShell terminal after the processes have started:

```powershell
$checks = @(
  'http://localhost:5200/health/live',
  'http://localhost:59038/health/live',
  'http://localhost:59050/health/live',
  'http://localhost:59047/health/live',
  'http://localhost:53954/health/live',
  'http://localhost:59042/health/live',
  'http://localhost:59048/health/live',
  'http://localhost:59044/health/live',
  'http://localhost:59040/health/live'
)

foreach ($uri in $checks) {
  try {
    $response = Invoke-WebRequest -Uri $uri -SkipCertificateCheck -UseBasicParsing
    "{0} {1}" -f $response.StatusCode, $uri
  } catch {
    "FAIL {0}: {1}" -f $uri, $_.Exception.Message
  }
}
```

Then check readiness for services that use a database or storage:

```powershell
Invoke-WebRequest http://localhost:59038/health/ready -UseBasicParsing
Invoke-WebRequest http://localhost:59050/health/ready -UseBasicParsing
Invoke-WebRequest http://localhost:53954/health/ready -UseBasicParsing
Invoke-WebRequest http://localhost:59042/health/ready -UseBasicParsing
Invoke-WebRequest http://localhost:59048/health/ready -UseBasicParsing
Invoke-WebRequest http://localhost:59040/health/ready -UseBasicParsing
```

Use `https://localhost:7067/health/live` for the Gateway when it is running with the HTTPS profile. Add `-SkipCertificateCheck` in PowerShell if the development certificate is not trusted.

## 8. Start and verify the front end

In another terminal:

```powershell
npm --prefix .\src\Web\Edp.Web run dev
```

Open http://localhost:5173. The expected routes are:

- `/login`
- `/organizations`
- `/templates`
- `/documents`
- `/workflows`
- `/approvals`
- `/audit`

The front end uses cookie credentials and the Gateway proxy. Do not point the browser directly at a backend API for the normal UI test. Use the Gateway HTTPS process and keep the Vite development server running.

## 9. End-to-end user case: create and approve a customer contract

Use this scenario for the complete local walkthrough. The test user creates a reusable template, generates a customer document, starts a workflow, completes an approval, and checks the audit trail.

### Step 1: Sign in

1. Open http://localhost:5173/login.
2. Select **Login with Microsoft Entra ID**.
3. Complete the Entra sign-in flow.
4. Confirm that the browser returns to the application and that protected routes are accessible.
5. Confirm the signed-in principal has an organization context. Template, Document, Workflow, and Audit calls require the organization context resolved from the authenticated claims.

If Entra is not configured for the local environment, use the direct API checks in section 10 with a valid access token. Do not invent a production token or commit a locally generated secret.

### Step 2: Select or create the organization

1. Open **Organizations**.
2. Create or select `Contoso Legal Operations`.
3. Record the organization ID for troubleshooting and API checks.
4. Refresh the page and confirm the selected organization remains available.

Expected result: the organization is created once and subsequent requests use the same organization context.

### Step 3: Create and activate a template

1. Open **Templates**.
2. Create a template named `Customer Contract Template` with code `customer-contract`.
3. Upload `docs/test-data/sample-template.docx` as version 1.
4. Confirm the version is stored in Blob Storage and appears in the version list.
5. Validate the version.
6. Activate the version.
7. Review the detected placeholders and confirm they match the data used in the next step.

Expected result: the template has an active version and the Blob Storage emulator contains the uploaded file.

### Step 4: Create and generate a document

1. Open **Documents**.
2. Create `Customer Contract - ACME` from the active template version.
3. Enter these values:

```text
CustomerName: Acme Corporation
ContractNumber: CT-2026-001
IssueDate: 2026-09-19
Amount: 125000.00
```

4. Save the document and record its ID.
5. Select **Generate** and request DOCX and PDF output if those options are available.
6. Refresh until the status is `Completed` or `Failed`.
7. For `Completed`, download or inspect the generated artifact and verify the customer name and contract number are present.
8. For `Failed`, capture the correlation ID and inspect the Document API logs before retrying.

Expected result: the document references the active template version and generated output is stored through the configured storage path.

### Step 5: Start and exercise a workflow

1. Open **Workflows**.
2. Create or select a workflow definition with an approval state.
3. Start an instance for the generated document.
4. Open **Approvals** as the assigned approver.
5. Confirm the approval task displays the correct document and organization.
6. Approve the task.
7. Confirm the workflow instance and approval history show the state transition.

The current local implementation may require workflow operations to be started directly through the Workflow API. If an automatically generated document event does not start a workflow, record that as an expected current limitation rather than retrying indefinitely.

### Step 6: Check audit activity

1. Open **Audit**.
2. Filter by the organization and document ID.
3. Confirm the relevant document, workflow, and approval events are visible when the corresponding service paths emit them.
4. Compare timestamps and correlation IDs with the service logs.

### Step 7: Check notifications and signing boundaries

1. Open the Notification route or call its health endpoint.
2. Confirm the notification service is alive.
3. Treat actual email or in-app delivery as not verified unless a real message provider and Service Bus are configured.
4. For digital signing, create a signing request through the Digital Signature API and exercise the Local Demo Signature Provider. Verify state transitions and the audit trail without using production signing credentials.

## 10. Direct API checkpoints

Use these calls when the UI is unavailable or when isolating a service. Replace `$token` with an Entra access token and use the organization ID from the organization step.

```powershell
$token = '<access-token>'
$headers = @{ Authorization = "Bearer $token"; Accept = 'application/json' }
```

Identity and organization:

```powershell
Invoke-RestMethod -Method Get -Uri 'http://localhost:59038/api/Users' -Headers $headers
Invoke-RestMethod -Method Get -Uri 'http://localhost:59050/api/v1/Organizations' -Headers $headers
```

Template:

```powershell
$templateBody = @{ name = 'Customer Contract Template'; code = 'customer-contract'; description = 'Local test template' } | ConvertTo-Json
$template = Invoke-RestMethod -Method Post -Uri 'http://localhost:53954/api/v1/templates' -Headers ($headers + @{ 'Content-Type' = 'application/json' }) -Body $templateBody
$template.id
```

Document:

```powershell
$documentBody = @{
  name = 'Customer Contract - ACME'
  documentType = 'Contract'
  description = 'September 2026 local flow'
  templateId = '<template-id>'
  templateVersion = 1
  data = @{ CustomerName = 'Acme Corporation'; ContractNumber = 'CT-2026-001'; IssueDate = '2026-09-19'; Amount = 125000.00 }
} | ConvertTo-Json -Depth 10

$document = Invoke-RestMethod -Method Post -Uri 'http://localhost:59042/api/v1/documents' -Headers ($headers + @{ 'Content-Type' = 'application/json' }) -Body $documentBody
$document.id
```

Workflow, audit, notification, and signing routes are exposed by their controllers. Use Scalar/OpenAPI on each API to confirm the exact request schema before posting commands:

```text
http://localhost:59048/scalar
http://localhost:59040/scalar
http://localhost:59044/scalar
http://localhost:59051/scalar
```

## 11. Failure checks

The following negative checks are part of a complete local test:

| Check | Expected result |
| --- | --- |
| Request a protected endpoint without a token | `401 Unauthorized` |
| Use a token without organization context | `403 Forbidden` or an organization-context error |
| Read a document from another organization | No cross-organization data is returned |
| Generate with an inactive or missing template version | A clear validation/not-found response |
| Approve as a non-assigned user | A forbidden response when authorization is enforced |
| Send the same command twice | The operation is idempotent or returns a conflict without duplicating state |
| Stop Azurite and upload a template | The readiness or upload operation fails clearly |

Record the HTTP status, response body, correlation ID, and service log entry for every failed check.

## 12. Troubleshooting

### The front end cannot reach the Gateway

Confirm the Gateway is running with `--launch-profile https` on port `7067`. Confirm the Vite proxy in `src/Web/Edp.Web/vite.config.ts` still targets that port. Restart Vite after changing proxy settings.

### A service reports unhealthy

Call both `/health/live` and `/health/ready`. Liveness confirms the process is running; readiness usually identifies a database, Blob Storage, or configuration problem. Inspect the terminal that started that service.

### Template upload fails

Confirm Azurite is running, the Blob Storage connection string is `UseDevelopmentStorage=true`, and `docs/test-data/sample-template.docx` exists. Check that the template version is validated before activation.

### Requests return 401 or 403

Confirm the token audience and issuer match the API configuration, the token is not expired, and the authenticated principal contains the organization claim expected by the shared current-organization context. Also check that the Gateway cookie session is still valid.

### Workflow or notification events do not arrive

Azurite does not provide Service Bus. Configure a real Service Bus connection and the expected topics/subscriptions, or test the service APIs directly. Document this as an infrastructure-dependent test result.

### Local databases are missing

Check the service's `appsettings.Development.json`, verify SQL Server/LocalDB is running, and apply that service's EF Core migrations with `dotnet ef database update`.

## 13. Stop the local stack

Press `Ctrl+C` in each service, Gateway, front-end, and Azurite terminal. To stop containers started separately:

```powershell
docker compose down
```

## 14. Completion checklist

- [ ] Solution build passes.
- [ ] All automated test projects pass or failures are recorded.
- [ ] Azurite is running and Blob Storage upload was verified.
- [ ] All API `/health/live` endpoints return success.
- [ ] Required `/health/ready` endpoints return success.
- [ ] Gateway HTTPS is reachable on `https://localhost:7067`.
- [ ] Front end builds and loads on http://localhost:5173.
- [ ] User can sign in and select an organization.
- [ ] Template version can be uploaded, validated, and activated.
- [ ] Document can be created and generated.
- [ ] Workflow and approval behavior was verified or its current limitation was recorded.
- [ ] Audit records were checked.
- [ ] Notification and Service Bus limitations were recorded when not configured.
- [ ] No token, secret, or connection string was committed.