using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Weppy.AIProvider
{
    public partial class ChatCliProviderManager : IDisposable
    {
        private readonly List<ChatCliProviderEntry> _providerEntries = new List<ChatCliProviderEntry>();
        private readonly Dictionary<ChatCliProviderType, ChatCliProviderAbstract> _providers =
            new Dictionary<ChatCliProviderType, ChatCliProviderAbstract>();
        private bool _disposed;

        public ChatCliProviderManager()
        {
        }

        public ChatCliProviderManager(IEnumerable<ChatCliProviderEntry> entries_)
        {
            if (entries_ != null)
            {
                foreach (ChatCliProviderEntry entry in entries_)
                {
                    AddProviderInternal(entry);
                }
            }
        }

        private bool AddProviderInternal(ChatCliProviderEntry entry_)
        {
            if (entry_ == null)
                return false;

            if (entry_.Settings == null)
                return false;

            ChatCliProviderEntry existing = _providerEntries.FirstOrDefault(e => e.ProviderType == entry_.ProviderType);
            if (existing != null)
            {
                _providerEntries.Remove(existing);
                DisposeProvider(existing.ProviderType);
            }

            _providerEntries.Add(entry_);
            return true;
        }

        private bool AddProviderInternal(ChatCliProviderType providerType_, ChatCliProviderSettings settings_)
        {
            return AddProviderInternal(new ChatCliProviderEntry(providerType_, settings_));
        }

        private bool RemoveProviderInternal(ChatCliProviderType providerType_)
        {
            ChatCliProviderEntry entry = _providerEntries.FirstOrDefault(e => e.ProviderType == providerType_);
            if (entry == null)
                return false;

            _providerEntries.Remove(entry);
            DisposeProvider(providerType_);
            return true;
        }

        private void SetProviderEnabledInternal(ChatCliProviderType providerType_, bool enabled_)
        {
            ChatCliProviderEntry entry = _providerEntries.FirstOrDefault(e => e.ProviderType == providerType_);
            if (entry != null)
            {
                entry.IsEnabled = enabled_;
            }
        }

        private void SetProviderDefaultModelInternal(ChatCliProviderType providerType_, string model_)
        {
            ChatCliProviderEntry entry = _providerEntries.FirstOrDefault(e => e.ProviderType == providerType_);
            if (entry != null && entry.Settings != null)
            {
                entry.Settings.DefaultModel = model_;
                DisposeProvider(providerType_);
            }
        }

        private async Task<ChatCliResponse> SendMessageInternalAsync(
            ChatCliRequestParams requestParams_,
            CancellationToken cancellationToken_)
        {
            if (requestParams_ == null || requestParams_.RequestPayload == null)
                return ChatCliResponse.FromError("Invalid params");

            List<ChatCliRequestProviderTarget> targets = GetOrderedTargets(requestParams_.Providers, onlyEnabled_: true);

            if (targets.Count == 0)
                return ChatCliResponse.FromError("No enabled providers available");

            Exception lastException = null;
            string lastError = null;
            List<string> providerErrors = new List<string>();

            foreach (ChatCliRequestProviderTarget target in targets)
            {
                ChatCliProviderEntry entry = _providerEntries.FirstOrDefault(e => e.ProviderType == target.ProviderType);
                if (entry == null || !IsEntryAvailable(entry, onlyEnabled_: true))
                    continue;

                string model = ResolveModel(target.Model, requestParams_.RequestPayload.Model, entry.Settings?.DefaultModel);

                try
                {
                    ChatCliProviderAbstract provider = GetOrCreateProvider(entry);
                    if (provider == null)
                        continue;

                    ChatCliResponse response = await provider.SendMessageAsync(requestParams_.RequestPayload, model, cancellationToken_);

                    if (response.IsSuccess)
                    {
                        response.ProviderType = entry.ProviderType;
                        return response;
                    }

                    string formattedError = FormatCliFailureMessage(
                        entry.ProviderType,
                        model,
                        "ProviderError",
                        response.ErrorMessage,
                        isPersistent_: false);
                    providerErrors.Add(formattedError);
                    AIProviderLogger.LogError(formattedError);
                    lastError = response.ErrorMessage;
                }
                catch (OperationCanceledException)
                {
                    AIProviderLogger.LogWarning(FormatCliCancellationMessage(entry.ProviderType, model, isPersistent_: false));
                    return ChatCliResponse.FromError("Request was cancelled");
                }
                catch (Exception ex)
                {
                    string formattedError = FormatCliFailureMessage(
                        entry.ProviderType,
                        model,
                        ex.GetType().Name,
                        ex.Message,
                        isPersistent_: false);
                    providerErrors.Add(formattedError);
                    AIProviderLogger.LogError(formattedError);
                    lastException = ex;
                    lastError = ex.Message;
                }
            }

            if (providerErrors.Count > 0)
                AIProviderLogger.LogError($"[Chat CLI] All providers failed. Attempts: {providerErrors.Count}");

            return ChatCliResponse.FromError(lastError ?? lastException?.Message ?? "All providers failed");
        }

        private async Task<Dictionary<ChatCliProviderType, ChatCliResponse>> SendMessageToAllProvidersInternalAsync(
            ChatCliRequestParams requestParams_,
            bool onlyEnabled_,
            CancellationToken cancellationToken_)
        {
            Dictionary<ChatCliProviderType, ChatCliResponse> results = new Dictionary<ChatCliProviderType, ChatCliResponse>();
            if (requestParams_ == null || requestParams_.RequestPayload == null)
                return results;

            List<ChatCliRequestProviderTarget> targets = GetOrderedTargets(requestParams_.Providers, onlyEnabled_);

            List<Task<KeyValuePair<ChatCliProviderType, ChatCliResponse>>> tasks =
                new List<Task<KeyValuePair<ChatCliProviderType, ChatCliResponse>>>();

            foreach (ChatCliRequestProviderTarget target in targets)
            {
                ChatCliProviderEntry entry = _providerEntries.FirstOrDefault(e => e.ProviderType == target.ProviderType);
                if (entry == null || !IsEntryAvailable(entry, onlyEnabled_))
                    continue;

                string model = ResolveModel(target.Model, requestParams_.RequestPayload.Model, entry.Settings?.DefaultModel);
                tasks.Add(SendToProviderInternalAsync(entry, requestParams_.RequestPayload, model, cancellationToken_));
            }

            KeyValuePair<ChatCliProviderType, ChatCliResponse>[] responses = await Task.WhenAll(tasks);

            foreach (KeyValuePair<ChatCliProviderType, ChatCliResponse> kvp in responses)
            {
                results[kvp.Key] = kvp.Value;
            }

            return results;
        }

        private async Task SendMessageToAllProvidersInternalAsync(
            ChatCliRequestParams requestParams_,
            System.Action<ChatCliProviderType, ChatCliResponse> onProviderCompleted_,
            CancellationToken cancellationToken_)
        {
            if (requestParams_ == null || requestParams_.RequestPayload == null)
                return;

            List<ChatCliRequestProviderTarget> targets = GetOrderedTargets(requestParams_.Providers, onlyEnabled_: true);

            List<Task> tasks = new List<Task>();

            foreach (ChatCliRequestProviderTarget target in targets)
            {
                ChatCliProviderEntry entry = _providerEntries.FirstOrDefault(e => e.ProviderType == target.ProviderType);
                if (entry == null || !IsEntryAvailable(entry, onlyEnabled_: true))
                    continue;

                string model = ResolveModel(target.Model, requestParams_.RequestPayload.Model, entry.Settings?.DefaultModel);

                Task task = Task.Run(async () =>
                {
                    KeyValuePair<ChatCliProviderType, ChatCliResponse> result =
                        await SendToProviderInternalAsync(entry, requestParams_.RequestPayload, model, cancellationToken_);

                    onProviderCompleted_?.Invoke(result.Key, result.Value);
                }, cancellationToken_);

                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
        }

        private bool IsProviderAvailableInternal(ChatCliProviderType providerType_)
        {
            ChatCliProviderEntry entry = _providerEntries.FirstOrDefault(e => e.ProviderType == providerType_);
            return entry != null && entry.IsEnabled && HasProviderAuth(entry.Settings);
        }

        private List<ChatCliProviderType> GetAvailableProviderTypesInternal()
        {
            return _providerEntries
                .Where(e => IsEntryAvailable(e, onlyEnabled_: true))
                .Select(e => e.ProviderType)
                .ToList();
        }

        private ChatCliProviderType GetHighestPriorityProviderInternal()
        {
            ChatCliProviderEntry entry = _providerEntries.FirstOrDefault(e => IsEntryAvailable(e, onlyEnabled_: true));
            return entry != null ? entry.ProviderType : ChatCliProviderType.NONE;
        }

        private async Task<ChatCliResponse> SendPersistentMessageInternalAsync(
            ChatCliRequestParams requestParams_,
            CancellationToken cancellationToken_)
        {
            if (requestParams_ == null || requestParams_.RequestPayload == null)
                return ChatCliResponse.FromError("Invalid params");

            List<ChatCliRequestProviderTarget> targets = GetOrderedTargets(requestParams_.Providers, onlyEnabled_: true);

            if (targets.Count == 0)
                return ChatCliResponse.FromError("No enabled providers available");

            Exception lastException = null;
            string lastError = null;
            List<string> providerErrors = new List<string>();

            foreach (ChatCliRequestProviderTarget target in targets)
            {
                ChatCliProviderEntry entry = _providerEntries.FirstOrDefault(e => e.ProviderType == target.ProviderType);
                if (entry == null || !IsEntryAvailable(entry, onlyEnabled_: true))
                    continue;

                string model = ResolveModel(target.Model, requestParams_.RequestPayload.Model, entry.Settings?.DefaultModel);

                try
                {
                    ChatCliProviderAbstract provider = GetOrCreateProvider(entry);
                    if (provider == null)
                        continue;

                    ChatCliResponse response = await provider.SendPersistentMessageAsync(requestParams_.RequestPayload, model, cancellationToken_);

                    if (response.IsSuccess)
                    {
                        response.ProviderType = entry.ProviderType;
                        return response;
                    }

                    string formattedError = FormatCliFailureMessage(
                        entry.ProviderType,
                        model,
                        "ProviderError",
                        response.ErrorMessage,
                        isPersistent_: true);
                    providerErrors.Add(formattedError);
                    AIProviderLogger.LogError(formattedError);
                    lastError = response.ErrorMessage;
                }
                catch (OperationCanceledException)
                {
                    AIProviderLogger.LogWarning(FormatCliCancellationMessage(entry.ProviderType, model, isPersistent_: true));
                    return ChatCliResponse.FromError("Request was cancelled");
                }
                catch (Exception ex)
                {
                    string formattedError = FormatCliFailureMessage(
                        entry.ProviderType,
                        model,
                        ex.GetType().Name,
                        ex.Message,
                        isPersistent_: true);
                    providerErrors.Add(formattedError);
                    AIProviderLogger.LogError(formattedError);
                    lastException = ex;
                    lastError = ex.Message;
                }
            }

            if (providerErrors.Count > 0)
                AIProviderLogger.LogError($"[Chat CLI Stream] All providers failed. Attempts: {providerErrors.Count}");

            return ChatCliResponse.FromError(lastError ?? lastException?.Message ?? "All providers failed");
        }

        private string FormatCliFailureMessage(
            ChatCliProviderType providerType_,
            string model_,
            string errorType_,
            string errorMessage_,
            bool isPersistent_)
        {
            string prefix = isPersistent_ ? "[Chat CLI Stream]" : "[Chat CLI]";
            string normalizedMessage = NormalizeLogMessage(errorMessage_);
            string normalizedErrorType = string.IsNullOrEmpty(errorType_) ? "UnknownException" : errorType_;
            return $"{prefix} Failed - Provider: {providerType_}, Model: {model_ ?? "(default)"}, ErrorType: {normalizedErrorType}, Error: {normalizedMessage}";
        }

        private string FormatCliCancellationMessage(ChatCliProviderType providerType_, string model_, bool isPersistent_)
        {
            string prefix = isPersistent_ ? "[Chat CLI Stream]" : "[Chat CLI]";
            return $"{prefix} Cancelled - Provider: {providerType_}, Model: {model_ ?? "(default)"}";
        }

        private string NormalizeLogMessage(string message_)
        {
            string message = message_ ?? "Unknown error";
            message = message.Replace('\r', ' ').Replace('\n', ' ').Trim();
            return string.IsNullOrEmpty(message) ? "Unknown error" : message;
        }

        private void ResetSessionInternal(ChatCliProviderType providerType_)
        {
            if (_providers.TryGetValue(providerType_, out ChatCliProviderAbstract provider))
            {
                provider.ResetSession();
            }
        }

        private void ResetAllSessionsInternal()
        {
            foreach (ChatCliProviderAbstract provider in _providers.Values)
            {
                provider.ResetSession();
            }
        }

        private void DisposeInternal()
        {
            if (_disposed)
                return;

            foreach (ChatCliProviderAbstract provider in _providers.Values)
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

        private async Task<KeyValuePair<ChatCliProviderType, ChatCliResponse>> SendToProviderInternalAsync(
            ChatCliProviderEntry entry_,
            ChatCliRequestPayload requestPayload_,
            string model_,
            CancellationToken cancellationToken_)
        {
            try
            {
                ChatCliProviderAbstract provider = GetOrCreateProvider(entry_);
                if (provider == null)
                    return new KeyValuePair<ChatCliProviderType, ChatCliResponse>(
                        entry_.ProviderType,
                        ChatCliResponse.FromError($"Failed to create provider '{entry_.ProviderType}'"));

                ChatCliResponse response = await provider.SendMessageAsync(requestPayload_, model_, cancellationToken_);
                return new KeyValuePair<ChatCliProviderType, ChatCliResponse>(entry_.ProviderType, response);
            }
            catch (OperationCanceledException)
            {
                return new KeyValuePair<ChatCliProviderType, ChatCliResponse>(
                    entry_.ProviderType,
                    ChatCliResponse.FromError("Request was cancelled"));
            }
            catch (Exception ex)
            {
                return new KeyValuePair<ChatCliProviderType, ChatCliResponse>(
                    entry_.ProviderType,
                    ChatCliResponse.FromError(ex.Message));
            }
        }

        private List<ChatCliRequestProviderTarget> GetOrderedTargets(List<ChatCliRequestProviderTarget> targets_, bool onlyEnabled_)
        {
            List<ChatCliRequestProviderTarget> targets = targets_ != null
                ? new List<ChatCliRequestProviderTarget>(targets_)
                : new List<ChatCliRequestProviderTarget>();

            if (targets.Count == 0)
            {
                foreach (ChatCliProviderEntry entry in _providerEntries)
                {
                    if (!IsEntryAvailable(entry, onlyEnabled_))
                        continue;

                    targets.Add(new ChatCliRequestProviderTarget
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

        private bool IsEntryAvailable(ChatCliProviderEntry entry_, bool onlyEnabled_)
        {
            if (entry_ == null)
                return false;

            if (onlyEnabled_ && !entry_.IsEnabled)
                return false;

            return HasProviderAuth(entry_.Settings);
        }

        private bool HasProviderAuth(ChatCliProviderSettings settings_)
        {
            if (settings_ == null)
                return false;

            if (!settings_.UseApiKey)
                return true;

            return !string.IsNullOrEmpty(settings_.ApiKey);
        }

        private string ResolveModel(string targetModel_, string payloadModel_, string defaultModel_)
        {
            if (!string.IsNullOrEmpty(targetModel_))
                return targetModel_;

            if (!string.IsNullOrEmpty(payloadModel_))
                return payloadModel_;

            return defaultModel_;
        }

        private ChatCliProviderAbstract GetOrCreateProvider(ChatCliProviderEntry entry_)
        {
            if (_providers.TryGetValue(entry_.ProviderType, out ChatCliProviderAbstract existingProvider))
            {
                return existingProvider;
            }

            try
            {
                ChatCliProviderAbstract provider = ChatCliProviderFactory.Create(entry_.ProviderType, entry_.Settings);
                if (provider == null)
                    return null;

                _providers[entry_.ProviderType] = provider;
                return provider;
            }
            catch
            {
                return null;
            }
        }

        private void DisposeProvider(ChatCliProviderType providerType_)
        {
            if (_providers.TryGetValue(providerType_, out ChatCliProviderAbstract provider))
            {
                ((IDisposable)provider).Dispose();
                _providers.Remove(providerType_);
            }
        }
    }
}
