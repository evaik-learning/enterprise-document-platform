// DEV environment. Secrets come from environment variables (set by the pipeline from secret variables / Key Vault,
// or by you when deploying by hand):  SQL_ADMIN_PASSWORD, ENTRA_TENANT_ID, ENTRA_CLIENT_ID, ENTRA_CLIENT_SECRET
using './main.bicep'

param environmentName = 'dev'
param location = readEnvironmentVariable('AZURE_LOCATION', 'eastus')
param appNamePrefix = 'edp'
param imageTag = readEnvironmentVariable('IMAGE_TAG', 'latest')

// Compute: one plan hosts the Gateway, 9 services and the web app. Cheapest size that fits 11 containers.
param appServiceSku = { name: 'B3', tier: 'Basic' }
param appServiceInstanceCount = 1
param enableAutoscale = false
param alwaysOn = true

// Data
param sqlAdministratorLogin = 'edpsqladmin'
param sqlAdministratorPassword = readEnvironmentVariable('SQL_ADMIN_PASSWORD')
param sqlDatabaseSku = { name: 'S0', tier: 'Standard' }
param sqlBackupStorageRedundancy = 'Local'
param storageSkuName = 'Standard_LRS'
param serviceBusSku = 'Standard'
param acrSku = 'Basic'
param enableKeyVaultPurgeProtection = false
param logRetentionDays = 30

// Identity (see docs/guides/02-Azure-Deployment-Guide.md, "Entra ID app registration")
param entraTenantId = readEnvironmentVariable('ENTRA_TENANT_ID', '')
param entraClientId = readEnvironmentVariable('ENTRA_CLIENT_ID', '')
param entraClientSecret = readEnvironmentVariable('ENTRA_CLIENT_SECRET', '')
param webPublicUrl = readEnvironmentVariable('WEB_PUBLIC_URL', '')

param tags = {
  environment: 'dev'
  application: 'enterprise-document-platform'
  owner: 'platform-team'
}
