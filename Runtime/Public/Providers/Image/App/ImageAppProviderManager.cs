using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Weppy.AIProvider
{
    /// <summary>
    /// Manages app-based image generation providers and routes requests by priority.
    /// </summary>
    public partial class ImageAppProviderManager
    {
        /// <summary>
        /// Gets the registered app provider entries.
        /// </summary>
        public IReadOnlyList<ImageAppProviderEntry> ProviderEntries => _providerEntries;

        /// <summary>
        /// Registers an app provider entry.
        /// </summary>
        /// <param name="entry_">Provider entry to add.</param>
        /// <returns>True if the provider was added.</returns>
        public bool AddProvider(ImageAppProviderEntry entry_)
        {
            return AddProviderInternal(entry_);
        }

        /// <summary>
        /// Registers an app provider with settings.
        /// </summary>
        /// <param name="providerType_">Provider type to add.</param>
        /// <param name="settings_">Provider settings.</param>
        /// <returns>True if the provider was added.</returns>
        public bool AddProvider(ImageAppProviderType providerType_, ImageAppProviderSettings settings_)
        {
            return AddProviderInternal(providerType_, settings_);
        }

        /// <summary>
        /// Removes a registered app provider.
        /// </summary>
        /// <param name="providerType_">Provider type to remove.</param>
        /// <returns>True if the provider was removed.</returns>
        public bool RemoveProvider(ImageAppProviderType providerType_)
        {
            return RemoveProviderInternal(providerType_);
        }

        /// <summary>
        /// Enables or disables a provider entry.
        /// </summary>
        /// <param name="providerType_">Provider type to toggle.</param>
        /// <param name="enabled_">Whether the provider is enabled.</param>
        public void SetProviderEnabled(ImageAppProviderType providerType_, bool enabled_)
        {
            SetProviderEnabledInternal(providerType_, enabled_);
        }

        /// <summary>
        /// Sets the default model for a provider.
        /// </summary>
        /// <param name="providerType_">Provider type to update.</param>
        /// <param name="model_">Default model ID.</param>
        public void SetProviderDefaultModel(ImageAppProviderType providerType_, string model_)
        {
            SetProviderDefaultModelInternal(providerType_, model_);
        }

        /// <summary>
        /// Generates an image using the highest-priority available app provider.
        /// </summary>
        /// <param name="requestPayload_">Request payload to send.</param>
        /// <param name="cancellationToken_">Cancellation token.</param>
        /// <returns>Image response.</returns>
        public Task<ImageResponse> GenerateImageAsync(
            ImageRequestPayload requestPayload_,
            CancellationToken cancellationToken_ = default)
        {
            if (requestPayload_ == null)
                return Task.FromResult(ImageResponse.FromError("Invalid params"));

            ImageAppProviderType providerType = GetHighestPriorityProvider();
            if (providerType == ImageAppProviderType.NONE)
                return Task.FromResult(ImageResponse.FromError("No enabled providers available"));

            ImageAppProviderEntry entry = _providerEntries.FirstOrDefault(e => e.ProviderType == providerType);
            ImageAppRequestParams requestParams = new ImageAppRequestParams
            {
                RequestPayload = requestPayload_,
                Providers = new List<ImageAppRequestProviderTarget>
                {
                    new ImageAppRequestProviderTarget
                    {
                        ProviderType = providerType,
                        Model = entry?.Settings?.DefaultModel,
                        Priority = 0
                    }
                }
            };

            return GenerateImageInternalAsync(requestParams, cancellationToken_);
        }

        /// <summary>
        /// Generates an image using specified app provider targets.
        /// </summary>
        /// <param name="requestParams_">Request parameters including provider targets.</param>
        /// <param name="cancellationToken_">Cancellation token.</param>
        /// <returns>Image response.</returns>
        public Task<ImageResponse> GenerateImageWithProvidersAsync(
            ImageAppRequestParams requestParams_,
            CancellationToken cancellationToken_ = default)
        {
            return GenerateImageInternalAsync(requestParams_, cancellationToken_);
        }

        /// <summary>
        /// Generates images on all app providers and returns their responses.
        /// </summary>
        /// <param name="requestParams_">Request parameters including provider targets.</param>
        /// <param name="onlyEnabled_">Whether to send only to enabled providers.</param>
        /// <param name="cancellationToken_">Cancellation token.</param>
        /// <returns>Responses keyed by provider type.</returns>
        public Task<Dictionary<ImageAppProviderType, ImageResponse>> GenerateImageToAllProvidersAsync(
            ImageAppRequestParams requestParams_,
            bool onlyEnabled_ = true,
            CancellationToken cancellationToken_ = default)
        {
            return GenerateImageToAllProvidersInternalAsync(requestParams_, onlyEnabled_, cancellationToken_);
        }

        /// <summary>
        /// Generates images on all app providers with a per-provider callback.
        /// </summary>
        /// <param name="requestParams_">Request parameters including provider targets.</param>
        /// <param name="onProviderCompleted_">Callback invoked per provider.</param>
        /// <param name="cancellationToken_">Cancellation token.</param>
        public Task GenerateImageToAllProvidersAsync(
            ImageAppRequestParams requestParams_,
            Action<ImageAppProviderType, ImageResponse> onProviderCompleted_,
            CancellationToken cancellationToken_ = default)
        {
            return GenerateImageToAllProvidersInternalAsync(requestParams_, onProviderCompleted_, cancellationToken_);
        }

        /// <summary>
        /// Checks whether a provider is available and enabled.
        /// </summary>
        /// <param name="providerType_">Provider type to check.</param>
        /// <returns>True if available.</returns>
        public bool IsProviderAvailable(ImageAppProviderType providerType_)
        {
            return IsProviderAvailableInternal(providerType_);
        }

        /// <summary>
        /// Gets provider types that are enabled and configured.
        /// </summary>
        /// <returns>Available provider types.</returns>
        public List<ImageAppProviderType> GetAvailableProviderTypes()
        {
            return GetAvailableProviderTypesInternal();
        }

        /// <summary>
        /// Gets the highest-priority provider type.
        /// </summary>
        /// <returns>Highest-priority provider type or NONE.</returns>
        public ImageAppProviderType GetHighestPriorityProvider()
        {
            return GetHighestPriorityProviderInternal();
        }

        /// <summary>
        /// Releases provider resources.
        /// </summary>
        public void Dispose()
        {
            DisposeInternal();
        }
    }
}
