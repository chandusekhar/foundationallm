﻿using FluentValidation;
using FoundationaLLM.Authorization.Models;
using FoundationaLLM.Common.Constants.Authorization;
using FoundationaLLM.Common.Constants.ResourceProviders;
using FoundationaLLM.Common.Exceptions;
using FoundationaLLM.Common.Extensions;
using FoundationaLLM.Common.Interfaces;
using FoundationaLLM.Common.Models.Authentication;
using FoundationaLLM.Common.Models.Authorization;
using FoundationaLLM.Common.Models.Configuration.Instance;
using FoundationaLLM.Common.Models.ResourceProviders;
using FoundationaLLM.Common.Services.ResourceProviders;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace FoundationaLLM.Authorization.ResourceProviders
{
    /// <summary>
    /// Implements the FoundationaLLM.Authorization resource provider.
    /// </summary>
    /// <param name="instanceOptions">The options providing the <see cref="InstanceSettings"/> with instance settings.</param>
    /// <param name="authorizationService">The <see cref="IAuthorizationService"/> providing authorization services.</param>
    /// <param name="resourceValidatorFactory">The <see cref="IResourceValidatorFactory"/> providing the factory to create resource validators.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> of the main dependency injection container.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> used to provide loggers for logging.</param>
    public class AuthorizationResourceProviderService(
        IOptions<InstanceSettings> instanceOptions,
        IAuthorizationService authorizationService,
        IResourceValidatorFactory resourceValidatorFactory,
        IServiceProvider serviceProvider,
        ILoggerFactory loggerFactory)
        : ResourceProviderServiceBase(
            instanceOptions.Value,
            authorizationService,
            null,
            null,
            resourceValidatorFactory,
            serviceProvider,
            loggerFactory.CreateLogger<AuthorizationResourceProviderService>(),
            [])
    {
        protected override Dictionary<string, ResourceTypeDescriptor> GetResourceTypes() =>
            AuthorizationResourceProviderMetadata.AllowedResourceTypes;

        /// <inheritdoc/>
        protected override string _name => ResourceProviderNames.FoundationaLLM_Authorization;

        /// <inheritdoc/>
        protected override async Task InitializeInternal() =>
            await Task.CompletedTask;

        #region Support for Management API

        /// <inheritdoc/>
        protected override async Task<object> GetResourcesAsync(ResourcePath resourcePath, UnifiedUserIdentity userIdentity) =>
            resourcePath.ResourceTypeInstances[0].ResourceType switch
            {
                AuthorizationResourceTypeNames.RoleAssignments => await LoadRoleAssignments(resourcePath.ResourceTypeInstances[0], userIdentity),
                AuthorizationResourceTypeNames.RoleDefinitions => LoadRoleDefinitions(resourcePath.ResourceTypeInstances[0], userIdentity),
                AuthorizationResourceTypeNames.AuthorizableActions => LoadAuthorizableActions(resourcePath.ResourceTypeInstances[0], userIdentity),
                _ => throw new ResourceProviderException($"The resource type {resourcePath.ResourceTypeInstances[0].ResourceType} is not supported by the {_name} resource provider.",
                    StatusCodes.Status400BadRequest)
            };

        #region Helpers for GetResourcesAsyncInternal

        private static List<RoleDefinition> LoadRoleDefinitions(ResourceTypeInstance instance, UnifiedUserIdentity userIdentity)
        {
            if (instance.ResourceId == null)
                return RoleDefinitions.All.Values.ToList();
            else
            {
                if (RoleDefinitions.All.TryGetValue(instance.ResourceId, out var roleDefinition))
                    return [roleDefinition];
                else
                    return [];
            }
        }

        private static List<AuthorizableAction> LoadAuthorizableActions(ResourceTypeInstance instance, UnifiedUserIdentity userIdentity)
        {
            if (instance.ResourceId == null)
                return [.. AuthorizableActions.Actions.Values];
            else
            {
                if (AuthorizableActions.Actions.TryGetValue(instance.ResourceId, out var authorizableAction))
                    return [authorizableAction];
                else
                    return [];
            }
        }

        private async Task<List<ResourceProviderGetResult<RoleAssignment>>> LoadRoleAssignments(ResourceTypeInstance instance, UnifiedUserIdentity userIdentity)
        {
            var roleAssignments = new List<RoleAssignment>();
            var allRoleAssignments = new List<RoleAssignment>();

            var roleAssignmentObjects = await _authorizationService.GetRoleAssignments(_instanceSettings.Id);
            foreach (var obj in roleAssignmentObjects)
                allRoleAssignments.Add(JsonSerializer.Deserialize<RoleAssignment>(obj.ToString()!)!);

            allRoleAssignments = allRoleAssignments.Where(r => !r.Deleted).ToList();

            if (instance.ResourceId == null)
                roleAssignments = allRoleAssignments;
            else
            {
                var roleAssignment = roleAssignments.Where(roleAssignment => roleAssignment.ObjectId == instance.ResourceId).SingleOrDefault();

                if (roleAssignment == null)
                    throw new ResourceProviderException($"Could not locate the {instance.ResourceId} role assignment resource.",
                        StatusCodes.Status404NotFound);
                else
                    roleAssignments = [roleAssignment];
            }

            return await _authorizationService.FilterResourcesByAuthorizableAction(
               _instanceSettings.Id, userIdentity, roleAssignments,
               AuthorizableActionNames.FoundationaLLM_Authorization_RoleAssignments_Read);
        }

        #endregion

        /// <inheritdoc/>
        protected override async Task<object> UpsertResourceAsync(ResourcePath resourcePath, string serializedResource, UnifiedUserIdentity userIdentity) =>
            resourcePath.ResourceTypeInstances[0].ResourceType switch
            {
                AuthorizationResourceTypeNames.RoleAssignments => await UpdateRoleAssignments(resourcePath, serializedResource, userIdentity),
                _ => throw new ResourceProviderException($"The resource type {resourcePath.ResourceTypeInstances[0].ResourceType} is not supported by the {_name} resource provider.",
                    StatusCodes.Status400BadRequest)
            };

        #region Helpers for UpsertResourceAsync

        private async Task<ResourceProviderUpsertResult> UpdateRoleAssignments(ResourcePath resourcePath, string serializedRoleAssignment, UnifiedUserIdentity userIdentity)
        {
            var roleAssignment = JsonSerializer.Deserialize<RoleAssignment>(serializedRoleAssignment)
                ?? throw new ResourceProviderException("The object definition is invalid.",
                    StatusCodes.Status400BadRequest);

            if (resourcePath.ResourceTypeInstances[0].ResourceId != roleAssignment.Name)
                throw new ResourceProviderException("The resource path does not match the object definition (name mismatch).",
                    StatusCodes.Status400BadRequest);

            roleAssignment.ObjectId = resourcePath.GetObjectId(_instanceSettings.Id, _name);

            var roleAssignmentValidator = _resourceValidatorFactory.GetValidator<RoleAssignment>()!;
            var context = new ValidationContext<object>(roleAssignment);
            var validationResult = await roleAssignmentValidator.ValidateAsync(context);
            if (!validationResult.IsValid)
            {
                throw new ResourceProviderException($"Validation failed: {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}",
                    StatusCodes.Status400BadRequest);
            }

            var roleAssignmentResult = await _authorizationService.ProcessRoleAssignmentRequest(
                _instanceSettings.Id,
                new RoleAssignmentRequest()
                {
                    Name = roleAssignment.Name,
                    Description = roleAssignment.Description,
                    ObjectId = roleAssignment.ObjectId,
                    PrincipalId = roleAssignment.PrincipalId,
                    PrincipalType = roleAssignment.PrincipalType,
                    RoleDefinitionId = roleAssignment.RoleDefinitionId,
                    Scope = roleAssignment.Scope
                });

            if (roleAssignmentResult.Success)
                return new ResourceProviderUpsertResult
                {
                    ObjectId = roleAssignment.ObjectId
                };

            throw new ResourceProviderException("The role assignment failed.");
        }

        #endregion

        /// <inheritdoc/>
        protected override async Task DeleteResourceAsync(ResourcePath resourcePath, UnifiedUserIdentity userIdentity)
        {
            switch (resourcePath.ResourceTypeInstances.Last().ResourceType)
            {
                case AuthorizationResourceTypeNames.RoleAssignments:
                    await _authorizationService.RevokeRole(_instanceSettings.Id, resourcePath.ResourceTypeInstances.Last().ResourceId!);
                    break;
                default:
                    throw new ResourceProviderException($"The resource type {resourcePath.ResourceTypeInstances.Last().ResourceType} is not supported by the {_name} resource provider.",
                    StatusCodes.Status400BadRequest);
            };
        }

        #endregion
    }
}
