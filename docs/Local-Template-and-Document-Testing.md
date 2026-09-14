# Local Template + Document Service Testing Guide

This guide explains how to test the end-to-end flow for the Gateway, Identity, Organization, Template, and Document services locally, and how to generate an Entra ID bearer token for direct API testing.

---

## 1. Service URLs and roles

These are the local endpoints used in the solution:

- Gateway: http://localhost:5200 or https://localhost:7067
- Identity API: http://localhost:5001 (or whatever your local launchSettings sets)
- Organization API: http://localhost:5002 (or the local service port)
- Template API: http://localhost:53954
- Document API: http://localhost:59042
- LocalDB: (localdb)\MSSQLLocalDB
- Blob storage: UseDevelopmentStorage=true or Azurite

The Gateway is usually the first place to test the full UI flow. The individual service APIs are also callable directly, but the service code expects an authenticated principal with an `organization_id` claim.

---

## 2. Prerequisites

1. Install the .NET 10 SDK.
2. Install LocalDB / SQL Server on Windows.
3. Install Azurite for local blob storage:

```powershell
npm install -g azurite
azurite --silent --location .azurite --debug .azurite/debug.log
```

4. Ensure the app registrations and tenant config are set for Entra ID in the Gateway and API projects.
5. Set the local environment variables for the Database and Blob settings if the defaults are not enough.

---

## 3. Local environment setup

### 3.1 Database settings

```powershell
$env:ConnectionStrings__EdpDb = "Server=(localdb)\MSSQLLocalDB;Database=EdpDb;Trusted_Connection=True;TrustServerCertificate=True;"
$env:ConnectionStrings__BlobStorage = "UseDevelopmentStorage=true"
```

### 3.2 Optional Service Bus settings

```powershell
$env:ConnectionStrings__ServiceBus = "<your-service-bus-connection-string>"
```

---

## 4. Start the services in order

### 4.1 Start the Identity service

```powershell
dotnet run --project src/Services/Identity/Edp.Identity.Api --urls http://localhost:5001
```

### 4.2 Start the Organization service

```powershell
dotnet run --project src/Services/Organization/Edp.Organization.Api --urls http://localhost:5002
```

### 4.3 Start the Template service

```powershell
dotnet run --project src/Services/Template/Edp.Template.Api --urls http://localhost:53954
```

### 4.4 Start the Document service

```powershell
dotnet run --project src/Services/Document/Edp.Document.Api --urls http://localhost:59042
```

### 4.5 Start the Gateway

```powershell
dotnet run --project src/Gateway/Edp.Gateway --urls http://localhost:5200
```

You can also use the project launch profiles to start the services from Visual Studio.

---

## 5. Gateway UI flow test

This is the best user-facing testing path when Entra ID is configured.

### 5.1 Open Gateway UI

Open the browser to:

- http://localhost:5200
- or https://localhost:7067 if running with HTTPS

### 5.2 Log in through Entra ID

The Gateway config uses OIDC and will redirect to the configured Microsoft Entra authority. After login, the session is established in the cookie-based Gateway auth.

### 5.3 Create or select an organization

The APIs read the current organization from claims such as:

- `organization_id`
- `org_id`
- `tenant_id`
- `tid`

For end-to-end testing, make sure your login and token include an organization claim. In a real Entra setup, this is usually mapped from the user’s claims or an app-specific claim.

### 5.3a Add the organizationId to the user claim

The service code resolves the active tenant/org context from the principal using the claim names above. If the token does not contain one of them, requests will fail with a 403 because the organization context is missing.

For local development, there are two common approaches:

1. Use your Entra token and add custom claims in the app registration / token customizer so that the token includes `organization_id`.
2. For pure local smoke tests without Entra, mint a JWT locally that contains:
   - `oid` or `sub` for the user identity
   - `name` or `email` for the principal
   - `roles` or `role` if your policy checks them
   - `organization_id` for the active organization

Example local-development JWT payload:

```json
{
  "aud": "api://edp-api",
  "iss": "local-edp",
  "sub": "11111111-2222-3333-4444-555555666666",
  "oid": "11111111-2222-3333-4444-555555666666",
  "name": "Test User",
  "email": "user@example.com",
  "roles": ["Template.Read", "Template.Update"],
  "organization_id": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"
}
```

If you are using a custom auth flow, make sure the generated token includes the exact same claim name the code is reading:

```text
organization_id
```

This is the claim that `CurrentOrganization.FromClaimsPrincipal(...)` resolves before allowing template/document operations.

### 5.4 Create an identity user

Use the Identity API:

