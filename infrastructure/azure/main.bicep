// main.bicep — Azure infrastructure for Local-First Enterprise RAG
// Uses basic/cheap tiers to minimize portfolio hosting costs
//
// Resources:
//   - Azure SQL Database (Basic tier, ~$5/month)
//   - Azure Container App (Ollama with GPU when available, CPU fallback)
//   - Azure App Service (Free/Basic tier for .NET 9 web app)
//   - Managed Identity for zero-trust SQL access

@description('Azure region for all resources')
param location string = resourceGroup().location

@description('Unique prefix for resource names')
param projectName string = 'enterpriserag'

@description('SQL Server administrator password')
@secure()
param sqlAdminPassword string

@description('App Service plan SKU (B1 = Basic, F1 = Free)')
param appServiceSku string = 'B1'

// ─── SQL Server (Basic tier) ───────────────────────────────────────
resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: '${projectName}-sql'
  location: location
  properties: {
    administratorLogin: 'sqladmin'
    administratorLoginPassword: sqlAdminPassword
    version: '12.0'
    minimalTlsVersion: '1.2'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AdventureWorksDW2020'
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
    capacity: 5
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 2147483648 // 2 GB
  }
}

// Allow Azure services to access SQL
resource sqlFirewallAzure 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// ─── App Service Plan ──────────────────────────────────────────────
resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: '${projectName}-plan'
  location: location
  kind: 'linux'
  sku: {
    name: appServiceSku
  }
  properties: {
    reserved: true // Linux
  }
}

// ─── Web App (.NET 9) ──────────────────────────────────────────────
resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: '${projectName}-web'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|9.0'
      alwaysOn: appServiceSku != 'F1'
      appSettings: [
        {
          name: 'AZURE_ENVIRONMENT'
          value: 'true'
        }
        {
          name: 'Ollama__BaseUrl'
          value: 'https://${containerApp.properties.configuration.ingress.fqdn}'
        }
        {
          name: 'Ollama__EmbeddingModel'
          value: 'mxbai-embed-large'
        }
        {
          name: 'Ollama__GenerationModel'
          value: 'llama3.2'
        }
        {
          name: 'Database__ConnectionString'
          value: 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Database=AdventureWorksDW2020;Authentication=Active Directory Managed Identity;TrustServerCertificate=false;Encrypt=true;'
        }
        {
          name: 'Search__DefaultTopK'
          value: '5'
        }
        {
          name: 'Search__SimilarityThreshold'
          value: '0.3'
        }
      ]
    }
  }
}

// Grant web app Managed Identity access to SQL
resource sqlRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(sqlServer.id, webApp.id, 'sql-contributor')
  scope: sqlServer
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b24988ac-6180-42a0-ab88-20f7382dd24c')
    principalId: webApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// ─── Container App Environment (for Ollama) ────────────────────────
resource containerEnv 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: '${projectName}-env'
  location: location
  properties: {
    zoneRedundant: false
  }
}

resource containerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: '${projectName}-ollama'
  location: location
  properties: {
    managedEnvironmentId: containerEnv.id
    configuration: {
      ingress: {
        external: false // Only accessible within the environment
        targetPort: 11434
        transport: 'http'
      }
    }
    template: {
      containers: [
        {
          name: 'ollama'
          image: 'ollama/ollama:latest'
          resources: {
            cpu: json('2.0')
            memory: '4Gi'
          }
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
  }
}

// ─── Outputs ───────────────────────────────────────────────────────
output webAppUrl string = 'https://${webApp.properties.defaultHostName}'
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output webAppPrincipalId string = webApp.identity.principalId
