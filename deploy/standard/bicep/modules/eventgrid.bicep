/** Inputs **/
@description('Action Group Id for alerts')
param actionGroupId string

@description('DNS resource group name')
param dnsResourceGroupName string

@description('KeyVault resource suffix for all resources')
param kvResourceSuffix string = resourceSuffix

@description('Location for all resources')
param location string = resourceGroup().location

@description('Log Analytic Workspace Id to use for diagnostics')
param logAnalyticWorkspaceId string

@description('Log Analytic Workspace Resource Id to use for diagnostics')
param logAnalyticWorkspaceResourceId string

param networkingResourceGroupName string

@description('OPS Resource Group name.')
param opsResourceGroupName string = resourceGroup().name

@description('Private DNS Zones for private endpoint')
param privateDnsZones array

@description('Resource suffix for all resources')
param resourceSuffix string

@description('Subnet Id for private endpoint')
param subnetId string

@description('Tags for all resources')
param tags object

@description('Timestamp for nested deployments')
param timestamp string = utcNow()

param topics array = []

/** Locals **/
@description('Metric alerts for the resource.')
var alerts = [
  // {
  //   description: 'Node CPU utilization greater than 95% for 1 hour'
  //   evaluationFrequency: 'PT5M'
  //   metricName: 'node_cpu_usage_percentage'
  //   name: 'node-cpu'
  //   operator: 'GreaterThan'
  //   severity: 3
  //   threshold: 95
  //   timeAggregation: 'Average'
  //   windowSize: 'PT5M'
  // }
  // {
  //   description: 'Node memory utilization greater than 95% for 1 hour'
  //   evaluationFrequency: 'PT5M'
  //   metricName: 'node_memory_working_set_percentage'
  //   name: 'node-memory'
  //   operator: 'GreaterThan'
  //   severity: 3
  //   threshold: 100
  //   timeAggregation: 'Average'
  //   windowSize: 'PT5M'
  // }
]

@description('Formatted untruncated resource name')
var kvFormattedName = toLower('${kvServiceType}-${substring(kvResourceSuffix, 0, length(kvResourceSuffix) - 4)}')

@description('The Resource Name')
var kvTruncatedName = substring(kvFormattedName,0,min([length(kvFormattedName),20]))
var kvName = '${kvTruncatedName}-${substring(kvResourceSuffix, length(kvResourceSuffix) - 3, 3)}'

@description('The Resource Service Type token')
var kvServiceType = 'kv'

@description('The Resource logs to enable')
var logs = [
  // 'CassandraRequests'
  // 'ControlPlaneRequests'
  // 'DataPlaneRequests'
  // 'GremlinRequests'
  // 'MongoRequests'
  // 'PartitionKeyRUConsumption'
  // 'PartitionKeyStatistics'
  // 'QueryRuntimeStatistics'
  // 'TableApiRequests'
]

@description('The Resource Name')
var name = '${serviceType}-${resourceSuffix}'

@description('The Resource Service Type token')
var serviceType = 'eg'


resource main 'Microsoft.EventGrid/namespaces@2023-12-15-preview' = {
  name: name
  location: location
  sku: {
    name: 'Standard'
    capacity: 1
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    isZoneRedundant: true
    publicNetworkAccess: 'Disabled'
    inboundIpRules: []
  }
  tags: tags
}

resource egTopics 'Microsoft.EventGrid/namespaces/topics@2023-12-15-preview' = [
  for topic in topics: {
    parent: main
    name: topic
    properties: {
      publisherType: 'Custom'
      inputSchema: 'CloudEventSchemaV1_0'
      eventRetentionInDays: 1
    }
  }
]


var secretNames = [
  'foundationallm-events-azureeventgrid-apikey'
  'event-grid-key'
]

module eventGridKey 'kvSecret.bicep' = [
  for (secretName,i) in secretNames: {
    name: 'egKey-${i}'
    scope: resourceGroup(opsResourceGroupName)
    params: {
      kvName: kvName
      secretName: secretName
      secretValue: main.listKeys().key1
      tags: tags
    }
  }
]

@description('Diagnostic settings for the resource')
resource diagnostics 'Microsoft.Insights/diagnosticSettings@2017-05-01-preview' = {
  scope: main
  name: 'diag-${serviceType}'
  properties: {
    workspaceId: logAnalyticWorkspaceId
    logs: [for log in logs: {
      category: log
      enabled: true
    }]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
      }
    ]
  }
}

/** Nested Modules **/
@description('Metric alerts for the resource')
module metricAlerts 'utility/metricAlerts.bicep' = {
  name: 'alert-${main.name}-${timestamp}'
  params: {
    actionGroupId: actionGroupId
    alerts: alerts
    metricNamespace: 'Microsoft.EventGrid/namespaces'
    nameSuffix: name
    serviceId: main.id
    tags: tags
  }
}

@description('Private endpoint for the resource')
module privateEndpoint 'utility/privateEndpoint.bicep' = {
  name: 'pe-${main.name}-${timestamp}'
  params: {
    groupId: 'topic'
    location: location
    privateDnsZones: privateDnsZones
    subnetId: subnetId
    tags: tags

    service: {
      id: main.id
      name: main.name
    }
  }
}

output endpoint string = 'https://${main.properties.topicsConfiguration.hostname}'
output id string = main.id
output keySecretName string = eventGridKey[0].name
output keySecretRef string = eventGridKey[0].outputs.secretUri
output name string = main.name
output topicNames array = topics
output topicIds array = [for (topicName,i) in topics: egTopics[i].id]