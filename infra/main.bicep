// Enterprise Document Platform - Azure infrastructure (one resource group per environment).
//
// One template, two parameter files (dev.bicepparam / prod.bicepparam). Everything that differs between environments
// (names, region, SKUs, instance counts, retention, Entra settings) is a parameter; nothing is hard-coded per environment.
//
// Secrets (SQL password, storage key, Service Bus key, Entra client secret) are stored in Key Vault. App Service reads them
// through Key Vault references using each app's managed identity; images are pulled from ACR with managed identity.
targetScope = 'resourceGroup'

// ---------------------------------------------------------------------------------------------------------------------
// Parameters
// ---------------------------------------------------------------------------------------------------------------------
@description('Short environment name, e.g. dev, test, prod. Used in resource names.')
@minLength(2)
@maxLength(8)
param environmentName string

@description('Azure region.')
param location string = resourceGroup().location

@description('Short product prefix used in resource names.')
@minLength(2)
@maxLength(8)
param appNamePrefix string = 'edp'

@description('Tags applied to every resource.')
param tags object = {}

@description('ASP.NET Core environment for the services. Keep "Production": the demo sign-in only works in Development.')
param aspNetCoreEnvironment string = 'Production'

@description('Container image tag deployed to every app (the pipeline passes the build id).')
param imageTag string = 'latest'

// --- Compute ---------------------------------------------------------------------------------------------------------
@description('App Service plan SKU shared by the Gateway, the 9 services and the web app. 11 containers need roughly 6 GB of RAM or more.')
param appServiceSku object = {
  name: 'B3'
  tier: 'Basic'
}

@description('Number of plan instances (initial value; autoscale may change it when enabled).')
@minValue(1)
param appServiceInstanceCount int = 1

@description('Spread plan instances across availability zones (needs a PremiumV3 plan and at least 2 instances).')
param zoneRedundant bool = false

@description('Enable CPU-based autoscale on the App Service plan.')
param enableAutoscale bool = false

@minValue(1)
param autoscaleMinInstances int = 2

@minValue(1)
param autoscaleMaxInstances int = 4

@description('Keep apps loaded (recommended; required for the background event subscribers).')
param alwaysOn bool = true

// --- Data ------------------------------------------------------------------------------------------------------------
@description('Azure SQL administrator login.')
param sqlAdministratorLogin string = 'edpsqladmin'

@secure()
@description('Azure SQL administrator password (>= 16 characters, mixed case, digits, symbols).')
param sqlAdministratorPassword string

@description('Azure SQL database SKU for the shared platform database and the signing database.')
param sqlDatabaseSku object = {
  name: 'S0'
  tier: 'Standard'
}

@allowed(['Local', 'Zone', 'Geo', 'GeoZone'])
param sqlBackupStorageRedundancy string = 'Local'

@description('Allow Azure-hosted services (including Microsoft-hosted pipeline agents) to reach the SQL server. The server stays protected by SQL authentication; tighten with sqlAllowedIpRanges + private endpoints for stricter setups.')
param allowAzureServicesToSql bool = true

@description('Extra firewall rules, e.g. [{ name: "office", start: "203.0.113.4", end: "203.0.113.4" }].')
param sqlAllowedIpRanges array = []

@allowed(['Standard_LRS', 'Standard_ZRS', 'Standard_GRS', 'Standard_RAGRS', 'Standard_GZRS'])
param storageSkuName string = 'Standard_LRS'

@description('Service Bus tier. Topics require Standard or Premium.')
@allowed(['Standard', 'Premium'])
param serviceBusSku string = 'Standard'

@description('Event topics and their subscriptions. Must match what the services publish and consume.')
param serviceBusTopics array = [
  { name: 'template-events', subscriptions: ['audit', 'notification'] }
  { name: 'document-events', subscriptions: ['audit', 'notification', 'workflow'] }
  { name: 'workflow-events', subscriptions: ['audit', 'notification'] }
  { name: 'signing-events', subscriptions: ['audit', 'notification', 'workflow'] }
  { name: 'audit-events', subscriptions: ['archive'] }
  { name: 'notification-events', subscriptions: ['archive'] }
]

@description('Blob containers used by the platform.')
param blobContainers array = ['templates', 'documents', 'signed-documents']

// --- Platform --------------------------------------------------------------------------------------------------------
@allowed(['Basic', 'Standard', 'Premium'])
param acrSku string = 'Basic'

@description('Enable Key Vault purge protection (recommended for production; cannot be disabled afterwards).')
param enableKeyVaultPurgeProtection bool = false