```bash
curl -X POST "http://localhost:5001/api/Users/register" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "firstName": "Test",
    "lastName": "User",
    "password": "Password123!"
  }'
```

### 5.5 Create an organization

```bash
curl -X POST "http://localhost:5002/api/v1/Organizations" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Contoso Org"
  }'
```

Capture the returned organization ID, then ensure the token or session contains that organization ID as a claim.

### 5.6 Use the Template service

#### Create template

```bash
curl -X POST "http://localhost:53954/api/v1/templates" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <token>" \
  -d '{
    "name": "Customer Contract Template",
    "code": "customer-contract",
    "description": "Contract template for smoke testing"
  }'
```

#### Upload a sample template

```bash
curl -X POST "http://localhost:53954/api/v1/templates/{templateId}/versions" \
  -H "Authorization: Bearer <token>" \
  -F "file=@docs/test-data/sample-template.docx" \
  -F "changeDescription=Initial upload"
```

#### Validate the version

```bash
curl -X POST "http://localhost:53954/api/v1/templates/{templateId}/versions/{versionId}/validate" \
  -H "Authorization: Bearer <token>"
```

#### Activate the version

```bash
curl -X POST "http://localhost:53954/api/v1/templates/{templateId}/versions/{versionId}/activate" \
  -H "Authorization: Bearer <token>"
```

### 5.7 Use the Document service

#### Create a document

```bash
curl -X POST "http://localhost:59042/api/v1/documents" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <token>" \
  -d '{
    "name": "Customer Contract - ACME",
    "documentType": "Contract",
    "description": "Generated contract for Acme Corp",
    "templateId": "<templateId>",
    "templateVersion": 1,
    "data": {
      "CustomerName": "Acme Corporation",
      "ContractNumber": "CT-2026-001",
      "IssueDate": "2026-08-20",
      "Amount": 125000.00
    }
  }'
```

#### Generate the document

```bash
curl -X POST "http://localhost:59042/api/v1/documents/{documentId}/generate" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <token>" \
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

---

## 6. Direct API testing with Entra ID token

Use this when you want to call the APIs directly from curl/Postman without going through the browser-based Gateway UI.

### 6.1 Required setup in Entra ID

In Azure Entra ID, you need:

1. A tenant and app registration for the API.
2. An API scope or application permission depending on your auth model.
3. A client app or user account that can request the token.

Typical scopes are:

- `api://<api-app-id>/user_impersonation`
- or an app-specific custom scope such as `api://<api-app-id>/access_as_user`

### 6.2 Get a token with Azure CLI

#### User delegated flow

```powershell
az login --tenant <tenant-id>
az account get-access-token --resource api://<api-app-id> --query accessToken -o tsv
```

This returns a bearer token you can use directly in requests.

#### Use a custom scope

```powershell
az login --tenant <tenant-id>
az account get-access-token --scope api://<api-app-id>/.default --query accessToken -o tsv
```

If your app registration uses delegated permissions instead of application permissions, use the user flow from `az login` and request the custom scope or `user_impersonation` scope.

#### Local dev note: add the organization claim

If you are not using Entra token customization, the token you hand to the API must still contain a valid organization claim. For local development, mint a JWT with the following standard claims:

```text
sub
oid
name
email
organization_id
```

The organization claim value should be the GUID returned when you create/select the organization in the Organization API.

In a local dev auth setup, the flow is usually:

1. create org via Organization API
2. capture the returned organization ID
3. include `organization_id` in the token for the user that will access the Template/Document APIs
4. send `Authorization: Bearer <token>` on every request

### 6.3 Example: direct call to Template API

```powershell
$token = az account get-access-token --resource api://<api-app-id> --query accessToken -o tsv

curl -X POST "http://localhost:53954/api/v1/templates" `
  -H "Content-Type: application/json" `
  -H "Authorization: Bearer $token" `
  -d '{
    "name": "Customer Contract Template",
    "code": "customer-contract",
    "description": "Smoke test using Entra token"
  }'
```

### 6.4 Example: direct call to Document API

```powershell
$token = az account get-access-token --resource api://<api-app-id> --query accessToken -o tsv

curl -X POST "http://localhost:59042/api/v1/documents" `
  -H "Content-Type: application/json" `
  -H "Authorization: Bearer $token" `
  -d '{
    "name": "Customer Contract - ACME",
    "documentType": "Contract",
    "description": "Test document",
    "templateId": "<template-id>",
    "templateVersion": 1,
    "data": {
      "CustomerName": "Acme Corporation",
      "ContractNumber": "CT-2026-001",
      "IssueDate": "2026-08-20",
      "Amount": 125000.00
    }
  }'
```

