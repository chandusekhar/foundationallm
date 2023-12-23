﻿using FoundationaLLM.Common.Models.Orchestration;
using FoundationaLLM.Gatekeeper.Core.Interfaces;

namespace FoundationaLLM.Gatekeeper.Core.Services
{
    /// <summary>
    /// Implements the <see cref="IGatekeeperService"/> interface.
    /// </summary>
    public class GatekeeperService : IGatekeeperService
    {
        private readonly IAgentFactoryAPIService _agentFactoryAPIService;
        private readonly IRefinementService _refinementService;
        private readonly IContentSafetyService _contentSafetyService;

        /// <summary>
        /// Constructor for the Gatekeeper service.
        /// </summary>
        /// <param name="agentFactoryAPIService">The Agent Factory API client.</param>
        /// <param name="refinementService">The user prompt Refinement service.</param>
        /// <param name="contentSafetyService">The user prompt Content Safety service.</param>
        public GatekeeperService(
            IAgentFactoryAPIService agentFactoryAPIService,
            IRefinementService refinementService,
            IContentSafetyService contentSafetyService)
        {
            _agentFactoryAPIService = agentFactoryAPIService;
            _refinementService = refinementService;
            _contentSafetyService = contentSafetyService;
        }

        /// <summary>
        /// Gets a completion from the Gatekeeper service.
        /// </summary>
        /// <param name="completionRequest">The completion request containing the user prompt and message history.</param>
        /// <returns>The completion response.</returns>
        public async Task<CompletionResponse> GetCompletion(CompletionRequest completionRequest)
        {
            //TODO: Call RefinementService to refine userPrompt
            //await _refinementService.RefineUserPrompt(completionRequest.Prompt);

            using var activity = Common.Logging.ActivitySources.StartActivity("GetCompletion.AnalyzeText", Common.Logging.ActivitySources.GatekeeperAPIActivitySource);

            var result = await _contentSafetyService.AnalyzeText(completionRequest.UserPrompt ?? string.Empty);

            activity?.Stop();
            
            if (result.Safe)
                return await _agentFactoryAPIService.GetCompletion(completionRequest);
            else
            {
                //log the result
                using var activity2 = Common.Logging.ActivitySources.StartActivity("GetCompletion.ContentSafety.NotSafe", Common.Logging.ActivitySources.GatekeeperAPIActivitySource);
                activity2.AddTag("ContentSafetyNotSafeReason", result.Reason);
            }

            return new CompletionResponse() { Completion = result.Reason };
        }

        /// <summary>
        /// Gets a summary from the Gatekeeper service.
        /// </summary>
        /// <param name="summaryRequest">The summarize request containing the user prompt.</param>
        /// <returns>The summary response.</returns>
        public async Task<SummaryResponse> GetSummary(SummaryRequest summaryRequest)
        {
            //TODO: Call RefinementService to refine userPrompt
            //await _refinementService.RefineUserPrompt(summaryRequest.Prompt);

            using var activity = Common.Logging.ActivitySources.StartActivity("GetSummary.AnalyzeText", Common.Logging.ActivitySources.GatekeeperAPIActivitySource);

            var result = await _contentSafetyService.AnalyzeText(summaryRequest.UserPrompt ?? string.Empty);

            if (result.Safe)
                return await _agentFactoryAPIService.GetSummary(summaryRequest);

            return new SummaryResponse() { Summary = result.Reason };
        }
    }
}
