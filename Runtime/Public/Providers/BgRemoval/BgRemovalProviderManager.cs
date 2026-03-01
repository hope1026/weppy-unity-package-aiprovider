using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Weppy.AIProvider
{
    /// <summary>
    /// Manages background removal providers and routes requests by priority.
    /// </summary>
    public partial class BgRemovalProviderManager
    {
        /// <summary>
        /// Gets the registered background removal provider entries.
        /// </summary>
        public IReadOnlyList<BgRemovalProviderEntry> ProviderEntries => _providerEntries;

        /// <summary>
        /// Registers a background removal provider entry.
        /// </summary>
        /// <param name="entry_">Provider entry to add.</param>
        /// <returns>True if the provider was added.</returns>
        public bool AddProvider(BgRemovalProviderEntry entry_)
        {
            return AddProviderInternal(entry_);
        }

        /// <summary>
        /// Registers a background removal provider with settings.
        /// </summary>
        /// <param name="providerType_">Provider type to add.</param>
        /// <param name="settings_">Provider settings.</param>
        /// <returns>True if the provider was added.</returns>
        public bool AddProvider(BgRemovalProviderType providerType_, BgRemovalProviderSettings settings_)
        {
            return AddProviderInternal(providerType_, settings_);
        }

        /// <summary>
        /// Removes a registered background removal provider.
        /// </summary>
        /// <param name="providerType_">Provider type to remove.</param>
        /// <returns>True if the provider was removed.</returns>
        public bool RemoveProvider(BgRemovalProviderType providerType_)
        {
            return RemoveProviderInternal(providerType_);
        }

        /// <summary>
        /// Enables or disables a provider entry.
        /// </summary>
        /// <param name="providerType_">Provider type to toggle.</param>
        /// <param name="enabled_">Whether the provider is enabled.</param>
        public void SetProviderEnabled(BgRemovalProviderType providerType_, bool enabled_)
        {
            SetProviderEnabledInternal(providerType_, enabled_);
        }

        /// <summary>
        /// Sets the default model for a provider.
        /// </summary>
        /// <param name="providerType_">Provider type to update.</param>
        /// <param name="model_">Default model ID.</param>
        public void SetProviderDefaultModel(BgRemovalProviderType providerType_, string model_)
        {
            SetProviderDefaultModelInternal(providerType_, model_);
        }

        /// <summary>
        /// Removes background using the highest-priority available provider.
        /// </summary>
        /// <param name="requestPayload_">Request payload to send.</param>
        /// <param name="cancellationToken_">Cancellation token.</param>
        /// <returns>Background removal response.</returns>
        public Task<BgRemovalResponse> RemoveBackgroundAsync(
            BgRemovalRequestPayload requestPayload_,
            CancellationToken cancellationToken_ = default)
        {
            if (requestPayload_ == null)
                return Task.FromResult(BgRemovalResponse.FromError("Invalid params"));

            BgRemovalProviderType providerType = GetHighestPriorityProvider();
            if (providerType == BgRemovalProviderType.NONE)
                return Task.FromResult(BgRemovalResponse.FromError("No enabled providers available"));

            BgRemovalRequestParams requestParams = new BgRemovalRequestParams
            {
                RequestPayload = requestPayload_,
                Providers = new List<BgRemovalRequestProviderTarget>
                {
                    new BgRemovalRequestProviderTarget
                    {
                        ProviderType = providerType,
                        Model = null,
                        Priority = 0
                    }
                }
            };

            return RemoveBackgroundInternalAsync(requestParams, cancellationToken_);
        }

        /// <summary>
        /// Removes background using specified provider targets.
        /// </summary>
        /// <param name="requestParams_">Request parameters including provider targets.</param>
        /// <param name="cancellationToken_">Cancellation token.</param>
        /// <returns>Background removal response.</returns>
        public Task<BgRemovalResponse> RemoveBackgroundWithProvidersAsync(
            BgRemovalRequestParams requestParams_,
            CancellationToken cancellationToken_ = default)
        {
            return RemoveBackgroundInternalAsync(requestParams_, cancellationToken_);
        }

        /// <summary>
        /// Removes background on all providers and returns their responses.
        /// </summary>
        /// <param name="requestParams_">Request parameters including provider targets.</param>
        /// <param name="onlyEnabled_">Whether to send only to enabled providers.</param>
        /// <param name="cancellationToken_">Cancellation token.</param>
        /// <returns>Responses keyed by provider type.</returns>
        public Task<Dictionary<BgRemovalProviderType, BgRemovalResponse>> RemoveBackgroundToAllProvidersAsync(
            BgRemovalRequestParams requestParams_,
            bool onlyEnabled_ = true,
            CancellationToken cancellationToken_ = default)
        {
            return RemoveBackgroundToAllProvidersInternalAsync(requestParams_, onlyEnabled_, cancellationToken_);
        }

        /// <summary>
        /// Checks whether a provider is available and enabled.
        /// </summary>
        /// <param name="providerType_">Provider type to check.</param>
        /// <returns>True if available.</returns>
        public bool IsProviderAvailable(BgRemovalProviderType providerType_)
        {
            return IsProviderAvailableInternal(providerType_);
        }

        /// <summary>
        /// Gets provider types that are enabled and configured.
        /// </summary>
        /// <returns>Available provider types.</returns>
        public List<BgRemovalProviderType> GetAvailableProviderTypes()
        {
            return GetAvailableProviderTypesInternal();
        }

        /// <summary>
        /// Gets the highest-priority provider type.
        /// </summary>
        /// <returns>Highest-priority provider type or NONE.</returns>
        public BgRemovalProviderType GetHighestPriorityProvider()
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
