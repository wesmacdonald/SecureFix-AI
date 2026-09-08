targetScope = 'resourceGroup'

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('Name of the SecureFix Container App.')
param containerAppName string = 'securefix-api'

@description('Name of the Azure Container Apps managed environment.')
param managedEnvironmentName string = 'securefix-environment'

@description('Name of the Log Analytics workspace.')
param logAnalyticsWorkspaceName string = 'securefix-logs'

@description('Name of an existing Azure Container Registry that hosts the application image.')
param containerRegistryName string

@description('Image tag deployed from the Azure Container Registry.')
param imageTag string

@secure()
@description('Bearer token required by the demo authentication handler. Only used when authMode is "demo".')
param demoToken string = ''

@description('Authentication mode for the API. "entra" is required for production deployments; "demo" is for local/dev only.')
@allowed([
  'entra'
  'demo'
])
param authMode string = 'entra'

@description('Microsoft Entra ID tenant ID. Required when authMode is "entra".')
param azureAdTenantId string = ''

@description('Microsoft Entra ID app registration (client) ID for the SecureFix API. Required when authMode is "entra".')
param azureAdClientId string = ''

@description('Expected token audience (App ID URI), e.g. api://<client-id>. Required when authMode is "entra".')
param azureAdAudience string = ''

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsWorkspaceName
  location: location
  properties: {
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
    retentionInDays: 30
    sku: {
      name: 'PerGB2018'
    }
  }
}

resource managedEnvironment 'Microsoft.App/managedEnvironments@2025-01-01' = {
  name: managedEnvironmentName
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalyticsWorkspace.properties.customerId
        sharedKey: logAnalyticsWorkspace.listKeys().primarySharedKey
      }
    }
  }
}

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
  name: containerRegistryName
}

resource pullIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${containerAppName}-identity'
  location: location
}

resource acrPullRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(containerRegistry.id, pullIdentity.id, 'AcrPull')
  scope: containerRegistry
  properties: {
    principalId: pullIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      '7f951dda-4ed3-4680-a7ca-43fe172d538d'
    )
  }
}

resource containerApp 'Microsoft.App/containerApps@2025-01-01' = {
  name: containerAppName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${pullIdentity.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: managedEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 5000
        transport: 'auto'
      }
      registries: [
        {
          server: containerRegistry.properties.loginServer
          identity: pullIdentity.id
        }
      ]
      secrets: [
        {
          name: 'demo-token'
          value: demoToken
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'securefix-api'
          image: '${containerRegistry.properties.loginServer}/securefix:${imageTag}'
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: authMode == 'demo' ? 'Development' : 'Production'
            }
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://+:5000'
            }
            {
              name: 'AI_PROVIDER'
              value: 'mock'
            }
            {
              name: 'Database__Path'
              value: '/tmp/securefix.db'
            }
            {
              name: 'AUTH_MODE'
              value: authMode
            }
            {
              name: 'AzureAd__TenantId'
              value: azureAdTenantId
            }
            {
              name: 'AzureAd__ClientId'
              value: azureAdClientId
            }
            {
              name: 'AzureAd__Audience'
              value: azureAdAudience
            }
            {
              name: 'SECUREFIX_DEMO_TOKEN'
              secretRef: 'demo-token'
            }
          ]
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: '/health'
                port: 5000
              }
              initialDelaySeconds: 10
              periodSeconds: 10
            }
            {
              type: 'Readiness'
              httpGet: {
                path: '/ready'
                port: 5000
              }
              initialDelaySeconds: 5
              periodSeconds: 10
            }
          ]
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 3
      }
    }
  }
  dependsOn: [
    acrPullRoleAssignment
  ]
}

output containerAppUrl string = 'https://${containerApp.properties.configuration.ingress.fqdn}'
output managedIdentityPrincipalId string = pullIdentity.properties.principalId
