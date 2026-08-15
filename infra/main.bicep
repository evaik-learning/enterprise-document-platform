targetScope = 'resourceGroup'

@description('Deployment environment name.')
param environmentName string = 'dev'

@description('Azure region for the deployment.')
param location string = resourceGroup().location

@description('Application naming prefix.')
param appNamePrefix string = 'edp'

@description('Tags applied to all Azure resources.')
param tags object = {}

@description('Azure SQL administrator login.')
param sqlAdministratorLogin string = 'sqladmin'

@secure()
@description('Azure SQL administrator password.')
param sqlAdministratorPassword string

@description('Backend API names to provision.')
param backendApiNames array = [
  'gateway'
  'identity'
  'organization'
  'template'
  'document'
  'workflow'
  'storage'
  'notification'
  'audit'
]

@description('App Service plan SKU.')
param appServiceSku object = {
  name: 'B1'
  tier: 'Basic'
}

@description('Azure Container Registry SKU.')
param acrSku string = 'Basic'

var uniqueSuffix = uniqueString(resourceGroup().id)
var normalizedEnvironment = toLower(environmentName)
var appConfigName = 'appcs-${appNamePrefix}-${normalizedEnvironment}-${uniqueSuffix}'
var acrName = take(toLower(replace('acr${appNamePrefix}${normalizedEnvironment}${uniqueSuffix}', '-', '')), 50)
var sqlServerName = 'sql-${appNamePrefix}-${normalizedEnvironment}-${uniqueSuffix}'
var sqlDatabaseName = '${appNamePrefix}-${normalizedEnvironment}-db'
var serviceBusNamespaceName = 'sb-${appNamePrefix}-${normalizedEnvironment}-${uniqueSuffix}'
var storageAccountName = take(toLower(replace('st${appNamePrefix}${normalizedEnvironment}${uniqueSuffix}', '-', '')), 24)
var logAnalyticsName = 'log-${appNamePrefix}-${normalizedEnvironment}-${uniqueSuffix}'
var appInsightsName = 'appi-${appNamePrefix}-${normalizedEnvironment}-${uniqueSuffix}'
var appServicePlanName = 'asp-${appNamePrefix}-${normalizedEnvironment}'
var webAppName = 'web-${appNamePrefix}-${normalizedEnvironment}-${uniqueSuffix}'
var backendAppNamePrefix = 'api-${appNamePrefix}-${normalizedEnvironment}'
var appConfigEndpoint = appConfig.properties.endpoint
var appInsightsConnectionString = appInsights.properties.ConnectionString
var appInsightsInstrumentationKey = appInsights.properties.InstrumentationKey
var sqlConnectionString = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${sqlDatabaseName};Persist Security Info=False;User ID=${sqlAdministratorLogin};Password=${sqlAdministratorPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'

resource appConfig 'Microsoft.AppConfiguration/configurationStores@2024-05-01' = {
  name: appConfigName
  location: location
  tags: tags
  sku: {
    name: 'Standard'
  }
  properties: {
    disableLocalAuth: false
    publicNetworkAccess: 'Enabled'
  }
}

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: logAnalyticsName
  location: location
  tags: tags
  properties: {
    retentionInDays: 30
    sku: {
      name: 'PerGB2018'
    }
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
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
    Flow_Type: 'Bluefield'
    Request_Source: 'rest'
  }
}

resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: acrName
  location: location
  tags: tags
  sku: {
    name: acrSku
  }
  properties: {
    adminUserEnabled: true
    publicNetworkAccess: 'Enabled'
    networkRuleBypassOptions: 'AzureServices'
  }
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2024-01-01' = {
  name: storageAccountName
  location: location
  kind: 'StorageV2'
  tags: tags
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    publicNetworkAccess: 'Enabled'
    allowSharedKeyAccess: true
  }
}

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
    restrictOutboundNetworkAccess: 'Disabled'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: sqlDatabaseName
  location: location
  tags: tags
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 2147483648
    zoneRedundant: false
    readScale: 'Disabled'
    requestedBackupStorageRedundancy: 'Local'
  }
}

resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2024-01-01' = {
  name: serviceBusNamespaceName
  location: location
  tags: tags
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }
  properties: {
    minimumTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: appServicePlanName
  location: location
  kind: 'linux'
  tags: tags
  sku: {
    name: appServiceSku.name
    tier: appServiceSku.tier
    capacity: 1
  }
  properties: {
    reserved: true
  }
}

