using './main.bicep'

param environmentName = 'prod'
param location = 'eastus2'
param appNamePrefix = 'edp'
param sqlAdministratorLogin = 'sqladmin'
param sqlAdministratorPassword = ''
param appServiceSku = {
  name: 'P1v3'
  tier: 'PremiumV3'
}
param acrSku = 'Standard'
param tags = {
  environment: 'prod'
  application: 'enterprise-document-platform'
  owner: 'platform-team'
}
