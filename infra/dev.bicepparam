using './main.bicep'

param environmentName = 'dev'
param location = 'eastus'
param appNamePrefix = 'edp'
param sqlAdministratorLogin = 'sqladmin'
param sqlAdministratorPassword = ''
param appServiceSku = {
  name: 'B1'
  tier: 'Basic'
}
param acrSku = 'Basic'
param tags = {
  environment: 'dev'
  application: 'enterprise-document-platform'
  owner: 'platform-team'
}
