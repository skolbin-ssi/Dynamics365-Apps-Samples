﻿// Code generated by Microsoft (R) AutoRest Code Generator 0.9.7.0
// Changes may cause incorrect behavior and will be lost if the code is regenerated.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataExportSales.Models;
using Microsoft.Rest;

namespace DataExportSales
{
    public partial interface IProfiles
    {
        /// <param name='id'>
        /// Required. Profile Id
        /// </param>
        /// <param name='cancellationToken'>
        /// Cancellation token.
        /// </param>
        Task<HttpOperationResponse<object>> ActivateWithOperationResponseAsync(string id, CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        
        /// <param name='id'>
        /// Required. Profile Id
        /// </param>
        /// <param name='cancellationToken'>
        /// Cancellation token.
        /// </param>
        Task<HttpOperationResponse<object>> ActivateDataWithOperationResponseAsync(string id, CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        
        /// <param name='id'>
        /// Required. Profile Id
        /// </param>
        /// <param name='cancellationToken'>
        /// Cancellation token.
        /// </param>
        Task<HttpOperationResponse<object>> ActivateMetadataWithOperationResponseAsync(string id, CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        
        /// <param name='registration'>
        /// Required. Description of the new Profile to be created
        /// </param>
        /// <param name='cancellationToken'>
        /// Cancellation token.
        /// </param>
        Task<HttpOperationResponse<object>> CreateProfileWithOperationResponseAsync(ProfileDescriptionBase registration, CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        
        /// <param name='id'>
        /// Required. Profile Id
        /// </param>
        /// <param name='cancellationToken'>
        /// Cancellation token.
        /// </param>
        Task<HttpOperationResponse<object>> DeactivateWithOperationResponseAsync(string id, CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        
        /// <param name='id'>
        /// Required. Profile Id
        /// </param>
        /// <param name='cancellationToken'>
        /// Cancellation token.
        /// </param>
        Task<HttpOperationResponse<object>> DeleteProfileByIdWithOperationResponseAsync(string id, CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        
        /// <param name='id'>
        /// Required. Profile Id
        /// </param>
        /// <param name='status'>
        /// Optional. Include Profile Status
        /// </param>
        /// <param name='cancellationToken'>
        /// Cancellation token.
        /// </param>
        Task<HttpOperationResponse<object>> GetProfileByIdWithOperationResponseAsync(string id, bool? status = null, CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        
        /// <param name='id'>
        /// Required. Profile Id
        /// </param>
        /// <param name='cancellationToken'>
        /// Cancellation token.
        /// </param>
        Task<HttpOperationResponse<object>> GetProfileFailuresInfoByIdWithOperationResponseAsync(string id, CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        
        /// <param name='organizationId'>
        /// Required. Organization Id
        /// </param>
        /// <param name='status'>
        /// Optional. Include Profile Status
        /// </param>
        /// <param name='cancellationToken'>
        /// Cancellation token.
        /// </param>
        Task<HttpOperationResponse<object>> GetProfilesByOrganizationIdWithOperationResponseAsync(string organizationId, bool? status = null, CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        
        /// <param name='id'>
        /// Required. Profile Id
        /// </param>
        /// <param name='cancellationToken'>
        /// Cancellation token.
        /// </param>
        Task<HttpOperationResponse<object>> GetTestResultByIdWithOperationResponseAsync(string id, CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        
        /// <param name='id'>
        /// Required. Profile Id
        /// </param>
        /// <param name='updatedProfile'>
        /// Required. Description of updates to the Profile
        /// </param>
        /// <param name='cancellationToken'>
        /// Cancellation token.
        /// </param>
        Task<HttpOperationResponse<object>> UpdateProfileWithOperationResponseAsync(string id, ProfileDetailsDTO updatedProfile, CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        
        /// <param name='registration'>
        /// Required. Description of the new Profile to be created
        /// </param>
        /// <param name='cancellationToken'>
        /// Cancellation token.
        /// </param>
        Task<HttpOperationResponse<object>> ValidateBeforeProfileCreationWithOperationResponseAsync(ProfileDescriptionBase registration, CancellationToken cancellationToken = default(System.Threading.CancellationToken));
    }
}