using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Weppy.AIProvider
{
    public partial class ChatProviderManager : IDisposable
    {
        private readonly List<ChatProviderEntry> _providerEntries = new List<ChatProviderEntry>();
        private readonly Dictionary<ChatProviderType, ChatProviderAbstract> _providers = new Dictionary<ChatProviderType, ChatProviderAbstract>();
        private bool _disposed;

        public ChatProviderManager() { }

        private bool AddProviderInternal(ChatProviderEntry entry_)
        {
            if (entry_ == null)
                return false;

            if (entry_.Settings == null)
                return false;

            ChatProviderEntry existing = _providerEntries.FirstOrDefault(e => e.ProviderType == entry_.ProviderType);
            if (existing != null)
            {
                _providerEntries.Remove(existing);
                DisposeProvider(existing.ProviderType);
            }

            _providerEntries.Add(entry_);
            return true;
        }

        private bool AddProviderInternal(ChatProviderType providerType_, ChatProviderSettings settings_)
        {
            return AddProviderInternal(new ChatProviderEntry(providerType_, settings_));
        }

        private bool RemoveProviderInternal(ChatProviderType providerType_)
        {
            ChatProviderEntry entry = _providerEntries.FirstOrDefault(e => e.ProviderType == providerType_);
            if (entry == null)
                return false;

            _providerEntries.Remove(entry);
            DisposeProvider(providerType_);
            return true;
        }

        private void SetProviderEnabledInternal(ChatProviderType providerType_, bool enabled_)
        {
            ChatProviderEntry entry = _providerEntries.FirstOrDefault(e => e.ProviderType == providerType_);
            if (entry != null)
            {
                entry.IsEnabled = enabled_;
            }
        }

        private void SetProviderDefaultModelInternal(ChatProviderType providerType_, string model_)
        {
            ChatProviderEntry entry = _providerEntries.FirstOrDefault(e => e.ProviderType == providerType_);
            if (entry != null && entry.Settings != null)
            {
                entry.Settings.DefaultModel = model_;
                DisposeProvider(providerType_);
            }
        }

        private async Task<ChatResponse> SendMessageInternalAsync(ChatRequestParams requestParams_, CancellationToken cancellationToken_)
        {
            if (requestParams_ == null || requestParams_.RequestPayload == null)
                return ChatResponse.FromError("Invalid params");

            List<ChatRequestProviderTarget> targets = GetOrderedTargets(requestParams_.Providers, onlyEnabled_: true);

            if (targets.Count == 0)
                return ChatResponse.FromError("No enabled providers available");

            Exception lastException = null;
            string lastError = null;

            foreach (ChatRequestProviderTarget target in targets)
            {
                ChatProviderEntry entry = _providerEntries.FirstOrDefault(e => e.ProviderType == target.ProviderType);
                if (entry == null || !IsEntryAvailable(entry, onlyEnabled_: true))
                    continue;

                try
                {
                    ChatProviderAbstract provider = GetOrCreateProvider(entry);
                    if (provider == null)
                        continue;

                    string model = ResolveModel(target.Model, requestParams_.RequestPayload.Model, entry.Settings?.DefaultModel);

                    AIProviderLogger.Log($"[Chat] Requesting {entry.ProviderType} (Model: {model})...");
                    AIProviderLogger.LogVerbose($"[Chat] Request Details: {UnityEngine.JsonUtility.ToJson(requestParams_.RequestPayload)}");

                    ChatResponse response = await provider.SendMessageAsync(requestParams_.RequestPayload, model, cancellationToken_);

                    AIProviderLogger.Log($"[Chat] Response from {entry.ProviderType}: Success={response.IsSuccess}, Error={response.ErrorMessage}");

                    if (response.IsSuccess)
                    {
                        response.ProviderType = entry.ProviderType;
                        return response;
                    }

                    lastError = response.ErrorMessage;
                }
                catch (OperationCanceledException)
                {
                    return ChatResponse.FromError("Request was cancelled");
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    lastError = ex.Message;
                }
            }

            return ChatResponse.FromError(lastError ?? lastException?.Message ?? "All providers failed");
        }

        private async Task<Dictionary<ChatProviderType, ChatResponse>> SendMessageToAllProvidersInternalAsync(ChatRequestParams requestParams_, bool onlyEnabled_,
                                                                                                              CancellationToken cancellationToken_)
        {
            Dictionary<ChatProviderType, ChatResponse> results = new Dictionary<ChatProviderType, ChatResponse>();
            if (requestParams_ == null || requestParams_.RequestPayload == null)
                return results;

            List<ChatRequestProviderTarget> targets = GetOrderedTargets(requestParams_.Providers, onlyEnabled_);

            List<Task<KeyValuePair<ChatProviderType, ChatResponse>>> tasks = new List<Task<KeyValuePair<ChatProviderType, ChatResponse>>>();

            foreach (ChatRequestProviderTarget target in targets)
            {
                ChatProviderEntry entry = _providerEntries.FirstOrDefault(e => e.ProviderType == target.ProviderType);
                if (entry == null || !IsEntryAvailable(entry, onlyEnabled_))
                    continue;

                string model = ResolveModel(target.Model, requestParams_.RequestPayload.Model, entry.Settings?.DefaultModel);
                tasks.Add(SendToProviderInternalAsync(entry, requestParams_.RequestPayload, model, cancellationToken_));
            }

            KeyValuePair<ChatProviderType, ChatResponse>[] responses = await Task.WhenAll(tasks);

            foreach (KeyValuePair<ChatProviderType, ChatResponse> kvp in responses)
            {
                results[kvp.Key] = kvp.Value;
            }

            return results;
        }

        private async Task SendMessageToAllProvidersInternalAsync(
            ChatRequestParams requestParams_,
            System.Action<ChatProviderType, ChatResponse> onProviderCompleted_,
            CancellationToken cancellationToken_)
        {
            if (requestParams_ == null || requestParams_.RequestPayload == null)
                return;

            List<ChatRequestProviderTarget> targets = GetOrderedTargets(requestParams_.Providers, onlyEnabled_: true);

            List<Task> tasks = new List<Task>();

            foreach (ChatRequestProviderTarget target in targets)
            {
                ChatProviderEntry entry = _providerEntries.FirstOrDefault(e => e.ProviderType == target.ProviderType);
                if (entry == null || !IsEntryAvailable(entry, onlyEnabled_: true))
                    continue;

                string model = ResolveModel(target.Model, requestParams_.RequestPayload.Model, entry.Settings?.DefaultModel);

                Task task = Task.Run(async () =>
                {
                    KeyValuePair<ChatProviderType, ChatResponse> result =
                        await SendToProviderInternalAsync(entry, requestParams_.RequestPayload, model, cancellationToken_);

                    if (onProviderCompleted_ != null)
                    {
                        try
                        {
                            onProviderCompleted_.Invoke(result.Key, result.Value);
                        }
                        catch (Exception callbackException)
                        {
                            AIProviderLogger.LogError(
                                $"[Chat] Provider callback failed - Provider: {result.Key}, Error: {callbackException.Message}");
                        }
                    }
                }, cancellationToken_);

                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
        }

        private async Task StreamMessageInternalAsync(
            ChatRequestParams requestParams_,
            System.Func<string, System.Threading.Tasks.Task> onChunkReceived_,
            System.Func<ChatProviderType, string, System.Threading.Tasks.Task> onError_,
            CancellationToken cancellationToken_)
        {
            if (onChunkReceived_ == null)
            {
                const string errorMessage = "[ChatProviderManager] StreamMessageInternalAsync: onChunkReceived_ is null";
                UnityEngine.Debug.LogWarning(errorMessage);
                await NotifyStreamingErrorAsync(ChatProviderType.NONE, errorMessage, onError_);
                return;
            }

            if (requestParams_ == null || requestParams_.RequestPayload == null)
            {
                const string errorMessage = "[ChatProviderManager] StreamMessageInternalAsync: requestParams_ is null";
                UnityEngine.Debug.LogWarning(errorMessage);
                await NotifyStreamingErrorAsync(ChatProviderType.NONE, errorMessage, onError_);
                return;
            }

            List<ChatRequestProviderTarget> targets = GetOrderedTargets(requestParams_.Providers, onlyEnabled_: true);

            if (targets.Count == 0)
            {
                const string errorMessage = "[ChatProviderManager] No enabled providers found for streaming";
                UnityEngine.Debug.LogWarning(errorMessage);
                await NotifyStreamingErrorAsync(ChatProviderType.NONE, errorMessage, onError_);
                return;
            }

            List<string> streamErrors = new List<string>();

            foreach (ChatRequestProviderTarget target in targets)
            {
                ChatProviderEntry entry = _providerEntries.FirstOrDefault(e => e.ProviderType == target.ProviderType);
                if (entry == null || !IsEntryAvailable(entry, onlyEnabled_: true))
                    continue;

                ChatProviderAbstract provider = GetOrCreateProvider(entry);
                if (provider == null)
                {
                    continue;
                }

                string model = ResolveModel(target.Model, requestParams_.RequestPayload.Model, entry.Settings?.DefaultModel);

                try
                {
                    AIProviderLogger.Log($"[Chat] Streaming request to {entry.ProviderType} (Model: {model})...");
                    AIProviderLogger.LogVerbose($"[Chat] Stream Request Details: {UnityEngine.JsonUtility.ToJson(requestParams_.RequestPayload)}");

                    await provider.StreamMessageAsync(requestParams_.RequestPayload, model, onChunkReceived_, cancellationToken_);

                    AIProviderLogger.Log($"[Chat] Streaming finished for {entry.ProviderType}");
                    return;
                }
                catch (OperationCanceledException)
                {
                    string cancelledMessage = $"[Chat Stream] Cancelled - Provider: {entry.ProviderType}, Model: {model ?? "(default)"}";
                    AIProviderLogger.LogWarning(cancelledMessage);
                    await NotifyStreamingErrorAsync(entry.ProviderType, cancelledMessage, onError_);
                    return;
                }
                catch (Exception ex)
                {
                    string formattedError = FormatStreamingErrorMessage(entry.ProviderType, model, ex);
                    streamErrors.Add(formattedError);
                    AIProviderLogger.LogError(formattedError);
                    await NotifyStreamingErrorAsync(entry.ProviderType, formattedError, onError_);
                }
            }

            if (streamErrors.Count > 0)
            {
                AIProviderLogger.LogError($"[Chat Stream] All providers failed. Attempts: {streamErrors.Count}");
            }
        }

        private string FormatStreamingErrorMessage(ChatProviderType providerType_, string model_, Exception exception_)
        {
            string errorType = exception_?.GetType().Name ?? "UnknownException";
            string message = exception_?.Message ?? "Unknown error";
            message = message.Replace('\r', ' ').Replace('\n', ' ').Trim();
            if (string.IsNullOrEmpty(message))
                message = "Unknown error";

            return $"[Chat Stream] Failed - Provider: {providerType_}, Model: {model_ ?? "(default)"}, ErrorType: {errorType}, Error: {message}";
        }

        private async Task NotifyStreamingErrorAsync(
            ChatProviderType providerType_,
            string errorMessage_,
            System.Func<ChatProviderType, string, System.Threading.Tasks.Task> onError_)
        {
            if (onError_ == null || string.IsNullOrEmpty(errorMessage_))
                return;

            try
            {
                await onError_(providerType_, errorMessage_);
            }
            catch (Exception callbackException)
            {
                string callbackErrorMessage = callbackException?.Message ?? "Unknown callback error";
                callbackErrorMessage = callbackErrorMessage.Replace('\r', ' ').Replace('\n', ' ').Trim();
                if (string.IsNullOrEmpty(callbackErrorMessage))
                    callbackErrorMessage = "Unknown callback error";

                AIProviderLogger.LogError(
                    $"[Chat Stream] Error callback failed - Provider: {providerType_}, ErrorType: {callbackException?.GetType().Name ?? "UnknownException"}, Error: {callbackErrorMessage}");
            }
        }

        private bool IsProviderAvailableInternal(ChatProviderType providerType_)
        {
            ChatProviderEntry entry = _providerEntries.FirstOrDefault(e => e.ProviderType == providerType_);
            return entry != null && entry.IsEnabled && !string.IsNullOrEmpty(entry.Settings?.ApiKey);
        }

        private List<ChatProviderType> GetAvailableProviderTypesInternal()
        {
            return _providerEntries
                .Where(e => IsEntryAvailable(e, onlyEnabled_: true))
                .Select(e => e.ProviderType)
                .ToList();
        }

        private ChatProviderType GetHighestPriorityProviderInternal()
        {
            ChatProviderEntry entry = _providerEntries.FirstOrDefault(e => IsEntryAvailable(e, onlyEnabled_: true));
            return entry?.ProviderType ?? ChatProviderType.NONE;
        }

        private void DisposeInternal()
        {
            if (_disposed)
                return;

            foreach (ChatProviderAbstract provider in _providers.Values)
            {
                if (provider is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            _providers.Clear();
            _providerEntries.Clear();
            _disposed = true;
        }

        private async Task<KeyValuePair<ChatProviderType, ChatResponse>> SendToProviderInternalAsync(
            ChatProviderEntry entry_,
            ChatRequestPayload requestPayload_,
            string model_,
            CancellationToken cancellationToken_)
        {
            try
            {
                ChatProviderAbstract provider = GetOrCreateProvider(entry_);
                if (provider == null)
                {
                    return new KeyValuePair<ChatProviderType, ChatResponse>(entry_.ProviderType,
                                                                            ChatResponse.FromError($"Failed to create provider '{entry_.ProviderType}'"));
                }

                AIProviderLogger.Log($"[Chat] Requesting {entry_.ProviderType} (Model: {model_})...");
                AIProviderLogger.LogVerbose($"[Chat] Request Details: {UnityEngine.JsonUtility.ToJson(requestPayload_)}");

                ChatResponse response = await provider.SendMessageAsync(requestPayload_, model_, cancellationToken_);

                AIProviderLogger.Log($"[Chat] Response from {entry_.ProviderType}: Success={response.IsSuccess}, Error={response.ErrorMessage}");

                return new KeyValuePair<ChatProviderType, ChatResponse>(entry_.ProviderType, response);
            }
            catch (OperationCanceledException)
            {
                return new KeyValuePair<ChatProviderType, ChatResponse>(entry_.ProviderType,
                                                                        ChatResponse.FromError("Request was cancelled"));
            }
            catch (Exception ex)
            {
                return new KeyValuePair<ChatProviderType, ChatResponse>(entry_.ProviderType,
                                                                        ChatResponse.FromError(ex.Message));
            }
        }

        private List<ChatRequestProviderTarget> GetOrderedTargets(List<ChatRequestProviderTarget> targets_, bool onlyEnabled_)
        {
            List<ChatRequestProviderTarget> targets = targets_ != null
                ? new List<ChatRequestProviderTarget>(targets_)
                : new List<ChatRequestProviderTarget>();

            if (targets.Count == 0)
            {
                foreach (ChatProviderEntry entry in _providerEntries)
                {
                    if (!IsEntryAvailable(entry, onlyEnabled_))
                        continue;

                    targets.Add(new ChatRequestProviderTarget
                    {
                        ProviderType = entry.ProviderType,
                        Model = null,
                        Priority = 0
                    });
                }
            }

            return targets
                .Select((target, index) => new { target, index })
                .OrderByDescending(pair => pair.target.Priority)
                .ThenBy(pair => pair.index)
                .Select(pair => pair.target)
                .ToList();
        }

        private bool IsEntryAvailable(ChatProviderEntry entry_, bool onlyEnabled_)
        {
            if (entry_ == null)
                return false;

            if (onlyEnabled_ && !entry_.IsEnabled)
                return false;

            return !string.IsNullOrEmpty(entry_.Settings?.ApiKey);
        }

        private string ResolveModel(string targetModel_, string payloadModel_, string defaultModel_)
        {
            if (!string.IsNullOrEmpty(targetModel_))
                return targetModel_;

            if (!string.IsNullOrEmpty(payloadModel_))
                return payloadModel_;

            return defaultModel_;
        }

        private ChatProviderAbstract GetOrCreateProvider(ChatProviderEntry entry_)
        {
            if (_providers.TryGetValue(entry_.ProviderType, out ChatProviderAbstract existingProvider))
            {
                return existingProvider;
            }

            try
            {
                ChatProviderAbstract provider = ChatProviderFactory.Create(entry_.ProviderType, entry_.Settings);
                _providers[entry_.ProviderType] = provider;
                return provider;
            }
            catch
            {
                return null;
            }
        }

        private void DisposeProvider(ChatProviderType providerType_)
        {
            if (_providers.TryGetValue(providerType_, out ChatProviderAbstract provider))
            {
                ((IDisposable)provider).Dispose();
                _providers.Remove(providerType_);
            }
        }
    }
}
