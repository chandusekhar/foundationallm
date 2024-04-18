﻿using FoundationaLLM.Common.Constants.ResourceProviders;
using FoundationaLLM.Common.Exceptions;
using FoundationaLLM.Common.Interfaces;
using FoundationaLLM.Common.Models.Authentication;
using FoundationaLLM.Common.Models.ResourceProviders.Vectorization;
using FoundationaLLM.Common.Settings;
using FoundationaLLM.Vectorization.Interfaces;
using FoundationaLLM.Vectorization.Models;
using Json.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FoundationaLLM.Vectorization.Handlers
{
    /// <summary>
    /// Implements basic vectorization step handler functionality.
    /// </summary>
    /// <param name="stepId">The identifier of the vectorization step.</param>
    /// <param name="messageId">The identifier of underlying message retrieved from the request source.</param>
    /// <param name="parameters">The dictionary of named parameters used to configure the handler.</param>
    /// <param name="stepsConfiguration">The app configuration section containing the configuration for vectorization pipeline steps.</param>
    /// <param name="stateService">The <see cref="IVectorizationStateService"/> that manages vectorization state.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> implemented by the dependency injection container.</param>    
    /// <param name="loggerFactory">The logger factory used to create loggers for logging.</param>
    public class VectorizationStepHandlerBase(
        string stepId,
        string messageId,
        Dictionary<string, string> parameters,
        IConfigurationSection? stepsConfiguration,
        IVectorizationStateService stateService,
        IServiceProvider serviceProvider,        
        ILoggerFactory loggerFactory) : IVectorizationStepHandler
    {
        /// <summary>
        /// The identifier of the vectorization step.
        /// </summary>
        protected readonly string _stepId = stepId;
        /// <summary>
        /// The identifier of underlying message retrieved from the request source.
        /// </summary>
        protected readonly string _messageId = messageId;
        /// <summary>
        /// The dictionary of named parameters used to configure the handler.
        /// </summary>
        protected readonly Dictionary<string, string> _parameters = parameters;
        /// <summary>
        /// The app configuration section containing the configuration for vectorization pipeline steps.
        /// </summary>
        protected readonly IConfigurationSection? _stepsConfiguration = stepsConfiguration;
        /// <summary>
        /// The vectorization state service.
        /// </summary>
        protected readonly IVectorizationStateService _stateService = stateService;
        /// <summary>
        /// The service provider implemented by the dependency injection container.
        /// </summary>
        protected readonly IServiceProvider _serviceProvider = serviceProvider;
       
        /// <summary>
        /// The logger used for logging.
        /// </summary>
        protected readonly ILogger<VectorizationStepHandlerBase> _logger =
            loggerFactory.CreateLogger<VectorizationStepHandlerBase>();

        /// <inheritdoc/>
        public string StepId => _stepId;

        /// <inheritdoc/>
        public async Task<bool> Invoke(VectorizationRequest request, VectorizationState state, CancellationToken cancellationToken)
        {
            var success = true;           
            try
            {
                state.LogHandlerStart(this, request.Id!, _messageId);
                _logger.LogInformation("Starting handler [{HandlerId}] for request {RequestId} (message id {MessageId}).", _stepId, request.Id, _messageId);

                var stepConfiguration = default(IConfigurationSection);

                if (_parameters.TryGetValue("configuration_section", out string? configurationSection))
                {
                    stepConfiguration = _stepsConfiguration!.GetSection(configurationSection);

                    if (stepConfiguration == null
                        || (
                            stepConfiguration.Value == null
                            && !stepConfiguration.GetChildren().Any()
                            ))
                    {
                        var errorMessage = $"The configuration section {configurationSection} expected by the {_stepId} handler is not available.";                        
                        _logger.LogError(errorMessage);
                        throw new VectorizationException(errorMessage);
                    }
                }

                ValidateRequest(request);
                success = await ProcessRequest(request, state, stepConfiguration, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                success = false;
                //update the request execution state with the error message.
                state.LogHandlerError(this, request.Id!, _messageId, ex);
                //update the request state with the error message.
                request.ErrorMessages.Add($"Error in executing {_stepId} step handler for request {request.Id} (message id {_messageId}): {ex.Message}.");                
                await UpdateResourceState(request.ObjectId!, request);
                _logger.LogError(ex, "Error in executing [{HandlerId}] step handler for request {VectorizationRequestId} (message id {MessageId}).", _stepId, request.Id, _messageId);
            }
            finally
            {
                //update execution state
                state.UpdateRequest(request);
                state.LogHandlerEnd(this, request.Id!, _messageId);
                _logger.LogInformation("Finished handler [{HandlerId}] for request {RequestId} (message id {MessageId}).", _stepId, request.Id, _messageId);
                //update the request state
                await UpdateResourceState(request.ObjectId!, request);
            }
            return success;
        }

        private void ValidateRequest(VectorizationRequest request)
        {
            if (request[_stepId] == null)
                throw new VectorizationException($"The request with id {request.Id} does not contain a step with id {_stepId}.");
        }

        /// <summary>
        /// Processes a vectorization request.
        /// The vectorization state will be updated with the result(s) of the processing.
        /// </summary>
        /// <param name="request">The <see cref="VectorizationRequest"/> to be processed.</param>
        /// <param name="state">The <see cref="VectorizationState"/> associated with the vectorization request.</param>
        /// <param name="stepConfiguration">The <see cref="IConfigurationSection"/> providing the configuration required by the step.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that signals stopping the processing.</param>
        /// <returns></returns>
        protected virtual async Task<bool> ProcessRequest(
            VectorizationRequest request,
            VectorizationState state,
            IConfigurationSection? stepConfiguration,
            CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            return false;
        }

        /// <summary>
        /// Updates the state of a vectorization resource.
        /// </summary>
        /// <exception cref="VectorizationException"></exception>
        private async Task UpdateResourceState(string resourcePath, object request)
        {
            var vectorizationResourceProviderService = _serviceProvider.GetService<IResourceProviderService>();
            if (vectorizationResourceProviderService == null)
                throw new VectorizationException($"The resource provider {ResourceProviderNames.FoundationaLLM_Vectorization} was not loaded.");
            // await vectorizationResourceProviderService.UpsertResourceAsync(request.ObjectId!, request);

            // use HandlePostAsync to go through Authorization layer using the managed identity of the vectorization API
            var jsonSerializerOptions = CommonJsonSerializerOptions.GetJsonSerializerOptions();
            jsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            var requestBody = JsonSerializer.Serialize(request, jsonSerializerOptions);
            var unifiedIdentity = new UnifiedUserIdentity
            {
                Name = "VectorizationAPI",
                UserId = "VectorizationAPI",
                Username = "VectorizationAPI"
            };                      
            await vectorizationResourceProviderService.HandlePostAsync(resourcePath, requestBody, unifiedIdentity);
        }
    }
}