@minValue(30)
@maxValue(730)
param logRetentionDays int = 30

// --- Identity (Microsoft Entra ID) -----------------------------------------------------------------------------------
@description('Entra tenant id. Leave empty to deploy without sign-in (the Gateway then rejects all API calls).')
param entraTenantId string = ''

@description('Client id of the Entra app registration used by the Gateway (web sign-in) and as the API audience.')
param entraClientId string = ''

@secure()
@description('Client secret of the app registration. Stored in Key Vault. Leave empty to skip.')
param entraClientSecret string = ''

@description('Scope name exposed by the app registration ("Expose an API").')
param entraApiScopeName string = 'access_as_user'

@description('Token audience accepted by the services. Defaults to the client id (v2 access tokens).')
param jwtAudience string = ''

@description('Public URL of the web app if it is served from a custom domain, e.g. https://docs.contoso.com. Empty = the default azurewebsites.net host.')
param webPublicUrl string = ''

@description('Gateway request limit per minute per caller.')
param gatewayRateLimitPerMinute int = 100

// ---------------------------------------------------------------------------------------------------------------------
// Naming
// ---------------------------------------------------------------------------------------------------------------------
var env = toLower(environmentName)
var prefix = toLower(appNamePrefix)
var uniqueSuffix = uniqueString(resourceGroup().id)
var shortSuffix = take(uniqueSuffix, 6)

var acrName = take('acr${prefix}${env}${uniqueSuffix}', 50)
var keyVaultName = take('kv-${prefix}-${env}-${uniqueSuffix}', 24)
var storageAccountName = take('st${prefix}${env}${uniqueSuffix}', 24)
var sqlServerName = 'sql-${prefix}-${env}-${uniqueSuffix}'
var serviceBusName = 'sb-${prefix}-${env}-${uniqueSuffix}'
var logAnalyticsName = 'log-${prefix}-${env}-${uniqueSuffix}'
var appInsightsName = 'appi-${prefix}-${env}-${uniqueSuffix}'
var planName = 'asp-${prefix}-${env}'
var edpDatabaseName = 'EdpDb'
var signingDatabaseName = 'EdpSigningDb'

// Site names must be globally unique. They are deterministic so apps can reference each other without a dependency cycle.
func siteName(productPrefix string, envName string, key string, suffix string) string => '${productPrefix}-${envName}-${key}-${suffix}'

var apiKeys = ['identity', 'organization', 'template', 'document', 'workflow', 'storage', 'notification', 'audit', 'digital-signature']
var backendKeys = concat(['gateway'], apiKeys)
var gatewayHost = '${siteName(prefix, env, 'gateway', shortSuffix)}.azurewebsites.net'
var webSiteName = siteName(prefix, env, 'web', shortSuffix)
var webPublicBaseUrl = empty(webPublicUrl) ? 'https://${webSiteName}.azurewebsites.net' : webPublicUrl

var downstreamNames = {
  identity: 'Identity'
  organization: 'Organization'
  template: 'Template'
  document: 'Document'
  workflow: 'Workflow'
  storage: 'Storage'
  notification: 'Notification'
  audit: 'Audit'
  'digital-signature': 'DigitalSignature'
}

var roleAcrPull = '7f951dda-4ed3-4680-a7ca-43fe172d538d'
var roleKeyVaultSecretsUser = '4633458b-17de-408a-b874-0445c86b69e6'

var oidcConfigured = !empty(entraTenantId) && !empty(entraClientId)
var jwtAuthority = empty(entraTenantId) ? '' : '${environment().authentication.loginEndpoint}${entraTenantId}/v2.0'
var effectiveAudience = empty(jwtAudience) ? entraClientId : jwtAudience
var gatewayScopes = concat(['openid', 'profile', 'email'], oidcConfigured ? ['api://${entraClientId}/${entraApiScopeName}'] : [])

var subscriptionList = flatten(map(serviceBusTopics, topic => map(topic.subscriptions, subscriptionName => {
  topic: topic.name
  name: subscriptionName
})))

// ---------------------------------------------------------------------------------------------------------------------
// Monitoring
// ---------------------------------------------------------------------------------------------------------------------
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsName
  location: location
  tags: tags
  properties: {
    retentionInDays: logRetentionDays
    sku: { name: 'PerGB2018' }
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  tags: tags
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
  }
}

