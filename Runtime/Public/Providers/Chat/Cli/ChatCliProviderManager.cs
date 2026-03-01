using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Weppy.AIProvider
{
    /// <summary>
    /// Manages CLI-based chat providers and routes requests by priority.
    /// </summary>
    public partial class ChatCliProviderManager
    {
        /// <summary>
        /// Gets the registered CLI chat provider entries.
        /// </summary>
        public IReadOnlyList<ChatCliProviderEntry> ProviderEntries => _providerEntries;

        /// <summary>
        /// Registers a CLI chat provider entry.
        /// </summary>
        /// <param name="entry_">Provider entry to add.</param>
        /// <returns>True if the provider was added.</returns>
        public bool AddProvider(ChatCliProviderEntry entry_)
        {
            return AddProviderInternal(entry_);
        }

        /// <summary>
        /// Registers a CLI chat provider with settings.
        /// </summary>
        /// <param name="providerType_">Provider type to add.</param>
        /// <param name="settings_">Provider settings.</param>
        /// <returns>True if the provider was added.</returns>
        public bool AddProvider(ChatCliProviderType providerType_, ChatCliProviderSettings settings_)
        {
            return AddProviderInternal(providerType_, settings_);
        }

        /// <summary>
        /// Removes a registered CLI chat provider.
        /// </summary>
        /// <param name="providerType_">Provider type to remove.</param>
        /// <returns>True if the provider was removed.</returns>
        public bool RemoveProvider(ChatCliProviderType providerType_)
        {
            return RemoveProviderInternal(providerType_);
        }

        /// <summary>
        /// Enables or disables a provider entry.
        /// </summary>
        /// <param name="providerType_">Provider type to toggle.</param>
        /// <param name="enabled_">Whether the provider is enabled.</param>
        public void SetProviderEnabled(ChatCliProviderType providerType_, bool enabled_)
        {
            SetProviderEnabledInternal(providerType_, enabled_);
        }

        /// <summary>
        /// Sets the default model for a provider.
        /// </summary>
        /// <param name="providerType_">Provider type to update.</param>
        /// <param name="model_">Default model ID.</param>
        public void SetProviderDefaultModel(ChatCliProviderType providerType_, string model_)
        {
            SetProviderDefaultModelInternal(providerType_, model_);
        }

        /// <summary>
        /// Sends a chat request using the highest-priority available CLI provider.
        /// </summary>
        /// <param name="requestPayload_">Request payload to send.</param>
        /// <param name="cancellationToken_">Cancellation token.</param>
        /// <returns>Chat response.</returns>
        public Task<ChatCliResponse> SendMessageAsync(
            ChatCliRequestPayload requestPayload_,
            CancellationToken cancellationToken_ = default)
        {
            if (requestPayload_ == null)
                return Task.FromResult(ChatCliResponse.FromError("Invalid params"));

            ChatCliProviderType providerType = GetHighestPriorityProvider();
            if (providerType == ChatCliProviderType.NONE)
                return Task.FromResult(ChatCliResponse.FromError("No enabled providers available"));

            ChatCliProviderEntry entry = _providerEntries.FirstOrDefault(e => e.ProviderType == providerType);
            ChatCliRequestParams requestParams = new ChatCliRequestParams
            {
                RequestPayload = requestPayload_,
                Providers = new List<ChatCliRequestProviderTarget>
                {
                    new ChatCliRequestProviderTarget
                    {
                        ProviderType = providerType,
                        Model = entry?.Settings?.DefaultModel,
                        Priority = 0
                    }
                }
            };

            return SendMessageInternalAsync(requestParams, cancellationToken_);
        }

        /// <summary>
        /// Sends a chat request to a specified set of CLI providers.
        /// </summary>
        /// <param name="requestParams_">Request parameters including provider targets.</param>
        /// <param name="cancellationToken_">Cancellation token.</param>
        /// <returns>Chat response.</returns>
        public Task<ChatCliResponse> SendMessageWithProvidersAsync(
            ChatCliRequestParams requestParams_,
            CancellationToken cancellationToken_ = default)
        {
            return SendMessageInternalAsync(requestParams_, cancellationToken_);
        }

        /// <summary>
        /// Sends a chat request to all CLI providers and returns their responses.
        /// </summary>
        /// <param name="requestParams_">Request parameters including provider targets.</param>
        /// <param name="onlyEnabled_">Whether to send only to enabled providers.</param>
        /// <param name="cancellationToken_">Cancellation token.</param>
        /// <returns>Responses keyed by provider type.</returns>
        public Task<Dictionary<ChatCliProviderType, ChatCliResponse>> SendMessageToAllProvidersAsync(
            ChatCliRequestParams requestParams_,
            bool onlyEnabled_ = true,
            CancellationToken cancellationToken_ = default)
        {
            return SendMessageToAllProvidersInternalAsync(requestParams_, onlyEnabled_, cancellationToken_);
        }

        /// <summary>
        /// Sends a chat request to all CLI providers with a per-provider callback.
        /// </summary>
        /// <param name="requestParams_">Request parameters including provider targets.</param>
        /// <param name="onProviderCompleted_">Callback invoked per provider.</param>
        /// <param name="cancellationToken_">Cancellation token.</param>
        public Task SendMessageToAllProvidersAsync(
            ChatCliRequestParams requestParams_,
            System.Action<ChatCliProviderType, ChatCliResponse> onProviderCompleted_,
            CancellationToken cancellationToken_ = default)
        {
            return SendMessageToAllProvidersInternalAsync(requestParams_, onProviderCompleted_, cancellationToken_);
        }

        /// <summary>
        /// Sends a persistent chat request using the highest-priority available CLI provider.
        /// The CLI process stays alive between calls, preserving conversation context.
        /// </summary>
        /// <param name="requestPayload_">Request payload to send.</param>
        /// <param name="cancellationToken_">Cancellation token.</param>
        /// <returns>Chat response.</returns>
        public Task<ChatCliResponse> SendPersistentMessageAsync(
            ChatCliRequestPayload requestPayload_,
            CancellationToken cancellationToken_ = default)
        {
            if (requestPayload_ == null)
                return Task.FromResult(ChatCliResponse.FromError("Invalid params"));

            ChatCliProviderType providerType = GetHighestPriorityProvider();
            if (providerType == ChatCliProviderType.NONE)
                return Task.FromResult(ChatCliResponse.FromError("No enabled providers available"));

            ChatCliProviderEntry entry = _providerEntries.FirstOrDefault(e => e.ProviderType == providerType);
            ChatCliRequestParams requestParams = new ChatCliRequestParams
            {
                RequestPayload = requestPayload_,
                Providers = new List<ChatCliRequestProviderTarget>
                {
                    new ChatCliRequestProviderTarget
                    {
                        ProviderType = providerType,
                        Model = entry?.Settings?.DefaultModel,
                        Priority = 0
                    }
                }
            };

            return SendPersistentMessageInternalAsync(requestParams, cancellationToken_);
        }

        /// <summary>
        /// Sends a persistent chat request to a specified set of CLI providers.
        /// The CLI process stays alive between calls, preserving conversation context.
        /// </summary>
        /// <param name="requestParams_">Request parameters including provider targets.</param>
        /// <param name="cancellationToken_">Cancellation token.</param>
        /// <returns>Chat response.</returns>
        public Task<ChatCliResponse> SendPersistentMessageWithProvidersAsync(
            ChatCliRequestParams requestParams_,
            CancellationToken cancellationToken_ = default)
        {
            return SendPersistentMessageInternalAsync(requestParams_, cancellationToken_);
        }

        /// <summary>
        /// Resets the persistent session for a specific provider, killing the background process.
        /// </summary>
        /// <param name="providerType_">Provider type to reset.</param>
        public void ResetSession(ChatCliProviderType providerType_)
        {
            ResetSessionInternal(providerType_);
        }

        /// <summary>
        /// Resets all persistent sessions, killing all background processes.
        /// </summary>
        public void ResetAllSessions()
        {
            ResetAllSessionsInternal();
        }

        /// <summary>
        /// Checks whether a CLI provider is available and enabled.
        /// </summary>
        /// <param name="providerType_">Provider type to check.</param>
        /// <returns>True if available.</returns>
        public bool IsProviderAvailable(ChatCliProviderType providerType_)
        {
            return IsProviderAvailableInternal(providerType_);
        }

        /// <summary>
        /// Gets provider types that are enabled and configured.
        /// </summary>
        /// <returns>Available provider types.</returns>
        public List<ChatCliProviderType> GetAvailableProviderTypes()
        {
            return GetAvailableProviderTypesInternal();
        }

        /// <summary>
        /// Gets the highest-priority provider type.
        /// </summary>
        /// <returns>Highest-priority provider type or NONE.</returns>
        public ChatCliProviderType GetHighestPriorityProvider()
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
