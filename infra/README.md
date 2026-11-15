# Infrastructure

`main.bicep` provisions one environment into one resource group; `dev.bicepparam` and `prod.bicepparam` hold the per-environment values. Full procedure: [guide 02](../docs/guides/02-Azure-Deployment-Guide.md).

**Creates:** App Service plan (Linux) + 11 container web apps (Gateway, 9 services, web), Azure SQL (`EdpDb`, `EdpSigningDb`), Storage (containers `templates`, `documents`, `signed-documents`), Service Bus (topics/subscriptions), Key Vault (all secrets), Container Registry (managed-identity pull only), Log Analytics, Application Insights, optional autoscale, role assignments for managed identities.

**Secrets** are never in parameter files: they are read from environment variables (`SQL_ADMIN_PASSWORD`, `ENTRA_TENANT_ID`, `ENTRA_CLIENT_ID`, `ENTRA_CLIENT_SECRET`, optional `WEB_PUBLIC_URL`, `IMAGE_TAG`, `AZURE_LOCATION`) and stored in Key Vault.

```powershell
$env:SQL_ADMIN_PASSWORD = '<strong password>'
az group create -n rg-edp-dev -l eastus
az deployment group what-if -g rg-edp-dev --parameters infra/dev.bicepparam
az deployment group create  -g rg-edp-dev --parameters infra/dev.bicepparam
az bicep build --file infra/main.bicep      # compile/lint only
```

New environment: copy a `.bicepparam`, change the values, add a variables file and a stage in `pipelines/cd.yml`.