// ---------------------------------------------------------------------------------------------------------------------
// Container registry (managed-identity pull only; no admin user)
// ---------------------------------------------------------------------------------------------------------------------
resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: acrName
  location: location
  tags: tags
  sku: { name: acrSku }
  properties: {
    adminUserEnabled: false
  }
}

// ---------------------------------------------------------------------------------------------------------------------
// Storage
// ---------------------------------------------------------------------------------------------------------------------
resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  kind: 'StorageV2'
  tags: tags
  sku: { name: storageSkuName }
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    // Shared-key access is required because the services use connection strings. Switch the services to
    // DefaultAzureCredential and set this to false to remove the key entirely.
    allowSharedKeyAccess: true
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storage
  name: 'default'
  properties: {
    deleteRetentionPolicy: { enabled: true, days: 7 }
    containerDeleteRetentionPolicy: { enabled: true, days: 7 }
  }
}

resource containers 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = [for name in blobContainers: {
  parent: blobService
  name: name
  properties: { publicAccess: 'None' }
}]

// ---------------------------------------------------------------------------------------------------------------------
// SQL (shared platform database + digital signature database)
// ---------------------------------------------------------------------------------------------------------------------
resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: sqlServerName
  location: location
  tags: tags
  properties: {
    administratorLogin: sqlAdministratorLogin
    administratorLoginPassword: sqlAdministratorPassword
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    version: '12.0'
  }
}

resource edpDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: edpDatabaseName
  location: location
  tags: tags
  sku: sqlDatabaseSku
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    requestedBackupStorageRedundancy: sqlBackupStorageRedundancy
  }
}

resource signingDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: signingDatabaseName
  location: location
  tags: tags
  sku: sqlDatabaseSku
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    requestedBackupStorageRedundancy: sqlBackupStorageRedundancy
  }
}

resource sqlAzureServicesRule 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = if (allowAzureServicesToSql) {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: { startIpAddress: '0.0.0.0', endIpAddress: '0.0.0.0' }
}

resource sqlExtraRules 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = [for range in sqlAllowedIpRanges: {
  parent: sqlServer
  name: range.name
  properties: { startIpAddress: range.start, endIpAddress: range.end }
}]

// ---------------------------------------------------------------------------------------------------------------------
// Service Bus
// ---------------------------------------------------------------------------------------------------------------------
resource serviceBus 'Microsoft.ServiceBus/namespaces@2024-01-01' = {
  name: serviceBusName
  location: location
  tags: tags
  sku: { name: serviceBusSku, tier: serviceBusSku }
  properties: {
    minimumTlsVersion: '1.2'
  }
}

resource topics 'Microsoft.ServiceBus/namespaces/topics@2024-01-01' = [for topic in serviceBusTopics: {
  parent: serviceBus
  name: topic.name
  properties: {
    defaultMessageTimeToLive: 'P1D'
    requiresDuplicateDetection: false
  }
}]

resource subscriptions 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2024-01-01' = [for item in subscriptionList: {
  name: '${serviceBus.name}/${item.topic}/${item.name}'
  properties: {
    lockDuration: 'PT1M'
    maxDeliveryCount: 10
    defaultMessageTimeToLive: 'P1D'
    deadLetteringOnMessageExpiration: true
  }
  dependsOn: [topics]
}]

resource serviceBusRootRule 'Microsoft.ServiceBus/namespaces/authorizationRules@2024-01-01' existing = {
  parent: serviceBus
  name: 'RootManageSharedAccessKey'
}

// ---------------------------------------------------------------------------------------------------------------------
// Key Vault + secrets
// ---------------------------------------------------------------------------------------------------------------------
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  tags: tags
  properties: {
    tenantId: subscription().tenantId
    sku: { family: 'A', name: 'standard' }
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 30
    enablePurgeProtection: enableKeyVaultPurgeProtection ? true : null
  }
}

var storageConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${storage.name};AccountKey=${storage.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
var sqlHost = sqlServer.properties.fullyQualifiedDomainName
var sqlConnectionString = 'Server=tcp:${sqlHost},1433;Initial Catalog=${edpDatabaseName};User ID=${sqlAdministratorLogin};Password=${sqlAdministratorPassword};Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=True;Connection Timeout=30;'
var signingConnectionString = 'Server=tcp:${sqlHost},1433;Initial Catalog=${signingDatabaseName};User ID=${sqlAdministratorLogin};Password=${sqlAdministratorPassword};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'

resource secretSql 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'sql-edp-connection'
  properties: { value: sqlConnectionString }
}

resource secretSigning 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'sql-signing-connection'
  properties: { value: signingConnectionString }
}