resource webApp 'Microsoft.Web/sites@2023-01-01' = {
  name: webAppName
  location: location
  kind: 'app,linux'
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'NODE|22-lts'
      alwaysOn: true
      ftpsState: 'FtpsOnly'
      minTlsVersion: '1.2'
      http20Enabled: true
    }
  }
}

resource webAppSettings 'Microsoft.Web/sites/config@2023-01-01' = {
  name: 'appsettings'
  parent: webApp
  properties: {
    ASPNETCORE_ENVIRONMENT: environmentName
    DOTNET_ENVIRONMENT: environmentName
    AzureAppConfiguration__Endpoint: appConfigEndpoint
    APPLICATIONINSIGHTS_CONNECTION_STRING: appInsightsConnectionString
    APPINSIGHTS_INSTRUMENTATIONKEY: appInsightsInstrumentationKey
    WEBSITES_PORT: '8080'
    NODE_ENV: 'production'
  }
}

resource backendApps 'Microsoft.Web/sites@2023-01-01' = [for apiName in backendApiNames: {
  name: '${backendAppNamePrefix}-${apiName}'
  location: location
  kind: 'app,linux,container'
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOCKER|${acr.properties.loginServer}/${apiName}:latest'
      alwaysOn: true
      ftpsState: 'FtpsOnly'
      minTlsVersion: '1.2'
      http20Enabled: true
    }
  }
}]

resource backendAppSettings 'Microsoft.Web/sites/config@2023-01-01' = [for (apiName, index) in backendApiNames: {
  name: 'appsettings'
  parent: backendApps[index]
  properties: {
    ASPNETCORE_ENVIRONMENT: environmentName
    DOTNET_ENVIRONMENT: environmentName
    AzureAppConfiguration__Endpoint: appConfigEndpoint
    APPLICATIONINSIGHTS_CONNECTION_STRING: appInsightsConnectionString
    APPINSIGHTS_INSTRUMENTATIONKEY: appInsightsInstrumentationKey
    WEBSITES_PORT: '8080'
    SQLSERVER_CONNECTION_STRING: sqlConnectionString
    ServiceBus__ConnectionString: listKeys('${serviceBusNamespace.id}/AuthorizationRules/RootManageSharedAccessKey', serviceBusNamespace.apiVersion).primaryConnectionString
    Storage__ConnectionString: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${listKeys(storageAccount.id, '2024-01-01').keys[0].value};EndpointSuffix=core.windows.net'
    Config__Environment: environmentName
  }
}]

resource appConfigKeyGateway 'Microsoft.AppConfiguration/configurationStores/keyValues@2024-05-01' = {
  parent: appConfig
  name: 'Gateway:ServiceName'
  properties: {
    value: 'Edp.Gateway'
    tags: {
      environment: environmentName
    }
  }
}

resource appConfigKeySql 'Microsoft.AppConfiguration/configurationStores/keyValues@2024-05-01' = {
  parent: appConfig
  name: 'ConnectionStrings:Sql'
  properties: {
    value: sqlConnectionString
    tags: {
      environment: environmentName
    }
  }
}

resource appConfigKeyServiceBus 'Microsoft.AppConfiguration/configurationStores/keyValues@2024-05-01' = {
  parent: appConfig
  name: 'ConnectionStrings:ServiceBus'
  properties: {
    value: listKeys('${serviceBusNamespace.id}/AuthorizationRules/RootManageSharedAccessKey', serviceBusNamespace.apiVersion).primaryConnectionString
    tags: {
      environment: environmentName
    }
  }
}

resource appConfigKeyStorage 'Microsoft.AppConfiguration/configurationStores/keyValues@2024-05-01' = {
  parent: appConfig
  name: 'ConnectionStrings:Storage'
  properties: {
    value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${listKeys(storageAccount.id, '2024-01-01').keys[0].value};EndpointSuffix=core.windows.net'
    tags: {
      environment: environmentName
    }
  }
}

output environmentName string = environmentName
output location string = location
output resourceGroupName string = resourceGroup().name
output appConfigurationEndpoint string = appConfigEndpoint
output appConfigurationName string = appConfig.name
output acrName string = acr.name
output acrLoginServer string = acr.properties.loginServer
output sqlServerName string = sqlServer.name
output sqlDatabaseName string = sqlDatabase.name
output serviceBusNamespaceName string = serviceBusNamespace.name
output storageAccountName string = storageAccount.name
output webAppName string = webApp.name
output backendAppNames array = [for i in range(0, length(backendApiNames)): backendApps[i].name]
