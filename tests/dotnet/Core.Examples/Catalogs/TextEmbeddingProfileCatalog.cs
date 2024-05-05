﻿using FoundationaLLM.Common.Models.ResourceProviders.Vectorization;

namespace FoundationaLLM.Core.Examples.Catalogs
{
    public static class TextEmbeddingProfileCatalog
    {
        public static readonly List<TextEmbeddingProfile> Items =
        [
            new TextEmbeddingProfile { Name = "text_embedding_profile", Settings = new Dictionary<string, string> { { "model_name", "text-embedding-ada-002"} },  TextEmbedding = TextEmbeddingType.GatewayTextEmbedding },
            new TextEmbeddingProfile { Name = "TestingEmbeddingProfile", TextEmbedding = TextEmbeddingType.SemanticKernelTextEmbedding, ConfigurationReferences = new Dictionary<string, string>{ { "APIKey", "FoundationaLLM:Vectorization:SemanticKernelTextEmbeddingService:APIKey" }, { "APIVersion", "FoundationaLLM:Vectorization:SemanticKernelTextEmbeddingService:APIVersion" }, { "AuthenticationType", "FoundationaLLM:Vectorization:SemanticKernelTextEmbeddingService:AuthenticationType" }, { "DeploymentName", "FoundationaLLM:Vectorization:SemanticKernelTextEmbeddingService:DeploymentName" }, { "Endpoint", "FoundationaLLM:Vectorization:SemanticKernelTextEmbeddingService:Endpoint" } } }
        ];

        public static List<TextEmbeddingProfile> GetTextEmbeddingProfiles()
        {
            var items = new List<TextEmbeddingProfile>();
            items.AddRange(Items);
            return items;
        }
    }
}