### 6.5 Important claim requirement

The APIs specifically resolve the tenant/org context from the active principal. If your token does not include an `organization_id` claim, you will get a 403 error:

> "An organization context is required to access templates/documents."

For local development, make sure either:

- your Entra app token includes an organization claim, or
- you are using the Gateway session where the app maps the org context correctly, or
- a local dev auth layer injects the org claim, or
- you generate a local dev JWT with the exact `organization_id` claim included.

A minimal JWT for local testing should have at least:

```json
{
  "sub": "<user-guid>",
  "oid": "<user-guid>",
  "name": "Local Test User",
  "email": "test@local.dev",
  "organization_id": "<organization-guid>",
  "roles": ["Template.Read", "Template.Update"]
}
```

This is the claim that the service reads when resolving the current organization.

---

## 7. Copy/paste PowerShell smoke test

Use this when you want a single script to exercise the end-to-end flow with a local token and live service calls.

```powershell
$ErrorActionPreference = 'Stop'

$tenantId = '<tenant-id>'
$apiAppId = '<api-app-id>'

$token = az account get-access-token --tenant $tenantId --resource api://$apiAppId --query accessToken -o tsv
if (-not $token) { throw 'No token was returned from Azure CLI.' }

$headers = @{ Authorization = "Bearer $token"; "Content-Type" = "application/json" }

# 1) Create an organization
$orgBody = @{ name = 'Contoso Org' } | ConvertTo-Json
$org = Invoke-RestMethod -Method Post -Uri 'http://localhost:5002/api/v1/Organizations' -Headers $headers -Body $orgBody
$organizationId = $org.id
Write-Host "Organization created: $organizationId"

# 2) Create a template
$templateBody = @{
    name = 'Customer Contract Template'
    code = 'customer-contract'
    description = 'Smoke test template'
} | ConvertTo-Json

$template = Invoke-RestMethod -Method Post -Uri 'http://localhost:53954/api/v1/templates' -Headers $headers -Body $templateBody
$templateId = $template.id
Write-Host "Template created: $templateId"

# 3) Upload the sample .docx file as a new template version
$filePath = 'C:\evaik-learning\2026\enterprise-document-platform\docs\test-data\sample-template.docx'
$uploadUrl = "http://localhost:53954/api/v1/templates/$templateId/versions"

$multipart = [System.Collections.Generic.List[System.Collections.Specialized.NameValueCollection]]::new()
$boundary = [System.Guid]::NewGuid().ToString()

# Using Invoke-WebRequest with multipart/form-data is simpler for PowerShell when working with a file upload.
$form = @{ file = Get-Item $filePath; changeDescription = 'Initial smoke test upload' }
$uploadResponse = Invoke-RestMethod -Method Post -Uri $uploadUrl -Headers @{ Authorization = "Bearer $token" } -Form $form
$templateVersionId = $uploadResponse.versionId
Write-Host "Template version uploaded: $templateVersionId"

# 4) Validate and activate the template version
Invoke-RestMethod -Method Post -Uri "http://localhost:53954/api/v1/templates/$templateId/versions/$templateVersionId/validate" -Headers @{ Authorization = "Bearer $token" }
Invoke-RestMethod -Method Post -Uri "http://localhost:53954/api/v1/templates/$templateId/versions/$templateVersionId/activate" -Headers @{ Authorization = "Bearer $token" }

# 5) Create a document
$documentBody = @{
    name = 'Customer Contract - ACME'
    documentType = 'Contract'
    description = 'Generated contract for Acme Corp'
    templateId = $templateId
    templateVersion = 1
    data = @{
        CustomerName = 'Acme Corporation'
        ContractNumber = 'CT-2026-001'
        IssueDate = '2026-08-20'
        Amount = 125000.00
    }
} | ConvertTo-Json -Depth 10

$document = Invoke-RestMethod -Method Post -Uri 'http://localhost:59042/api/v1/documents' -Headers $headers -Body $documentBody
$documentId = $document.id
Write-Host "Document created: $documentId"

# 6) Generate the document output
$generateBody = @{
    name = 'Customer Contract - ACME'
    outputFormats = @('DOCX', 'PDF')
    data = @{
        CustomerName = 'Acme Corporation'
        ContractNumber = 'CT-2026-001'
        IssueDate = '2026-08-20'
        Amount = 125000.00
    }
} | ConvertTo-Json -Depth 10

$generation = Invoke-RestMethod -Method Post -Uri "http://localhost:59042/api/v1/documents/$documentId/generate" -Headers $headers -Body $generateBody
Write-Host "Generation started or completed:"
$generation | ConvertTo-Json -Depth 20
```

