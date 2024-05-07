using FoundationaLLM.Common.Interfaces;
using FoundationaLLM.Common.Models.Agents;
using FoundationaLLM.Common.Models.ResourceProviders.Configuration;
using FoundationaLLM.Common.Models.ResourceProviders.Vectorization;
using FoundationaLLM.Common.Models.ResourceProviders.Agent;
using FoundationaLLM.Core.Examples.Exceptions;

namespace FoundationaLLM.Core.Examples.Interfaces;

/// <summary>
/// Provides methods to interact with the FoundationaLLM Management API.
/// </summary>
/// <param name="httpClientManager"></param>
public interface IManagementAPITestManager
{
    /// <summary>
    /// Retrieves an agent and its dependencies from the test catalog, creates the dependencies, and then creates the agent.
    /// </summary>
    /// <param name="agentName">The name of the agent and its dependencies to retrieve from the test catalog and create.</param>
    /// <returns>The created agent.</returns>
    Task<AgentBase> CreateAgent(string agentName);

    /// <summary>
    /// Deletes an agent and its dependencies.
    /// </summary>
    /// <param name="agentName">The name of the agent and its dependencies to delete.</param>
    /// <returns></returns>
    Task DeleteAgent(string agentName);

    /// <summary>
    /// Retrieves one or more resources.
    /// </summary>
    /// <param name="instanceId">The FoundationaLLM instance identifier.</param>
    /// <param name="resourceProvider">The name of the resource provider that should handle the request.</param>
    /// <param name="resourcePath">The logical path of the resource type.</param>
    /// <returns></returns>
    /// <exception cref="FoundationaLLMException"></exception>
    Task<T?> GetResourcesAsync<T>(string instanceId, string resourceProvider, string resourcePath);

    /// <summary>
    /// Creates or updates resources.
    /// </summary>
    /// <param name="instanceId">The FoundationaLLM instance identifier.</param>
    /// <param name="resourceProvider">The name of the resource provider that should handle the request.</param>
    /// <param name="resourcePath">The logical path of the resource type.</param>
    /// <param name="resource">The resource to insert or update.</param>
    /// <returns>The ObjectId of the created or updated resource.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="FoundationaLLMException"></exception>
    Task<string> UpsertResourceAsync(string instanceId, string resourceProvider, string resourcePath,
        object resource);

    /// <summary>
    /// Deletes a resource then purges it, so we can reuse the name.
    /// </summary>
    /// <param name="instanceId">The FoundationaLLM instance identifier.</param>
    /// <param name="resourceProvider">The name of the resource provider that should handle the request.</param>
    /// <param name="resourcePath">The logical path of the resource type.</param>
    /// <returns></returns>
    /// <exception cref="FoundationaLLMException"></exception>
    Task DeleteResourceAsync(string instanceId, string resourceProvider, string resourcePath);

    Task CreateAppConfiguration(AppConfigurationKeyValue appConfigurationKeyValue);

    Task CreateDataSource(string name);

    Task CreateContentSourceProfile(string name);

    Task CreateTextPartitioningProfile(string name);

    Task CreateTextEmbeddingProfile(string name);

    Task CreateIndexingProfile(string name);

    Task<VectorizationRequest> GetVectorizationRequest(VectorizationRequest vectorizationRequest);

    Task<string> CreateVectorizationRequest(VectorizationRequest vectorizationRequest);

    Task DeleteVectorizationRequest(VectorizationRequest vectorizationRequest);

    Task DeleteAppConfiguration(string name);

    Task DeleteDataSource(string name, List<AppConfigurationKeyValue> configurationKeyValues);

    Task DeleteContentSourceProfile(string name);

    Task DeleteTextPartitioningProfile(string name);

    Task DeleteIndexingProfile(string name);

    Task DeleteTextEmbeddingProfile(string name);
    Task<IndexingProfile> GetIndexingProfile(string name);

    Task<TextEmbeddingProfile> GetTextEmbeddingProfile(string name);

    Task<TextPartitioningProfile> GetTextPartitioningProfile(string name);
}