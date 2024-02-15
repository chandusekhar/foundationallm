﻿using Azure.Messaging;
using FoundationaLLM.Agent.Constants;
using FoundationaLLM.Agent.Models.Resources;
using FoundationaLLM.Common.Constants;
using FoundationaLLM.Common.Exceptions;
using FoundationaLLM.Common.Interfaces;
using FoundationaLLM.Common.Models.Agents;
using FoundationaLLM.Common.Models.Configuration.Instance;
using FoundationaLLM.Common.Models.Events;
using FoundationaLLM.Common.Models.ResourceProvider;
using FoundationaLLM.Common.Models.ResourceProviders;
using FoundationaLLM.Common.Services.ResourceProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace FoundationaLLM.Agent.ResourceProviders
{
    /// <summary>
    /// Implements the FoundationaLLM.Agent resource provider.
    /// </summary>
    public class AgentResourceProviderService(
        IOptions<InstanceSettings> instanceOptions,
        [FromKeyedServices(DependencyInjectionKeys.FoundationaLLM_ResourceProvider_Agent)] IStorageService storageService,
        IEventService eventService,
        ILoggerFactory loggerFactory)
        : ResourceProviderServiceBase(
            instanceOptions.Value,
            storageService,
            eventService,
            loggerFactory.CreateLogger<AgentResourceProviderService>(),
            [
                EventSetEventNamespaces.FoundationaLLM_ResourceProvider_Agent
            ])
    {
        /// <inheritdoc/>
        protected override Dictionary<string, ResourceTypeDescriptor> GetResourceTypes() => new()
        {
            {
                AgentResourceTypeNames.Agents,
                new ResourceTypeDescriptor(
                        AgentResourceTypeNames.Agents)
                {
                    AllowedTypes = [
                            new ResourceTypeAllowedTypes(HttpMethod.Get.Method, [], [], [typeof(KnowledgeManagementAgent)]),
                            new ResourceTypeAllowedTypes(HttpMethod.Post.Method, [], [typeof(KnowledgeManagementAgent)], [typeof(ResourceProviderUpsertResult)]),
                            new ResourceTypeAllowedTypes(HttpMethod.Delete.Method, [], [], []),
                    ],
                    Actions = [
                            new ResourceTypeAction(AgentResourceProviderActions.CheckName, false, true, [
                                new ResourceTypeAllowedTypes(HttpMethod.Post.Method, [], [typeof(ResourceName)], [typeof(ResourceNameCheckResult)])
                            ])
                        ]
                }
            },
            {
                AgentResourceTypeNames.AgentReferences,
                new ResourceTypeDescriptor(AgentResourceTypeNames.AgentReferences)
            }
        };

        private ConcurrentDictionary<string, AgentReference> _agentReferences = [];

        private const string AGENT_REFERENCES_FILE_NAME = "_agent-references.json";
        private const string AGENT_REFERENCES_FILE_PATH = $"/{ResourceProviderNames.FoundationaLLM_Agent}/_agent-references.json";

        /// <inheritdoc/>
        protected override string _name => ResourceProviderNames.FoundationaLLM_Agent;

        /// <inheritdoc/>
        protected override async Task InitializeInternal()
        {
            _logger.LogInformation("Starting to initialize the {ResourceProvider} resource provider...", _name);

            if (await _storageService.FileExistsAsync(_storageContainerName, AGENT_REFERENCES_FILE_PATH, default))
            {
                var fileContent = await _storageService.ReadFileAsync(_storageContainerName, AGENT_REFERENCES_FILE_PATH, default);
                var agentReferenceStore = JsonSerializer.Deserialize<AgentReferenceStore>(
                    Encoding.UTF8.GetString(fileContent.ToArray()));

                _agentReferences = new ConcurrentDictionary<string, AgentReference>(
                    agentReferenceStore!.ToDictionary());
            }
            else
            {
                await _storageService.WriteFileAsync(
                    _storageContainerName,
                    AGENT_REFERENCES_FILE_PATH,
                    JsonSerializer.Serialize(new AgentReferenceStore { AgentReferences = [] }),
                    default,
                    default);
            }

            _logger.LogInformation("The {ResourceProvider} resource provider was successfully initialized.", _name);
        }

        #region Support for Management API

        /// <inheritdoc/>
        protected override async Task<object> GetResourcesAsyncInternal(List<ResourceTypeInstance> instances) =>
            instances[0].ResourceType switch
            {
                AgentResourceTypeNames.Agents => await LoadAgents(instances[0]),
                _ => throw new ResourceProviderException($"The resource type {instances[0].ResourceType} is not supported by the {_name} resource provider.")
            };

        #region Helpers for GetResourcesAsyncInternal

        private async Task<List<AgentBase>> LoadAgents(ResourceTypeInstance instance)
        {
            if (instance.ResourceId == null)
            {
                return
                [
                    .. (await Task.WhenAll(
                            _agentReferences.Values.Select(ar => LoadAgent(ar))))
                ];
            }
            else
            {
                //TODO: Find a more efficient way to load and agent when the type is not known in advance
                // (ideally avoiding the double load below).

                var agentReference = new AgentReference
                {
                    Name = instance.ResourceId,
                    Type = AgentTypes.Basic,
                    Filename = $"/{_name}/{instance.ResourceId}.json"
                };
                var agent = await LoadAgent(agentReference);

                agentReference.Type = agent.Type;
                agent = await LoadAgent(agentReference);

                _agentReferences.AddOrUpdate(
                    agentReference.Name,
                    agentReference,
                    (k, v) => v);

                return [agent];
            }
        }

        private async Task<AgentBase> LoadAgent(AgentReference agentReference)
        {
            if (await _storageService.FileExistsAsync(_storageContainerName, agentReference.Filename, default))
            {
                var fileContent = await _storageService.ReadFileAsync(_storageContainerName, agentReference.Filename, default);
                return JsonSerializer.Deserialize(
                    Encoding.UTF8.GetString(fileContent.ToArray()),
                    agentReference.AgentType,
                    _serializerSettings) as AgentBase
                    ?? throw new ResourceProviderException($"Failed to load the agent {agentReference.Name}.");
            }

            throw new ResourceProviderException($"Could not locate the {agentReference.Name} agent resource.");
        }

        #endregion

        /// <inheritdoc/>
        protected override async Task<object> UpsertResourceAsync(List<ResourceTypeInstance> instances, string serializedResource) =>
            instances[0].ResourceType switch
            {
                AgentResourceTypeNames.Agents => await UpdateAgent(instances, serializedResource),
                _ => throw new ResourceProviderException($"The resource type {instances[0].ResourceType} is not supported by the {_name} resource provider.")
            };

        #region Helpers for UpsertResourceAsync

        private async Task<ResourceProviderUpsertResult> UpdateAgent(List<ResourceTypeInstance> instances, string serializedAgent)
        {
            var agentBase = JsonSerializer.Deserialize<AgentBase>(serializedAgent)
                ?? throw new ResourceProviderException("The object definition is invalid.");

            if (instances[0].ResourceId != agentBase.Name)
                throw new ResourceProviderException("The resource path does not match the object definition (name mismatch).");

            var agentReference = new AgentReference
            {
                Name = agentBase.Name!,
                Type = agentBase.Type!,
                Filename = $"/{_name}/{agentBase.Name}.json"
            };

            var agent = JsonSerializer.Deserialize(serializedAgent, agentReference.AgentType, _serializerSettings);
            (agent as AgentBase)!.ObjectId = GetObjectId(instances);

            await _storageService.WriteFileAsync(
                _storageContainerName,
                agentReference.Filename,
                JsonSerializer.Serialize(agent, agentReference.AgentType, _serializerSettings),
                default,
                default);

            _agentReferences.AddOrUpdate(agentReference.Name, agentReference, (k, v) => agentReference);

            await _storageService.WriteFileAsync(
                    _storageContainerName,
                    AGENT_REFERENCES_FILE_PATH,
                    JsonSerializer.Serialize(AgentReferenceStore.FromDictionary(_agentReferences.ToDictionary())),
                    default,
                    default);

            return new ResourceProviderUpsertResult
            {
                ObjectId = (agent as AgentBase)!.ObjectId
            };
        }

        #endregion

        /// <inheritdoc/>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        protected override async Task<object> ExecuteActionAsync(List<ResourceTypeInstance> instances, string serializedAction) =>
            instances.Last().ResourceType switch
            {
                AgentResourceTypeNames.Agents => instances.Last().Action switch
                {
                    AgentResourceProviderActions.CheckName => CheckAgentName(serializedAction),
                    _ => throw new ResourceProviderException($"The action {instances.Last().Action} is not supported by the {_name} resource provider.")
                },
                _ => throw new ResourceProviderException()
            };
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

        #region Helpers for ExecuteActionAsync

        private ResourceNameCheckResult CheckAgentName(string serializedAction)
        {
            var resourceName = JsonSerializer.Deserialize<ResourceName>(serializedAction);
            return _agentReferences.Values.Any(ar => ar.Name == resourceName!.Name)
                ? new ResourceNameCheckResult
                {
                    Name = resourceName!.Name,
                    Type = resourceName.Type,
                    Status = NameCheckResultType.Denied,
                    Message = "A resource with the specified name already exists."
                }
                : new ResourceNameCheckResult
                {
                    Name = resourceName!.Name,
                    Type = resourceName.Type,
                    Status = NameCheckResultType.Allowed
                };
        }

        #endregion

        #endregion

        #region Event handling

        /// <inheritdoc/>
        protected override async Task HandleEvents(EventSetEventArgs e)
        {
            _logger.LogInformation("{EventsCount} events received in the {EventsNamespace} events namespace.",
                e.Events.Count, e.Namespace);

            switch (e.Namespace)
            {
                case EventSetEventNamespaces.FoundationaLLM_ResourceProvider_Agent:
                    foreach (var @event in e.Events)
                        await HandleAgentResourceProviderEvent(@event);
                    break;
                default:
                    // Ignore sliently any event namespace that's of no interest.
                    break;
            }

            await Task.CompletedTask;
        }

        private async Task HandleAgentResourceProviderEvent(CloudEvent e)
        {
            if (string.IsNullOrWhiteSpace(e.Subject))
                return;

            var fileName = e.Subject.Split("/").Last();

            _logger.LogInformation("The file [{FileName}] managed by the [{ResourceProvider}] resource provider has changed and will be reloaded.",
                fileName, _name);

            var agentReference = new AgentReference
            {
                Name = Path.GetFileNameWithoutExtension(fileName),
                Filename = $"/{_name}/{fileName}",
                Type = AgentTypes.Basic
            };

            var agent = await LoadAgent(agentReference);
            agentReference.Name = agent.Name;
            agentReference.Type = agent.Type;

            _agentReferences.AddOrUpdate(
                agentReference.Name,
                agentReference,
                (k, v) => v);

            _logger.LogInformation("The agent reference for the [{AgentName}] agent or type [{AgentType}] was loaded.",
                agentReference.Name, agentReference.Type);
        }

        #endregion
    }
}