This script is useful for a local smoke test when your token already includes the required organization claim context.

---

## 8. Expected response examples

When the service calls succeed, the JSON should look roughly like the examples below.

### 8.1 Organization response

```json
{
  "id": "11111111-2222-3333-4444-555555666666",
  "name": "Contoso Org",
  "createdAt": "2026-08-25T10:30:00Z",
  "status": "Active"
}
```

### 8.2 Template response

```json
{
  "id": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
  "name": "Customer Contract Template",
  "code": "customer-contract",
  "description": "Smoke test template",
  "tenantId": "<tenant-id>",
  "organizationId": "11111111-2222-3333-4444-555555666666",
  "status": "Draft"
}
```

### 8.3 Template version upload response

```json
{
  "id": "bbbbbbbb-cccc-dddd-eeee-ffffffffffff",
  "templateId": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
  "versionNumber": 1,
  "status": "Uploaded",
  "changeDescription": "Initial smoke test upload",
  "fileName": "sample-template.docx",
  "blobUri": "https://<storage-account>.blob.core.windows.net/templates/sample-template.docx"
}
```

### 8.4 Document response

```json
{
  "id": "cccccccc-dddd-eeee-ffff-000000000000",
  "name": "Customer Contract - ACME",
  "documentType": "Contract",
  "templateId": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
  "templateVersion": 1,
  "status": "Created",
  "createdAt": "2026-08-25T10:35:00Z"
}
```

### 8.5 Generation response

```json
{
  "documentId": "cccccccc-dddd-eeee-ffff-000000000000",
  "status": "Processing",
  "outputFormats": ["DOCX", "PDF"],
  "generatedFiles": [],
  "startedAt": "2026-08-25T10:35:05Z"
}
```

The exact field names may differ slightly depending on the service contract, but the important part is that the API returns a document ID, template ID, version metadata, and a successful status transition.

---

## 9. Health and verification checks

### Gateway

```bash
curl http://localhost:5200/health
```

### Identity API

```bash
curl http://localhost:5001/health/live
curl http://localhost:5001/health/ready
```

### Organization API

```bash
curl http://localhost:5002/health/live
curl http://localhost:5002/health/ready
```

### Template API

```bash
curl http://localhost:53954/health
curl http://localhost:53954/health/live
curl http://localhost:53954/health/ready
```

### Document API

```bash
curl http://localhost:59042/health
curl http://localhost:59042/health/live
curl http://localhost:59042/health/ready
```

---

## 10. Troubleshooting

### 403 Forbidden

Check:

- the token is valid and not expired
- the token audience matches the API app registration
- the `organization_id` claim is present
- the user is authorized for the request

### Missing template or document generation data

Check:

- the template version is active
- the template placeholders match the data being sent
- the Blob storage emulator is running
- the database connection strings are correct

### LocalDB connection fails

Use SQL Server Express or update the environment variables to point at your local SQL instance.

---

## 11. Summary

The normal local flow is:

- log in via Gateway UI with Entra ID
- create/select organization
- use the Identity API to register and validate a user
- use Organization API to create the org context
- upload and activate a template in Template API
- create and generate a document in Document API
- validate the output in database/blob storage

For direct API testing, generate an Entra ID access token using Azure CLI and pass it as the `Authorization: Bearer <token>` header for each request.

---

## 12. Gateway UI checklist

Use this checklist when testing the full browser-driven workflow in the Gateway.

1. Open the Gateway UI at http://localhost:5200 or https://localhost:7067.
2. Sign in with the configured Entra ID account.
3. Confirm the login session includes the user and organization context.
4. Create or select an organization from the organization page.
5. Go to the Templates page and create a new template record.
6. Upload the sample .docx from `docs/test-data/sample-template.docx`.
7. Confirm the template version is created and appears in the version list.
8. Validate the template version.
9. Activate the template version.
10. Open the Documents page and create a new document using the template.
11. Provide the placeholder values for the template data objects.
12. Trigger Generate Document.
13. Wait for the status to change from processing to completed or failed.
14. Review the generated artifact list or the storage output for the produced file.
15. Verify the status and metadata in the UI match the backend response.

---

## 13. Recommended smoke-test order

1. Start Identity API
2. Start Organization API
3. Start Template API
4. Start Document API
5. Start Gateway
6. Get Entra token
7. Create organization
8. Create template
9. Upload sample .docx
10. Validate and activate version
11. Create document
12. Generate document outputs
13. Verify generated artifact storage and status updates

This sequence validates the Template-to-Document workflow from business metadata through actual document generation.