resource secretStorage 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'storage-connection'
  properties: { value: storageConnectionString }
}

resource secretServiceBus 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'servicebus-connection'
  properties: { value: serviceBusRootRule.listKeys().primaryConnectionString }
}

resource secretEntra 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = if (!empty(entraClientSecret)) {
  parent: keyVault
  name: 'entra-client-secret'
  properties: { value: entraClientSecret }
}

func kvRef(vault string, secretName string) string => '@Microsoft.KeyVault(VaultName=${vault};SecretName=${secretName})'

// ---------------------------------------------------------------------------------------------------------------------
// App Service plan and apps
// ---------------------------------------------------------------------------------------------------------------------
resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: planName
  location: location
  kind: 'linux'
  tags: tags
  sku: {
    name: appServiceSku.name
    tier: appServiceSku.tier
    capacity: appServiceInstanceCount
  }
  properties: {
    reserved: true
    zoneRedundant: zoneRedundant
  }
}

resource autoscale 'Microsoft.Insights/autoscalesettings@2022-10-01' = if (enableAutoscale) {
  name: 'autoscale-${planName}'
  location: location
  tags: tags
  properties: {
    enabled: true
    targetResourceUri: plan.id
    profiles: [
      {
        name: 'cpu'
        capacity: {
          minimum: string(autoscaleMinInstances)
          maximum: string(autoscaleMaxInstances)
          default: string(autoscaleMinInstances)
        }
        rules: [
          {
            metricTrigger: {
              metricName: 'CpuPercentage'
              metricResourceUri: plan.id
              timeGrain: 'PT1M'
              statistic: 'Average'
              timeWindow: 'PT10M'
              timeAggregation: 'Average'
              operator: 'GreaterThan'
              threshold: 70
            }
            scaleAction: { direction: 'Increase', type: 'ChangeCount', value: '1', cooldown: 'PT10M' }
          }
          {
            metricTrigger: {
              metricName: 'CpuPercentage'
              metricResourceUri: plan.id
              timeGrain: 'PT1M'
              statistic: 'Average'
              timeWindow: 'PT15M'
              timeAggregation: 'Average'
              operator: 'LessThan'
              threshold: 30
            }
            scaleAction: { direction: 'Decrease', type: 'ChangeCount', value: '1', cooldown: 'PT15M' }
          }
        ]
      }
    ]
  }
}

var commonSettings = {
  ASPNETCORE_ENVIRONMENT: aspNetCoreEnvironment
  ASPNETCORE_FORWARDEDHEADERS_ENABLED: 'true'
  WEBSITES_PORT: '8080'
  WEBSITES_ENABLE_APP_SERVICE_STORAGE: 'false'
  APPLICATIONINSIGHTS_CONNECTION_STRING: appInsights.properties.ConnectionString
}

// Services validate the access token the Gateway forwards (issued by Entra for the API audience).
var serviceSettings = union(commonSettings, {
  ConnectionStrings__EdpDb: kvRef(keyVault.name, secretSql.name)
  ConnectionStrings__BlobStorage: kvRef(keyVault.name, secretStorage.name)
  ConnectionStrings__Storage: kvRef(keyVault.name, secretStorage.name)
  ConnectionStrings__ServiceBus: kvRef(keyVault.name, secretServiceBus.name)
  Database__ApplyMigrations: 'false'
  Jwt__Authority: jwtAuthority
  Jwt__Issuer: jwtAuthority
  Jwt__Audience: effectiveAudience
})

var signingSettings = union(serviceSettings, {
  ConnectionStrings__SigningDb: kvRef(keyVault.name, secretSigning.name)
})

var downstreamSettings = toObject(apiKeys, key => 'Gateway__ServiceCommunication__DownstreamServices__${downstreamNames[key]}', key => 'https://${siteName(prefix, env, key, shortSuffix)}.azurewebsites.net')
var scopeSettings = toObject(range(0, length(gatewayScopes)), index => 'Gateway__EntraId__Scopes__${index}', index => gatewayScopes[index])

var gatewaySettings = union(commonSettings, downstreamSettings, scopeSettings, {
  Gateway__FrontendBaseUrl: webPublicBaseUrl
  Gateway__PublicBaseUrl: webPublicBaseUrl
  Gateway__EntraId__TenantId: entraTenantId
  Gateway__EntraId__ClientId: entraClientId
  Gateway__RateLimiting__PermitLimit: string(gatewayRateLimitPerMinute)
}, empty(entraClientSecret) ? {} : { Gateway__EntraId__ClientSecret: kvRef(keyVault.name, 'entra-client-secret') })

