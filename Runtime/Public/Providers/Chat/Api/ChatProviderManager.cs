using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Weppy.AIProvider.Chat
{
    /// <summary>
    /// Manages chat providers and routes requests based on priority and availability.
    /// </summary>
    public partial class ChatProviderManager
    {
        /// <summary>
        /// Gets the registered chat provider entries.
        /// </summary>
        public IReadOnlyList<ChatProviderEntry> ProviderEntries => _providerEntries;

        /// <summary>
        /// Registers a chat provider entry.
        /// </summary>
        /// <param name="entry_">Provider entry to add.</param>
        /// <returns>True if the provider was added.</returns>
        public bool AddProvider(ChatProviderEntry entry_)
        {
            return AddProviderInternal(entry_);
        }

        /// <summary>
        /// Registers a chat provider with settings.
        /// </summary>
        /// <param name="providerType_">Provider type to add.</param>
        /// <param name="settings_">Provider settings.</param>
        /// <returns>True if the provider was added.</returns>
        public bool AddProvider(ChatProviderType providerType_, ChatProviderSettings settings_)
        {
            return AddProviderInternal(providerType_, settings_);
        }

        /// <summary>
        /// Removes a registered chat provider.
        /// </summary>
        /// <param name="providerType_">Provider type to remove.</param>
        /// <returns>True if the provider was removed.</returns>
        public bool RemoveProvider(ChatProviderType providerType_)
        {
            return RemoveProviderInternal(providerType_);
        }

        /// <summary>
        /// Enables or disables a provider entry.
        /// </summary>
        /// <param name="providerType_">Provider type to toggle.</param>
        /// <param name="enabled_">Whether the provider is enabled.</param>
        public void SetProviderEnabled(ChatProviderType providerType_, bool enabled_)
        {
            SetProviderEnabledInternal(providerType_, enabled_);
        }

        /// <summary>
        /// Sets the default model for a provider.
        /// </summary>
        /// <param name="providerType_">Provider type to update.</param>
        /// <param name="model_">Default model ID.</param>
        public void SetProviderDefaultModel(ChatProviderType providerType_, string model_)
        {
            SetProviderDefaultModelInternal(providerType_, model_);
        }

        /// <summary>
        /// Sends a chat request using the highest-priority available provider.
        /// </summary>
        /// <param name="requestPayload_">Request payload to send.</param>
        /// <param name="cancellationToken_">Cancellation token.</param>
        /// <returns>Chat response.</returns>
        public Task<ChatResponse> SendMessageAsync(ChatRequestPayload requestPayload_, CancellationToken cancellationToken_ = default)
        {
            if (requestPayload_ == null)
                return Task.FromResult(ChatResponse.FromError("Invalid params"));

            ChatProviderType providerType = GetHighestPriorityProvider();
            if (providerType == ChatProviderType.NONE)
                return Task.FromResult(ChatResponse.FromError("No enabled providers available"));

            ChatProviderEntry entry = _providerEntries.FirstOrDefault(e => e.ProviderType == providerType);
            ChatRequestParams requestParams = new ChatRequestParams
            {
                RequestPayload = requestPayload_,
                Providers = new List<ChatRequestProviderTarget>
                {
                    new ChatRequestProviderTarget
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
        /// Sends a chat request to a specified set of providers.
        /// </summary>
        /// <param name="requestParams_">Request parameters including provider targets.</param>
        /// <param name="cancellationToken_">Cancellation token.</param>
        /// <returns>Chat response.</returns>
        public Task<ChatResponse> SendMessageWithProvidersAsync(
            ChatRequestParams requestParams_,
            CancellationToken cancellationToken_ = default)
        {
            return SendMessageInternalAsync(requestParams_, cancellationToken_);
        }

        /// <summary>
        /// Sends a chat request to all providers and returns their responses.
        /// </summary>
        /// <param name="requestParams_">Request parameters including provider targets.</param>
        /// <param name="onlyEnabled_">Whether to send only to enabled providers.</param>
        /// <param name="cancellationToken_">Cancellation token.</param>
        /// <returns>Responses keyed by provider type.</returns>
        public Task<Dictionary<ChatProviderType, ChatResponse>> SendMessageToAllProvidersAsync(
            ChatRequestParams requestParams_,
            bool onlyEnabled_ = true,
            CancellationToken cancellationToken_ = default)
        {
            return SendMessageToAllProvidersInternalAsync(requestParams_, onlyEnabled_, cancellationToken_);
        }

        /// <summary>
        /// Sends a chat request to all providers with a per-provider callback.
        /// </summary>
        /// <param name="requestParams_">Request parameters including provider targets.</param>
        /// <param name="onProviderCompleted_">Callback invoked per provider.</param>
        /// <param name="cancellationToken_">Cancellation token.</param>
        public Task SendMessageToAllProvidersAsync(
            ChatRequestParams requestParams_,
            System.Action<ChatProviderType, ChatResponse> onProviderCompleted_,
            CancellationToken cancellationToken_ = default)
        {
            return SendMessageToAllProvidersInternalAsync(requestParams_, onProviderCompleted_, cancellationToken_);
        }

        /// <summary>
        /// Streams chat responses using the highest-priority available provider.
        /// </summary>
        /// <param name="requestPayload_">Request payload to send.</param>
        /// <param name="onChunkReceived_">Callback invoked for each received text chunk.</param>
        /// <param name="cancellationToken_">Cancellation token.</param>
        public Task StreamMessageAsync(
            ChatRequestPayload requestPayload_,
            System.Func<string, System.Threading.Tasks.Task> onChunkReceived_,
            CancellationToken cancellationToken_ = default)
        {
            if (requestPayload_ == null)
                return Task.CompletedTask;

            ChatProviderType providerType = GetHighestPriorityProvider();
            if (providerType == ChatProviderType.NONE)
                return Task.CompletedTask;

            ChatProviderEntry entry = _providerEntries.FirstOrDefault(e => e.ProviderType == providerType);
            ChatRequestParams requestParams = new ChatRequestParams
            {
                RequestPayload = requestPayload_,
                Providers = new List<ChatRequestProviderTarget>
                {
                    new ChatRequestProviderTarget
                    {
                        ProviderType = providerType,
                        Model = entry?.Settings?.DefaultModel,
                        Priority = 0
                    }
                }
            };

            return StreamMessageInternalAsync(requestParams, onChunkReceived_, cancellationToken_);
        }

        /// <summary>
        /// Streams chat responses using specified provider targets.
        /// </summary>
        /// <param name="requestParams_">Request parameters including provider targets.</param>
        /// <param name="onChunkReceived_">Callback invoked for each received text chunk.</param>
        /// <param name="cancellationToken_">Cancellation token.</param>
        public Task StreamMessageWithProvidersAsync(
            ChatRequestParams requestParams_,
            System.Func<string, System.Threading.Tasks.Task> onChunkReceived_,
            CancellationToken cancellationToken_ = default)
        {
            return StreamMessageInternalAsync(requestParams_, onChunkReceived_, cancellationToken_);
        }

        /// <summary>
        /// Checks whether a provider is available and enabled.
        /// </summary>
        /// <param name="providerType_">Provider type to check.</param>
        /// <returns>True if available.</returns>
        public bool IsProviderAvailable(ChatProviderType providerType_)
        {
            return IsProviderAvailableInternal(providerType_);
        }

        /// <summary>
        /// Gets provider types that are enabled and configured.
        /// </summary>
        /// <returns>Available provider types.</returns>
        public List<ChatProviderType> GetAvailableProviderTypes()
        {
            return GetAvailableProviderTypesInternal();
        }

        /// <summary>
        /// Gets the highest-priority provider type.
        /// </summary>
        /// <returns>Highest-priority provider type or NONE.</returns>
        public ChatProviderType GetHighestPriorityProvider()
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
