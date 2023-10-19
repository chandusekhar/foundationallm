﻿using Asp.Versioning;
using FoundationaLLM.AgentFactory.Core.Interfaces;
using FoundationaLLM.Common.Authentication;
using FoundationaLLM.Common.Models.Orchestration;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Context.Propagation;
using System.Diagnostics;

namespace FoundationaLLM.AgentFactory.API.Controllers
{
    [ApiVersion(1.0)]
    [ApiController]
    [APIKeyAuthentication]
    [Route("[controller]")]
    public class OrchestrationController : ControllerBase
    {
        private readonly IAgentFactoryService _agentFactoryService;
        private readonly ILogger<OrchestrationController> _logger;

        private static readonly ActivitySource Activity = new(nameof(OrchestrationController));
        private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

        public OrchestrationController(
            IAgentFactoryService agentFactoryService,
            ILogger<OrchestrationController> logger)
        {
            _agentFactoryService = agentFactoryService;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves a completion from an orchestration service
        /// </summary>
        /// <param name="completionRequest"></param>
        /// <returns></returns>
        [HttpPost("completion")]
        public async Task<CompletionResponse> GetCompletion([FromBody] CompletionRequest completionRequest)
        {
            return await _agentFactoryService.GetCompletion(completionRequest);
        }

        [HttpPost("summary")]
        public async Task<SummaryResponse> GetSummary([FromBody] SummaryRequest content)
        {
            return await _agentFactoryService.GetSummary(content);
        }
    }
}
