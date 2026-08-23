# Local Template + Document Service Testing Guide

This guide is for running the Template and Document services together on a developer machine and validating the end-to-end document generation flow using a sample .docx template.

## What this repo expects

The current codebase is configured with these defaults:

- Template API: http://localhost:53954
- Document API: http://localhost:59042
- Gateway: http://localhost:5200 or https://localhost:7067
- Local SQL Server: (localdb)\MSSQLLocalDB
- Blob storage: UseDevelopmentStorage=true, or a real Azure Storage connection string
- Service Bus: optional; if no Service Bus connection is configured, the code falls back to a null publisher

The Template and Document APIs read the organization context from the current claims principal. In practice, that means the request must contain an authenticated user context with an `organization_id` claim, or the API returns a 403 with: "An organization context is required to access templates/documents."

> The service projects are not fully wired to a real JWT handler in this repo snapshot, so the most reliable local path is to run through the Gateway with OIDC or add a temporary local dev auth layer that injects `organization_id` for testing.

---

## Prerequisites

1. Install the .NET 10 SDK.
2. Ensure LocalDB is available on Windows.
3. Run a local blob emulator:
   - Preferred: Azurite
   - Command:

   ```bash
   npm install -g azurite
   azurite --silent --location .azurite --debug .azurite/debug.log
   ```

4. Create the local databases if needed.
5. Optional: configure Service Bus connection string in environment variables.

---

## Local database setup

The service code uses these default connection names:

- Template DB: `TemplateDb`
- Document DB: `DocumentDb`

### Windows LocalDB defaults

```powershell
$env:ConnectionStrings__TemplateDb = "Server=(localdb)\MSSQLLocalDB;Database=TemplateDb;Trusted_Connection=True;TrustServerCertificate=True;"
$env:ConnectionStrings__DocumentDb = "Server=(localdb)\MSSQLLocalDB;Database=DocumentDb;Trusted_Connection=True;TrustServerCertificate=True;"
$env:ConnectionStrings__BlobStorage = "UseDevelopmentStorage=true"
```

If you prefer to use a custom DB server, override the variable with your own server name and catalog name.

---

## Start the services

### Option 1: Run the services directly

Open two terminals and run:

```powershell
dotnet run --project src/Services/Template/Edp.Template.Api --urls http://localhost:53954
```

```powershell
dotnet run --project src/Services/Document/Edp.Document.Api --urls http://localhost:59042
```

### Option 2: Use Docker Compose

```powershell
docker compose up --build
```

The compose file maps the Template API to port 7003 and the Document API to port 7004.

---

## Health checks

Template API:

```bash
curl http://localhost:53954/health
```

Document API:

```bash
curl http://localhost:59042/health
```

---

## Sample template asset

A sample DOCX template has been added at:

`docs/test-data/sample-template.docx`

It contains placeholders like:

- `{{CustomerName}}`
- `{{ContractNumber}}`
- `{{IssueDate}}`
- `{{Amount}}`

This file is suitable for smoke testing the Template upload flow and the later Document generation flow.

---

## Template flow

### 1. Create a template

Use a local authenticated request context that includes an `organization_id` claim.

```bash
curl -X POST "http://localhost:53954/api/v1/templates" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <test-token>" \
  -d '{
    "name": "Customer Contract Template",
    "code": "customer-contract",
    "description": "Template used for contract generation smoke testing"
  }'
```

Expected behavior:

- Success: 201 Created
- Without organization context: 403 Forbidden

### 2. Upload the sample template version

```bash
curl -X POST "http://localhost:53954/api/v1/templates/{templateId}/versions" \
  -H "Authorization: Bearer <test-token>" \
  -F "file=@docs/test-data/sample-template.docx" \
  -F "changeDescription=Initial customer contract template upload"
```

Expected behavior:

- The DTO includes the template version metadata and extracted placeholders.
- The file is stored in the configured blob container.

### 3. Validate the version

```bash
curl -X POST "http://localhost:53954/api/v1/templates/{templateId}/versions/{versionId}/validate" \
  -H "Authorization: Bearer <test-token>"
```

### 4. Activate the version

```bash
curl -X POST "http://localhost:53954/api/v1/templates/{templateId}/versions/{versionId}/activate" \
  -H "Authorization: Bearer <test-token>"
```

---

## Document flow

### 1. Create a document record

```bash
curl -X POST "http://localhost:59042/api/v1/documents" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <test-token>" \
  -d '{
    "name": "Customer Contract - ACME",
    "documentType": "Contract",
    "description": "Generated contract for Acme Corp",
    "templateId": "{templateId}",
    "templateVersion": 1,
    "data": {
      "CustomerName": "Acme Corporation",
      "ContractNumber": "CT-2026-001",
      "IssueDate": "2026-08-20",
      "Amount": 125000.00
    }
  }'
```

### 2. Trigger document generation

```bash
curl -X POST "http://localhost:59042/api/v1/documents/{documentId}/generate" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <test-token>" \
  -d '{
    "name": "Customer Contract - ACME",
    "outputFormats": ["DOCX", "PDF"],
    "data": {
      "CustomerName": "Acme Corporation",
      "ContractNumber": "CT-2026-001",
      "IssueDate": "2026-08-20",
      "Amount": 125000.00
    }
  }'
```

Expected behavior:

- Document generation creates a job, resolves the template data, produces DOCX/PDF output, and stores the artifacts.
- The status transitions through generation processing to completed or failed states.

---

## local auth note

The service APIs rely on `ICurrentOrganization.OrganizationId`, which resolves from claims such as:

- `organization_id`
- `org_id`
- `tenant_id`
- `tid`

If your local environment does not already provide these claims, the request fails with a 403 before the service can do real work. For local validation, either:

1. run the request through the Gateway with a working OIDC session, or
2. add a temporary local development authentication middleware that injects an `organization_id` claim for the test user.

This is the key requirement for both Template and Document service smoke tests.

---

## Troubleshooting

### 403 Forbidden

Check whether the current principal contains an `organization_id` claim.

### Template upload fails

Check:

- template file is a valid .docx file
- file size below the configured limit
- blob storage emulator is running
- TemplateDb is reachable via LocalDB

### Document generation fails

Check:

- the document record exists
- the template version is active/valid
- the template data values match the placeholders in the DOCX
- DocumentDb and blob storage are available

### LocalDB is not available

Use a local SQL Express or SQL Server instance and set the `ConnectionStrings__TemplateDb` and `ConnectionStrings__DocumentDb` environment variables to point at it.

---

## Recommended smoke-test order

1. Start Template API
2. Start Document API
3. Start Azurite or ensure storage is available
4. Create template
5. Upload sample .docx
6. Validate and activate version
7. Create document
8. Generate document outputs
9. Verify generated artifact storage and status updates

This sequence validates the Template-to-Document workflow from business metadata through actual document generation.
