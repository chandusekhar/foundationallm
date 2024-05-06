﻿using FoundationaLLM.Common.Authentication;
using FoundationaLLM.Common.Exceptions;
using FoundationaLLM.Common.Interfaces;
using FoundationaLLM.Common.Models.Vectorization;
using FoundationaLLM.Common.Settings;
using FoundationaLLM.SemanticKernel.Core.Models.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureAISearch;
using Microsoft.SemanticKernel.Memory;
using System.ComponentModel;
using System.Text.Json;

#pragma warning disable SKEXP0001, SKEXP0020

namespace FoundationaLLM.SemanticKernel.Core.Services
{
    /// <summary>
    /// Provides vector embedding indexing based on Azure AI Search.
    /// </summary>
    public class AzureAISearchIndexingService : IIndexingService
    {
        private readonly AzureAISearchIndexingServiceSettings _settings;
        private readonly ILogger<AzureAISearchIndexingService> _logger;
        private readonly AzureAISearchMemoryStore _memoryStore;

        /// <summary>
        /// Creates a new <see cref="SemanticKernelTextEmbeddingService"/> instance.
        /// </summary>
        /// <param name="options">The <see cref="IOptions{TOptions}"/> providing configuration settings.</param>
        /// <param name="logger">The <see cref="ILogger"/> used for logging.</param>
        public AzureAISearchIndexingService(
            IOptions<AzureAISearchIndexingServiceSettings> options,
            ILogger<AzureAISearchIndexingService> logger)
        {
            _settings = options.Value;
            _logger = logger;
            _memoryStore = CreateMemoryStore();
        }

        /// <inheritdoc/>
        public async Task<List<string>> IndexEmbeddingsAsync(EmbeddedContent embeddedContent, string indexName)
        {
            var indexIds = new List<string>();
            var memoryRecords = embeddedContent.ContentParts.Select(cp => MemoryRecord.LocalRecord(
                cp.Id,
                cp.Content,
                "Generated by FoundationaLLM.",
                cp.Embedding.Vector,
                JsonSerializer.Serialize<ContentIdentifier>(embeddedContent.ContentId),
                embeddedContent.ContentId.UniqueId,
                DateTimeOffset.UtcNow)).ToList();

            await foreach (var id in _memoryStore.UpsertBatchAsync(
                indexName, memoryRecords))
            {
                indexIds.Add(id);
            }

            return indexIds;
        }        

        /// <summary>
        /// Creates an <see cref="AzureAISearchMemoryStore"/> instance using the endpoint and the API key.
        /// </summary>
        /// <param name="endpoint">The endpoint of the Azure AI Search deployment.</param>
        /// <param name="apiKey">The API key used to connect to the Azure AI Search deployment.</param>
        /// <returns>The <see cref="AzureAISearchMemoryStore"/> instance.</returns>
        private AzureAISearchMemoryStore CreateMemoryStoreFromAPIKey(string endpoint, string apiKey) =>
            new(endpoint, apiKey);

        /// <summary>
        /// Creates an <see cref="AzureAISearchMemoryStore"/> instance using the endpoint and the Azure identity.
        /// </summary>
        /// <param name="endpoint">The endpoint of the Azure AI Search deployment.</param>
        /// <returns>The <see cref="Kernel"/> instance.</returns>
        private AzureAISearchMemoryStore CreateMemoryStoreFromIdentity(string endpoint) =>
            new(endpoint, DefaultAuthentication.GetAzureCredential());

        private void ValidateEndpoint(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                _logger.LogCritical("The Azure AI Search endpoint is invalid.");
                throw new ConfigurationValueException("The Azure AI Search endpoint is invalid.");
            }
        }

        private AzureAISearchMemoryStore CreateMemoryStore()
        {
            switch (_settings.AuthenticationType)
            {
                case AzureAISearchAuthenticationTypes.AzureIdentity:
                    ValidateEndpoint(_settings.Endpoint);
                    return CreateMemoryStoreFromIdentity(_settings.Endpoint);
                default:
                    throw new InvalidEnumArgumentException($"The authentication type {_settings.AuthenticationType} is not supported.");
            }
        }
    }
}
