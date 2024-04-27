﻿using FoundationaLLM.Common.Exceptions;
using FoundationaLLM.Common.Interfaces;
using FoundationaLLM.Common.Models.ResourceProviders.Vectorization;
using FoundationaLLM.Vectorization.Interfaces;
using FoundationaLLM.Vectorization.Services;

namespace FoundationaLLM.Vectorization.Extensions
{
    public static class VectorizationStateServiceExtensions
    {
        /// <summary>
        /// Retrieves the state of the specified pipeline execution by polling the status of the vectorization requests created by the pipeline execution.
        /// </summary>
        /// <param name="stateService">The state service used to pull the existing pipeline state.</param>
        /// <param name="vectorizationResourceProvider">The vectorization resource provider used to poll vectorization request statuses.</param>
        /// <param name="pipelineName">The name of the pipeline being executed.</param>
        /// <param name="pipelineExecutionId">The unique identifier of the pipeline execution.</param>
        /// <returns>The current pipeline processing state.</returns>
        /// <exception cref="VectorizationException"></exception>
        public static async Task<VectorizationProcessingState> GetPipelineExecutionProcessingState(
            this IVectorizationStateService stateService,
            IResourceProviderService vectorizationResourceProvider,
            string pipelineName,
            string pipelineExecutionId)
        {
            var pipelineProcessingState = VectorizationProcessingState.New;
            var unifiedIdentity = new VectorizationServiceUnifiedUserIdentity();

            // check if the pipeline execution state exists
            var pipelineState = await stateService.ReadPipelineState(pipelineName, pipelineExecutionId);
            if (pipelineState == null)
            {
                throw new VectorizationException($"Pipeline state not found for pipeline {pipelineName} and execution id {pipelineExecutionId}");
            }

            // pipelines are put into InProgress upon creation, so if the state is populated it means it has a final state.
            if(pipelineState.ProcessingState != VectorizationProcessingState.InProgress)
            {
                return pipelineState.ProcessingState;
            }

            // calcuate the pipeline state based on its associated vectorization requests.
            var requestProcessingStates = new List<VectorizationProcessingState>();
            foreach (var vectorizationRequestObectId in pipelineState.VectorizationRequestObjectIds)
            {
                var vectorizationRequest = await vectorizationResourceProvider.HandleGetAsync(
                                       vectorizationRequestObectId,
                                       unifiedIdentity) as VectorizationRequest;
                if (vectorizationRequest == null)
                {
                    throw new VectorizationException($"Vectorization request not found for object id {vectorizationRequestObectId}");
                }

                requestProcessingStates.Add(vectorizationRequest.ProcessingState);
            }

            if (requestProcessingStates.All(s => s == VectorizationProcessingState.Completed))
            {
                pipelineProcessingState = VectorizationProcessingState.Completed;
            }
            else if (requestProcessingStates.Any(s => s == VectorizationProcessingState.InProgress))
            {
                pipelineProcessingState = VectorizationProcessingState.InProgress;
            }
            else if (requestProcessingStates.Any(s => s == VectorizationProcessingState.Failed))
            {
                // if no requests are in-progress and there exists a failed request, the pipeline is considered failed
                pipelineProcessingState = VectorizationProcessingState.Failed;
            }

            return pipelineProcessingState;
        }
    }
}
