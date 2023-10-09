﻿using FoundationaLLM.AgentFactory.Core.Interfaces;
using FoundationaLLM.AgentFactory.Core.Models.Messages;
using FoundationaLLM.AgentFactory.Interfaces;
using FoundationaLLM.AgentFactory.Models.ConfigurationOptions;
using FoundationaLLM.AgentFactory.Models.Orchestration;
using FoundationaLLM.AgentFactory.Services;
using FoundationaLLM.Common.Models.Orchestration;
using FoundationaLLM.Common.Settings;
using Microsoft.AspNetCore.Server.IIS.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net.Http;
using System.Runtime;
using System.Text;

namespace FoundationaLLM.AgentFactory.Core.Services;

public class AgentHubService : IAgentHubService
{
    readonly AgentHubSettings _settings;
    readonly ILogger<AgentHubService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    readonly JsonSerializerSettings _jsonSerializerSettings;

    public AgentHubService(
            IOptions<AgentHubSettings> options,
            ILogger<AgentHubService> logger,
            IHttpClientFactory httpClientFactory)
    {
        _settings = options.Value;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string> Status()
    {
        try
        {
            var client = _httpClientFactory.CreateClient(Common.Constants.HttpClients.AgentHubAPIClient);

            var responseMessage = await client.GetAsync("/status");

            if (responseMessage.IsSuccessStatusCode)
            {
                var responseContent = await responseMessage.Content.ReadAsStringAsync();
                return responseContent;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting agent hub status.");
            throw ex;
        }

        return null;
    }

    public async Task<List<AgentHubResponse>> ResolveRequest(string user_prompt, string user_context)
    {
        try
        {
            AgentHubMessage ahm = new AgentHubMessage();
            ahm.user_prompt = user_prompt;
            ahm.user_context = user_context;

            var client = _httpClientFactory.CreateClient(Common.Constants.HttpClients.AgentHubAPIClient);

            var responseMessage = await client.PostAsync("/resolve_request", new StringContent(
                    JsonConvert.SerializeObject(ahm),
                    Encoding.UTF8, "application/json"));

            if (responseMessage.IsSuccessStatusCode)
            {
                var responseContent = await responseMessage.Content.ReadAsStringAsync();
                var completionResponse = JsonConvert.DeserializeObject<List<AgentHubResponse>>(responseContent);

                return completionResponse;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error resolving request for Agent Hub.");
            throw ex;
        }

        return new List<AgentHubResponse>();
    }    
}