resource backendApps 'Microsoft.Web/sites@2023-12-01' = [for key in backendKeys: {
  name: siteName(prefix, env, key, shortSuffix)
  location: location
  kind: 'app,linux,container'
  tags: union(tags, { 'edp-service': key })
  identity: { type: 'SystemAssigned' }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    clientAffinityEnabled: false
    keyVaultReferenceIdentity: 'SystemAssigned'
    siteConfig: {
      linuxFxVersion: 'DOCKER|${acr.properties.loginServer}/edp-${key}:${imageTag}'
      acrUseManagedIdentityCreds: true
      alwaysOn: alwaysOn
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      http20Enabled: true
      healthCheckPath: '/health/live'
      appSettings: map(items(key == 'gateway' ? gatewaySettings : (key == 'digital-signature' ? signingSettings : serviceSettings)), setting => {
        name: setting.key
        value: setting.value
      })
    }
  }
}]

resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: webSiteName
  location: location
  kind: 'app,linux,container'
  tags: union(tags, { 'edp-service': 'web' })
  identity: { type: 'SystemAssigned' }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    clientAffinityEnabled: false
    siteConfig: {
      linuxFxVersion: 'DOCKER|${acr.properties.loginServer}/edp-web:${imageTag}'
      acrUseManagedIdentityCreds: true
      alwaysOn: alwaysOn
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      http20Enabled: true
      healthCheckPath: '/healthz'
      appSettings: [
        { name: 'WEBSITES_PORT', value: '8080' }
        { name: 'WEBSITES_ENABLE_APP_SERVICE_STORAGE', value: 'false' }
        { name: 'GATEWAY_URL', value: 'https://${gatewayHost}' }
      ]
    }
  }
}

// ---------------------------------------------------------------------------------------------------------------------
// Role assignments (managed identities)
// ---------------------------------------------------------------------------------------------------------------------
resource acrPullBackend 'Microsoft.Authorization/roleAssignments@2022-04-01' = [for (key, index) in backendKeys: {
  name: guid(acr.id, backendApps[index].id, roleAcrPull)
  scope: acr
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roleAcrPull)
    principalId: backendApps[index].identity.principalId
    principalType: 'ServicePrincipal'
  }
}]

resource acrPullWeb 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(acr.id, webApp.id, roleAcrPull)
  scope: acr
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roleAcrPull)
    principalId: webApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

resource keyVaultReaders 'Microsoft.Authorization/roleAssignments@2022-04-01' = [for (key, index) in backendKeys: {
  name: guid(keyVault.id, backendApps[index].id, roleKeyVaultSecretsUser)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roleKeyVaultSecretsUser)
    principalId: backendApps[index].identity.principalId
    principalType: 'ServicePrincipal'
  }
}]

// ---------------------------------------------------------------------------------------------------------------------
// Diagnostics: console + HTTP logs to Log Analytics
// ---------------------------------------------------------------------------------------------------------------------
resource backendDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = [for (key, index) in backendKeys: {
  name: 'to-log-analytics'
  scope: backendApps[index]
  properties: {
    workspaceId: logAnalytics.id
    logs: [
      { category: 'AppServiceConsoleLogs', enabled: true }
      { category: 'AppServiceHTTPLogs', enabled: true }
    ]
  }
}]

resource webDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'to-log-analytics'
  scope: webApp
  properties: {
    workspaceId: logAnalytics.id
    logs: [
      { category: 'AppServiceHTTPLogs', enabled: true }
    ]
  }
}

// ---------------------------------------------------------------------------------------------------------------------
// Outputs (consumed by the pipelines)
// ---------------------------------------------------------------------------------------------------------------------
output resourceGroupName string = resourceGroup().name
output acrName string = acr.name
output acrLoginServer string = acr.properties.loginServer
output keyVaultName string = keyVault.name
output sqlServerName string = sqlServer.name
output sqlServerFqdn string = sqlHost
output sqlDatabaseName string = edpDatabaseName
output storageAccountName string = storage.name
output serviceBusNamespace string = serviceBus.name
output webAppName string = webApp.name
output webUrl string = webPublicBaseUrl
output gatewayUrl string = 'https://${gatewayHost}'
output backendAppNames array = [for (key, index) in backendKeys: backendApps[index].name]
output entraRedirectUri string = '${webPublicBaseUrl}/signin-oidc'
output entraSignedOutRedirectUri string = '${webPublicBaseUrl}/signout-callback-oidc'
