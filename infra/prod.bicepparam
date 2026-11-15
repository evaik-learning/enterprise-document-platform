// PROD environment. Secrets come from environment variables (set by the pipeline from secret variables / Key Vault):
//   SQL_ADMIN_PASSWORD, ENTRA_TENANT_ID, ENTRA_CLIENT_ID, ENTRA_CLIENT_SECRET
using './main.bicep'

param environmentName = 'prod'
param location = readEnvironmentVariable('AZURE_LOCATION', 'eastus2')
param appNamePrefix = 'edp'
param imageTag = readEnvironmentVariable('IMAGE_TAG', 'latest')

// Compute: PremiumV3, two zone-redundant instances, CPU autoscale up to 4.
param appServiceSku = { name: 'P1v3', tier: 'PremiumV3' }
param appServiceInstanceCount = 2
param zoneRedundant = true
param enableAutoscale = true
param autoscaleMinInstances = 2
param autoscaleMaxInstances = 4
param alwaysOn = true

// Data
param sqlAdministratorLogin = 'edpsqladmin'
param sqlAdministratorPassword = readEnvironmentVariable('SQL_ADMIN_PASSWORD')
param sqlDatabaseSku = { name: 'S2', tier: 'Standard' }
param sqlBackupStorageRedundancy = 'Geo'
param storageSkuName = 'Standard_GZRS'
param serviceBusSku = 'Standard'
param acrSku = 'Standard'
param enableKeyVaultPurgeProtection = true
param logRetentionDays = 90
param gatewayRateLimitPerMinute = 300

// Identity (see docs/guides/02-Azure-Deployment-Guide.md, "Entra ID app registration")
param entraTenantId = readEnvironmentVariable('ENTRA_TENANT_ID', '')
param entraClientId = readEnvironmentVariable('ENTRA_CLIENT_ID', '')
param entraClientSecret = readEnvironmentVariable('ENTRA_CLIENT_SECRET', '')
param webPublicUrl = readEnvironmentVariable('WEB_PUBLIC_URL', '')

param tags = {
  environment: 'prod'
  application: 'enterprise-document-platform'
  owner: 'platform-team'
}
